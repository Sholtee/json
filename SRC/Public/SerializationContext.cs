/********************************************************************************
* SerializationContext.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    public readonly partial struct SerializationContext
    {
        public readonly ref struct Entry(in SerializationContext context, object? value, string? name = null)
        {
            public readonly SerializationContext Context = context;
            public readonly string? Name = name;
            public readonly object? Value = value;
        }

        public delegate bool EntryEnumerator(out Entry entry);

        public delegate EntryEnumerator EnumEntriesDelegate(object value);

        public delegate JsonDataTypes GetTypeDelegate(object? obj);

        /// <summary>
        /// Converts the given object to its string representation.
        /// </summary>
        /// <param name="obj">Object to be converted</param>
        /// <param name="buffer">Buffer to hold the converted string. If the buffer is insufficient the implementation is supposed to resize it</param>
        public delegate ReadOnlySpan<char> ToStringDelegate(object? obj, Buffer<char> buffer);

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
    }
}
