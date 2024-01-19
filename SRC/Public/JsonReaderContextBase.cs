/********************************************************************************
* JsonReaderContextBase.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Json
{
    public abstract class JsonReaderContextBase(in CancellationToken cancellation) : IJsonReaderContext
    {
        private char[] FBuffer = new char[128];

        private readonly CancellationToken FCancellation = cancellation;

        public virtual int Row { get; set; }

        public virtual int Column { get; set; }

        public void ThrowIfCancellationRequested() => FCancellation.ThrowIfCancellationRequested();

        public Span<char> GetBuffer(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (length > FBuffer.Length)
                Array.Resize(ref FBuffer, length);

            return FBuffer.AsSpan(0, length);
        }

        public abstract object CreateRawObject();
        public abstract void PopState();
        public abstract bool PushState(ReadOnlySpan<char> property, StringComparison comparison);
        public abstract void SetValue(object obj, object? value);
        public abstract void SetValue(object obj, ReadOnlySpan<char> value);
        
    }
}
