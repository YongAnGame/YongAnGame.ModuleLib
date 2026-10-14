#if !NETCOREAPP2_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// 为 .NET 7 以下框架补齐参数校验的静态辅助方法
/// </summary>
/// <remarks>
/// 仅在低版本目标框架下编译，高版本走 BCL 内置实现
/// 命名空间为 <c>System</c>，调用方无需额外 using
/// <para>
/// 与 Polyfill 配合使用：Polyfill 补齐 <c>CallerArgumentExpression</c> 等特性，
/// 本类补齐 <c>ThrowIfXxx</c> 静态方法
/// </para>
/// </remarks>
internal static class ExceptionThrowExtensions
{
    extension(ArgumentException)
    {
        /// <summary>
        /// 为 null 或空字符串时抛 <see cref="ArgumentException"/>
        /// </summary>
        public static void ThrowIfNullOrEmpty(
            string? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (string.IsNullOrEmpty(argument))
                throw new ArgumentException("值不能为 null 或空", paramName);
        }

        /// <summary>
        /// 为 null、空或仅含空白字符时抛 <see cref="ArgumentException"/>
        /// </summary>
        public static void ThrowIfNullOrWhiteSpace(
            string? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (string.IsNullOrWhiteSpace(argument))
                throw new ArgumentException("值不能为 null、空或仅含空白字符", paramName);
        }
    }

    extension(ArgumentNullException)
    {
        /// <summary>
        /// 为 null 时抛 <see cref="ArgumentNullException"/>
        /// </summary>
        public static void ThrowIfNull(
            object? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
                throw new ArgumentNullException(paramName);
        }
    }

    extension(ArgumentOutOfRangeException)
    {
        /// <summary>
        /// 为负数时抛 <see cref="ArgumentOutOfRangeException"/>
        /// </summary>
        public static void ThrowIfNegative(
            int argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument < 0)
                throw new ArgumentOutOfRangeException(paramName, argument, "值不能为负数");
        }

        /// <summary>
        /// 为负数时抛 <see cref="ArgumentOutOfRangeException"/>
        /// </summary>
        public static void ThrowIfNegative(
            long argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument < 0)
                throw new ArgumentOutOfRangeException(paramName, argument, "值不能为负数");
        }

        /// <summary>
        /// 为 0 时抛 <see cref="ArgumentOutOfRangeException"/>
        /// </summary>
        public static void ThrowIfZero(
            int argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument == 0)
                throw new ArgumentOutOfRangeException(paramName, argument, "值不能为 0");
        }
    }
}
#endif