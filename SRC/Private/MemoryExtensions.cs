/********************************************************************************
* MemoryExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json.Internals
{
    internal static class MemoryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ReadOnlySpan<byte> ToByteSpan(this Span<char> buffer, int length)
        {
            fixed (void* ptr = buffer)
            {
                return new ReadOnlySpan<byte>
                (
                    ptr,
                    Math.Min(length, buffer.Length) * sizeof(char)
                );
            }
        }

        private static readonly IndexOfAnyExceptDelegate? FIndexOfAnyExcept = GetOfAnyExceptDelegateNative();

        private delegate int IndexOfAnyExceptDelegate(ReadOnlySpan<char> span, ReadOnlySpan<char> values);

        private static IndexOfAnyExceptDelegate? GetOfAnyExceptDelegateNative()
        {
            MethodInfo? m = typeof(System.MemoryExtensions).GetMethods().FirstOrDefault
            (
                static m => m.Name == nameof(IndexOfAnyExcept) && m
                    .GetParameters()
                    .Select(static p => p.ParameterType.IsGenericType ? p.ParameterType.GetGenericTypeDefinition() : p.ParameterType)
                    .SequenceEqual
                    (
                        [
                            typeof(ReadOnlySpan<char>).GetGenericTypeDefinition(),
                            typeof(ReadOnlySpan<char>).GetGenericTypeDefinition()
                        ]
                    )
            )?.MakeGenericMethod(typeof(char));
            if (m is null)
                return null;

            ParameterExpression[] paramz = m
                .GetParameters()
                .Select(static p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();

            return Expression.Lambda<IndexOfAnyExceptDelegate>
            (
                Expression.Call(null, m, paramz),
                paramz
            ).Compile();
        }

        public static int IndexOfAnyExcept(this ReadOnlySpan<char> span, ReadOnlySpan<char> values)
        {
            if (FIndexOfAnyExcept is not null)
                return FIndexOfAnyExcept(span, values);

            for (int i = 0; i < span.Length; i++)
            {
                if (values.IndexOf(span[i]) < 0)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
