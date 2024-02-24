/********************************************************************************
* DeserializationContextFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    using Properties;

    /// <summary>
    /// Base class of factories responsible for creating <see cref="DeserializationContext"/> instances
    /// </summary>
    public abstract class DeserializationContextFactory
    {
        /// <summary>
        /// Throws if the given type is not supported by this factory.
        /// </summary>
        protected void EnsureValidType(Type type)
        {
            if (!IsSupported(type))
                throw new ArgumentException(Resources.INVALID_TYPE, nameof(type));
        }

        /// <summary>
        /// Creates the concrete <see cref="DeserializationContext"/>.
        /// </summary>
        public abstract DeserializationContext CreateContext(Type type, object? config = null);

        /// <summary>
        /// Determines if the given <paramref name="type"/> is supported.
        /// </summary>
        public abstract bool IsSupported(Type type);
    }
}
