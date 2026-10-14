using YongAnGame.ModuleLib.Exceptions;
using YongAnGame.ModuleLib.Features.Serializers;

namespace YongAnGame.ModuleLib.Tests.Features.Serializers;

[TestClass]
public class YamlConfigSerializerTests
{
    private sealed class Sample
    {
        public string Name { get; init; } = "";
        public int Value { get; init; }
    }

    [TestMethod]
    public void RoundTrip_PreservesValues()
    {
        YamlConfigSerializer serializer = new();
        var original = new Sample { Name = "test", Value = 42 };

        string yaml = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<Sample>(yaml);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.Name, deserialized.Name);
        Assert.AreEqual(original.Value, deserialized.Value);
    }

    [TestMethod]
    public void Deserialize_EmptyString_ReturnsNull()
    {
        YamlConfigSerializer serializer = new();

        var result = serializer.Deserialize<Sample>("");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Deserialize_InvalidYaml_ThrowsSerializationException()
    {
        YamlConfigSerializer serializer = new();

        Assert.ThrowsExactly<SerializationException>(
            () => serializer.Deserialize<Sample>("{ invalid: yaml: :"));
    }

    [TestMethod]
    public void AddTypeConverter_WrongType_ThrowsSerializationException()
    {
        var serializer = new YamlConfigSerializer();

        Assert.ThrowsExactly<SerializationException>(
            () => serializer.AddTypeConverter(new object()));
    }

    [TestMethod]
    public void Deserialize_Concurrent_DoesNotThrow()
    {
        YamlConfigSerializer serializer = new();
        string yaml = "name: test\nvalue: 42";

        Parallel.For(0, 1000, _ =>
        {
            var result = serializer.Deserialize<Sample>(yaml);
            Assert.IsNotNull(result);
        });
    }
}