namespace YongAnGame.ModuleLib.Interfaces.Repositories;

/// <summary>
/// 可写的标准仓储，键为 <typeparamref name="TKey"/>，数据为 <typeparamref name="TData"/>
/// </summary>
/// <remarks>
/// 键由 <typeparamref name="TData"/> 自身提供，
/// 因此 <typeparamref name="TKey"/> 与 <typeparamref name="TData"/> 的 Id 类型应保持一致
/// 读取侧契约由 <see cref="IReadOnlyStandardRepository{TKey, TData}"/> 提供
/// <para>
/// 线程安全：所有成员均可并发调用
/// 实现应保证并发 <see cref="Add"/> / <see cref="TryAdd"/> 同一键时只有一个线程成功
/// </para>
/// </remarks>
/// <typeparam name="TKey">键类型</typeparam>
/// <typeparam name="TData">数据类型，必须实现 <see cref="IHasId"/></typeparam>
public interface IStandardRepository<TKey, TData>
    : IReadOnlyStandardRepository<TKey, TData>
    where TData : IHasId
{
    /// <summary>
    /// 添加一项数据，键已存在时抛异常
    /// </summary>
    /// <param name="data">要添加的数据</param>
    /// <remarks>
    /// 需要明确知道键是否已存在而不抛异常时，使用 <see cref="TryAdd"/>
    /// <para>线程安全：可并发调用</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> 为 <see langword="null"/></exception>
    /// <exception cref="ArgumentException">相同键的项已存在</exception>
    void Add(TData data);

    /// <summary>
    /// 尝试添加一项数据
    /// </summary>
    /// <param name="data">要添加的数据</param>
    /// <returns>添加成功返回 <see langword="true"/>，键已存在返回 <see langword="false"/></returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// <para>
    /// 并发添加同一键时只有一个线程返回 <see langword="true"/>，其余返回 <see langword="false"/>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> 为 <see langword="null"/></exception>
    bool TryAdd(TData data);
}