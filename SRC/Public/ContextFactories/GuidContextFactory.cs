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
    using Internals;

    using static Internals.Consts;
    using static Properties.Resources;

    /// <summary>
    /// Creates context for <see cref="Guid"/> [de]serialization.
    /// </summary>
    public class GuidContextFactory : ContextFactory
    {
        private static string FDefaultConfig = "N";

        private static readonly string[] FValidStyles = ["N", "D", "B", "P", "X"];

        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            string? format = (config ?? DefaultConfig) as string;
            if (Array.IndexOf(FValidStyles, format) is -1)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

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
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            string? format = (config ?? "N") as string;
            if (Array.IndexOf(FValidStyles, format) is -1)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return new SerializationContext
            {
                GetTypeOf = val => val switch
                {
                    Guid => JsonDataTypes.String,
                    null when type == typeof(Guid?) => JsonDataTypes.Null,
                    _ => JsonDataTypes.Unkown
                },
                ConvertToString = (object? val, ref char[] buffer) => val switch
                {
                    Guid guid => ToString(in guid, ref buffer),
                    null when type == typeof(Guid?) => NULL.AsSpan(),
                    _ => throw new ArgumentNullException(nameof(val), INVALID_INSTANCE)
                }
            };

            ReadOnlySpan<char> ToString(scoped in Guid guid, ref char[] buffer)
            {
                //
                // https://learn.microsoft.com/en-us/dotnet/api/system.guid.tostring?view=net-8.0#system-guid-tostring(system-string-system-iformatprovider)
                //

                const int MIN_BUFFER_SIZE = 68;

                if (buffer.Length < MIN_BUFFER_SIZE)
                    buffer = new char[MIN_BUFFER_SIZE];

                return guid.Format(format!, buffer, CultureInfo.InvariantCulture);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupported(Type type) => type == typeof(Guid) || type == typeof(Guid?);

        public static string DefaultConfig
        {
            get => FDefaultConfig;
            set
            {
                if (Array.IndexOf(FValidStyles, value) is -1)
                    throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(value));
                FDefaultConfig = value;
            }
        }

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => IsSupported(type);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => IsSupported(type);
    }
}
