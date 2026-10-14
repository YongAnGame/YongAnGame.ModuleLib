using System.Diagnostics.CodeAnalysis;
using YongAnGame.ModuleLib.Features.Serializers;
using YongAnGame.ModuleLib.Interfaces.Configs;

namespace YongAnGame.ModuleLib.Compatibility
{
    /// <summary>
    /// <see cref="IConfigSerializerFactory"/> 的便捷扩展方法
    /// </summary>
    /// <remarks>
    /// 提供针对具体序列化器实现的链式注册入口，
    /// 让调用方无需直接引用 <c>Features.Serializers</c> 命名空间
    /// <para>
    /// 线程安全：所有方法均可并发调用，
    /// 内部转发的 <see cref="IConfigSerializerFactory.Register"/> 已保证线程安全
    /// </para>
    /// </remarks>
    [SuppressMessage(
        "Design",
        "CA1034:嵌套类型应不可见",
        Justification = "extension 块会生成编译器合成的嵌套类型，此处不可避免")]
    public static class ConfigSerializerFactoryExtensions
    {
        extension(IConfigSerializerFactory factory)
        {
            /// <summary>
            /// 注册一个 YAML 序列化器
            /// </summary>
            /// <returns>工厂本身，支持链式调用</returns>
            /// <remarks>
            /// 需要项目中引用 YamlDotNet 包
            /// <para>线程安全：可并发调用</para>
            /// </remarks>
            public IConfigSerializerFactory WithYaml()
            {
                return factory.Register(new YamlConfigSerializer());
            }

            /// <summary>
            /// 注册一个 JSON 序列化器
            /// </summary>
            /// <returns>工厂本身，支持链式调用</returns>
            /// <remarks>
            /// 工厂构造时已默认添加一个 JSON 序列化器作为兜底，
            /// 调用本方法会再注册一个**新的**实例并优先命中，
            /// 使默认兜底实例不再参与扩展名匹配
            /// <para>
            /// 若只是想让 JSON 作为兜底，构造工厂时不需要调用本方法
            /// </para>
            /// <para>线程安全：可并发调用</para>
            /// </remarks>
            public IConfigSerializerFactory WithJson()
            {
                return factory.Register(new JsonConfigSerializer());
            }
        }
    }
}