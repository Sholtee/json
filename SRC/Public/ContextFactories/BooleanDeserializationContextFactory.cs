/********************************************************************************
* BooleanDeserializationContextFactory.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    using Properties;

    /// <summary>
    /// Creates context for <see cref="bool"/> deserialization.
    /// </summary>
    public class BooleanDeserializationContextFactory : DeserializationContextFactory
    {
        /// <inheritdoc/>
        protected override DeserializationContext CreateContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

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
        public override bool IsSupported(Type type) => type == typeof(bool) || type == typeof(bool?);
    }
}
