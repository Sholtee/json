/********************************************************************************
* IFormattableExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Json.Internals
{
#if NETSTANDARD2_1_OR_GREATER
    using Primitives;

    internal static class IFormattableExtensions
    {
        private static class TryFromat<T> where T : struct, IFormattable
        {
            public delegate bool TryFormatDelegate(T target, Span<char> dst, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider);

            public static readonly TryFormatDelegate? Delegate = AssembleTryFormatDelegate();

            private static TryFormatDelegate? AssembleTryFormatDelegate()
            {
                Type? spanFormattable = Type.GetType("System.ISpanFormattable", throwOnError: false);
                if (spanFormattable is null || !spanFormattable.IsAssignableFrom(typeof(T)))
                    return null;

                MethodInfo? tryFormat = spanFormattable.GetMethod
                (
                    "TryFormat",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    [typeof(Span<char>), typeof(int).MakeByRefType(), typeof(ReadOnlySpan<char>), typeof(IFormatProvider)],
                    null
                );
                if (tryFormat is null)
                    return null;

                ParameterExpression
                    target         = Expression.Parameter(typeof(T), nameof(target)),
                    dst            = Expression.Parameter(typeof(Span<char>), nameof(dst)),
                    charsWritten   = Expression.Parameter(typeof(int).MakeByRefType(), nameof(charsWritten)),
                    format         = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(format)),
                    formatProvider = Expression.Parameter(typeof(IFormatProvider), nameof(formatProvider));

                Expression<TryFormatDelegate> expr = Expression.Lambda<TryFormatDelegate>
                (
                    Expression.Block
                    (
                        type: typeof(bool),
                        Expression.Call
                        (
                            target,
                            tryFormat,
                            dst, charsWritten, format, formatProvider
                        )
                    ),
                    target, dst, charsWritten, format, formatProvider
                );

                Debug.Write(expr.GetDebugView());

                return expr.Compile();
            }
        }

        public static ReadOnlySpan<char> Format<T>(this T self, string format, Span<char> buffer, IFormatProvider formatProvider) where T : struct, IFormattable
        {
            if (TryFromat<T>.Delegate?.Invoke(self, buffer, out int charsWritten, format.AsSpan(), formatProvider) is true)
                return buffer.Slice(0, charsWritten);

            Debug.WriteLine("Cannot format the input. Using the legacy ToString() implementation");
            return self.ToString(format, formatProvider).AsSpan();
        }
    }
#endif
}
