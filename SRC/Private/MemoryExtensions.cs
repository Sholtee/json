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

        private static TDelegate? GetNativeDelegate<TDelegate>(string name, params Type[] paramTypes) where TDelegate: Delegate
        {
            MethodInfo? m = typeof(System.MemoryExtensions).GetMethods().SingleOrDefault
            (
                m => m.Name == name && m
                    .GetParameters()
                    .Select(static p => p.ParameterType.IsGenericType ? p.ParameterType.GetGenericTypeDefinition() : p.ParameterType)
                    .SequenceEqual(paramTypes)
            )?.MakeGenericMethod(typeof(char));
            if (m is null)
                return null;

            ParameterExpression[] paramz = m
                .GetParameters()
                .Select(static p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();

            return Expression.Lambda<TDelegate>
            (
                Expression.Call(null, m, paramz),
                paramz
            ).Compile();
        }

        private delegate int IndexOfAnyExceptDelegate(ReadOnlySpan<char> span, ReadOnlySpan<char> values);

        private static readonly IndexOfAnyExceptDelegate? FIndexOfAnyExcept = GetNativeDelegate<IndexOfAnyExceptDelegate>
        (
            nameof(IndexOfAnyExcept),
            typeof(ReadOnlySpan<>),
            typeof(ReadOnlySpan<>)
        );

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
