namespace YongAnGame.ModuleLib.Exceptions
{
    /// <summary>
    /// 序列化 / 反序列化失败
    /// </summary>
    /// <remarks>
    /// 所有实现都应该把底层异常（JsonException、YamlException ……）
    /// 包装成这个异常，让调用方只需要 catch 一种
    /// <para>
    /// 涉及文件位置时，应将路径、行号、列号等信息包含在消息中
    /// </para>
    /// </remarks>
    public sealed class SerializationException : Exception
    {
        /// <summary>
        /// 构造一个序列化异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public SerializationException(string message) : base(message) { }

        /// <summary>
        /// 构造一个序列化异常（带内层异常）
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="inner">底层异常</param>
        public SerializationException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// 构造一个序列化异常（无消息）
        /// </summary>
        public SerializationException() { }
    }
}