using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YongAnGame.ModuleLib.Exceptions;
using YongAnGame.ModuleLib.Interfaces.Configs;

namespace YongAnGame.ModuleLib.Features.Serializers
{
    /// <summary>
    /// 基于 <see cref="YamlDotNet"/> 的序列化器
    /// </summary>
    /// <remarks>
    /// 自定义类型的转换通过
    /// <see cref="IYamlTypeConverter"/> 注册
    /// <para>与 JSON 版本的差异：</para>
    /// <list type="bullet">
    ///   <item>支持注释（YAML 原生特性）</item>
    ///   <item>字符串不需要转义引号（更易手写）</item>
    ///   <item>缩进敏感——多行字符串用 <c>|</c> 或 <c>&gt;</c></item>
    /// </list>
    /// </remarks>
    public sealed class YamlConfigSerializer : IConfigSerializer
    {
        // Rebuild 的唯一数据来源——YamlDotNet 的 ISerializer/IDeserializer
        // 构建后不可变，新增转换器只能靠这份列表整体重建
        private readonly List<IYamlTypeConverter> _converters = [];

        // 只保护 AddTypeConverter 的“加列表 + 重建 + 回写”这段临界区，
        // 读路径（Serialize/Deserialize）不加锁，靠 volatile 读最新实例
#if NET10_0_OR_GREATER
        private readonly Lock _rebuildLock = new();
#else
        private readonly object _rebuildLock = new();
#endif

        // YamlDotNet 的实例构建后不可变，AddTypeConverter 时整体重建并替换
        // volatile 保证读线程能看到最新引用
        [AllowNull]

        private volatile ISerializer _serializer;

        [AllowNull]
        private volatile IDeserializer _deserializer;

        /// <summary>
        /// 初始化序列化器，先构建一份无转换器的默认实例
        /// </summary>
        public YamlConfigSerializer() => Rebuild([]);

        /// <inheritdoc />
        public IReadOnlyList<string> Extensions { get; } = [".yaml", ".yml"];

        /// <inheritdoc />
        public T? Deserialize<T>(string text) where T : class
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                return _deserializer.Deserialize<T>(text);
            }
            catch (YamlException ex)
            {
                throw new SerializationException(
                    $"YAML 反序列化失败（行 {ex.Start.Line}, 列 {ex.Start.Column}）: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public object? Deserialize(string text, Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                return _deserializer.Deserialize(text, type);
            }
            catch (YamlException ex)
            {
                throw new SerializationException($"YAML 反序列化失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public string Serialize<T>(T value) where T : class
        {
            ArgumentNullException.ThrowIfNull(value);

            try
            {
                return _serializer.Serialize(value);
            }
            catch (YamlException ex)
            {
                throw new SerializationException($"YAML 序列化失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public T? DeserializeFromFile<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"文件不存在: {filePath}", filePath);

            string text;
            try
            {
                text = File.ReadAllText(filePath);
            }
            catch (IOException ex)
            {
                throw new SerializationException($"读取文件失败: {filePath}", ex);
            }

            try
            {
                return Deserialize<T>(text);
            }
            catch (SerializationException ex)
            {
                // 从字符串反序列化时不知道文件路径，这里补上，方便定位
                throw new SerializationException($"{filePath}: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public void SerializeToFile<T>(T value, string filePath) where T : class
        {
            var text = Serialize(value);

            try
            {
                var dir = Path.GetDirectoryName(filePath);
                // 目标目录可能不存在，先创建，避免 File.WriteAllText 抛 DirectoryNotFoundException
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(filePath, text);
            }
            catch (IOException ex)
            {
                throw new SerializationException($"写入文件失败: {filePath}", ex);
            }
        }

        /// <summary>
        /// 添加一个 YAML 类型转换器，并立即重建序列化器与反序列化器
        /// </summary>
        /// <remarks>
        /// 本方法线程安全，可并发调用
        /// 传入对象必须实现 <see cref="IYamlTypeConverter"/>，否则抛 <see cref="SerializationException"/>
        /// 构建失败时不改变现有实例，调用方可安全重试
        /// </remarks>
        /// <exception cref="SerializationException">转换器类型不匹配，或重建失败</exception>
        public void AddTypeConverter(object converter)
        {
            if (converter is not IYamlTypeConverter yamlConverter)
            {
                throw new SerializationException(
                    $"不支持的转换器类型：{converter?.GetType().FullName ?? "null"}，" +
                    $"期望类型：{typeof(IYamlTypeConverter).FullName}");
            }

            lock (_rebuildLock)
            {
                // 先构建成功，再落列表，避免构建失败导致状态不一致
                var nextConverters = new List<IYamlTypeConverter>(_converters) { yamlConverter };
                Rebuild(nextConverters);

                _converters.Clear();
                _converters.AddRange(nextConverters);
            }
        }

        /// <summary>
        /// 用指定的转换器列表重建序列化器与反序列化器
        /// </summary>
        /// <remarks>
        /// 先构建到局部变量，全部成功后再一次性替换字段，避免读到半成品
        /// 构建失败时字段保持不变，调用方状态不受影响
        /// </remarks>
        private void Rebuild(IReadOnlyList<IYamlTypeConverter> converters)
        {
            DeserializerBuilder deserializerBuilder = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties();

            SerializerBuilder serializerBuilder = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull);

            foreach (IYamlTypeConverter converter in converters)
            {
                deserializerBuilder.WithTypeConverter(converter);
                serializerBuilder.WithTypeConverter(converter);
            }

            // 两个都构建成功后再赋值
            IDeserializer deserializer = deserializerBuilder.Build();
            ISerializer serializer = serializerBuilder.Build();

            _deserializer = deserializer;
            _serializer = serializer;
        }
    }
}