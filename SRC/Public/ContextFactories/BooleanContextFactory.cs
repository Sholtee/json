/********************************************************************************
* BooleanContextFactory.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json
{
    using static Internals.Consts;
    using static Properties.Resources;

    /// <summary>
    /// Creates context for <see cref="bool"/> [de]serialization.
    /// </summary>
    public class BooleanContextFactory : ContextFactory
    {
        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return type == typeof(bool?)
                ? new DeserializationContext
                {
                    SupportedTypes = JsonDataTypes.Boolean | JsonDataTypes.Null,
                    Convert = static (object? val, out object? converted) =>
                    {
                        converted = val is bool b ? new bool?(b) : null;
                        return true;
                    }
                }
                : new DeserializationContext
                {
                    SupportedTypes = JsonDataTypes.Boolean
                };
        }

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return new SerializationContext
            {
                GetTypeOf = val => val switch
                {
                    bool => JsonDataTypes.Boolean,
                    null when type == typeof(bool?) => JsonDataTypes.Null,
                    _ => JsonDataTypes.Unkown
                },
                ConvertToString = (object? val, Buffer<char> buffer) => 
                (
                    val switch
                    {
                        true => TRUE,
                        false => FALSE,
                        null when type == typeof(bool?) => NULL,     
                        _ => throw new ArgumentNullException(nameof(val), INVALID_INSTANCE)
                    }
                ).AsSpan()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupported(Type type) => type == typeof(bool) || type == typeof(bool?);

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => IsSupported(type);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => IsSupported(type);
    }
}
