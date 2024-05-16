/********************************************************************************
* MemoryExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json.Internals
{
    internal static class MemoryExtensions
    {
        public static int GetHashCode(this ReadOnlySpan<char> self, bool ignoreCase)
        {
            Span<char> buffer = ignoreCase ? stackalloc char[4] : default;

            //
            // https://github.com/bryc/code/blob/master/jshash/hashes/murmurhash3.js
            //

            unchecked
            {
                uint h = 1986 /*seed*/, k; 

                int i = 0;
                for (int b = self.Length & -4; i < b; i += 4)
                {
                    ReadOnlySpan<char> span = ignoreCase ? BlockToUpper(self.Slice(i, 4), buffer) : self.Slice(i, 4);
  
                    k = (uint) (span[3] << 24 | span[2] << 16 | span[1] << 8 | span[0]);
                    k *= 3432918353; k = k << 15 | k >>> 17;
                    h ^= k * 461845907; h = h << 13 | h >>> 19;
                    h *= 5 + 3864292196;
                }

                int m = self.Length & 3;
                if (m > 0)
                {
                    k = 0;
                    switch (m)
                    {
                        case 3:
                            k ^= (uint) (ignoreCase ? CharToUpper(self[i + 2]) : self[i + 2]) << 16;
                            goto case 2;
                        case 2:
                            k ^= (uint) (ignoreCase ? CharToUpper(self[i + 1]) : self[i + 1]) << 8;
                            goto case 1;
                        case 1:
                            k ^= ignoreCase ? CharToUpper(self[i]) : self[i];
                            k *= 3432918353; k = k << 15 | k >>> 17;
                            h ^= k * 461845907;
                            break;
                    }
                }

                h ^= (uint) self.Length;

                h ^= h >>> 16; h *= 2246822507;
                h ^= h >>> 13; h *= 3266489909;
                h ^= h >>> 16;

                return (int) h >>> 0;
            }

            //
            // https://github.com/dotnet/runtime/blob/ecc8cb5bc0411e0fb0549230f70dfe8ab302c65c/src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf16Utility.cs#L98
            //

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static ReadOnlySpan<char> BlockToUpper(ReadOnlySpan<char> chars, Span<char> buffer)
            {
                Debug.Assert(chars.Length == 4);
                Debug.Assert(buffer.Length == 4);

                ulong l = Unsafe.As<char, ulong>(ref Unsafe.AsRef(in chars[0]));

                if ((l & ~0x007F_007F_007F_007Ful) == 0)
                {
                    //
                    // All the 4 chars are ASCII
                    //

                    ulong
                        lowerIndicator = l + 0x0080_0080_0080_0080ul - 0x0061_0061_0061_0061ul,              
                        upperIndicator = l + 0x0080_0080_0080_0080ul - 0x007B_007B_007B_007Bul,         
                        combinedIndicator = lowerIndicator ^ upperIndicator,
                        mask = (combinedIndicator & 0x0080_0080_0080_0080ul) >> 2;

                    Unsafe.As<char, ulong>(ref buffer[0]) = l ^ mask;
                }
                else
                {
                    //
                    // Slow like hell
                    //

                    chars.ToUpperInvariant(buffer);
                }
                return buffer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static char CharToUpper(char chr)
            {
                if ((chr & ~0x007F) == 0)
                {
                    int
                        lowerIndicator = chr + 0x0080 - 0x0061,
                        upperIndicator = chr + 0x0080 - 0x007B,
                        combinedIndicator = lowerIndicator ^ upperIndicator,
                        mask = (combinedIndicator & 0x0080) >> 2;

                    return (char) (chr ^ mask);
                }

                //
                // Slow...
                //

                return char.ToUpperInvariant(chr);
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
            //
            // Since this will be available on .NET8+ systems only we should borrow the actual code from here:
            // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/MemoryExtensions.cs#L1024
            //

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
