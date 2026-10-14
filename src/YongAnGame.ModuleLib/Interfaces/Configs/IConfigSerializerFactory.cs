namespace YongAnGame.ModuleLib.Interfaces.Configs
{
    /// <summary>
    /// 序列化器工厂，按文件扩展名自动选择合适的 <see cref="IConfigSerializer"/>
    /// </summary>
    /// <remarks>
    /// 调用方只需面向本接口，无需关心具体实现了哪些格式
    /// <para>
    /// 线程安全：所有成员均可并发调用
    /// 实现采用不可变快照替换，读路径无锁，注册路径加锁
    /// </para>
    /// </remarks>
    public interface IConfigSerializerFactory
    {
        /// <summary>
        /// 注册一个序列化器，后注册的优先于先注册的（对相同扩展名）
        /// </summary>
        /// <param name="serializer">要注册的序列化器</param>
        /// <returns>工厂本身，支持链式调用</returns>
        /// <remarks>
        /// 线程安全：可并发调用，注册期间查询会看到旧快照，注册后立即可见
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="serializer"/> 为 null</exception>
        IConfigSerializerFactory Register(IConfigSerializer serializer);

        /// <summary>
        /// 按文件扩展名选择序列化器
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>匹配的序列化器，没有匹配时返回兜底</returns>
        /// <remarks>
        /// 扩展名比较不区分大小写
        /// <para>线程安全：可并发调用</para>
        /// </remarks>
        IConfigSerializer ForFile(string filePath);

        /// <summary>
        /// 所有已注册的序列化器支持的扩展名
        /// </summary>
        /// <returns>扩展名列表（去重，顺序不保证）</returns>
        /// <remarks>
        /// 线程安全：可并发调用
        /// </remarks>
        IReadOnlyList<string> AllExtensions();
    }
}