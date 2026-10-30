# 序列化器

`IConfigSerializer` 是读写配置的统一入口，屏蔽底层 JSON / YAML 库的差异。

## 支持的格式

| 格式 | 实现类 | 扩展名 |
|------|--------|--------|
| JSON | `JsonConfigSerializer` | `.json` |
| YAML | `YamlConfigSerializer` | `.yaml`、`.yml` |

两者都实现 `IConfigSerializer`，调用方通过接口使用，不感知具体实现。

## 基本用法

```csharp
IConfigSerializer serializer = new JsonConfigSerializer();

// 序列化
string json = serializer.Serialize(myObject);

// 反序列化
var obj = serializer.Deserialize<MyConfig>(json);

// 从文件读
var config = serializer.DeserializeFromFile<MyConfig>("config.json");

// 写文件
serializer.SerializeToFile(myObject, "config.json");
```

## 运行时类型

当目标类型只在运行时确定时，使用非泛型重载：

```csharp
Type configType = typeof(MyConfig);
object? config = serializer.Deserialize(json, configType);
```

## 添加类型转换器

自定义类型通过转换器扩展：

### JSON

```csharp
public sealed class GridCellConverter : JsonConverter<GridCell>
{
    public override GridCell Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var text = reader.GetString()!;
        var parts = text.Split(',');
        return new GridCell(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public override void Write(
        Utf8JsonWriter writer,
        GridCell value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.X},{value.Y}");
    }
}

serializer.AddTypeConverter(new GridCellConverter());
```

### YAML

```csharp
public sealed class GridCellConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(GridCell);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var text = parser.Consume<Scalar>().Value;
        var parts = text.Split(',');
        return new GridCell(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var cell = (GridCell)value!;
        emitter.Emit(new Scalar($"{cell.X},{cell.Y}"));
    }
}

serializer.AddTypeConverter(new GridCellConverter());
```

## 多格式工厂

`IConfigSerializerFactory` 按文件扩展名选择合适的序列化器：

```csharp
IConfigSerializerFactory factory = new ConfigSerializerFactory()
    .WithYaml();

factory.ForFile("data.json");   // → JsonConfigSerializer
factory.ForFile("data.yaml");   // → YamlConfigSerializer
factory.ForFile("data.yml");    // → YamlConfigSerializer
```

工厂构造时默认注册 `JsonConfigSerializer` 作为兜底，扩展名不匹配任何实现时使用。

### 工厂与仓储的关系

`ConfigRepository` 的构造函数接收**单个** `IConfigSerializer`，而不是 `IConfigSerializerFactory`。也就是说：

- 工厂用于调用方按需获取序列化器实例
- 一个仓储实例只处理一个序列化器声明的扩展名
- 希望同一 MOD 支持多种格式时，为每种格式创建独立仓储实例

如果后续版本让仓储直接接收工厂，会在本节更新说明。

## 异常约定

所有序列化失败都包装为 `SerializationException`：

```csharp
try
{
    var config = serializer.DeserializeFromFile<MyConfig>("bad.json");
}
catch (SerializationException ex)
{
    // 统一处理，不用分别 catch JsonException、YamlException
    logger.LogError(ex, "配置加载失败");
}
```

异常消息包含文件路径和位置信息（如果底层库提供）。

## 线程安全

- 所有成员可并发调用
- 读路径无锁
- 写路径（`AddTypeConverter`）加锁重建，原子替换
- 添加转换器后立即生效，正在进行的序列化不受影响