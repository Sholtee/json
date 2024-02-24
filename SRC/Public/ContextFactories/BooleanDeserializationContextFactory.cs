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
        public override DeserializationContext CreateContext(Type type, object? config = null)
        {
            EnsureValidType(type);
            if (config is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.Boolean
            };
        }

        /// <inheritdoc/>
        public override bool IsSupported(Type type) => type == typeof(bool);
    }
}
