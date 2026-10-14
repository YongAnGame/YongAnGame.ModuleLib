using System.Diagnostics.CodeAnalysis;
using YongAnGame.ModuleLib.Components;

namespace YongAnGame.ModuleLib.Interfaces.Configs;

/// <summary>
/// 按类型分组的配置仓储，从 MOD 目录读取并缓存配置
/// </summary>
/// <remarks>
/// 配置格式由 <see cref="Serializer"/> 决定，具体文件扩展名由该序列化器声明
/// 同一配置项只加载一次，重复加载会被跳过
/// <para>
/// 线程安全：所有成员均可并发调用
/// 实现内部使用 <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/>
/// 保存状态，读路径无锁，写路径依赖原子操作
/// </para>
/// <para>
/// 典型生命周期：启动阶段并发调用 <see cref="Add"/> 与 <see cref="Load()"/>，
/// 运行阶段只调用 <see cref="GetValue{TType, TConfig}"/>、
/// <see cref="TryGetValue{TType, TConfig}"/> 等查询方法
/// </para>
/// </remarks>
public interface IConfigRepository
{
    /// <summary>
    /// 当前使用的配置序列化器
    /// </summary>
    /// <remarks>
    /// 线程安全：可并发读取
    /// </remarks>
    IConfigSerializer Serializer { get; }

    /// <summary>
    /// 所有已加载配置文件内容的整体哈希
    /// </summary>
    /// <remarks>
    /// 将所有文件的 SHA256 排序后拼接再取哈希，与文件加载顺序无关
    /// <para>
    /// 并发加载期间读取时，反映的是“调用瞬间”已加载的文件集合，
    /// 不保证对应某一完整时刻；加载完成后读取即为稳定值
    /// </para>
    /// </remarks>
    string Sha256 { get; }

    /// <summary>
    /// 注册一个配置分组及其读取方式
    /// </summary>
    /// <param name="classType">配置分组类型</param>
    /// <param name="config">反序列化目标类型与是否为根目录模式</param>
    /// <returns>注册成功返回 <see langword="true"/>，分组已存在返回 <see langword="false"/></returns>
    /// <remarks>
    /// 线程安全：可并发调用，重复注册同一分组时只有一个线程成功
    /// <para>
    /// 注册仅声明分组的存在和读取方式，不触发实际加载
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="classType"/> 为 null</exception>
    bool Add(Type classType, (Type configType, bool isRoot) config);

    /// <summary>
    /// 获取指定配置分组下所有已加载项的 Id
    /// </summary>
    /// <typeparam name="T">配置分组类型</typeparam>
    /// <returns>Id 序列；分组未注册或无已加载项时返回空序列</returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// <para>
    /// 返回的是调用瞬间的快照视图，之后的加载不会反映在本次结果中
    /// </para>
    /// </remarks>
    IEnumerable<Id> GetAllId<T>();

    /// <summary>
    /// 获取所有已注册的配置分组类型
    /// </summary>
    /// <returns>分组类型序列；无注册时返回空序列</returns>
    /// <remarks>
    /// 线程安全：可并发调用，返回调用瞬间的快照视图
    /// </remarks>
    IEnumerable<Type> GetAllConfigType();

    /// <summary>
    /// 获取指定配置分组下、指定 Id 的配置
    /// </summary>
    /// <typeparam name="TType">配置分组类型</typeparam>
    /// <typeparam name="TConfig">配置类型</typeparam>
    /// <param name="itemId">配置项的 Id</param>
    /// <returns>找到时返回配置对象，否则返回 <see langword="null"/></returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// </remarks>
    TConfig? GetValue<TType, TConfig>(string itemId);

    /// <summary>
    /// 尝试获取指定配置分组下、指定 Id 的配置
    /// </summary>
    /// <typeparam name="TType">配置分组类型</typeparam>
    /// <typeparam name="TConfig">配置类型</typeparam>
    /// <param name="itemId">配置项的 Id</param>
    /// <param name="obj">找到时为配置对象，否则为默认值</param>
    /// <returns>找到返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// <para>
    /// 仅当缓存中存在该 Id 且其实际类型可赋给 <typeparamref name="TConfig"/> 时返回 true
    /// </para>
    /// </remarks>
    bool TryGetValue<TType, TConfig>(string itemId, [NotNullWhen(true)] out TConfig? obj);

    /// <summary>
    /// 扫描所有已注册分组对应的目录并加载其中所有配置
    /// </summary>
    /// <remarks>
    /// 分组目录不存在时静默跳过
    /// 仅加载扩展名与 <see cref="Serializer"/> 匹配的文件
    /// <para>
    /// 线程安全：可并发调用
    /// 遍历分组时看到的是调用瞬间的快照，期间新增的分组可能不被本次加载
    /// </para>
    /// </remarks>
    void Load();

    /// <summary>
    /// 加载单个配置文件
    /// </summary>
    /// <param name="classType">配置分组类型</param>
    /// <param name="configType">反序列化目标类型</param>
    /// <param name="itemId">配置项的 Id</param>
    /// <param name="isRoot">为 <see langword="true"/> 时从 MOD 根目录查找，否则从分组子目录查找</param>
    /// <returns>加载成功返回 <see langword="true"/>，文件缺失或反序列化失败返回 <see langword="false"/></returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// <para>
    /// 并发加载同一项时只有一个线程成功，其余返回 false 并记录 Warning
    /// </para>
    /// </remarks>
    bool Load(Type classType, Type configType, string itemId, bool isRoot = false);

    /// <summary>
    /// 泛型版本的 <see cref="Load(Type, Type, string, bool)"/>
    /// </summary>
    /// <typeparam name="TClass">配置分组类型</typeparam>
    /// <typeparam name="TConfig">反序列化目标类型</typeparam>
    /// <param name="itemId">配置项的 Id</param>
    /// <param name="isRoot">为 <see langword="true"/> 时从 MOD 根目录查找，否则从分组子目录查找</param>
    /// <returns>加载成功返回 <see langword="true"/>，文件缺失或反序列化失败返回 <see langword="false"/></returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// </remarks>
    bool Load<TClass, TConfig>(string itemId, bool isRoot = false);
}