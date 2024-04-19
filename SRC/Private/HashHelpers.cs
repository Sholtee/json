/********************************************************************************
* HashHelpers.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO.Hashing;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json.Internals
{
    internal static class HashHelpers
    {
        public delegate int GetHashCodeDelegate(ReadOnlySpan<char> input);

        public static new readonly GetHashCodeDelegate GetHashCode = GetGetHashCodeDelegate();

        private static GetHashCodeDelegate GetGetHashCodeDelegate()
        {
            //
            // Modern systems must have this delegate built in
            //

            MethodInfo getHashCode = typeof(string).GetMethod
            (
                nameof(GetHashCode),
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(ReadOnlySpan<char>), typeof(StringComparison)],
                null
            );

            if (getHashCode is null)
                return GetXxHashCode;

            ParameterExpression input = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(input));
            return Expression.Lambda<GetHashCodeDelegate>
            (
                Expression.Call
                (
                    null,
                    getHashCode,
                    input,
                    Expression.Constant(StringComparison.OrdinalIgnoreCase)
                ),
                input
            ).Compile();
        }

        private static readonly int FSeed = new Random().Next();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetXxHashCode(ReadOnlySpan<char> self)
        {
            XxHash32 hash = new(FSeed);

            //
            // XxHash32 uses 16 bytes long stripes. Due to performance considerations we don't want the engine
            // to slice the input so we use 8 chars (16 bytes) long chunks
            //

            Span<char> buffer = stackalloc char[8];

            int j = 0;

            for (int i = 0; i < self.Length; i++)
            {
                buffer[j] = char.ToUpper(self[i]);

                if (j == buffer.Length - 1)
                {
                    hash.Append(buffer.ToByteSpan(buffer.Length));
                    j = 0;
                    continue;
                }
                j++;
            }

            if (j > 0)
                hash.Append(buffer.ToByteSpan(j));

            return (int) hash.GetCurrentHashAsUInt32();
        }
    }
}
