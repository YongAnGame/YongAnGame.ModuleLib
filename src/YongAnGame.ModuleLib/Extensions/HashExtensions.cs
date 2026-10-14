using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace YongAnGame.ModuleLib.Extensions;

[SuppressMessage(
    "Design",
    "CA1034:嵌套类型应不可见",
    Justification = "extension 块会生成编译器合成的嵌套类型，此处不可避免")]
public static class HashExtensions
{
    extension(string input)
    {
        /// <summary>
        /// 计算字符串的 SHA256 哈希值，返回大写十六进制字符串
        /// </summary>
        /// <param name="encoding">编码方式，默认 <see cref="Encoding.UTF8"/></param>
        /// <returns>大写十六进制哈希值</returns>
        public string Sha256Text(Encoding? encoding = null)
        {
            ArgumentNullException.ThrowIfNull(input);

            encoding ??= Encoding.UTF8;
            byte[] data = encoding.GetBytes(input);
            byte[] hash = SHA256.HashData(data);
            return Convert.ToHexString(hash);
        }
    }
}