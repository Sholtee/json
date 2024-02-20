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

        private static readonly int FSeed = new Random().Next();

        /// <summary>
        /// Hashes the given text. The algorithm is case insensitive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetXxHashCode(this ReadOnlySpan<char> self)
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
