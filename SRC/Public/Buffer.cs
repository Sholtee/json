/********************************************************************************
* Buffer.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json
{
    using Internals;

    /// <summary>
    /// Defines a generic, resizable buffer.
    /// </summary>
    public ref struct Buffer<T>(int initialSize)
    {
        private T[] FBuffer = MemoryPool<T>.Get(initialSize);

        public readonly Span<T> Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FBuffer.AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (FBuffer is not null)
            {
                MemoryPool<T>.Return(FBuffer);
                FBuffer = null!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int newSize) => Array.Resize(ref FBuffer, newSize);
    }
}
