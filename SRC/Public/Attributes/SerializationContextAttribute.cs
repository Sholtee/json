/********************************************************************************
* SerializationContextAttribute.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json.Attributes
{
    using static Properties.Resources;

    /// <summary>
    /// Specifies the serialization context to be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Property, AllowMultiple = false)]
    public class SerializationContextAttribute: Attribute
    {
        public SerializationContextAttribute(Type contextFactory)
        {
            if (contextFactory is null || contextFactory.IsAbstract || !typeof(DeserializationContextFactory).IsAssignableFrom(contextFactory))
                throw new ArgumentException(string.Format(Culture, INVALID_CONTEXT, nameof(contextFactory)), nameof(contextFactory));
            ContextFactory = (DeserializationContextFactory) Activator.CreateInstance(contextFactory);
        }

        /// <summary>
        /// The <see cref="DeserializationContextFactory"/> used to create the actual context.
        /// </summary>
        public DeserializationContextFactory ContextFactory { get; }

        /// <summary>
        /// Config to be passed to the <see cref="DeserializationContextFactory.CreateContext(Type, object?)"/> method.
        /// </summary>
        public object? Config { get; init; }
    }

    /// <summary>
    /// Specifies the serialization context to be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SerializationContextAttribute<TContextFactory> : SerializationContextAttribute where TContextFactory : DeserializationContextFactory, new()
    {
        public SerializationContextAttribute(): base(typeof(TContextFactory))
        {
        }
    }
}
