/********************************************************************************
* StringDeserializationContextFactory.cs                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    using Internals;
    using Properties;

    /// <summary>
    /// Creates context for <see cref="string"/> deserialization.
    /// </summary>
    public class StringDeserializationContextFactory : DeserializationContextFactory
    {
        /// <inheritdoc/>
        public override DeserializationContext CreateContext(Type type, object? config = null)
        {
            EnsureValidType(type);
            if (config is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.String,
                ConvertString = static (ReadOnlySpan<char> input, bool _, out object? value) =>
                {
                    value = input.AsString();
                    return true;
                }
            };
        }

        /// <inheritdoc/>
        public override bool IsSupported(Type type) => type == typeof(string);
    }
}
