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

        public int PeekChar() => CharsLeft is 0 && PeekText(1).Length is 0
            ? -1
            : FBuffer[FPosition];

        public void Advance(int chars)
        {
            if (chars < 0)
                throw new ArgumentOutOfRangeException(nameof(chars));

            int missingChars = chars - CharsLeft;
            if (missingChars > 0)
            {
                PeekText(chars);

                if (chars > CharsLeft)
                    throw new ArgumentOutOfRangeException(nameof(chars));
            }

            FPosition += chars;
        }

        public void Dispose()
        {
            if (FBuffer is not null)
            {
                MemoryPool<char>.Return(FBuffer);
                FBuffer = null!;
            }

            if (textReader is not null)
            {
                textReader.Dispose();
                textReader = null!;
            }

            FPosition = FCharsRead = -1;
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
        /// Characters left in the underlying buffer
        /// </summary>
        public int CharsLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FCharsRead - FPosition;
        }

        public int BufferSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FBuffer.Length;
        }
    }
}
