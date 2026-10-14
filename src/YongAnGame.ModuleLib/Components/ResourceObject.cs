using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace YongAnGame.ModuleLib.Components;

/// <summary>
/// 对任意对象的轻量包装，用于在编译期类型未知的场景下传递值
/// </summary>
/// <remarks>
/// 提供显式的类型转换入口，避免调用方到处写 <see langword="as"/> / <see langword="is"/> 判断
/// 包装对象本身不可变，可安全地被多线程并发读取
/// <para>
/// null 处理约定：包装的值为 <see langword="null"/> 时，
/// <see cref="TryGetObject{T}"/> 对引用类型和可空值类型返回 <see langword="true"/> 并输出 <see langword="null"/>，
/// <see cref="GetObject{T}"/> 同样返回 <see langword="null"/>
/// </para>
/// </remarks>
/// <param name="obj">要包装的对象，允许为 <see langword="null"/></param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class ResourceObject(object? obj)
{
    private readonly object? _obj = obj;

    /// <summary>
    /// 获取被包装对象的运行时类型，包装的值为 <see langword="null"/> 时返回 <see langword="null"/>
    /// </summary>
    /// <remarks>
    /// 返回的是精确类型，不是“是否兼容某个接口”的判断，
    /// 后者用 <see cref="Is{T}"/>
    /// </remarks>
    public Type? GetObjectType() => _obj?.GetType();

    /// <summary>
    /// 尝试将被包装对象转换为 <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="value">转换成功时为结果值，失败时为 <see langword="default"/></param>
    /// <returns>
    /// 类型兼容返回 <see langword="true"/>，否则返回 <see langword="false"/>
    /// 包装的值为 <see langword="null"/> 且 <typeparamref name="T"/> 为引用类型或可空值类型时返回 <see langword="true"/>
    /// </returns>
    /// <remarks>
    /// 线程安全：可并发调用
    /// </remarks>
    public bool TryGetObject<T>([MaybeNullWhen(false)] out T? value)
    {
        if (_obj is T t)
        {
            value = t;
            return true;
        }

        if (_obj is null && default(T) is null)
        {
            value = default;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 将被包装对象转换为 <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>
    /// 兼容时返回转换结果
    /// 包装的值为 <see langword="null"/> 且 <typeparamref name="T"/> 为引用类型或可空值类型时返回 <see langword="null"/>
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// 实际类型与 <typeparamref name="T"/> 不兼容
    /// </exception>
    /// <remarks>
    /// 线程安全：可并发调用
    /// </remarks>
    public T GetObject<T>()
    {
        if (_obj is T t)
            return t;

        if (_obj is null && default(T) is null)
            return default!;

        throw new InvalidCastException(
            $"ResourceObject 实际存储类型为 {Describe(_obj)}，无法转换为 {typeof(T)}");
    }

    /// <summary>
    /// 判断被包装对象是否可转换为 <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>兼容返回 <see langword="true"/>，否则返回 <see langword="false"/></returns>
    /// <remarks>
    /// 与 <see cref="TryGetObject{T}"/> 的判断条件一致
    /// 包装的值为 <see langword="null"/> 且 <typeparamref name="T"/> 为引用类型或可空值类型时返回 <see langword="true"/>
    /// <para>线程安全：可并发调用</para>
    /// </remarks>
    public bool Is<T>() => _obj is T || (_obj is null && default(T) is null);

    /// <summary>
    /// 返回被包装对象的字符串表示，包装的值为 <see langword="null"/> 时返回空字符串
    /// </summary>
    /// <returns>被包装对象的 <see cref="object.ToString"/> 结果</returns>
    /// <remarks>
    /// 若被包装对象的 <see cref="object.ToString"/> 抛异常，本方法会一并传播
    /// </remarks>
    public override string ToString()
        => _obj?.ToString() ?? string.Empty;

    private static string Describe(object? obj)
        => obj is null ? "null" : obj.GetType().ToString();

    private string DebuggerDisplay
        => _obj is null ? "(null)" : $"{_obj.GetType().Name}: {_obj}";
}