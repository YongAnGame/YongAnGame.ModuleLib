using YongAnGame.ModuleLib.Interfaces.Configs;

namespace YongAnGame.ModuleLib.Features.Configs;

/// <summary>
/// <see cref="IConfigOptions"/> 的默认实现
/// </summary>
/// <remarks>
/// 不可变值对象，构造后所有属性不再改变
/// 因此可安全地被多线程并发读取
/// </remarks>
public sealed class ConfigOptions : IConfigOptions
{
    /// <inheritdoc />
    public string ModPath { get; }

    /// <summary>
    /// 使用 MOD 根目录路径构造
    /// </summary>
    /// <param name="modPath">
    /// MOD 根目录路径
    /// 不能为 null、空或仅含空白字符
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="modPath"/> 为 <see langword="null"/>、空或仅含空白字符
    /// </exception>
    public ConfigOptions(string modPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modPath);
        ModPath = modPath;
    }
}