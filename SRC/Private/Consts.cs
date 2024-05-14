/********************************************************************************
* Consts.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Json.Internals
{
    internal static class Consts
    {
        public const string
            TRUE = "true",
            FALSE = "false",
            NULL = "null",
            DOUBLE_SLASH = "//",
            SLASH_ASTERISK = "/*",

            //
            // Control characters occupy the range of 0 to 31:
            // https://www.ascii-code.com/articles/ASCII-Control-Characters
            //

            BACKSLASH_QUOTES_CONTROLS = "\\'\"\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000A\u000B\u000C\u000D\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F",
            INTEGRAL = "0123456789-",
            FLOATING = "0123456789+-.eE";
    }
}
