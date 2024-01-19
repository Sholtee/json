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
        Eof = 1 << 0,
        DoubleSlash = 1 << 1,
        CurlyOpen = 1 << 2,
        CurlyClose = 1 << 3,
        SquaredOpen = 1 << 4,
        SquaredClose = 1 << 5,
        Colon = 1 << 6,
        Comma = 1 << 7,
        SingleQuote = 1 << 8,
        DoubleQuote = 1 << 9,
        Number = 1 << 10,
        True = 1 << 11,
        False = 1 << 12,
        Null = 1 << 13
    }
}
