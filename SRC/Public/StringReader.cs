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

        public int CharsLeft => str.Length - FPosition;

        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            FPosition += Math.Min(count, CharsLeft);
        }

        public bool PeekChar(out char chr)
        {
            if (FPosition < str.Length)
            {
                chr = str[FPosition];
                return true;
            }

            chr = default;
            return false;
        }

        public int PeekText(Span<char> buffer)
        {
            ReadOnlySpan<char> span = str.AsSpan(FPosition, Math.Min(buffer.Length, CharsLeft));
            span.CopyTo(buffer);
            return span.Length;
        }
    }
}
