using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace YongAnGame.ModuleLib.Components;

/// <summary>
/// 表示形如 <c>ModId.ItemId:SubId</c> 的标识符
/// </summary>
/// <remarks>
/// 不可变值类型，可用作字典键
/// <list type="bullet">
///   <item>ModId 与 ItemId 之间用 <c>.</c> 分隔</item>
///   <item>ItemId 与 SubId 之间用 <c>:</c> 分隔，省略时按 0 处理</item>
///   <item>ModId 与 ItemId 均不得为空或仅含空白字符</item>
///   <item>SubId 为非负整数</item>
/// </list>
/// <para>
/// 默认值 <see langword="default"/> 表示未初始化的实例，
/// 其 <see cref="ModId"/> 与 <see cref="ItemId"/> 为 <see langword="null"/>，
/// 不应直接使用
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct Id
    : IEquatable<Id>
#if NET8_0_OR_GREATER
    , IParsable<Id>
#endif
{
    /// <summary>
    /// ModId 与 ItemId 之间的分隔符
    /// </summary>
    public const char Separator = '.';

    /// <summary>
    /// ItemId 与 SubId 之间的分隔符
    /// </summary>
    public const char SubIdSeparator = ':';

    /// <summary>
    /// 获取 ModId
    /// </summary>
    /// <value>非 null、非空、非仅空白字符的字符串</value>
    public string ModId { get; }

    /// <summary>
    /// 获取 ItemId
    /// </summary>
    /// <value>非 null、非空、非仅空白字符的字符串</value>
    public string ItemId { get; }

    /// <summary>
    /// 获取 SubId
    /// </summary>
    /// <value>非负整数，未指定时为 0</value>
    public int SubId { get; }

    /// <summary>
    /// 使用 ModId、ItemId 与 SubId 构造
    /// </summary>
    /// <param name="modId">ModId 部分，不能为 null、空或仅含空白字符</param>
    /// <param name="itemId">ItemId 部分，不能为 null、空或仅含空白字符</param>
    /// <param name="subId">SubId 部分，非负整数，默认为 0</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="modId"/> 或 <paramref name="itemId"/> 为
    /// <see langword="null"/>、空或仅含空白字符
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="subId"/> 为负数
    /// </exception>
    public Id(string modId, string itemId, int subId = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentOutOfRangeException.ThrowIfNegative(subId);

        ModId = modId;
        ItemId = itemId;
        SubId = subId;
    }

    /// <summary>
    /// 使用 <c>ModId.ItemId:SubId</c> 格式构造
    /// </summary>
    /// <param name="id">
    /// 形如 <c>ModId.ItemId</c> 或 <c>ModId.ItemId:SubId</c> 的字符串
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="id"/> 为 <see langword="null"/>
    /// </exception>
    /// <exception cref="FormatException">
    /// <paramref name="id"/> 不符合格式，或任一部分非法
    /// </exception>
    public Id(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (!TryParseCore(id, out string? modId, out string? itemId, out int subId))
        {
            throw new FormatException(
                $"无效的 Id 格式：'{id}'，应为 'ModId.ItemId' 或 'ModId.ItemId:SubId'");
        }

        ModId = modId;
        ItemId = itemId;
        SubId = subId;
    }

    /// <summary>
    /// 按 <c>ModId.ItemId:SubId</c> 格式拆分并校验
    /// </summary>
    /// <param name="s">待拆分的字符串，调用方保证非 null</param>
    /// <param name="modId">拆分出的 ModId</param>
    /// <param name="itemId">拆分出的 ItemId</param>
    /// <param name="subId">拆分出的 SubId，无 <c>:</c> 部分时为 0</param>
    /// <returns>拆分且校验通过返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 构造函数与 <see cref="TryParse"/> 共用，避免解析逻辑重复与行为分歧
    /// </remarks>
    private static bool TryParseCore(
        string s,
        [NotNullWhen(true)] out string? modId,
        [NotNullWhen(true)] out string? itemId,
        out int subId)
    {
        modId = null;
        itemId = null;
        subId = 0;

        int dot = s.IndexOf(Separator, StringComparison.Ordinal);
        if (dot <= 0 || dot >= s.Length - 1) return false;

        string left = s[..dot];
        string right = s[(dot + 1)..];

        if (string.IsNullOrWhiteSpace(left)) return false;

        int colon = right.IndexOf(SubIdSeparator, StringComparison.Ordinal);
        string itemPart;
        string subIdPart;

        if (colon < 0)
        {
            itemPart = right;
            subIdPart = "0";
        }
        else
        {
            if (colon == 0 || colon == right.Length - 1) return false;
            itemPart = right[..colon];
            subIdPart = right[(colon + 1)..];
        }

        if (string.IsNullOrWhiteSpace(itemPart)) return false;
        if (!int.TryParse(subIdPart, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
            return false;
        if (parsed < 0) return false;

        modId = left;
        itemId = itemPart;
        subId = parsed;
        return true;
    }

    #region 比较

    /// <summary>
    /// 判断两个 Id 的 ModId 是否相同
    /// </summary>
    /// <param name="other">要比较的 Id</param>
    /// <returns>ModId 相同返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 只比较 ModId，忽略 ItemId 与 SubId
    /// </remarks>
    public bool SameMod(Id other)
        => string.Equals(ModId, other.ModId, StringComparison.Ordinal);

    /// <summary>
    /// 判断两个 Id 的 ItemId 是否相同
    /// </summary>
    /// <param name="other">要比较的 Id</param>
    /// <returns>ItemId 相同返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 只比较 ItemId，忽略 ModId 与 SubId，
    /// 不同模组下的同名项目被视为相同
    /// </remarks>
    public bool SameItem(Id other)
        => string.Equals(ItemId, other.ItemId, StringComparison.Ordinal);

    /// <summary>
    /// 判断两个 Id 的 ModId 与 ItemId 是否都相同
    /// </summary>
    /// <param name="other">要比较的 Id</param>
    /// <returns>都相同返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 忽略 SubId
    /// </remarks>
    public bool SameModAndItem(Id other)
        => SameMod(other) && SameItem(other);

    /// <summary>
    /// 判断两个 Id 的 ItemId 与 SubId 是否都相同
    /// </summary>
    /// <param name="other">要比较的 Id</param>
    /// <returns>都相同返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 忽略 ModId，不同模组下的同名同序号项目被视为相同
    /// </remarks>
    public bool SameItemAndSubId(Id other)
        => SameItem(other) && SubId == other.SubId;

    /// <summary>
    /// 判断两个 Id 的 ModId、ItemId 与 SubId 是否都相同
    /// </summary>
    /// <param name="other">要比较的 Id</param>
    /// <returns>都相同返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 等价于 <see cref="Equals(Id)"/>
    /// </remarks>
    public bool SameAll(Id other) => Equals(other);

    #endregion

    #region 相等性

    /// <inheritdoc />
    public bool Equals(Id other)
        => string.Equals(ModId, other.ModId, StringComparison.Ordinal)
        && string.Equals(ItemId, other.ItemId, StringComparison.Ordinal)
        && SubId == other.SubId;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Id other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(ModId, ItemId, SubId);

    public static bool operator ==(Id left, Id right) => left.Equals(right);

    public static bool operator !=(Id left, Id right) => !left.Equals(right);

    #endregion

    #region 解析

    /// <summary>
    /// 将 <c>ModId.ItemId:SubId</c> 格式的字符串解析为 <see cref="Id"/>
    /// </summary>
    /// <param name="s">要解析的字符串</param>
    /// <param name="provider">格式提供程序，当前实现忽略此参数</param>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> 为 <see langword="null"/></exception>
    /// <exception cref="FormatException"><paramref name="s"/> 不符合格式</exception>
    public static Id Parse(string s, IFormatProvider? provider) => new(s);

    /// <summary>
    /// 尝试将 <c>ModId.ItemId[:SubId]</c> 格式的字符串解析为 <see cref="Id"/>
    /// </summary>
    /// <param name="s">要解析的字符串</param>
    /// <param name="provider">格式提供程序，当前实现忽略此参数</param>
    /// <param name="result">解析成功时为结果值，失败时为 <see langword="default"/></param>
    /// <returns>解析成功返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 本方法不抛异常，任何非法输入都通过返回 <see langword="false"/> 表达
    /// </remarks>
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        out Id result)
    {
        result = default;
        if (s is null || !TryParseCore(s, out string? modId, out string? itemId, out int subId))
            return false;

        result = new Id(modId, itemId, subId);
        return true;
    }

    #endregion

    /// <summary>
    /// 输出 <c>ModId.ItemId</c> 或 <c>ModId.ItemId:SubId</c> 格式
    /// </summary>
    /// <returns>
    /// <see cref="SubId"/> 为 0 时输出 <c>ModId.ItemId</c>，
    /// 否则输出 <c>ModId.ItemId:SubId</c>
    /// </returns>
    public override string ToString()
        => SubId == 0
            ? $"{ModId}{Separator}{ItemId}"
            : $"{ModId}{Separator}{ItemId}{SubIdSeparator}{SubId}";
}