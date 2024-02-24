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

    public class DateTimeDeserializationContextFactory : DeserializationContextFactory
    {
        public override DeserializationContext CreateContext(Type type, object? config = null)
        {
            EnsureValidType(type);
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

        public override bool IsSupported(Type type) => type == typeof(DateTime);
    }
}
