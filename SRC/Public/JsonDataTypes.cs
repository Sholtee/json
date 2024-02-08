/********************************************************************************
* JsonDataTypes.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    using Internals;

    /// <summary>
    /// Defines the supported data types
    /// </summary>
    [Flags]
    public enum JsonDataTypes
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        Unkown = 0,

        /// <summary>
        /// <see cref="string"/>
        /// </summary>
        String = JsonTokens.SingleQuote | JsonTokens.DoubleQuote,

        /// <summary>
        /// <see cref="bool"/>
        /// </summary>
        Boolean = JsonTokens.True | JsonTokens.False,

        /// <summary>
        /// <see cref="long"/> or <see cref="double"/>
        /// </summary>
        Number = JsonTokens.Number,

        /// <summary>
        /// Null
        /// </summary>
        Null = JsonTokens.Null,

        /// <summary>
        /// Object
        /// </summary>
        Object = JsonTokens.CurlyOpen,

        /// <summary>
        /// List
        /// </summary>
        List = JsonTokens.SquaredOpen,

        /// <summary>
        /// Any of supported types
        /// </summary>
        Any = Object | List | String | Number | Boolean | Null
    }
}
