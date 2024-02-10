/********************************************************************************
* SerializationContext.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    public sealed partial record SerializationContext
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
        /// <remarks>You can implement -for instance- <see cref="DateTime"/> to <see cref="string"/> conversation here.</remarks>
        public required ToStringDelegate ConvertToString { get; init; }
    }
}
