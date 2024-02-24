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
        public override DeserializationContext CreateContext(Type type, object? config = null)
        {
            EnsureValidType(type);

            string? format = (config ?? "N") as string;
            if (Array.IndexOf(FValidStyles, format) is -1)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.String,
                ConvertString = (ReadOnlySpan<char> input, bool _, out object? value) =>
                {

                    if
                    (
                        Guid.TryParseExact
                        (
#if NETSTANDARD2_1_OR_GREATER
                            input,
#else
                            input.AsString(),
#endif
                            format,
                            out Guid parsed
                        )
                    )
                    {
                        value = parsed;
                        return true;
                    }

                    value = null;
                    return false;
                }
            };
        }

        /// <inheritdoc/>
        public override bool IsSupported(Type type) => type == typeof(Guid);
    }
}
