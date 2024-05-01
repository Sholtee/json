/********************************************************************************
* SerializationContext.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Json
{
    public readonly partial struct SerializationContext  // TODO: convert record type
    {
        public readonly struct Entry(in SerializationContext context, object? value, string? name = null)
        {
            public readonly SerializationContext Context = context;
            public readonly string? Name = name;
            public readonly object? Value = value;
        }

        public delegate IEnumerable<Entry> EnumEntriesDelegate(object value);

        public delegate JsonDataTypes GetTypeDelegate(object? value);

        /// <summary>
        /// Converts the given object to its string representation.
        /// </summary>
        /// <param name="obj">Object to be converted</param>
        /// <param name="buffer">Buffer to hold the converted string. If the buffer is insufficient the implementation is allowed to resize it</param>
        public delegate ReadOnlySpan<char> ToStringDelegate(object? value, ref char[] buffer);

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
        public ToStringDelegate? ConvertToString { get; init; }

        /// <summary>
        /// If supported, enumerates the list/object entries alongside their serialization context.
        /// </summary>
        public EnumEntriesDelegate? EnumEntries { get; init; }

        /// <summary>
        /// Shortcut for <see cref="ContextFactory.CreateSerializationContextFor(Type, object?)"/>
        /// </summary>
        public static SerializationContext For(Type type, object? config = null) => ContextFactory.CreateSerializationContextFor(type, config);
    }
}
