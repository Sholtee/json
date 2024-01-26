/********************************************************************************
* UntypedDeserializationContext.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Solti.Utils.Json
{
    using Internals;

    /// <summary>
    /// Context used to create untyped result.
    /// </summary>
    /// <remarks>In untyped result objects are returned as <see cref="IDictionary"/> while lists as <see cref="IList"/>.</remarks>
    public class UntypedDeserializationContext : IDeserializationContext
    {
        /// <summary>
        /// This context supports all the data types.
        /// </summary>
        public virtual JsonDataTypes SupportedTypes { get; } = JsonDataTypes.Object | JsonDataTypes.List | JsonDataTypes.String | JsonDataTypes.Number | JsonDataTypes.Boolean | JsonDataTypes.Null;

        /// <summary>
        /// By default this method does nothing, ovverride to introduce your own implementation.
        /// </summary>
        public virtual void CommentParsed(ReadOnlySpan<char> value)
        {
        }

        /// <summary>
        /// Converts the <paramref name="value"/> to a regular <see cref="string"/>.
        /// </summary>
        public virtual object ConvertString(ReadOnlySpan<char> value) => value.AsString();

        /// <summary>
        /// Returns <see cref="Dictionary{string, object}"/> for <see cref="JsonDataTypes.Object"/> and <see cref="List{object}"/> for <see cref="JsonDataTypes.List"/>.
        /// </summary>
        public virtual object CreateRawObject(JsonDataTypes jsonDataType) => jsonDataType switch
        {
            JsonDataTypes.Object => new Dictionary<string, object?>(StringComparer.Ordinal),
            JsonDataTypes.List => new List<object?>(),
            _ => throw new NotSupportedException()
        };

        /// <summary>
        /// Returns this instance, no nested context created.
        /// </summary>
        public IDeserializationContext GetNestedContext(ReadOnlySpan<char> property, StringComparison comparison) => this;

        /// <summary>
        /// Returns this instance, no nested context created.
        /// </summary>
        public IDeserializationContext GetNestedContext(int index) => this;

        /// <summary>
        /// Updates the given <paramref name="instance"/>.
        /// </summary>
        public void SetValue(object instance, object? value)
        {
            switch (instance)
            {
                case List<object?> list:
                    list.Add(value);
                    break;
                case Dictionary<string, object?> dictionary:
                    // TODO
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// By default this method does nothing, ovverride to introduce your own implementation.
        /// </summary>
        public virtual void Verify(object? value)
        {
        }
    }
}
