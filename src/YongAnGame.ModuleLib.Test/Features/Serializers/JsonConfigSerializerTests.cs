using YongAnGame.ModuleLib.Exceptions;
using YongAnGame.ModuleLib.Features.Serializers;

namespace YongAnGame.ModuleLib.Tests.Features.Serializers;

[TestClass]
public class JsonConfigSerializerTests
{
    private sealed class Sample
    {
        public string Name { get; init; } = "";
        public int Value { get; init; }
    }

    [TestMethod]
    public void RoundTrip_PreservesValues()
    {
        JsonConfigSerializer serializer = new();
        var original = new Sample { Name = "test", Value = 42 };

        string json = serializer.Serialize(original);
        Sample? deserialized = serializer.Deserialize<Sample>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.Name, deserialized.Name);
        Assert.AreEqual(original.Value, deserialized.Value);
    }

    [TestMethod]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        JsonConfigSerializer serializer = new();

        Sample? result = serializer.Deserialize<Sample>("");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_Whitespace_ReturnsNull()
    {
        JsonConfigSerializer serializer = new();

        Sample? result = serializer.Deserialize<Sample>("   ");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_InvalidJson_ThrowsSerializationException()
    {
        JsonConfigSerializer serializer = new();

        Assert.ThrowsExactly<SerializationException>(
            () => serializer.Deserialize<Sample>("{ invalid json"));
    }

    [TestMethod]
    public void Serialize_Null_ThrowsArgumentNullException()
    {
        JsonConfigSerializer serializer = new();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => serializer.Serialize<Sample>(null!));
    }

    [TestMethod]
    public void Deserialize_RuntimeType_ReturnsObject()
    {
        JsonConfigSerializer serializer = new();
        string json = """{"name":"test","value":42}""";

        object? result = serializer.Deserialize<Sample>(json);

        Assert.IsNotNull(result);
        var sample = result as Sample;
        Assert.IsNotNull(sample);
        Assert.AreEqual("test", sample.Name);
    }

    // ──────────────────────────────────────────────
    // 文件
    // ──────────────────────────────────────────────

    [TestMethod]
    public void DeserializeFromFile_Missing_ThrowsFileNotFound()
    {
        JsonConfigSerializer serializer = new();

        Assert.ThrowsExactly<FileNotFoundException>(
            () => serializer.DeserializeFromFile<Sample>("nonexistent.json"));
    }

    [TestMethod]
    public void SerializeToFile_CreatesDirectory()
    {
        JsonConfigSerializer serializer = new();
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string filePath = Path.Combine(tempDir, "sub", "config.json");

        try
        {
            serializer.SerializeToFile(new Sample { Name = "x" }, filePath);

            Assert.IsTrue(File.Exists(filePath));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    // ──────────────────────────────────────────────
    // 并发
    // ──────────────────────────────────────────────

    [TestMethod]
    public void Deserialize_Concurrent_DoesNotThrow()
    {
        JsonConfigSerializer serializer = new();
        string json = """{"name":"test","value":42}""";

        Parallel.For(0, 1000, _ =>
        {
            Sample? result = serializer.Deserialize<Sample>(json);
            Assert.IsNotNull(result);
        });
    }

    [TestMethod]
    public void AddTypeConverter_ThenDeserialize_AppliesConverter()
    {
        var serializer = new JsonConfigSerializer();
        serializer.AddTypeConverter(new UpperCaseStringConverter());

        WithString? result = serializer.Deserialize<WithString>("""{"text":"hello"}""");

        Assert.AreEqual("HELLO", result!.Text);
    }

    private sealed class WithString
    {
        public string Text { get; set; } = "";
    }

    private sealed class UpperCaseStringConverter
        : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(
            ref System.Text.Json.Utf8JsonReader reader,
            Type typeToConvert,
            System.Text.Json.JsonSerializerOptions options)
            => reader.GetString()!.ToUpperInvariant();

        public override void Write(
            System.Text.Json.Utf8JsonWriter writer,
            string value,
            System.Text.Json.JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
}