/********************************************************************************
* TextReaderWrapper.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Runtime.CompilerServices;

using static System.Array;
using static System.Diagnostics.Debug;

namespace Solti.Utils.Json.Internals
{
    internal sealed class TextReaderWrapper(TextReader textReader, int initialBufferSize = -1 /*for testing*/) : IDisposable
    {
        private char[] FBuffer = MemoryPool<char>.Get(initialBufferSize);

        private int
            FPosition,

            //
            // Characters read last time
            //

            FCharsRead;

        /// <summary>
        /// Peeks <b>maximum</b> <paramref name="len"/> characters from the underlying <see cref="TextReader"/>
        /// </summary>
        /// <remarks>
        /// This method preserves the state so modification made to the returned <see cref="Span{char}"/> won't be lost between calls.
        /// </remarks>
        public Span<char> PeekText(int len)
        {
            if (len < 0)
                throw new ArgumentOutOfRangeException(nameof(len));

            //
            // Check if we have enough characters left to return immediately
            //

            if (CharsLeft > 0 && len <= CharsLeft)
                return FBuffer.AsSpan(FPosition, len);

            int charsToBeRead = len - CharsLeft;

            //
            // Try free up enough space to store the new chunk
            //

            if (CharsLeft > 0)
            {
                Copy(FBuffer, FPosition, FBuffer, 0, CharsLeft);
                FCharsRead = CharsLeft;
                FPosition = 0;
            }
            else
            {
                FCharsRead = FPosition = 0;
            }

            //
            // If we don't have enough free space, resize the buffer
            //

            if (FreeSpace < charsToBeRead)
            {
                Assert(len > BufferSize, "Cannot downsize the buffer");
                Resize(ref FBuffer, len);
            }

            //
            // Read the new chunk into the free space.
            //

            int startIndex = FPosition + CharsLeft;
            Assert(BufferSize - startIndex >= charsToBeRead, "Miscalculated parameter(s)");

            FCharsRead = CharsLeft + textReader.Read(FBuffer, startIndex, charsToBeRead);

            //
            // Note that the returned data may be shorter than the requested size
            //

            return FBuffer.AsSpan(FPosition, FCharsRead);
        }

        /// <summary>
        /// Peeks one character from the underlying <see cref="TextReader"/> or returns -1 if there is no more data.
        /// </summary>
        public int PeekChar() => CharsLeft is 0 && PeekText(1).Length is 0
            ? -1
            : FBuffer[FPosition];

        /// <summary>
        /// Advances the reader by <b>exactly</b> <paramref name="len"/> characters or throws if there is no enough charcters left
        /// </summary>
        public void Advance(int len)
        {
            if (len < 0)
                throw new ArgumentOutOfRangeException(nameof(len));

            int missingChars = len - CharsLeft;
            if (missingChars > 0)
            {
                PeekText(len);

                if (len > CharsLeft)
                    throw new ArgumentOutOfRangeException(nameof(len));
            }

            FPosition += len;
        }

        /// <summary>
        /// Disposes this instance releasing the underlying <see cref="TextReader"/> as well.
        /// </summary>
        public void Dispose()
        {
            if (FBuffer is not null)
            {
                MemoryPool<char>.Return(FBuffer);
                FBuffer = null!;
            }

            FPosition = FCharsRead = 0;
        }

        /// <summary>
        /// Free unused space in the underlying buffer.
        /// </summary>
        public int FreeSpace
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FBuffer.Length - FCharsRead;
        }

        /// <summary>
        /// Characters left in the underlying buffer.
        /// </summary>
        public int CharsLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FCharsRead - FPosition;
        }

        /// <summary>
        /// The maximum number of characters that can be stored in the underlying buffer. This value may change runtime.
        /// </summary>
        public int BufferSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FBuffer.Length;
        }

        public static implicit operator TextReaderWrapper(TextReader reader) => new(reader);
    }
}
