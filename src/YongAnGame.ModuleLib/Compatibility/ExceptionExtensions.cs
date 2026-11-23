#if !NETCOREAPP2_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// 为低版本目标框架补齐参数校验的静态辅助方法
/// </summary>
/// <remarks>
/// 各方法按引入版本分别用条件编译控制，高版本走 BCL 内置实现。
/// 命名空间为 <c>System</c>，调用方无需额外 using。
/// </remarks>
internal static class ExceptionThrowExtensions
{
#if !NET6_0_OR_GREATER
    extension(ArgumentException)
    {
        /// <summary>
        /// 为 <see langword="null"/> 或空字符串时抛出异常
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="argument"/> 为 <see langword="null"/></exception>
        /// <exception cref="ArgumentException"><paramref name="argument"/> 为空字符串</exception>
        public static void ThrowIfNullOrEmpty(
            [NotNull] string? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (argument.Length == 0)
            {
                throw new ArgumentException("值不能为空字符串。", paramName);
            }
        }
    }
#endif

#if !NET8_0_OR_GREATER
    extension(ArgumentException)
    {
        /// <summary>
        /// 为 <see langword="null"/>、空或仅含空白字符时抛出异常
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="argument"/> 为 <see langword="null"/></exception>
        /// <exception cref="ArgumentException"><paramref name="argument"/> 为空或仅含空白字符</exception>
        public static void ThrowIfNullOrWhiteSpace(
            [NotNull] string? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("值不能为空或仅含空白字符。", paramName);
            }
        }
    }
#endif

#if !NET6_0_OR_GREATER
    extension(ArgumentNullException)
    {
        /// <summary>
        /// 为 <see langword="null"/> 时抛出异常
        /// </summary>
        public static void ThrowIfNull(
            [NotNull] object? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
#endif

#if !NET7_0_OR_GREATER
    extension(ArgumentOutOfRangeException)
    {
        /// <summary>
        /// 为负数时抛出异常
        /// </summary>
        public static void ThrowIfNegative(
            int argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, argument, "值不能为负数。");
            }
        }

        /// <summary>
        /// 为负数时抛出异常
        /// </summary>
        public static void ThrowIfNegative(
            long argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, argument, "值不能为负数。");
            }
        }

        /// <summary>
        /// 为 0 时抛出异常
        /// </summary>
        public static void ThrowIfZero(
            int argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument == 0)
            {
                throw new ArgumentOutOfRangeException(paramName, argument, "值不能为 0。");
            }
        }
    }
#endif
}
#endif
