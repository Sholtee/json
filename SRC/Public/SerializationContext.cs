/********************************************************************************
* SerializationContext.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Solti.Utils.Json
{
    public sealed record SerializationContext
    {
        public delegate JsonDataTypes GetTypeDelegate(object? obj);

        public delegate string ToStringDelegate(object obj);

        /// <summary>
        /// Gets the type of the given value.
        /// </summary>
        public required GetTypeDelegate GetTypeOf { get; init; }

        /// <summary>
        /// Converts the given value to <see cref="string"/>.
        /// </summary>
        public required ToStringDelegate ConvertToString { get; init; }
    }
}
