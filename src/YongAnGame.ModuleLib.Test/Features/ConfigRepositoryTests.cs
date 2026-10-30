using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YongAnGame.ModuleLib.Components;
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

    /// <summary>
    /// ConfigRepository 依赖 Microsoft.Extensions.Logging.ILogger。
    /// 测试中使用 NullLogger 静默丢弃所有日志，避免测试代码
    /// 依赖任何具体的日志实现（如 Serilog）
    /// </summary>
    private static readonly ILogger Logger = NullLogger.Instance;

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
        ConfigRepository repo = CreateRepo();

        bool result = repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Add_DuplicateGroup_ReturnsFalse()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        bool result = repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Add_NullClassType_Throws()
    {
        ConfigRepository repo = CreateRepo();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => repo.Add(null!, (typeof(CharacterData), false)));
    }

    // ══════════════════════════════════════════════
    // Load（单个）
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Load_SingleItem_ReturnsTrue()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku","health":100}""");

        bool result = repo.Load<CharacterConfig, CharacterData>("char_001");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Load_SingleItem_ThenQuery_ReturnsData()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku","health":100}""");

        repo.Load<CharacterConfig, CharacterData>("char_001");
        bool found = repo.TryGetValue<CharacterConfig, CharacterData>(
            "char_001", out CharacterData? data);

        Assert.IsTrue(found);
        Assert.IsNotNull(data);
        Assert.AreEqual("Miku", data.Name);
        Assert.AreEqual(100, data.Health);
    }

    [TestMethod]
    public void Load_SingleItem_MissingFile_ReturnsFalse()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        bool result = repo.Load<CharacterConfig, CharacterData>("missing");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Load_SingleItem_UnregisteredGroup_ReturnsFalse()
    {
        ConfigRepository repo = CreateRepo();
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001"}""");

        bool result = repo.Load<CharacterConfig, CharacterData>("char_001");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Load_SingleItem_InvalidJson_ReturnsFalse()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", "{ invalid json");

        bool result = repo.Load<CharacterConfig, CharacterData>("char_001");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Load_SingleItem_Duplicate_SecondReturnsFalse()
    {
        ConfigRepository repo = CreateRepo();
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
        ConfigRepository repo = CreateRepo();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => repo.Load(null!, typeof(CharacterData), "x"));
    }

    [TestMethod]
    public void Load_NullConfigType_Throws()
    {
        ConfigRepository repo = CreateRepo();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => repo.Load(typeof(CharacterConfig), null!, "x"));
    }

    // ══════════════════════════════════════════════
    // Load（全量扫描）
    // ══════════════════════════════════════════════

    [TestMethod]
    public void LoadAll_ScansRegisteredGroups()
    {
        ConfigRepository repo = CreateRepo();
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
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        repo.Load();

        Assert.IsEmpty(repo.GetAllId<CharacterConfig>());
    }

    [TestMethod]
    public void LoadAll_IgnoresNonMatchingExtensions()
    {
        ConfigRepository repo = CreateRepo();
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
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku"}""");

        repo.Load();
        repo.Load();

        int count = repo.GetAllId<CharacterConfig>().Count();
        Assert.AreEqual(1, count);
    }

    // ══════════════════════════════════════════════
    // TryGetValue / GetValue
    // ══════════════════════════════════════════════

    [TestMethod]
    public void GetValue_Missing_ReturnsNull()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        CharacterData? result = repo.GetValue<CharacterConfig, CharacterData>("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryGetValue_Missing_ReturnsFalse()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        bool found = repo.TryGetValue<CharacterConfig, CharacterData>("missing", out CharacterData? data);

        Assert.IsFalse(found);
        Assert.IsNull(data);
    }

    [TestMethod]
    public void TryGetValue_WrongConfigType_ReturnsFalse()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001"}""");
        repo.Load();

        bool found = repo.TryGetValue<CharacterConfig, WeaponData>("char_001", out _);

        Assert.IsFalse(found);
    }

    // ══════════════════════════════════════════════
    // GetAllId / GetAllConfigType
    // ══════════════════════════════════════════════

    [TestMethod]
    public void GetAllId_ReturnsLoadedIds()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        WriteConfig(typeof(CharacterConfig), "char_002", """{"stringId":"char_002"}""");
        repo.Load();

        var ids = repo.GetAllId<CharacterConfig>()
            .Select(id => id.ItemId)
            .OrderBy(x => x)
            .ToList();

        CollectionAssert.AreEqual(expected, ids);
    }

    [TestMethod]
    public void GetAllId_UnregisteredGroup_ReturnsEmpty()
    {
        ConfigRepository repo = CreateRepo();

        var ids = repo.GetAllId<CharacterConfig>().ToList();

        Assert.IsEmpty(ids);
    }

    [TestMethod]
    public void GetAllId_IncludesModIdInReturnedId()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        repo.Load();

        Id id = repo.GetAllId<CharacterConfig>().First();

        Assert.AreEqual(_modId, id.ModId);
        Assert.AreEqual("char_001", id.ItemId);
    }

    [TestMethod]
    public void GetAllConfigType_ReturnsRegisteredTypes()
    {
        ConfigRepository repo = CreateRepo();
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
        ConfigRepository repo1 = CreateRepo();
        repo1.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        WriteConfig(typeof(CharacterConfig), "char_002", """{"stringId":"char_002"}""");
        repo1.Load<CharacterConfig, CharacterData>("char_001");
        repo1.Load<CharacterConfig, CharacterData>("char_002");

        ConfigRepository repo2 = CreateRepo();
        repo2.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        repo2.Load<CharacterConfig, CharacterData>("char_002");
        repo2.Load<CharacterConfig, CharacterData>("char_001");

        Assert.AreEqual(repo1.Sha256, repo2.Sha256);
    }

    [TestMethod]
    public void Sha256_DifferentContent_DifferentHash()
    {
        ConfigRepository repo1 = CreateRepo();
        repo1.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        repo1.Load();

        Cleanup();
        Setup();

        ConfigRepository repo2 = CreateRepo();
        repo2.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"different"}""");
        repo2.Load();

        Assert.AreNotEqual(repo1.Sha256, repo2.Sha256);
    }

    [TestMethod]
    public void Sha256_EmptyRepo_ReturnsStableValue()
    {
        ConfigRepository repo = CreateRepo();

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
        ConfigRepository repo = CreateRepo();

        Assert.AreSame(_serializer, repo.Serializer);
    }

    // ══════════════════════════════════════════════
    // Unload（单个）
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Unload_SingleItem_ReturnsTrue()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        repo.Load<CharacterConfig, CharacterData>("char_001");

        bool result = repo.Unload<CharacterConfig>("char_001");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Unload_SingleItem_MissingItem_ReturnsFalse()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        bool result = repo.Unload<CharacterConfig>("missing");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Unload_SingleItem_RemovesFromCache()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        repo.Load<CharacterConfig, CharacterData>("char_001");

        repo.Unload<CharacterConfig>("char_001");

        Assert.IsFalse(repo.TryGetValue<CharacterConfig, CharacterData>("char_001", out _));
        Assert.IsEmpty(repo.GetAllId<CharacterConfig>());
    }

    [TestMethod]
    public void Unload_SingleItem_ClearsSha256()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");

        string emptyHash = repo.Sha256;
        repo.Load<CharacterConfig, CharacterData>("char_001");
        Assert.AreNotEqual(emptyHash, repo.Sha256);

        repo.Unload<CharacterConfig>("char_001");

        // 唯一引用被移除后，哈希应回到空仓储状态
        Assert.AreEqual(emptyHash, repo.Sha256);
    }

    [TestMethod]
    public void Unload_SingleItem_NullClassType_Throws()
    {
        ConfigRepository repo = CreateRepo();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => repo.Unload(null!, "char_001"));
    }

    [TestMethod]
    public void Unload_SingleItem_NullItemId_Throws()
    {
        ConfigRepository repo = CreateRepo();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => repo.Unload<CharacterConfig>(null!));
    }

    [TestMethod]
    public void Unload_SingleItem_BlankItemId_Throws()
    {
        ConfigRepository repo = CreateRepo();

        Assert.ThrowsExactly<ArgumentException>(
            () => repo.Unload<CharacterConfig>("   "));
    }

    [TestMethod]
    public void Unload_ThenLoad_Succeeds()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku"}""");
        repo.Load<CharacterConfig, CharacterData>("char_001");

        repo.Unload<CharacterConfig>("char_001");
        bool reloaded = repo.Load<CharacterConfig, CharacterData>("char_001");

        Assert.IsTrue(reloaded);
        Assert.IsTrue(repo.TryGetValue<CharacterConfig, CharacterData>("char_001", out _));
    }

    // ══════════════════════════════════════════════
    // Unload（分组）
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Unload_Group_ReturnsTrue()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));

        bool result = repo.Unload(typeof(CharacterConfig));

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Unload_Group_Unregistered_ReturnsFalse()
    {
        ConfigRepository repo = CreateRepo();

        bool result = repo.Unload(typeof(CharacterConfig));

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Unload_Group_RemovesAllItems()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        WriteConfig(typeof(CharacterConfig), "char_002", """{"stringId":"char_002"}""");
        repo.Load();

        repo.Unload(typeof(CharacterConfig));

        Assert.IsEmpty(repo.GetAllId<CharacterConfig>());
        Assert.IsFalse(repo.TryGetValue<CharacterConfig, CharacterData>("char_001", out _));
        Assert.IsFalse(repo.TryGetValue<CharacterConfig, CharacterData>("char_002", out _));
    }

    [TestMethod]
    public void Unload_Group_KeepsGroupRegistered()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        repo.Load();

        repo.Unload(typeof(CharacterConfig));

        // 分组注册保留
        CollectionAssert.Contains(repo.GetAllConfigType().ToList(), typeof(CharacterConfig));

        // 可以重新加载
        Assert.IsTrue(repo.Load<CharacterConfig, CharacterData>("char_001"));
    }

    [TestMethod]
    public void Unload_SharedRootPath_KeepsHashUntilLastUnload()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), true));
        repo.Add(typeof(WeaponConfig), (typeof(WeaponData), true));

        // 同一文件被两个分组加载
        WriteConfig(typeof(CharacterConfig), "shared",
            """{"stringId":"shared","name":"shared","damage":10}""", isRoot: true);

        repo.Load<CharacterConfig, CharacterData>("shared", isRoot: true);
        repo.Load<WeaponConfig, WeaponData>("shared", isRoot: true);

        string hashBefore = repo.Sha256;

        // 卸载第一个分组：WeaponConfig 仍引用同一文件，哈希不应变化
        repo.Unload<CharacterConfig>("shared");
        Assert.AreEqual(hashBefore, repo.Sha256);

        // 卸载第二个分组：无其他引用，哈希应变化
        repo.Unload<WeaponConfig>("shared");
        Assert.AreNotEqual(hashBefore, repo.Sha256);
    }

    // ══════════════════════════════════════════════
    // UnloadAll
    // ══════════════════════════════════════════════

    [TestMethod]
    public void UnloadAll_ClearsAllItems()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        repo.Add(typeof(WeaponConfig), (typeof(WeaponData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        WriteConfig(typeof(WeaponConfig), "sword_01", """{"stringId":"sword_01"}""");
        repo.Load();

        repo.UnloadAll();

        Assert.IsEmpty(repo.GetAllId<CharacterConfig>());
        Assert.IsEmpty(repo.GetAllId<WeaponConfig>());
    }

    [TestMethod]
    public void UnloadAll_KeepsGroupsRegistered()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        repo.Add(typeof(WeaponConfig), (typeof(WeaponData), false));

        repo.UnloadAll();

        var types = repo.GetAllConfigType().ToList();
        CollectionAssert.Contains(types, typeof(CharacterConfig));
        CollectionAssert.Contains(types, typeof(WeaponConfig));
    }

    [TestMethod]
    public void UnloadAll_ResetsSha256()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");

        string emptyHash = repo.Sha256;
        repo.Load();
        Assert.AreNotEqual(emptyHash, repo.Sha256);

        repo.UnloadAll();

        Assert.AreEqual(emptyHash, repo.Sha256);
    }

    // ══════════════════════════════════════════════
    // 并发
    // ══════════════════════════════════════════════

    [TestMethod]
    public void Load_ConcurrentSameItem_OnlyOneSucceeds()
    {
        ConfigRepository repo = CreateRepo();
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
        ConfigRepository repo = CreateRepo();
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
        ConfigRepository repo = CreateRepo();
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
        ConfigRepository repo = CreateRepo();

        int successCount = 0;

        Parallel.For(0, 50, _ =>
        {
            if (repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false)))
                Interlocked.Increment(ref successCount);
        });

        Assert.AreEqual(1, successCount);
    }

    [TestMethod]
    public void Unload_ConcurrentSameItem_OnlyOneSucceeds()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001", """{"stringId":"char_001"}""");
        repo.Load<CharacterConfig, CharacterData>("char_001");

        int successCount = 0;

        Parallel.For(0, 50, _ =>
        {
            if (repo.Unload<CharacterConfig>("char_001"))
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
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), true));
        WriteConfig(typeof(CharacterConfig), "global_config",
            """{"stringId":"global_config","name":"global"}""", isRoot: true);

        bool result = repo.Load<CharacterConfig, CharacterData>("global_config", isRoot: true);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Load_IsRootFalse_FindsInSubdirectory()
    {
        ConfigRepository repo = CreateRepo();
        repo.Add(typeof(CharacterConfig), (typeof(CharacterData), false));
        WriteConfig(typeof(CharacterConfig), "char_001",
            """{"stringId":"char_001","name":"Miku"}""", isRoot: false);

        bool result = repo.Load<CharacterConfig, CharacterData>("char_001", isRoot: false);

        Assert.IsTrue(result);
    }

    public TestContext TestContext { get; set; }

    private static readonly string[] expected = ["char_001", "char_002"];
}