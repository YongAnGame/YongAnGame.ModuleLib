using YongAnGame.ModuleLib.Interfaces.Configs;

namespace YongAnGame.ModuleLib.Features.Serializers
{
    /// <summary>
    /// 序列化器工厂的默认实现，按文件扩展名自动选择合适的实现
    /// </summary>
    /// <remarks>
    /// 用法：
    /// <code>
    /// IConfigSerializerFactory factory = new ConfigSerializerFactory().WithYaml();
    /// var serializer = factory.ForFile("data/operators/char_001.yaml");
    /// // → YamlConfigSerializer
    /// </code>
    /// <para>
    /// 线程安全：采用不可变快照替换
    /// 注册走 <c>lock</c> 重建数组并原子替换，查询走 <c>volatile</c> 读快照
    /// 读写可并发，注册期间查询会看到旧快照，注册后立即可见
    /// </para>
    /// </remarks>
    public sealed class ConfigSerializerFactory : IConfigSerializerFactory
    {
        // 快照按优先级从高到低排列——Register 把新注册的放最前
        // volatile 保证注册线程写入后，查询线程能立即看到新数组
        private volatile IConfigSerializer[] _snapshot;

        // 只保护“重建 + 替换”这段临界区，查询路径不加锁
#if NET10_0_OR_GREATER
        private readonly Lock _registerLock = new();
#else
        private readonly object _registerLock = new();
#endif

        // 扩展名不匹配任何已注册实现时使用，保证 ForFile 永不返回 null
        private readonly IConfigSerializer _fallback;

        /// <summary>
        /// 构造一个工厂
        /// </summary>
        /// <param name="fallback">
        /// 兜底序列化器，扩展名不匹配任何实现时用它
        /// 为 null 时用 <see cref="JsonConfigSerializer"/>
        /// </param>
        public ConfigSerializerFactory(IConfigSerializer? fallback = null)
        {
            _fallback = fallback ?? new JsonConfigSerializer();
            _snapshot = [_fallback];
        }

        /// <inheritdoc />
        public IConfigSerializerFactory Register(IConfigSerializer serializer)
        {
            ArgumentNullException.ThrowIfNull(serializer);

            lock (_registerLock)
            {
                // 先构建新数组到局部变量，成功后再一次性替换快照
                // 旧快照在替换前始终对查询线程可用，不会读到半成品
                var current = _snapshot;
                var next = new IConfigSerializer[current.Length + 1];
                next[0] = serializer;
                Array.Copy(current, 0, next, 1, current.Length);
                _snapshot = next;
            }

            return this;
        }

        /// <inheritdoc />
        public IConfigSerializer ForFile(string filePath)
        {
            var ext = Path.GetExtension(filePath);

            // 一次性读取快照到本地——保证遍历过程中看到的是同一个数组
            var snapshot = _snapshot;
            foreach (var s in snapshot)
            {
                foreach (var e in s.Extensions)
                {
                    if (string.Equals(e, ext, StringComparison.OrdinalIgnoreCase))
                    {
                        return s;
                    }
                }
            }

            return _fallback;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> AllExtensions()
        {
            var snapshot = _snapshot;
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in snapshot)
            {
                foreach (var e in s.Extensions) set.Add(e);
            }
            return [.. set];
        }
    }
}