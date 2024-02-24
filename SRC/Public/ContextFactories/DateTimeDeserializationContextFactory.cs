/********************************************************************************
* DateTimeDeserializationContextFactory.cs                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Globalization;

namespace Solti.Utils.Json
{
    using Internals;
    using Properties;

    /// <summary>
    /// Creates context for <see cref="DateTime"/> deserialization.
    /// </summary>
    public class DateTimeDeserializationContextFactory : DeserializationContextFactory
    {
        /// <inheritdoc/>
        protected override DeserializationContext CreateContextCore(Type type, object? config = null)
        {
            string? format = (config ?? "s") as string ?? throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.String,
                ConvertString = (ReadOnlySpan<char> input, bool _, out object? value) =>
                {

                    if
                    (
                        DateTime.TryParseExact
                        (
#if NETSTANDARD2_1_OR_GREATER
                            input,
#else
                            input.AsString(),
#endif
                            format,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                            out DateTime parsed
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
        public override bool IsSupported(Type type) => type == typeof(DateTime);
    }
}
