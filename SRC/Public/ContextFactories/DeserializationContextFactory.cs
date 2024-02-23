/********************************************************************************
* DeserializationContextFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    public abstract class DeserializationContextFactory
    {
        public abstract DeserializationContext CreateContext(Type type, object? config = null);
    }
}
