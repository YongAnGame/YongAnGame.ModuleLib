# 快速入门

本指南帮助你在 5 分钟内完成一次配置加载。

## 1. 安装

```bash
dotnet add package YongAnGame.ModuleLib
```

仓储依赖 `Microsoft.Extensions.Logging.ILogger`，Mod 宿主通常已包含该包。如果没有，手动添加：

```bash
dotnet add package Microsoft.Extensions.Logging.Abstractions
```

## 2. 定义配置类型与分组接口

**分组类型必须是接口**，仓储用接口名去掉首字母 `I` 作为子目录名。

```csharp
using YongAnGame.ModuleLib.Interfaces;

// 分组接口：对应 Mods/{modId}/Character/ 目录
public interface ICharacterConfig;

// 配置数据
public sealed class CharacterData : IHasId
{
    public string StringId { get; init; } = "";
    public string Name { get; init; } = "";
    public int Health { get; init; }
    public int Attack { get; init; }
}
```

## 3. 准备配置文件

在 MOD 目录下创建配置文件：

```
Mods/
└── example_mod/
    └── Character/              # 对应 ICharacterConfig 接口名去掉首字母 I
        └── chestnut.json
```

`chestnut.json`：

```json
{
  "stringId": "chestnut",
  "name": "栗子",
  "health": 325,
  "attack": 799
}
```

## 4. 构建仓储

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YongAnGame.ModuleLib.Features;
using YongAnGame.ModuleLib.Features.Configs;
using YongAnGame.ModuleLib.Features.Serializers;
using YongAnGame.ModuleLib.Interfaces.Configs;

// 仓储只依赖 Microsoft.Extensions.Logging.ILogger，不绑定具体日志框架
ILogger logger = NullLogger.Instance;

IConfigOptions options = new ConfigOptions(
    Path.Combine(AppContext.BaseDirectory, "Mods"));

IConfigSerializer serializer = new JsonConfigSerializer();

IConfigRepository repo = new ConfigRepository(
    logger, options, serializer, "example_mod");

// 注册分组：ICharacterConfig 对应 Character 目录
repo.Add(typeof(ICharacterConfig), (typeof(CharacterData), isRoot: false));

// 扫描并加载
repo.Load();
```

如果宿主使用 Serilog 或 BepInEx，通过 `Serilog.Extensions.Logging` 等桥接包把 Serilog 的 `ILogger` 转成 `Microsoft.Extensions.Logging.ILogger` 后传入即可。**不要**直接把 `Serilog.ILogger` 传给仓储，两者不是同一个接口。

## 5. 读取配置

```csharp
if (repo.TryGetValue<ICharacterConfig, CharacterData>("chestnut", out var data))
{
    Console.WriteLine($"{data.Name}: HP={data.Health}, ATK={data.Attack}");
}
```

遍历一个分组下的所有项：

```csharp
foreach (Id id in repo.GetAllId<ICharacterConfig>())
{
    var data = repo.GetValue<ICharacterConfig, CharacterData>(id.ItemId);
    Console.WriteLine($"{id}: {data?.Name}");
}
```

## 6. 热重载单个配置

```csharp
// 卸载后重新加载，实现配置热重载
repo.Unload<ICharacterConfig>("chestnut");
repo.Load<ICharacterConfig, CharacterData>("chestnut");
```

`Unload` 只清理内存缓存，不删除磁盘文件。

## 7. 添加自定义类型转换器

当配置里有自定义类型（如坐标、枚举包装）时：

```csharp
public sealed class GridCellConverter : JsonConverter<GridCell>
{
    public override GridCell Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // 解析逻辑
    }

    public override void Write(
        Utf8JsonWriter writer,
        GridCell value,
        JsonSerializerOptions options)
    {
        // 写入逻辑
    }
}

serializer.AddTypeConverter(new GridCellConverter());
```

添加后立即对后续的 `Deserialize` / `Serialize` 生效，无需重建仓储。

## 8. 使用多个格式

`ConfigRepository` 的构造函数接收**单个** `IConfigSerializer`，一个仓储实例只处理该序列化器声明的扩展名。工厂的用法是：

```csharp
IConfigSerializerFactory factory = new ConfigSerializerFactory()
    .WithYaml();

// 按需为不同文件选择序列化器
IConfigSerializer jsonSerializer = factory.ForFile("Character/chestnut.json");
IConfigSerializer yamlSerializer = factory.ForFile("Character/chestnut.yaml");
```

如果同一 MOD 下同时存在 `.json` 和 `.yaml` 配置：

- 为每种格式各创建一个 `ConfigRepository` 实例，各自 `Add` 对应的分组
- 未匹配当前 `serializer.Extensions` 的文件会被静默跳过

## 下一步

- [序列化器](serialization.md)：了解 `IConfigSerializer` 的完整能力
- [配置仓储](repository.md)：深入了解加载、缓存、卸载、哈希机制
- [标识符](id.md)：`Id` 的格式与比较方法
- [线程安全模型](threading.md)：并发场景下的行为约定