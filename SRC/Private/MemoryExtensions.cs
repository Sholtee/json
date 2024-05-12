/********************************************************************************
* MemoryExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
    }
}
