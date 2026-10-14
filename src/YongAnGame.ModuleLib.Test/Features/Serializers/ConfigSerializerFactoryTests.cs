using YongAnGame.ModuleLib.Compatibility;
using YongAnGame.ModuleLib.Features.Serializers;
using YongAnGame.ModuleLib.Interfaces.Configs;

namespace YongAnGame.ModuleLib.Tests.Features.Serializers;

[TestClass]
public class ConfigSerializerFactoryTests
{
    [TestMethod]
    public void ForFile_Json_ReturnsJsonSerializer()
    {
        ConfigSerializerFactory factory = new();

        IConfigSerializer serializer = factory.ForFile("config.json");

        Assert.IsInstanceOfType<JsonConfigSerializer>(serializer);
    }

    [TestMethod]
    public void ForFile_Yaml_AfterWithYaml_ReturnsYamlSerializer()
    {
        IConfigSerializerFactory factory = new ConfigSerializerFactory()
            .WithYaml();

        IConfigSerializer serializer = factory.ForFile("config.yaml");

        Assert.IsInstanceOfType<YamlConfigSerializer>(serializer);
    }

    [TestMethod]
    public void ForFile_Yml_ReturnsYamlSerializer()
    {
        IConfigSerializerFactory factory = new ConfigSerializerFactory()
            .WithYaml();

        IConfigSerializer serializer = factory.ForFile("config.yml");

        Assert.IsInstanceOfType<YamlConfigSerializer>(serializer);
    }

    [TestMethod]
    public void ForFile_Unknown_ReturnsFallback()
    {
        ConfigSerializerFactory factory = new();

        IConfigSerializer serializer = factory.ForFile("config.txt");

        Assert.IsInstanceOfType<JsonConfigSerializer>(serializer);
    }

    [TestMethod]
    public void ForFile_CaseInsensitive_ReturnsYaml()
    {
        IConfigSerializerFactory factory = new ConfigSerializerFactory()
            .WithYaml();

        IConfigSerializer serializer = factory.ForFile("config.YAML");

        Assert.IsInstanceOfType<YamlConfigSerializer>(serializer);
    }

    [TestMethod]
    public void AllExtensions_WithYaml_ContainsJsonAndYaml()
    {
        IConfigSerializerFactory factory = new ConfigSerializerFactory()
            .WithYaml();

        IReadOnlyList<string> extensions = factory.AllExtensions();

        CollectionAssert.Contains(extensions.ToList(), ".json");
        CollectionAssert.Contains(extensions.ToList(), ".yaml");
        CollectionAssert.Contains(extensions.ToList(), ".yml");
    }

    [TestMethod]
    public void Register_Null_Throws()
    {
        ConfigSerializerFactory factory = new();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => factory.Register(null!));
    }

    // ──────────────────────────────────────────────
    // 并发
    // ──────────────────────────────────────────────

    [TestMethod]
    public void Register_Concurrent_DoesNotThrow()
    {
        ConfigSerializerFactory factory = new();

        var exceptions = new List<Exception>();

        Parallel.For(0, 100, _ =>
        {
            try
            {
                factory.Register(new JsonConfigSerializer());
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        });

        Assert.IsEmpty(exceptions);
    }

    [TestMethod]
    public async Task ForFile_ConcurrentWithRegister_DoesNotThrow()
    {
        IConfigSerializerFactory factory = new ConfigSerializerFactory()
            .WithYaml();

        var writers = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
                factory.Register(new JsonConfigSerializer());
        }, TestContext.CancellationToken);

        var readers = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
                _ = factory.ForFile($"file_{i}.json");
        }, TestContext.CancellationToken);

        await Task.WhenAll(writers, readers);

        // 只要不抛异常即通过
    }

    public TestContext TestContext { get; set; }
}