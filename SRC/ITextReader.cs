/********************************************************************************
* ITextReader.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.JSON
{
    /// <summary>
    /// Specifies the contract of text readers.
    /// </summary>
    public interface ITextReader
    {
        /// <summary>
        /// Reads the specific amount of characters from the underlying text source.
        /// </summary>
        /// <remarks>If there is no enough data in the underlying text source to satisfy the request, the length of returned data may be less then <paramref name="count"/>.</remarks>
        int PeekText(Span<char> buffer);

        /// <summary>
        /// Gets the character which the reader is positioned on.
        /// </summary>
        bool PeekChar(out char chr);

        /// <summary>
        /// Advances the reader by maximum <paramref name="count"/> characters
        /// </summary>
        void Advance(int count);

        /// <summary>
        /// Returns how many characters are left in the underlying text source.
        /// </summary>
        int CharsLeft { get; }
    }
}
