/********************************************************************************
* GuidDeserializationContextFactory.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    using Internals;
    using Properties;

    /// <summary>
    /// Creates context for <see cref="Guid"/> deserialization.
    /// </summary>
    public class GuidDeserializationContextFactory : DeserializationContextFactory
    {
        private static readonly string[] FValidStyles = ["N", "D", "B", "P", "X"];

        /// <inheritdoc/>
        protected override DeserializationContext CreateContextCore(Type type, object? config)
        {
            string? format = (config ?? "N") as string;
            if (Array.IndexOf(FValidStyles, format) is -1)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return type == typeof(Guid?)
                ? new DeserializationContext
                {
                    SupportedTypes = JsonDataTypes.String | JsonDataTypes.Null,
                    ConvertString = (ReadOnlySpan<char> input, bool _, out object? value) =>
                    {
                        if (TryParse(input, out Guid parsed))
                        {
                            value = (Guid?) parsed;
                            return true;
                        }

                        value = null;
                        return false;
                    }
                }
                : new DeserializationContext
                {
                    SupportedTypes = JsonDataTypes.String,
                    ConvertString = (ReadOnlySpan<char> input, bool _, out object? value) =>
                    {
                        if (TryParse(input, out Guid parsed))
                        {
                            value = parsed;
                            return true;
                        }

                        value = null;
                        return false;
                    }
                };

            bool TryParse(ReadOnlySpan<char> input, out Guid parsed) => Guid.TryParseExact
            (
#if NETSTANDARD2_1_OR_GREATER
                input,
#else
                input.AsString(),
#endif
                format,
                out parsed
            );
        }

        /// <inheritdoc/>
        public override bool IsSupported(Type type) => type == typeof(Guid) || type == typeof(Guid?);
    }
}
