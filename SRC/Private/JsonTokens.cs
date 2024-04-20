/********************************************************************************
* JsonTokens.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json.Internals
{
    [Flags]
    internal enum JsonTokens
    {
        Unknown = 0,
        
        //
        // Standard tokens
        //

        Eof = 1 << 0,
        CurlyOpen = 1 << 1,
        CurlyClose = 1 << 2,
        SquaredOpen = 1 << 3,
        SquaredClose = 1 << 4,
        Colon = 1 << 5,
        Comma = 1 << 6,     
        DoubleQuote = 1 << 7,
        Number = 1 << 8,
        True = 1 << 9,
        False = 1 << 10,
        Null = 1 << 11,

        //
        // NON standard tokens
        //

        SingleQuote = 1 << 12,
        DoubleSlash = 1 << 13,
        SlashAsterisk = 1 << 14
    }
}
