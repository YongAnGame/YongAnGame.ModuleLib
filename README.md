# YongAnGame.ModuleLib

面向游戏 MOD 系统的通用模块库，提供配置加载、序列化、仓储管理等基础设施

[项目文档](https://yongangame.github.io/YongAnGame.ModuleLib/)

## 特性

- **多种格式支持**：JSON（System.Text.Json）、YAML（YamlDotNet），通过统一接口 `IConfigSerializer` 抽象，换格式不改调用方
- **自动格式识别**：`IConfigSerializerFactory` 按文件扩展名自动选择合适的序列化器
- **线程安全**：所有公开接口承诺并发安全，读路径无锁，写路径采用不可变快照替换
- **结构化标识符**：`Id` 支持 `ModId.ItemId[:SubId]` 格式，内置多维度比较方法
- **分层接口设计**：调用方只依赖接口，不感知具体实现
- **面向 MOD 的配置仓储**：`IConfigRepository` 负责目录扫描、缓存、哈希，支持热加载
- **低框架兼容**：通过 Polyfill 为 `netstandard2.0` 等旧目标补齐新 API，调用点无需条件编译

## 项目结构

```

YongAnGame.ModuleLib/
├── Compatibility/          # 低框架静态扩展
├── Components/             # 通用值类型（Id 等）
├── Exceptions/             # 统一异常类型
├── Extensions/             # 扩展方法（工厂便捷入口等）
├── Features/
│   ├── Serializers/        # 序列化器实现（JSON、YAML）与工厂
│   ├── ConfigRepository    # 配置仓储默认实现
│   └── ConfigOptions       # 配置选项默认实现
└── Interfaces/
    ├── Configs/            # 配置相关接口
    ├── Repositories/       # 仓储相关接口
    └── IHasId.cs           # 带 Id 的数据对象契约

```

## 核心概念

### 序列化器（`IConfigSerializer`）

读写配置的统一入口，屏蔽底层 JSON / YAML 库差异

```csharp
IReadOnlyList<string> Extensions { get; }
void AddTypeConverter(object converter);
T? Deserialize<T>(string text) where T : class;
object? Deserialize(string text, Type type);
string Serialize<T>(T value) where T : class;
T? DeserializeFromFile<T>(string filePath) where T : class;
void SerializeToFile<T>(T value, string filePath) where T : class;
```

所有实现遵守以下约定：

- 线程安全，所有成员均可并发调用
- 底层异常统一包装为 `SerializationException`
- 错误消息中包含文件位置信息
- 新增转换器后立即生效，不影响正在进行的序列化

### 序列化器工厂（`IConfigSerializerFactory`）

按文件扩展名自动选择序列化器，调用方无需关心具体格式

```csharp
IConfigSerializerFactory factory = new ConfigSerializerFactory()
    .WithYaml();

var serializer = factory.ForFile("config.yaml");     // → YamlConfigSerializer
var jsonSerializer = factory.ForFile("config.json"); // → 默认 JSON
```

工厂构造时默认注册一个 JSON 序列化器作为兜底，扩展名不匹配任何已注册实现时使用

### 配置仓储（`IConfigRepository`）

从 MOD 目录加载配置并按类型分组缓存

```csharp
IConfigRepository repo = new ConfigRepository(logger, options, serializer, "example_mod");

repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
repo.Load();

if (repo.TryGetValue<CharacterConfig, CharacterData>("char_001", out var data))
{
    // 使用 data
}
```

内部使用 `ConcurrentDictionary` 保存状态，加载与查询可并发进行

### 标准仓储（`IStandardRepository<TKey, TData>`）

可复用的按类型仓储抽象，只读契约由 `IReadOnlyStandardRepository<TKey, TData>` 提供

```csharp
public interface IReadOnlyStandardRepository<TKey, TData> : IEnumerable<TData>
    where TData : IHasId
{
    TData? GetValue(TKey key);
    bool TryGetValue(TKey key, [NotNullWhen(true)] out TData? data);
    bool ContainsKey(TKey key);
}

public interface IStandardRepository<TKey, TData>
    : IReadOnlyStandardRepository<TKey, TData>
    where TData : IHasId
{
    void Add(TData data);
    bool TryAdd(TData data);
}
```

枚举结果是调用瞬间的快照，不因并发修改而抛异常

### 标识符（`Id`）

形如 `ModId.ItemId:SubId` 的结构化标识符，用于跨 MOD 引用

```csharp
var id = new Id("example_mod", "char_001", subId: 0);
id.SameMod(other);           // 是否同一 MOD
id.SameItem(other);          // 是否同一项目（忽略 MOD）
id.SameModAndItem(other);    // 同一 MOD 下的同一项目
id.SameItemAndSubId(other);  // 跨 MOD 的同一项目同一序号
id.SameAll(other);           // 完全相等
```

- `SubId` 省略时按 0 处理，`a.b` 与 `a.b:0` 表示同一个 Id
- 实现了 `IParsable<Id>`，可在泛型约束中统一处理
- 不可变值类型，天然线程安全

## 多目标框架兼容

项目同时支持现代框架与旧框架，通过三类工具补齐缺失的 API：

| 工具 | 职责 | 覆盖内容 |
|------|------|----------|
| **Polyfill**（SimonCropp） | BCL API 补齐 | `KeyValuePair.Deconstruct`、`HashData` 等 |
| **Microsoft.Bcl.HashCode** | 官方兼容包 | `HashCode.Combine` 等 |
| 本项目 **Compatibility** 文件wIfXxx` 静态扩展、`CallerArgumentExpression`（如未由 Polyfill 提夹 | 项目自维护 | `Thro供） |

调用点代码**无需条件编译**，例如下面写法在所有目标框架下均能通过：

```csharp
ArgumentException.ThrowIfNullOrWhiteSpace(modId);
ArgumentOutOfRangeException.ThrowIfNegative(subId);
```

低版本走 `Compatibility` 中的静态扩展，高版本走 BCL 内置实现

## 环境要求

- **核心库**：`.NET 9.0` 或更高（使用了 `System.Threading.Lock`）
- **多目标兼容**：可同时编译 `netstandard2.0` / `netstandard2.1` / `net48` / `net8.0` 旧框架
- **C# 14 扩展成员**：需要 `.NET 10 SDK` 或支持该特性的编译器
- **推荐 IDE**：Visual Studio 2026

# 引用项目

本库通过项目引用依赖以下子项目：

## NuGet 依赖

| 依赖 | 用途 |
|------|------|
| `System.Text.Json` | JSON 序列化 |
| `YamlDotNet` | YAML 序列化 |
| `Serilog` | 日志 |
| `Polyfill` | 低框架 BCL API Polyfill |
| `Microsoft.Bcl.HashCode` | `HashCode` 兼容 |
| `System.Memory` | 数组切片与 `Span` 支持 |

## 线程安全模型

所有公开接口均承诺线程安全，实现统一采用**不可变快照替换**：

- **读路径无锁**：`volatile` 字段直接读取，零开销
- **写路径加锁**：`Lock` 保护“构建新状态 + 原子替换”
- **旧状态对读者始终可用**：读者要么拿到旧快照、要么拿到新快照，不会看到半成品
- **枚举为快照语义**：`foreach` 看到的是进入循环那一刻的内容，不因并发修改抛异常

具体实现细节见各接口的 XML 注释