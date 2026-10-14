using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using YongAnGame.ModuleLib.Exceptions;
using YongAnGame.ModuleLib.Interfaces.Configs;

namespace YongAnGame.ModuleLib.Features.Serializers
{
    /// <summary>
    /// 基于 <see cref="System.Text.Json"/> 的序列化器
    /// </summary>
    /// <remarks>
    /// 自定义类型的转换通过 <see cref="JsonConverter{T}"/> 注册
    /// <para>
    /// <see cref="JsonSerializerOptions"/> 首次使用后会被冻结，无法再修改，
    /// 因此新增转换器时整体重建一份新的 Options 并原子替换
    /// </para>
    /// </remarks>
    public sealed class JsonConfigSerializer : IConfigSerializer
    {
        // Rebuild 的唯一数据来源——JsonSerializerOptions 冻结后不可修改，
        // 新增转换器只能靠这份列表整体重建
        private readonly List<JsonConverter> _converters = [];

        // 只保护 AddTypeConverter 的“加列表 + 重建 + 回写”这段临界区，
        // 读路径（Serialize/Deserialize）不加锁，靠 volatile 读最新实例
        private readonly object _rebuildLock = new();

        // 每次重建后整体替换，volatile 保证读线程能看到最新引用
        [AllowNull]
        private volatile JsonSerializerOptions _options;

        /// <summary>
        /// 初始化序列化器，先构建一份无转换器的默认实例
        /// </summary>
        public JsonConfigSerializer() => Rebuild([]);

        /// <inheritdoc />
        public IReadOnlyList<string> Extensions { get; } = [".json"];

        /// <inheritdoc />
        public T? Deserialize<T>(string text) where T : class
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                return JsonSerializer.Deserialize<T>(text, _options);
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"JSON 反序列化失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public object? Deserialize(string text, Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                return JsonSerializer.Deserialize(text, type, _options);
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"JSON 反序列化失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public string Serialize<T>(T value) where T : class
        {
            ArgumentNullException.ThrowIfNull(value);

            try
            {
                return JsonSerializer.Serialize(value, _options);
            }
            catch (JsonException ex)
            {
                throw new SerializationException($"JSON 序列化失败: {ex.Message}", ex);
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
        /// 添加一个 JSON 类型转换器，并立即重建选项
        /// </summary>
        /// <remarks>
        /// 本方法线程安全，可并发调用
        /// 传入对象必须继承 <see cref="JsonConverter"/>，否则抛 <see cref="SerializationException"/>
        /// 重建失败时不改变现有实例，调用方可安全重试
        /// </remarks>
        /// <exception cref="SerializationException">转换器类型不匹配</exception>
        public void AddTypeConverter(object converter)
        {
            // JsonConverterFactory 继承自 JsonConverter，无需单独列出
            if (converter is not JsonConverter jsonConverter)
            {
                throw new SerializationException(
                    $"不支持的转换器类型：{converter?.GetType().FullName ?? "null"}，" +
                    $"期望类型：{typeof(JsonConverter).FullName}");
            }

            lock (_rebuildLock)
            {
                // 先构建成功，再落列表，避免构建失败导致状态不一致
                var nextConverters = new List<JsonConverter>(_converters) { jsonConverter };
                Rebuild(nextConverters);

                _converters.Clear();
                _converters.AddRange(nextConverters);
            }
        }

        /// <summary>
        /// 用指定的转换器列表重建选项
        /// </summary>
        /// <remarks>
        /// 先构建到局部变量，成功后再赋值，避免读到半成品
        /// 构建失败时字段保持不变，调用方状态不受影响
        /// </remarks>
        private void Rebuild(IReadOnlyList<JsonConverter> converters)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = false,
                WriteIndented = true,
            };

            foreach (var converter in converters)
            {
                options.Converters.Add(converter);
            }

            _options = options;
        }
    }
}