using System.Security.Cryptography;
using System.Text;

namespace YongAnGame.ModuleLib.Extensions;

/// <summary>
/// 提供字符串哈希相关的扩展方法
/// </summary>
public static class HashExtensions
{
    extension(string input)
    {
        /// <summary>
        /// 计算字符串的 SHA-256 哈希值，并以大写十六进制字符串返回
        /// </summary>
        /// <param name="encoding">
        /// 将字符串转换为字节序列时使用的编码；
        /// 为 <see langword="null"/> 时使用 <see cref="Encoding.UTF8"/>
        /// </param>
        /// <returns>长度为 64 的大写十六进制字符串</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="input"/> 为 <see langword="null"/>
        /// </exception>
        /// <remarks>
        /// 等价于 <c>Convert.ToHexString(SHA256.HashData((encoding ?? Encoding.UTF8).GetBytes(input)))</c>。
        /// 同一字符串在不同编码下结果不同，跨进程/跨端比对时需保证编码一致
        /// </remarks>
        public string Sha256Text(Encoding? encoding = null)
        {
            ArgumentNullException.ThrowIfNull(input);

            return Convert.ToHexString(Sha256Bytes(input, encoding));
        }
    }

    /// <summary>
    /// 计算字符串的 SHA-256 哈希值
    /// </summary>
    /// <param name="input">待计算哈希的字符串，调用方保证非 <see langword="null"/></param>
    /// <param name="encoding">编码方式，为 <see langword="null"/> 时使用 <see cref="Encoding.UTF8"/></param>
    /// <returns>长度为 32 的哈希字节数组</returns>
    private static byte[] Sha256Bytes(string input, Encoding? encoding)
        => SHA256.HashData((encoding ?? Encoding.UTF8).GetBytes(input));
}
