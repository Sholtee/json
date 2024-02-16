/********************************************************************************
* SerializationContext.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Json
{
    public readonly partial struct SerializationContext
    {
        public readonly struct Entry
        {
            public required SerializationContext Context { get; init; }
            public readonly string? Name { get; init; }
            public required object? Value { get; init; }
        }

        public delegate IEnumerable<Entry> EnumEntriesDelegate(object value);

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
        /// If supported, enumerates the list/object entries alongside their serialization context.
        /// </summary>
        public EnumEntriesDelegate? EnumEntries { get; init; }

        /// <summary>
        /// Empty context. Using this context instructs the system to skip the fragment of object tree on which the writer is positioned.
        /// </summary>
        public static SerializationContext Empty { get; } = new() { ConvertToString = null!, GetTypeOf = null! };

        public static bool operator ==(in SerializationContext left, in SerializationContext right) =>
            left.GetTypeOf == right.GetTypeOf &&
            left.ConvertToString == right.ConvertToString &&
            left.EnumEntries == right.EnumEntries;

        public static bool operator !=(in SerializationContext left, in SerializationContext right) => !(left == right);

        public override bool Equals(object obj) => throw new NotSupportedException();

        public override int GetHashCode() => throw new NotSupportedException();
    }
}
