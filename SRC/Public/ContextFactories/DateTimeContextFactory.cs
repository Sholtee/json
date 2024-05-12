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
    using Internals;

    using static Internals.Consts;
    using static Properties.Resources;

    /// <summary>
    /// Creates context for <see cref="DateTime"/> [de]serialization.
    /// </summary>
    public class DateTimeContextFactory : ContextFactory
    {
        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            string format = (config ?? DefaultConfig) as string ?? throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));
  
            return type == typeof(DateTime?)
                ? new DeserializationContext
                {
                    SupportedTypes = JsonDataTypes.String | JsonDataTypes.Null,
                    ConvertString = (ReadOnlySpan<char> input, bool _, out object? value) =>
                    {
                        if (TryParse(input, out DateTime parsed))
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
                        if (TryParse(input, out DateTime parsed))
                        {
                            value = parsed;
                            return true;
                        }
                        value = null;
                        return false;
                    }
                };
    
            bool TryParse(ReadOnlySpan<char> input, out DateTime parsed) => DateTime.TryParseExact
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
        }

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            string format = (config ?? "s") as string ?? throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

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
                    DateTime dt => ToString(in dt, ref buffer),
                    null when type == typeof(DateTime?) => NULL.AsSpan(),
                    _ => throw new ArgumentNullException(nameof(val), INVALID_INSTANCE)
                }
            };

            ReadOnlySpan<char> ToString(scoped in DateTime dt, ref char[] buffer)
            {
                const int MIN_BUFFER_SIZE = 128;

                if (buffer.Length < MIN_BUFFER_SIZE)
                    buffer = new char[MIN_BUFFER_SIZE];

                return dt.Format(format, buffer, CultureInfo.InvariantCulture);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupported(Type type) => type == typeof(DateTime) || type == typeof(DateTime?);

        public static string DefaultConfig { get; set; } = "s";

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => IsSupported(type);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => IsSupported(type);
    }
}
