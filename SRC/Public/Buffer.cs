/********************************************************************************
* Buffer.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Buffers;

namespace Solti.Utils.Json
{
    public sealed class Buffer<T>(int minsize = 256) : IDisposable
    {
        private static readonly ArrayPool<T> FPool = ArrayPool<T>.Shared;

        public void Resize(int newSize)
        {
            T[] newValue = FPool.Rent(newSize);
            Value.AsSpan(0, Math.Min(Value.Length, newSize)).CopyTo(newValue);
            FPool.Return(Value);
            Value = newValue;
        }

        public void Dispose() => FPool.Return(Value);

        public T[] Value { get; private set; } = FPool.Rent(minsize);
    }
}
