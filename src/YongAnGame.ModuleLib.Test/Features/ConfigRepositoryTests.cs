using Serilog;
using YongAnGame.ModuleLib.Features.Configs;
using YongAnGame.ModuleLib.Features.Serializers;
using YongAnGame.ModuleLib.Interfaces;
using YongAnGame.ModuleLib.Interfaces.Configs;

namespace YongAnGame.ModuleLib.Tests.Features;

[TestClass]
public class ConfigRepositoryTests
{
    // ──────────────────────────────────────────────
    // 测试辅助类型
    // ──────────────────────────────────────────────

    private sealed class CharacterConfig;

    private sealed class WeaponConfig;

    private sealed class CharacterData : IHasId
    {
        public string StringId { get; init; } = "";
        public string Name { get; init; } = "";
        public int Health { get; init; }
    }

    private sealed class WeaponData : IHasId
    {
        public string StringId { get; init; } = "";
        public int Damage { get; init; }
    }

    private sealed class FakeConfigOptions(string modPath) : IConfigOptions
    {
        public string ModPath { get; } = modPath;
    }

    // ──────────────────────────────────────────────
    // 测试基础设施
    // ──────────────────────────────────────────────

    private static readonly ILogger Logger = new LoggerConfiguration().CreateLogger();

    private string _tempRoot = "";
    private string _modId = "";
    private IConfigOptions _options = null!;
    private IConfigSerializer _serializer = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "ModuleLibTests", Guid.NewGuid().ToString("N"));
        _modId = "test_mod";
        Directory.CreateDirectory(Path.Combine(_tempRoot, _modId));

        _options = new FakeConfigOptions(_tempRoot);
        _serializer = new JsonConfigSerializer();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, recursive: true); }
            catch { /* 忽略清理失败 */ }
        }
    }

    private ConfigRepository CreateRepo()
        => new(Logger, _options, _serializer, _modId);

    /// <summary>
    /// 在 MOD 目录下创建配置文件
    /// </summary>
    private void WriteConfig(
        Type classType,
        string itemId,
        string content,
        bool isRoot = false,
        string extension = ".json")
    {
        string dir = isRoot
            ? Path.Combine(_tempRoot, _modId)
            : Path.Combine(_tempRoot, _modId, classType.Name[1..]);
        Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, itemId + extension);
        File.WriteAllText(path, content);
    }

    // ══════════════════════════════════════════════
    // Add
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Add_NewGroup_ReturnsTrue()
    {
        var repo = CreateRepo();

        bool result = repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Add_DuplicateGroup_ReturnsFalse()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        bool result = repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Add_NullClassType_Throws()
    {
        var repo = CreateRepo();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => repo.Add(null!, (typeof(CharacterData), false)));
    }

    // ══════════════════════════════════════════════
    // Load（单个）
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Load_SingleItem_ReturnsTrue()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku","health":100}""");

        bool result = repo.Load<CharacterConfig, CharacterData>("char_001");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Load_SingleItem_ThenQuery_ReturnsData()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku","health":100}""");

        repo.Load<CharacterConfig, CharacterData>("char_001");
        bool found = repo.TryGetValue<CharacterConfig, CharacterData>(
            "char_001", out var data);

        Assert.IsTrue(found);
        Assert.IsNotNull(data);
        Assert.AreEqual("Miku", data.Name);
        Assert.AreEqual(100, data.Health);
    }

    [TestMethod]
    public void Load_SingleItem_MissingFile_ReturnsFalse()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        bool result = repo.Load<CharacterConfig, CharacterData>("missing");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Load_SingleItem_UnregisteredGroup_ReturnsFalse()
    {
        var repo = CreateRepo();
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001"}""");

        bool result = repo.Load<CharacterConfig, CharacterData>("char_001");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Load_SingleItem_InvalidJson_ReturnsFalse()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", "{ invalid json");

        bool result = repo.Load<CharacterConfig, CharacterData>("char_001");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Load_SingleItem_Duplicate_SecondReturnsFalse()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku"}""");

        bool first = repo.Load<CharacterConfig, CharacterData>("char_001");
        bool second = repo.Load<CharacterConfig, CharacterData>("char_001");

        Assert.IsTrue(first);
        Assert.IsFalse(second);
    }

    [TestMethod]
    public void Load_NullClassType_Throws()
    {
        var repo = CreateRepo();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => repo.Load(null!, typeof(CharacterData), "x"));
    }

    [TestMethod]
    public void Load_NullConfigType_Throws()
    {
        var repo = CreateRepo();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => repo.Load(typeof(CharacterConfig), null!, "x"));
    }

    // ══════════════════════════════════════════════
    // Load（泛型）
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Load_Generic_SingleItem_ReturnsTrue()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku"}""");

        bool result = repo.Load<CharacterConfig, CharacterData>("char_001");

        Assert.IsTrue(result);
    }

    // ══════════════════════════════════════════════
    // Load（全量扫描）
    // ══════════════════════════════════════════════

    [TestMethod]
    public void LoadAll_ScansRegisteredGroups()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        repo.Add(typeof(WeaponConfig), (typeof(WeaponData), false));

        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku"}""");
        WriteConfig(typeof(CharacterConfig), "char_002",
            """{"stringId":"char_002","name":"Rin"}""");
        WriteConfig(typeof(WeaponConfig), "sword_01",
            """{"stringId":"sword_01","damage":50}""");

        repo.Load();

        Assert.IsTrue(repo.TryGetValue<CharacterConfig, CharacterData>("char_001", out _));
        Assert.IsTrue(repo.TryGetValue<CharacterConfig, CharacterData>("char_002", out _));
        Assert.IsTrue(repo.TryGetValue<WeaponConfig, WeaponData>("sword_01", out _));
    }

    [TestMethod]
    public void LoadAll_MissingDirectory_SkipsSilently()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        // 不创建目录，直接 Load

        repo.Load();

        Assert.AreEqual(0, repo.GetAllId<CharacterConfig>().Count());
    }

    [TestMethod]
    public void LoadAll_IgnoresNonMatchingExtensions()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001"}""", extension: ".json");
        WriteConfig(typeof(CharacterConfig), "char_002",
            """not a config""", extension: ".txt");
        WriteConfig(typeof(CharacterConfig), "char_003",
            """not a config""", extension: ".xml");

        repo.Load();

        Assert.IsTrue(repo.TryGetValue<CharacterConfig, CharacterData>("char_001", out _));
        Assert.IsFalse(repo.TryGetValue<CharacterConfig, CharacterData>("char_002", out _));
        Assert.IsFalse(repo.TryGetValue<CharacterConfig, CharacterData>("char_003", out _));
    }

    [TestMethod]
    public void LoadAll_SkipsAlreadyLoaded()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku"}""");

        repo.Load();
        repo.Load();   // 第二次应跳过已加载项

        int count = repo.GetAllId<CharacterConfig>().Count();
        Assert.AreEqual(1, count);
    }

    // ══════════════════════════════════════════════
    // TryGetValue / GetValue
    // ══════════════════════════════════════════════

    [TestMethod]
    public void GetValue_Missing_ReturnsNull()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        var result = repo.GetValue<CharacterConfig, CharacterData>("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryGetValue_Missing_ReturnsFalse()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        bool found = repo.TryGetValue<CharacterConfig, CharacterData>("missing", out var data);

        Assert.IsFalse(found);
        Assert.IsNull(data);
    }

    [TestMethod]
    public void TryGetValue_WrongConfigType_ReturnsFalse()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001"}""");
        repo.Load();

        // 用 WeaponData 去查 CharacterConfig 分组
        bool found = repo.TryGetValue<CharacterConfig, WeaponData>("char_001", out _);

        Assert.IsFalse(found);
    }

    // ══════════════════════════════════════════════
    // GetAllId / GetAllConfigType
    // ══════════════════════════════════════════════

    [TestMethod]
    public void GetAllId_ReturnsLoadedIds()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        WriteConfig(typeof(CharacterConfig), "char_002", """{"stringId":"char_002"}""");
        repo.Load();

        var ids = repo.GetAllId<CharacterConfig>()
            .Select(id => id.ItemId)
            .OrderBy(x => x)
            .ToList();

        CollectionAssert.AreEqual(
            expected, ids);
    }

    [TestMethod]
    public void GetAllId_UnregisteredGroup_ReturnsEmpty()
    {
        var repo = CreateRepo();

        var ids = repo.GetAllId<CharacterConfig>().ToList();

        Assert.IsEmpty(ids);
    }

    [TestMethod]
    public void GetAllId_IncludesModIdInReturnedId()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        repo.Load();

        var id = repo.GetAllId<CharacterConfig>().First();

        Assert.AreEqual(_modId, id.ModId);
        Assert.AreEqual("char_001", id.ItemId);
    }

    [TestMethod]
    public void GetAllConfigType_ReturnsRegisteredTypes()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        repo.Add(typeof(WeaponConfig), (typeof(WeaponData), false));

        var types = repo.GetAllConfigType().ToList();

        CollectionAssert.Contains(types, typeof(CharacterConfig));
        CollectionAssert.Contains(types, typeof(WeaponConfig));
    }

    // ══════════════════════════════════════════════
    // Sha256
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Sha256_StableAcrossLoadOrder()
    {
        // 场景 1：先加载 char_001 再加载 char_002
        var repo1 = CreateRepo();
        repo1.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        WriteConfig(typeof(CharacterConfig), "char_002", """{"stringId":"char_002"}""");
        repo1.Load<CharacterConfig, CharacterData>("char_001");
        repo1.Load<CharacterConfig, CharacterData>("char_002");

        // 场景 2：反序加载
        var repo2 = CreateRepo();
        repo2.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        repo2.Load<CharacterConfig, CharacterData>("char_002");
        repo2.Load<CharacterConfig, CharacterData>("char_001");

        Assert.AreEqual(repo1.Sha256, repo2.Sha256);
    }

    [TestMethod]
    public void Sha256_DifferentContent_DifferentHash()
    {
        var repo1 = CreateRepo();
        repo1.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        repo1.Load();

        Cleanup();
        Setup();

        var repo2 = CreateRepo();
        repo2.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"different"}""");
        repo2.Load();

        Assert.AreNotEqual(repo1.Sha256, repo2.Sha256);
    }

    [TestMethod]
    public void Sha256_EmptyRepo_ReturnsStableValue()
    {
        var repo = CreateRepo();

        string hash = repo.Sha256;

        Assert.IsNotNull(hash);
        Assert.IsGreaterThan(0, hash.Length);
    }

    // ══════════════════════════════════════════════
    // Serializer 属性
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Serializer_ReturnsInjectedInstance()
    {
        var repo = CreateRepo();

        Assert.AreSame(_serializer, repo.Serializer);
    }

    // ══════════════════════════════════════════════
    // 并发
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Load_ConcurrentSameItem_OnlyOneSucceeds()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");

        int successCount = 0;

        Parallel.For(0, 50, _ =>
        {
            if (repo.Load<CharacterConfig, CharacterData>("char_001"))
                Interlocked.Increment(ref successCount);
        });

        Assert.AreEqual(1, successCount);
    }

    [TestMethod]
    public void Load_ConcurrentDifferentItems_AllSucceed()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        for (int i = 0; i < 20; i++)
        {
            WriteConfig(typeof(CharacterConfig), $"char_{i:D3}",
                $$"""{"stringId":"char_{{i:D3}}"}""");
        }

        Parallel.For(0, 20, i =>
        {
            repo.Load<CharacterConfig, CharacterData>($"char_{i:D3}");
        });

        Assert.AreEqual(20, repo.GetAllId<CharacterConfig>().Count());
    }

    [TestMethod]
    public void Query_ConcurrentWithLoad_DoesNotThrow()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        for (int i = 0; i < 20; i++)
        {
            WriteConfig(typeof(CharacterConfig), $"char_{i:D3}",
                $$"""{"stringId":"char_{{i:D3}}"}""");
        }

        var loadTask = Task.Run(() =>
        {
            for (int i = 0; i < 20; i++)
                repo.Load<CharacterConfig, CharacterData>($"char_{i:D3}");
        }, TestContext.CancellationToken);

        var queryTask = Task.Run(() =>
        {
            for (int i = 0; i < 500; i++)
            {
                _ = repo.TryGetValue<CharacterConfig, CharacterData>("char_000", out _);
                _ = repo.GetAllId<CharacterConfig>().Count();
            }
        }, TestContext.CancellationToken);

        Task.WaitAll(loadTask, queryTask);
    }

    [TestMethod]
    public void Add_Concurrent_OnlyOneSucceeds()
    {
        var repo = CreateRepo();

        int successCount = 0;

        Parallel.For(0, 50, _ =>
        {
            if (repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false)))
                Interlocked.Increment(ref successCount);
        });

        Assert.AreEqual(1, successCount);
    }

    // ══════════════════════════════════════════════
    // isRoot
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Load_IsRootTrue_FindsInModRoot()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), true));
        WriteConfig(typeof(CharacterConfig), "global_config",
            """{"stringId":"global_config","name":"global"}""", isRoot: true);

        bool result = repo.Load<CharacterConfig, CharacterData>("global_config", isRoot: true);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Load_IsRootFalse_FindsInSubdirectory()
    {
        var repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku"}""", isRoot: false);

        bool result = repo.Load<CharacterConfig, CharacterData>("char_001", isRoot: false);

        Assert.IsTrue(result);
    }

    public TestContext TestContext { get; set; }

    private static readonly string[] expected = ["char_001", "char_002"];
}