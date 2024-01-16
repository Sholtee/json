/********************************************************************************
* JsonReaderFlags.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.JSON
{
    /// <summary>
    /// Contains flags for fine-tuning the JSON parser.
    /// </summary>
    [Flags]
    public enum JsonReaderFlags
    {
        /// <summary>
        /// The default value.
        /// </summary>
        None = 0,

        /// <summary>
        /// Allow double slash comments.
        /// </summary>
        AllowComments = 1 << 0,

        /// <summary>
        /// Allows strings to be declared by single quotes.
        /// </summary>
        AllowSingleQuotedStrings = 1 << 1,

        /// <summary>
        /// Allows trailing comma after the last key-value pair or list item.
        /// </summary>
        AllowTrailingComma = 1 << 2,

        /// <summary>
        /// Instructs the parser to use case insensitive comparison.
        /// </summary>
        CaseInsensitive = 1 << 3,
    }
}
