namespace YongAnGame.ModuleLib.Interfaces
{
    /// <summary>
    /// 表示一个具有字符串标识符的数据对象
    /// </summary>
    /// <remarks>
    /// 实现类型应保证 <see cref="StringId"/> 在其生命周期内不变，
    /// 因为该值通常用作仓储的键
    /// <para>
    /// 唯一性范围：同一分组类型内唯一即可，不同分组之间可以重名
    /// </para>
    /// <para>
    /// 线程安全：<see cref="StringId"/> 可并发读取
    /// 实现应保证返回的是不可变字符串，且不随对象状态改变
    /// </para>
    /// </remarks>
    public interface IHasId
    {
        /// <summary>
        /// 获取该数据的字符串标识符
        /// </summary>
        /// <value>
        /// 用作仓储的键，同一分组类型内需唯一且稳定
        /// </value>
        /// <remarks>
        /// 线程安全：可并发读取
        /// </remarks>
        string StringId { get; }
    }
}