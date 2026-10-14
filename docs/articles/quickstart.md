# 快速入门

本指南帮助你在 5 分钟内完成一次配置加载。

## 1. 安装

```bash
dotnet add package YongAnGame.ModuleLib
```

## 2. 定义配置类型

配置类型需实现 `IHasId`，其 `StringId` 用作仓储的键。

```csharp
using YongAnGame.ModuleLib.Interfaces;

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
    └── Character/              # 对应配置分组类型名去掉前缀
        └── char_001.json
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
using Serilog;
using YongAnGame.ModuleLib.Extensions;
using YongAnGame.ModuleLib.Features;
using YongAnGame.ModuleLib.Interfaces.Configs;

ILogger logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

IConfigOptions options = new ConfigOptions(
    Path.Combine(AppContext.BaseDirectory, "Mods"));

IConfigSerializerFactory factory = new ConfigSerializerFactory()
    .WithJson();

IConfigSerializer serializer = factory.ForExtension("Character\\chestnut.yml");

IConfigRepository repo = new ConfigRepository(
    logger, options, serializer, "example_mod");

// 注册分组：CharacterConfig 对应 Character 目录
repo.Add(typeof(CharacterConfig), (typeof(CharacterData), isRoot: false));

// 扫描并加载
repo.Load();
```

## 5. 读取配置

```csharp
if (repo.TryGetValue<CharacterConfig, CharacterData>("chestnut", out var data))
{
    Console.WriteLine($"{data.Name}: HP={data.Health}, ATK={data.Attack}");
}
```

遍历一个分组下的所有项：

```csharp
foreach (Id id in repo.GetAllId<CharacterConfig>())
{
    var data = repo.GetValue<CharacterConfig, CharacterData>(id.ItemId);
    Console.WriteLine($"{id}: {data?.Name}");
}
```

## 6. 添加自定义类型转换器

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

## 7. 使用多个格式

同一个仓储可以同时使用多个格式：

```csharp
IConfigSerializerFactory factory = new ConfigSerializerFactory()
    .WithYaml();

// 注册后，.json 走默认 JSON，.yaml/.yml 走 YAML
var jsonSerializer = factory.ForFile("Character\\chestnut.json");
var yamlSerializer = factory.ForFile("Character\\chestnut.yaml");
```

## 下一步

- [序列化器](serialization.md)：了解 `IConfigSerializer` 的完整能力
- [配置仓储](repository.md)：深入了解加载、缓存、哈希机制
- [标识符](id.md)：`Id` 的格式与比较方法
- [线程安全模型](threading.md)：并发场景下的行为约定