/********************************************************************************
* DateTimeContextFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json
{
    using static Internals.Consts;
    using static Properties.Resources;

    /// <summary>
    /// Creates context for <see cref="DateTime"/> [de]serialization.
    /// </summary>
    public class DateTimeContextFactory : ContextFactory
    {
        private delegate bool TryParseDelegate(ReadOnlySpan<char> input, out DateTime parsed);

        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            string? format = config is null
                ? null 
                : config as string ?? throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            TryParseDelegate parser = format is not null ? TryParseExact : TryParse;

            return type == typeof(DateTime?)
                ? new DeserializationContext
                {
                    SupportedTypes = JsonDataTypes.String | JsonDataTypes.Null,
                    ConvertString = (ReadOnlySpan<char> input, bool _, out object? value) =>
                    {
                        if (parser(input, out DateTime parsed))
                        {
                            value = (DateTime?) parsed;
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
                        if (parser(input, out DateTime parsed))
                        {
                            value = parsed;
                            return true;
                        }
                        value = null;
                        return false;
                    }
                };
    
            bool TryParseExact(ReadOnlySpan<char> input, out DateTime parsed) => DateTime.TryParseExact
            (
#if NETSTANDARD2_1_OR_GREATER
                input,
#else
                input.ToString(),
#endif
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out parsed
            );

            static bool TryParse(ReadOnlySpan<char> input, out DateTime parsed) => DateTime.TryParse
            (
#if NETSTANDARD2_1_OR_GREATER
                input,
#else
                input.ToString(),
#endif
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out parsed
            );
        }

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            string? format = config is null
                ? null
                : config as string ?? throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return new SerializationContext
            {
                GetTypeOf = val => val switch
                {
                    DateTime => JsonDataTypes.String,
                    null when type == typeof(DateTime?) => JsonDataTypes.Null,
                    _ => JsonDataTypes.Unkown
                },
                ConvertToString = (object? val, ref char[] buffer) => val switch
                {
                    DateTime dt => dt.ToString(format, CultureInfo.InvariantCulture).AsSpan(),
                    null when type == typeof(DateTime?) => NULL.AsSpan(),
                    _ => throw new ArgumentNullException(nameof(val), INVALID_INSTANCE)
                }
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupported(Type type) => type == typeof(DateTime) || type == typeof(DateTime?);

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => IsSupported(type);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => IsSupported(type);
    }
}
