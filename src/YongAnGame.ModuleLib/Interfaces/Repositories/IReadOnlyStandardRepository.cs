using System.Diagnostics.CodeAnalysis;

namespace YongAnGame.ModuleLib.Interfaces.Repositories;

/// <summary>
/// 只读标准仓储，键为 <typeparamref name="TKey"/>，数据为 <typeparamref name="TData"/>
/// </summary>
/// <remarks>
/// 提供按键的读取入口，并支持以 <typeparamref name="TData"/> 序列进行枚举
/// 可写契约由 <see cref="IStandardRepository{TKey, TData}"/> 扩展
/// <para>
/// 线程安全：所有成员均可并发调用
/// 实现应保证查询与枚举期间不因其他线程的修改而抛异常
/// </para>
/// <para>
/// 枚举语义：<see cref="IEnumerable{TData}"/> 的枚举结果是调用瞬间的快照，
/// 枚举开始后其他线程的增删不会反映在本次枚举中
/// </para>
/// </remarks>
/// <typeparam name="TKey">键类型</typeparam>
/// <typeparam name="TData">数据类型，必须实现 <see cref="IHasId"/></typeparam>
public interface IReadOnlyStandardRepository<TKey, TData>
    : IEnumerable<TData>
    where TData : IHasId
{
    /// <summary>
    /// 获取指定键对应的数据
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <returns>键存在时为对应数据，否则为 <see langword="null"/></returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// <para>
    /// 只需判断存在性时用 <see cref="ContainsKey"/>，
    /// 需要同时拿到数据时用 <see cref="TryGetValue"/>，
    /// 避免 <see cref="ContainsKey"/> + <see cref="GetValue"/> 的两次查找
    /// </para>
    /// </remarks>
    TData? GetValue(TKey key);

    /// <summary>
    /// 尝试获取指定键对应的数据
    /// </summary>
    /// <param name="key">要查找的键</param>
    /// <param name="data">键存在时为对应数据，否则为 <see langword="default"/></param>
    /// <returns>键存在返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// </remarks>
    bool TryGetValue(TKey key, [NotNullWhen(true)] out TData? data);


    /// <summary>
    /// 判断仓储中是否包含指定键
    /// </summary>
    /// <param name="key">要检查的键</param>
    /// <returns>键存在返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// <para>
    /// 若随后还要读取数据，应直接用 <see cref="TryGetValue"/>，
    /// 避免检查与读取之间的状态变化以及重复查找
    /// </para>
    /// </remarks>
    bool ContainsKey(TKey key);
}