# 线程安全模型

本库所有公开接口均承诺线程安全，但**不同实现的内部机制不同**。阅读本节时按具体类型区分。

## 三类实现

| 类型 | 线程模型 |
|------|----------|
| `IConfigSerializer`（JSON / YAML） | 不可变快照替换 |
| `IConfigRepository`（`ConfigRepository`） | 并发字典 |
| `Id` | 不可变值类型 |

## 一、不可变快照替换（序列化器）

适用于 `IConfigSerializer` 的 `AddTypeConverter`，以及 `IStandardRepository`。

| 路径 | 策略 |
|------|------|
| 读 | 无锁，`volatile` 字段直接读 |
| 写 | 加锁，构建新状态后原子替换 |

读线程要么拿到旧快照、要么拿到新快照，**不会看到半成品**。

```csharp
private volatile IReadOnlyDictionary<Type, object> _converters;
private readonly Lock _writeLock = new();

public void AddTypeConverter(object converter)
{
    lock (_writeLock)
    {
        // 1. 基于旧状态构建新状态
        var next = new Dictionary<Type, object>(_converters)
        {
            [converter.GetType()] = converter
        };

        // 2. 原子替换
        _converters = next;
    }
}

public object? Find(Type type)
{
    // 一次读取到本地，后续遍历基于同一快照
    var snapshot = _converters;
    return snapshot.TryGetValue(type, out var converter) ? converter : null;
}
```

枚举语义：`foreach` 遍历快照集合时，看到的是进入循环那一刻的完整内容，不因并发修改抛异常。

## 二、并发字典（配置仓储）

`ConfigRepository` 使用三个 `ConcurrentDictionary`：

- 分组类型 → (itemId → 配置对象)
- 分组类型 → (itemId → 文件路径)
- 文件路径 → 文件哈希

| 路径 | 策略 |
|------|------|
| 读 | 无锁 |
| 写 | 原子操作（`TryAdd`、`TryRemove`、索引器赋值） |

并发加载同一项时只有一个线程成功（`TryAdd` 的返回值区分）；并发卸载同一项时也只有一个线程成功（`TryRemove`）。

**枚举语义**：`ConcurrentDictionary` 的枚举是弱一致快照——遍历期间其他线程的增删**不会抛异常**，但可能反映部分修改。这不同于不可变快照，但满足“不会崩溃、不会读到半初始化状态”的承诺。

**卸载的并发假设**：`Unload` 内部判断“路径是否仍被其他分组引用”时，扫描 `_loadedPaths` 不是原子操作。极端并发卸载场景下可能残留或重复删除哈希。Mod 场景下卸载通常发生在 Mod 卸载或热重载的串行时机，这一假设可接受。若需严格并发安全，调用方应串行化 `Unload` 与 `Load`。

## 三、不可变值类型（`Id`）

`Id` 是 `struct`，所有字段在构造时确定，无法修改。天然线程安全，可作为字典键跨线程共享。

## 并发场景

### 场景 1：并发读（最常见）

多个线程同时 `Deserialize` / `TryGetValue`，**完全无锁**，性能无损失。

### 场景 2：读 + 写混合

一个线程 `AddTypeConverter` / `Load` / `Unload`，其他线程读取：

- 读取线程看到旧状态，功能正常
- 新增的转换器 / 配置项在下次读取时生效
- 不会出现半成品

### 场景 3：并发写

多个线程同时 `AddTypeConverter`：

- `Lock` 保证“构建 + 替换”互斥
- 每次替换生成新快照，最终状态包含所有添加的转换器
- 顺序不定，但结果正确

多个线程同时 `Add` / `Load` 同一分组或项：

- `ConcurrentDictionary` 的 `TryAdd` 保证只有一个成功
- 其余返回 `false`，仓储状态一致

## 不保证的部分

| 项 | 说明 |
|----|------|
| 跨项一致性 | `Sha256` 在并发加载或卸载期间不保证对应某一完整时刻 |
| 枚举完整性 | `Load()` 遍历期间新增的分组可能不被本次加载 |
| 文件写入 | `SerializeToFile` 并发写同一路径内容取决于顺序 |
| 并发卸载共享路径 | 多个线程同时卸载共享根路径的不同分组，哈希清理可能不精确 |

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
    private readonly Lock _writeLock = new();
#else
    private readonly object _writeLock = new();
#endif
```

行为对调用方透明。