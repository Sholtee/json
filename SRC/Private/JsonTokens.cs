/********************************************************************************
* JsonTokens.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json.Internals
{
    /// <summary>
    /// For seamless testing
    /// </summary>
    internal sealed class TokenValueAttribute(string value, bool isLiteral = false, JsonReaderFlags requiredFlag = JsonReaderFlags.None) : Attribute
    {
        public string Value { get; } = value;

        public bool IsLiteral { get; } = isLiteral;

        public JsonReaderFlags RequiredFlag { get; } = requiredFlag;
    }

    [Flags]
    internal enum JsonTokens
    {
        Unknown = 0,
        Eof = 1 << 0,
        [TokenValue("//", requiredFlag: JsonReaderFlags.AllowComments)]
        DoubleSlash = 1 << 1,
        [TokenValue("{")]
        CurlyOpen = 1 << 2,
        [TokenValue("}")]
        CurlyClose = 1 << 3,
        [TokenValue("[")]
        SquaredOpen = 1 << 4,
        [TokenValue("]")]
        SquaredClose = 1 << 5,
        [TokenValue(":")]
        Colon = 1 << 6,
        [TokenValue(",")]
        Comma = 1 << 7,
        [TokenValue("'", requiredFlag: JsonReaderFlags.AllowSingleQuotedStrings)]
        SingleQuote = 1 << 8,
        [TokenValue("\"")]
        DoubleQuote = 1 << 9,
        Number = 1 << 10,
        [TokenValue("true", isLiteral: true)]
        True = 1 << 11,
        [TokenValue("false", isLiteral: true)]
        False = 1 << 12,
        [TokenValue("null", isLiteral: true)]
        Null = 1 << 13
    }
}
