/********************************************************************************
* DeserializationContextFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    using Properties;

    public abstract class DeserializationContextFactory
    {
        protected void EnsureValidType(Type type)
        {
            if (!IsSupported(type))
                throw new ArgumentException(Resources.INVALID_TYPE, nameof(type));
        }

        public abstract DeserializationContext CreateContext(Type type, object? config = null);

        public abstract bool IsSupported(Type type);
    }
}
