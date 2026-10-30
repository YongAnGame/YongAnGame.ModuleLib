using Microsoft.Extensions.Logging;
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
/// 所有成员均可并发调用。卸载操作建议与加载串行化，
/// 避免并发卸载共享根目录配置的分组
/// </para>
/// </remarks>
public partial class ConfigRepository(
    ILogger logger,
    IConfigOptions options,
    IConfigSerializer serializer,
    string modId)
    : IConfigRepository
{
    // 路径 → 文件哈希。并发加载时同一路径可能被多个线程读，TryAdd 保证只记一次
    private readonly ConcurrentDictionary<string, string> _configSha256s = new();

    // 分组类型 → (配置项 Id → 配置对象)
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> _loadedConfigs = new();

    // 分组类型 → (配置项 Id → 文件路径)。卸载时据此定位并清理哈希
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, string>> _loadedPaths = new();

    // 分组类型 → (配置项类型, 是否根目录)
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
        if (_loadedConfigs.TryGetValue(typeof(TType), out ConcurrentDictionary<string, object>? table)
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
        if (!_loadedConfigs.TryGetValue(type, out ConcurrentDictionary<string, object>? table))
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

        if (!_loadedConfigs.TryGetValue(classType, out ConcurrentDictionary<string, object>? table))
            return false;

        string className = classType.Name[1..];
        LogLoadingConfig(logger, itemId, className);

        string? path = FindConfigPath(classType, itemId, isRoot);
        if (path is null)
        {
            LogInvalidConfigId(logger, itemId, className);
            return false;
        }

        string fileContent;
        try
        {
            fileContent = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            LogReadFileFailed(logger, ex, itemId, className, path);
            return false;
        }

        object? config;
        try
        {
            config = Serializer.Deserialize(fileContent, configType);
        }
        catch (SerializationException ex)
        {
            LogDeserializeFailed(logger, ex, itemId, className, path);
            return false;
        }

        if (config is null)
        {
            LogInvalidConfigData(logger, itemId, className);
            return false;
        }

        if (!table.TryAdd(itemId, config))
        {
            LogDuplicateRegistration(logger, itemId, className);
            return false;
        }

        // 记录 itemId → path，供卸载时清理哈希
        _loadedPaths
            .GetOrAdd(classType, static _ => new ConcurrentDictionary<string, string>())
            [itemId] = path;

        _configSha256s.TryAdd(path, fileContent.Sha256Text());
        LogRegistrationCompleted(logger, itemId, className);
        return true;
    }

    /// <inheritdoc/>
    public bool Load<TClass, TConfig>(string itemId, bool isRoot = false)
        => Load(typeof(TClass), typeof(TConfig), itemId, isRoot);

    /// <inheritdoc/>
    public void Load()
    {
        foreach ((Type classType, (Type configType, bool isRoot)) in _typeConfigs)
        {
            string directory = Path.Combine(options.ModPath, modId, classType.Name[1..]);
            if (!Directory.Exists(directory)) continue;

            foreach (string path in Directory.EnumerateFiles(directory))
            {
                string extension = Path.GetExtension(path);
                if (!IsSupportedExtension(extension)) continue;

                string itemId = Path.GetFileNameWithoutExtension(path);
                if (_loadedConfigs.TryGetValue(classType, out ConcurrentDictionary<string, object>? table)
                    && table.ContainsKey(itemId)) continue;

                Load(classType, configType, itemId, isRoot);
            }
        }
    }

    /// <inheritdoc/>
    public bool Add(Type classType, (Type configType, bool isRoot) config)
    {
        ArgumentNullException.ThrowIfNull(classType);

        if (!_typeConfigs.TryAdd(classType, config))
            return false;
        if (!_loadedConfigs.TryAdd(classType, new ConcurrentDictionary<string, object>()))
            return false;
        return true;
    }

    /// <inheritdoc/>
    public bool Unload(Type classType, string itemId)
    {
        ArgumentNullException.ThrowIfNull(classType);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        string className = classType.Name[1..];

        if (!_loadedConfigs.TryGetValue(classType, out ConcurrentDictionary<string, object>? table)
            || !table.TryRemove(itemId, out _))
        {
            LogConfigNotLoaded(logger, itemId, className);
            return false;
        }

        if (_loadedPaths.TryGetValue(classType, out ConcurrentDictionary<string, string>? paths)
            && paths.TryRemove(itemId, out string? path)
            && !IsPathReferenced(path))
        {
            _configSha256s.TryRemove(path, out _);
        }

        LogConfigUnloaded(logger, itemId, className);
        return true;
    }

    /// <inheritdoc/>
    public bool Unload<TClass>(string itemId)
        => Unload(typeof(TClass), itemId);

    /// <inheritdoc/>
    public bool Unload(Type classType)
    {
        ArgumentNullException.ThrowIfNull(classType);

        string className = classType.Name[1..];

        if (!_loadedConfigs.TryGetValue(classType, out ConcurrentDictionary<string, object>? table))
        {
            LogUnloadUnknownGroup(logger, className);
            return false;
        }

        int count = table.Count;

        // 替换为空表，保留分组注册，后续可再次 Load
        _loadedConfigs[classType] = new ConcurrentDictionary<string, object>();

        if (_loadedPaths.TryRemove(classType, out ConcurrentDictionary<string, string>? paths))
        {
            foreach (string path in paths.Values)
            {
                if (!IsPathReferenced(path))
                    _configSha256s.TryRemove(path, out _);
            }
        }

        LogConfigGroupUnloaded(logger, className, count);
        return true;
    }

    /// <inheritdoc/>
    public bool Unload<TClass>()
        => Unload(typeof(TClass));

    /// <inheritdoc/>
    public void UnloadAll()
    {
        int count = 0;
        foreach (Type classType in _typeConfigs.Keys)
        {
            if (_loadedConfigs.TryGetValue(classType, out ConcurrentDictionary<string, object>? table))
            {
                count += table.Count;
                _loadedConfigs[classType] = new ConcurrentDictionary<string, object>();
            }
        }

        _loadedPaths.Clear();
        _configSha256s.Clear();
        LogAllConfigsUnloaded(logger, count);
    }

    /// <summary>
    /// 判断某个路径是否仍被其他已加载配置项引用
    /// </summary>
    /// <remarks>
    /// 根目录配置（<c>isRoot = true</c>）下，不同分组可能共享同一文件路径。
    /// 卸载时不能直接删除哈希，需确认无其他引用
    /// </remarks>
    private bool IsPathReferenced(string path)
    {
        foreach (ConcurrentDictionary<string, string> paths in _loadedPaths.Values)
        {
            foreach (string candidate in paths.Values)
            {
                if (string.Equals(candidate, path, StringComparison.Ordinal))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 按反序列化器支持的扩展名查找配置文件
    /// </summary>
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