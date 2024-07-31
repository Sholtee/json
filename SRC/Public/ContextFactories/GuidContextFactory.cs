/********************************************************************************
* GuidContextFactory.cs                                                         *
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
    /// Creates context for <see cref="Guid"/> [de]serialization.
    /// </summary>
    public class GuidContextFactory : ContextFactory
    {
        private delegate bool TryParseDelegate(ReadOnlySpan<char> input, out Guid parsed);

        private static string ValidateConfig(object config)
        {
            try
            {
                string format = (string) config;
                Guid.Empty.ToString(format);
                return format;
            }
            catch
            {
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));
            }
        }

        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            string? format = config is not null
                ? ValidateConfig(config)
                : null;

            TryParseDelegate parser = format is not null ? TryParseExact : TryParse;

            return type == typeof(Guid?)
                ? new DeserializationContext
                {
                    SupportedTypes = JsonDataTypes.String | JsonDataTypes.Null,
                    ConvertString = (ReadOnlySpan<char> input, bool _, out object? value) =>
                    {
                        if (parser(input, out Guid parsed))
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
                        if (parser(input, out Guid parsed))
                        {
                            value = parsed;
                            return true;
                        }

                        value = null;
                        return false;
                    }
                };

            bool TryParseExact(ReadOnlySpan<char> input, out Guid parsed) => Guid.TryParseExact
            (
#if NETSTANDARD2_1_OR_GREATER
                input,
#else
                input.ToString(),
#endif
                format,
                out parsed
            );

            static bool TryParse(ReadOnlySpan<char> input, out Guid parsed) => Guid.TryParse
            (
#if NETSTANDARD2_1_OR_GREATER
                input,
#else
                input.ToString(),
#endif
                out parsed
            );
        }

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            string? format = config is not null
                ? ValidateConfig(config)
                : null;

            return new SerializationContext
            {
                GetTypeOf = val => val switch
                {
                    Guid => JsonDataTypes.String,
                    null when type == typeof(Guid?) => JsonDataTypes.Null,
                    _ => JsonDataTypes.Unkown
                },
                ConvertToString = (object? val, Buffer<char> buffer) => val switch
                {
                    Guid guid => guid.ToString(format, CultureInfo.InvariantCulture).AsSpan(),
                    null when type == typeof(Guid?) => NULL.AsSpan(),
                    _ => throw new ArgumentNullException(nameof(val), INVALID_INSTANCE)
                }
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupported(Type type) => type == typeof(Guid) || type == typeof(Guid?);

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => IsSupported(type);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => IsSupported(type);
    }
}
