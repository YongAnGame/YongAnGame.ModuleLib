# 线程安全模型

本库所有公开接口均承诺线程安全，实现统一采用**不可变快照替换**。

## 核心机制

| 路径 | 策略 |
|------|------|
| 读 | 无锁，`volatile` 字段直接读 |
| 写 | 加锁，构建新状态后原子替换 |

读线程要么拿到旧快照、要么拿到新快照，**不会看到半成品**。

## 快照替换示意

```csharp
private volatile ISerializer _serializer;
private readonly Lock _writeLock = new();

public void Modify()
{
    lock (_writeLock)
    {
        // 1. 基于旧状态构建新状态
        var next = BuildNewState(_serializer);

        // 2. 原子替换
        _serializer = next;
    }
}

public Result Query()
{
    // 一次读取到本地，后续遍历基于同一快照
    var snapshot = _serializer;
    return snapshot.DoWork();
}
```

## 枚举语义

`foreach` 遍历仓储或集合时，看到的是**进入循环那一刻的快照**：

- 循环期间其他线程的增删**不会反映**在本次枚举中
- **不会抛** `InvalidOperationException`
- 每一项都是完整对象，不会读到半初始化状态

```csharp
foreach (var item in repo.GetAllId<T>())
{
    // item 是快照时刻的项
}
```

## 并发场景

### 场景 1：并发读（最常见）

多个线程同时 `Deserialize` / `TryGetValue`，**完全无锁**，性能无损失。

### 场景 2：读 + 写混合

一个线程 `AddTypeConverter` 或 `Load`，其他线程读取：

- 读取线程看到旧快照，功能正常
- 新增的转换器 / 配置项在下次读取时生效
- 不会出现半成品

### 场景 3：并发写

多个线程同时 `AddTypeConverter`：

- `Lock` 保证“构建 + 替换”互斥
- 每次替换生成新快照，最终状态包含所有添加的转换器
- 顺序不定，但结果正确

## 不保证的部分

| 项 | 说明 |
|----|------|
| 跨项一致性 | `Sha256` 在并发加载期间不保证对应某一完整时刻 |
| 枚举完整性 | `Load()` 遍历期间新增的分组可能不被本次加载 |
| 文件写入 | `SerializeToFile` 并发写同一路径内容取决于顺序 |

这些都写在各接口的 XML 注释中，调用方需按语义使用。

## 与业务线程安全的分工

本库保证的是**内部状态安全**：

- 不会因为并发而崩溃
- 不会读到半成品
- 不会因竞争丢失更新

**业务语义**（如“并发写同一文件不互相覆盖”）需调用方自行加锁。

## `Lock` 类型

在 `net9.0` 及以上目标使用 `System.Threading.Lock`；低版本回退到 `object`：

```csharp
#if NET9_0_OR_GREATER
    private readonly Lock _registerLock = new();
#else
    private readonly object _registerLock = new();
#endif
```

行为对调用方透明。