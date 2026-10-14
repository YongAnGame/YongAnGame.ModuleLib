using YongAnGame.ModuleLib.Exceptions;

namespace YongAnGame.ModuleLib.Interfaces.Configs
{
    /// <summary>
    /// 数据序列化器，读 / 写各种格式（JSON、YAML ……）
    /// </summary>
    /// <remarks>
    /// 所有需要读配置的地方（加载器、工具、测试）都依赖这个接口 ——
    /// 不依赖具体的 JSON 实现。换格式只换实现，调用方不动
    ///
    /// 实现要求：
    /// <list type="bullet">
    ///   <item>线程安全——所有成员均可并发调用</item>
    ///   <item>抛出 <see cref="SerializationException"/> 而不是底层异常</item>
    ///   <item>格式错误时给出包含文件位置的信息</item>
    /// </list>
    /// </remarks>
    public interface IConfigSerializer
    {
        /// <summary>
        /// 这个序列化器支持的文件扩展名（含点）
        /// </summary>
        /// <remarks>
        /// 例：<c>[".json"]</c> 或 <c>[".yaml", ".yml"]</c>
        /// 集合内容由实现固定，调用方不应依赖修改它来改变行为
        /// <para>线程安全：可并发读取</para>
        /// </remarks>
        IReadOnlyList<string> Extensions { get; }

        /// <summary>
        /// 添加一个类型转换器
        /// </summary>
        /// <param name="converter">要添加的类型转换器实例</param>
        /// <remarks>
        /// 类型转换器用于扩展或覆盖默认的序列化 / 反序列化行为
        /// 具体接受的类型由实现决定
        /// <para>线程安全：可并发调用，实现内部需保证列表与重建过程的互斥</para>
        /// </remarks>
        /// <exception cref="SerializationException">
        /// <paramref name="converter"/> 不是实现所支持的类型
        /// </exception>
        void AddTypeConverter(object converter);

        /// <summary>
        /// 从字符串反序列化
        /// </summary>
        /// <typeparam name="T">目标类型。必须是引用类型</typeparam>
        /// <param name="text">文本内容</param>
        /// <returns>反序列化后的对象。空文档返回 null</returns>
        /// <remarks>
        /// 线程安全：可并发调用，实现内部读路径不应加锁
        /// </remarks>
        /// <exception cref="SerializationException">格式错误或类型不匹配</exception>
        T? Deserialize<T>(string text) where T : class;

        /// <summary>
        /// 从字符串反序列化
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="type">目标类型</param>
        /// <returns>反序列化后的对象。空文档返回 null</returns>
        /// <remarks>
        /// 运行时类型版本，供只持有 <see cref="Type"/> 的调用方使用
        /// <para>线程安全：可并发调用</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> 为 null</exception>
        /// <exception cref="SerializationException">格式错误或类型不匹配</exception>
        object? Deserialize(string text, Type type);

        /// <summary>
        /// 序列化成字符串
        /// </summary>
        /// <typeparam name="T">源类型。必须是引用类型</typeparam>
        /// <param name="value">要序列化的对象</param>
        /// <returns>文本内容</returns>
        /// <remarks>
        /// 线程安全：可并发调用，实现内部读路径不应加锁
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 null</exception>
        /// <exception cref="SerializationException">序列化失败</exception>
        string Serialize<T>(T value) where T : class;

        /// <summary>
        /// 从文件反序列化
        /// </summary>
        /// <typeparam name="T">目标类型。必须是引用类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns>反序列化后的对象。空文件返回 null</returns>
        /// <remarks>
        /// 线程安全：可并发调用，但并发读同一文件时由文件系统保证一致性
        /// </remarks>
        /// <exception cref="FileNotFoundException">文件不存在</exception>
        /// <exception cref="SerializationException">读取失败或格式错误</exception>
        T? DeserializeFromFile<T>(string filePath) where T : class;

        /// <summary>
        /// 序列化到文件
        /// </summary>
        /// <typeparam name="T">源类型。必须是引用类型</typeparam>
        /// <param name="value">要序列化的对象</param>
        /// <param name="filePath">文件路径</param>
        /// <remarks>
        /// 目标目录不存在时由实现负责创建
        /// <para>线程安全：本方法自身可并发调用，但并发写同一文件时最终内容取决于写入顺序</para>
        /// </remarks>
        /// <exception cref="SerializationException">序列化失败或写入失败</exception>
        void SerializeToFile<T>(T value, string filePath) where T : class;
    }
}