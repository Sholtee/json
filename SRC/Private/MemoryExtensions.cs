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
#if !NETSTANDARD2_1_OR_GREATER
        unsafe
#endif
        public static string AsString(this ReadOnlySpan<char> self)
        {
#if NETSTANDARD2_1_OR_GREATER
            return  new string(self);
#else
            fixed (char* ptr = self)
            {
                return new string(ptr, 0, self.Length);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsString(this Span<char> self) => ((ReadOnlySpan<char>) self).AsString();
    }
}
