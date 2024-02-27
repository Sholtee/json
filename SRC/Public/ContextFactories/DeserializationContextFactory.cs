/********************************************************************************
* DeserializationContextFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Json
{
    using Attributes;
    using static Properties.Resources;

    /// <summary>
    /// Base class of factories responsible for creating <see cref="DeserializationContext"/> instances
    /// </summary>
    public abstract class DeserializationContextFactory  // TODO: rework to SerializationContextFactory supporting both direction
    {
        /// <summary>
        /// Creates the concrete <see cref="DeserializationContext"/>.
        /// </summary>
        protected abstract DeserializationContext CreateContextCore(Type type, object? config);

        /// <summary>
        /// Creates the concrete <see cref="DeserializationContext"/>.
        /// </summary>
        public DeserializationContext CreateContext(Type type, object? config = null)
        {
            if (!IsSupported(type))
                throw new ArgumentException(INVALID_TYPE, nameof(type));
            return CreateContextCore(type, config);
        }

        /// <summary>
        /// Determines if the given <paramref name="type"/> is supported.
        /// </summary>
        public abstract bool IsSupported(Type type);


        public static IList<DeserializationContextFactory> Globals { get; } = 
        [
            new StringDeserializationContextFactory(),
            new NumberDeserializationContextFactory(),
            new BooleanDeserializationContextFactory(),
            new EnumDeserializationContextFactory(),
            new DateTimeDeserializationContextFactory(),
            new GuidDeserializationContextFactory(),
            new DictionaryDeserializationContextFactory(),
            new StreamDeserializationContextFactory(),
            new ObjectDeserializationContextFactory()
        ];

        /// <summary>
        /// Creates the <see cref="DeserializationContext"/> for a particular <paramref name="type"/>.
        /// </summary>
        public static DeserializationContext CreateFor(Type type)
        {
            SerializationContextAttribute? attr = type.GetCustomAttribute<SerializationContextAttribute>(inherit: true);
            if (attr is not null)
                return attr.ContextFactory.CreateContext(type, attr.Config);
                
            DeserializationContextFactory? fact = Globals.FirstOrDefault(fact => fact.IsSupported(type));
            if (fact is null)
            {
                NotSupportedException ex = new(NOT_SERIALIZABLE);
                ex.Data[nameof(type)] = type;
                throw ex;
            }

            return fact.CreateContext(type);
        }
    }
}
