/********************************************************************************
* TextWriterWrapper.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Json.Internals
{
    using Primitives;

    internal sealed class TextWriterWrapper(TextWriter writer)
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

        //
        // Size supposed to be enough, if not, WriteFormat will fallback to the standard implementation
        //

        private readonly char[] FBuffer = new char[128];

        public TextWriter Writer { get; } = writer;

        public void WriteFormat<T>(T formattable, string format, IFormatProvider formatProvider) where T: struct, IFormattable
        {
            if (TryFromat<T>.Delegate?.Invoke(formattable, FBuffer.AsSpan(), out int charsWritten, format.AsSpan(), formatProvider) is true)
            {
                Writer.Write(FBuffer, 0, charsWritten);
            }
            else
            {
                Writer.Write(formattable.ToString(format, formatProvider));
            }
        }

        public void Write(string val)
        {
            if (val.Length > 0)
                Writer.Write(val);
        }

        public void Write(char val) => Writer.Write(val);

        public void Write(ReadOnlySpan<char> val)
        {
            if (val.Length > 0)
                Writer.Write
                (
                    val
#if !NETSTANDARD2_1_OR_GREATER
                        .ToArray()
#endif
                );
        }

        public static implicit operator TextWriterWrapper(TextWriter writer) => new(writer);
    }
}
