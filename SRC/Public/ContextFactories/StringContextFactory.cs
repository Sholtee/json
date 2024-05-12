/********************************************************************************
* StringContextFactory.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json
{
    using Internals;

    using static Properties.Resources;

    /// <summary>
    /// Creates context for <see cref="string"/> [de]serialization.
    /// </summary>
    public class StringContextFactory : ContextFactory
    {
        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.String | JsonDataTypes.Null,
                ConvertString = static (ReadOnlySpan<char> input, bool _, out object? value) =>
                {
                    value = input.ToString();
                    return true;
                }
            };
        }

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return new SerializationContext
            {
                GetTypeOf = static val => val switch
                {
                    string => JsonDataTypes.String,
                    null => JsonDataTypes.Null,
                    _ => JsonDataTypes.Unkown
                },
                ConvertToString = static (object? val, ref char[] buffer) => val switch
                {
                    string str => str.AsSpan(),
                    null => Consts.NULL.AsSpan(),
                    _ => throw new ArgumentException(INVALID_INSTANCE, nameof(val))
                }
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupported(Type type) => type == typeof(string);

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => IsSupported(type);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => IsSupported(type);
    }
}
