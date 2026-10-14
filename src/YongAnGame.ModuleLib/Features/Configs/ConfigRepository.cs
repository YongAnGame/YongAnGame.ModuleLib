using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using YongAnGame.ModuleLib.Components;
using YongAnGame.ModuleLib.Exceptions;
using YongAnGame.ModuleLib.Extensions;
using YongAnGame.ModuleLib.Interfaces.Configs;

namespace YongAnGame.ModuleLib.Features.Configs;

/// <summary>
/// <see cref="IConfigRepository"/> 的默认实现
/// </summary>
/// <remarks>
/// 使用 <see cref="IConfigSerializer"/> 处理实际的反序列化，
/// 自身只负责文件定位、缓存与哈希计算
/// <para>
/// 线程安全：使用 <see cref="ConcurrentDictionary{TKey, TValue}"/> 存储状态，
/// 所有成员均可并发调用
/// </para>
/// </remarks>
public class ConfigRepository(
    ILogger logger,
    IConfigOptions options,
    IConfigSerializer serializer,
    string modId)
    : IConfigRepository
{
    // 路径 → 文件哈希。并发加载时同一路径可能被多个线程读，TryAdd 保证只记一次
    private readonly ConcurrentDictionary<string, string> _configSha256s = new();

    // 分组类型 → (配置项 Id → 配置对象)。内层也是 ConcurrentDictionary，
    // 因为多个线程可能同时向同一分组加载不同配置项
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> _loadedConfigs = new();

    // 分组类型 → (配置项类型, 是否根目录)。注册阶段和 Load() 遍历可能并发
    private readonly ConcurrentDictionary<Type, (Type configType, bool isRoot)> _typeConfigs = new();

    /// <inheritdoc/>
    public IConfigSerializer Serializer { get; } = serializer;

    /// <inheritdoc/>
    public string Sha256
        => string.Join(",", _configSha256s.Values.Order(StringComparer.Ordinal)).Sha256Text();

    /// <inheritdoc/>
    public TConfig? GetValue<TType, TConfig>(string itemId)
    {
        TryGetValue<TType, TConfig>(itemId, out TConfig? obj);
        return obj;
    }

    /// <inheritdoc/>
    public bool TryGetValue<TType, TConfig>(string itemId, [NotNullWhen(true)] out TConfig? obj)
    {
        obj = default;
        if (_loadedConfigs.TryGetValue(typeof(TType), out var table)
            && table.TryGetValue(itemId, out object? config)
            && config is TConfig t)
        {
            obj = t;
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public IEnumerable<Id> GetAllId<T>()
    {
        Type type = typeof(T);
        if (!_loadedConfigs.TryGetValue(type, out var table))
            return [];
        return table.Keys.Select(itemId => new Id(modId, itemId));
    }

    /// <inheritdoc/>
    public IEnumerable<Type> GetAllConfigType() => _loadedConfigs.Keys;

    /// <inheritdoc/>
    public bool Load(Type classType, Type configType, string itemId, bool isRoot = false)
    {
        ArgumentNullException.ThrowIfNull(classType);
        ArgumentNullException.ThrowIfNull(configType);

        if (!_loadedConfigs.TryGetValue(classType, out var table))
            return false;

        string className = classType.Name[1..];
        logger.Information("正在尝试注册{ConfigId}到{ConfigTypeName}类型", itemId, className);

        string? path = FindConfigPath(classType, itemId, isRoot);
        if (path is null)
        {
            logger.Error(
                "无法注册{ConfigId}到{ConfigTypeName}类型，因为指定的{ConfigTypeName}类型ID无效",
                itemId, className);
            return false;
        }

        string fileContent;
        try
        {
            fileContent = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            logger.Error(ex,
                "无法注册{ConfigId}到{ConfigTypeName}类型，因为读取文件失败：{Path}",
                itemId, className, path);
            return false;
        }

        object? config;
        try
        {
            config = Serializer.Deserialize(fileContent, configType);
        }
        catch (SerializationException ex)
        {
            logger.Error(ex,
                "无法注册{ConfigId}到{ConfigTypeName}类型，因为配置数据无效：{Path}",
                itemId, className, path);
            return false;
        }

        if (config is null)
        {
            logger.Error(
                "无法注册{ConfigId}到{ConfigTypeName}类型，因为指定的{ConfigTypeName}类型数据无效",
                itemId, className);
            return false;
        }

        if (!table.TryAdd(itemId, config))
        {
            logger.Warning("配置{ConfigId}已在{ConfigTypeName}类型中注册，跳过重复注册", itemId, className);
            return false;
        }

        _configSha256s.TryAdd(path, fileContent.Sha256Text());
        logger.Information("完成注册{ConfigId}到{ConfigTypeName}类型", itemId, className);
        return true;
    }

    /// <inheritdoc/>
    public bool Load<TClass, TConfig>(string itemId, bool isRoot = false)
        => Load(typeof(TClass), typeof(TConfig), itemId, isRoot);

    /// <inheritdoc/>
    public void Load()
    {
        // ConcurrentDictionary 的枚举是安全的，反映调用时刻的一个快照
        foreach ((Type classType, (Type configType, bool isRoot)) in _typeConfigs)
        {
            string directory = Path.Combine(options.ModPath, modId, classType.Name[1..]);
            if (!Directory.Exists(directory)) continue;

            foreach (string path in Directory.EnumerateFiles(directory))
            {
                string extension = Path.GetExtension(path);
                if (!IsSupportedExtension(extension)) continue;

                string itemId = Path.GetFileNameWithoutExtension(path);
                if (_loadedConfigs.TryGetValue(classType, out var table)
                    && table.ContainsKey(itemId)) continue;

                Load(classType, configType, itemId, isRoot);
            }
        }
    }

    /// <inheritdoc/>
    public bool Add(Type classType, (Type configType, bool isRoot) config)
    {
        ArgumentNullException.ThrowIfNull(classType);

        // 两次 TryAdd 都成功才算注册成功
        // 若第一步成功第二步失败（几乎不可能，因为 ConcurrentDictionary 可以存空集合），
        // 会留下一个无内层字典的分组，GetValue 会因 TryGetValue 失败返回 false
        if (!_typeConfigs.TryAdd(classType, config))
            return false;
        if (!_loadedConfigs.TryAdd(classType, new ConcurrentDictionary<string, object>()))
            return false;
        return true;
    }

    /// <summary>
    /// 按反序列化器支持的扩展名查找配置文件
    /// </summary>
    /// <param name="classType">配置分组类型</param>
    /// <param name="itemId">配置项的 Id</param>
    /// <param name="isRoot">为 <see langword="true"/> 时从 MOD 根目录查找</param>
    /// <returns>找到时返回文件完整路径，否则返回 <see langword="null"/></returns>
    private string? FindConfigPath(Type classType, string itemId, bool isRoot)
    {
        string directory = isRoot
            ? Path.Combine(options.ModPath, modId)
            : Path.Combine(options.ModPath, modId, classType.Name[1..]);

        foreach (string extension in Serializer.Extensions)
        {
            string candidate = Path.Combine(directory, itemId + extension);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    private bool IsSupportedExtension(string extension)
        => Serializer.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
}