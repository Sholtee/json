/********************************************************************************
* MemoryExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json.Internals
{
    internal static class MemoryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !NETSTANDARD2_1_OR_GREATER
        unsafe
#endif
        public static string AsString(this ReadOnlySpan<char> self)
        {
#if NETSTANDARD2_1_OR_GREATER
            return new string(self);
#else
            fixed (char* ptr = self)
            {
                return new string(ptr, 0, self.Length);
            }
#endif
        }

        private static readonly int FSeed = new Random().Next();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static int GetXxHashCode(this ReadOnlySpan<char> self)
        {
            fixed (void* ptr = self)
            {
                return (int) XxHash32.HashToUInt32(new ReadOnlySpan<byte>(ptr, self.Length * sizeof(char)), FSeed);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsString(this Span<char> self) => AsString((ReadOnlySpan<char>) self);
    }
}
