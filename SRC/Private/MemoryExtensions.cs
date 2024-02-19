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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsString(this Span<char> self) => AsString((ReadOnlySpan<char>) self);

        private static readonly int FSeed = new Random().Next();

        /// <summary>
        /// Hashes the given text. The algorithm is case insensitive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetXxHashCode(this ReadOnlySpan<char> self, byte bufferSize = 32 /*for testing*/)
        {
            XxHash32 hash = new(FSeed);

            Span<char> buffer = stackalloc char[bufferSize];

            int j = 0;
            for (int i = 0; i < self.Length; i++)
            {
                buffer[j] = char.ToUpper(self[i]);

                if (j == buffer.Length - 1)
                {
                    AppendHash(buffer, buffer.Length, hash);
                    j = 0;
                    continue;
                }
                j++;
            }

            if (j > 0)
                AppendHash(buffer, j, hash);

            return (int) hash.GetCurrentHashAsUInt32();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static unsafe void AppendHash(Span<char> buffer, int length, XxHash32 hash)
            {
                fixed (void* ptr = buffer)
                {
                    hash.Append
                    (
                        new ReadOnlySpan<byte>
                        (
                            ptr,
                            length * sizeof(char)
                        )
                    );
                }
            }
        }
    }
}
