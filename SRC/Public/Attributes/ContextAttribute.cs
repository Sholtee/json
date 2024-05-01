/********************************************************************************
* ContextAttribute.cs                                                           *
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
    public class ContextAttribute: Attribute
    {
        public ContextAttribute(Type contextFactory)
        {
            if (contextFactory is null || contextFactory.IsAbstract || !typeof(ContextFactory).IsAssignableFrom(contextFactory))
                throw new ArgumentException(string.Format(Culture, INVALID_CONTEXT, nameof(contextFactory)), nameof(contextFactory));
            ContextFactory = (ContextFactory) Activator.CreateInstance(contextFactory);
        }

        /// <summary>
        /// The <see cref="Json.ContextFactory"/> used to create the actual context.
        /// </summary>
        public ContextFactory ContextFactory { get; }

        /// <summary>
        /// Config to be passed to the <see cref="ContextFactory.CreateDeserializationContext(Type, object?)"/> method.
        /// </summary>
        public object? Config { get; init; }
    }

    /// <summary>
    /// Specifies the serialization context to be used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ContextAttribute<TContextFactory> : ContextAttribute where TContextFactory : ContextFactory, new()
    {
        public ContextAttribute(): base(typeof(TContextFactory))
        {
        }
    }
}
