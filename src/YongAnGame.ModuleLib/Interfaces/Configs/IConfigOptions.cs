namespace YongAnGame.ModuleLib.Interfaces.Configs
{
    /// <summary>
    /// 配置系统的基础选项
    /// </summary>
    /// <remarks>
    /// 描述配置文件的根目录位置，供 <see cref="IConfigRepository"/> 定位文件
    /// <para>
    /// 实现要求：线程安全——属性值在实例生命周期内不应改变，
    /// 或保证并发读写安全
    /// </para>
    /// </remarks>
    public interface IConfigOptions
    {
        /// <summary>
        /// MOD 根目录路径
        /// </summary>
        /// <value>
        /// 一个目录路径，各 MOD 的子目录位于其下，
        /// 形如 <c>{ModPath}/{modId}/{分组名}/{itemId}.{ext}</c>
        /// </value>
        /// <remarks>
        /// 线程安全：可并发读取
        /// <para>
        /// 约定：
        /// <list type="bullet">
        ///   <item>返回非 null 非空字符串</item>
        ///   <item>不含末尾目录分隔符</item>
        ///   <item>绝对路径或相对于当前工作目录的路径</item>
        ///   <item>目录本身可以不存在，由调用方在使用时判断</item>
        /// </list>
        /// </para>
        /// </remarks>
        string ModPath { get; }
    }
}