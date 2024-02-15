/********************************************************************************
* SerializationContext.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Json
{
    public sealed partial record SerializationContext
    {
        public delegate IEnumerable<(SerializationContext?, object?)> EnumListEntriesDelegate(object value);

        public delegate IEnumerable<(SerializationContext?, string, object?)> EnumObjectEntriesDelegate(object value);

        public delegate JsonDataTypes GetTypeDelegate(object? obj);

        public delegate string ToStringDelegate(object? obj);

        /// <summary>
        /// Gets the type of the given value.
        /// </summary>
        /// <remarks>
        /// <code>
        /// new SerializationContext  // context that supports string serialization only
        /// {
        ///     GetTypeOf = static val => val is string ? JsonDataTypes.String : JsonDataTypes.Unknown,
        ///     ...
        /// }
        /// </code>
        /// </remarks>
        public required GetTypeDelegate GetTypeOf { get; init; }

        /// <summary>
        /// Converts the given value to <see cref="string"/>.
        /// </summary>
        /// <remarks>You can implement -for instance- <see cref="DateTime"/> to <see cref="string"/> conversation here.</remarks>
        public required ToStringDelegate ConvertToString { get; init; }

        /// <summary>
        /// If supported, enumerates the items alongside their serialization context.
        /// </summary>
        public EnumListEntriesDelegate? EnumListEntries { get; init; }

        /// <summary>
        /// If supported, enumerates the items alongside their serialization context.
        /// </summary>
        public EnumObjectEntriesDelegate? EnumObjectEntries { get; init; }
    }
}
