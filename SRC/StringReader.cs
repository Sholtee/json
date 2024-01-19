/********************************************************************************
* StringReader.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    public class StringReader(string str): ITextReader
    {
        private int FPosition = 0;

        private readonly string FUnderlyingString = str ?? throw new ArgumentNullException(nameof(str));

        public int CharsLeft => FUnderlyingString.Length - FPosition;

        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            FPosition += Math.Min(count, CharsLeft);
        }

        public bool PeekChar(out char chr)
        {
            if (FPosition < FUnderlyingString.Length)
            {
                chr = FUnderlyingString[FPosition];
                return true;
            }

            chr = default;
            return false;
        }

        public int PeekText(Span<char> buffer)
        {
            ReadOnlySpan<char> span = FUnderlyingString.AsSpan(FPosition, Math.Min(buffer.Length, CharsLeft));
            span.CopyTo(buffer);
            return span.Length;
        }
    }
}
