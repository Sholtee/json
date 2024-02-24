/********************************************************************************
* DeserializationContextFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Json
{
    using static Properties.Resources;

    /// <summary>
    /// Base class of factories responsible for creating <see cref="DeserializationContext"/> instances
    /// </summary>
    public abstract class DeserializationContextFactory
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

        //
        // TODO: Make it extensible
        //

        public static IList<DeserializationContextFactory> Globals { get; } = 
        [
            new StringDeserializationContextFactory(),
            new NumberDeserializationContextFactory(),
            new BooleanDeserializationContextFactory(),
            new EnumDeserializationContextFactory(),
            new DateTimeDeserializationContextFactory(),
            new GuidDeserializationContextFactory(),
            new StreamDeserializationContextFactory(),
            new ObjectDeserializationContextFactory()
        ];

        public static DeserializationContext CreateFor(Type type)
        {
            //
            // TODO: check if the "type" has its own context defined by an attribute
            //

            DeserializationContextFactory? fact = Globals.FirstOrDefault(fact => fact.IsSupported(type));
            if (fact is null)
            {
                NotSupportedException ex = new(NOT_SERIALIZABLE);
                ex.Data[nameof(type)] = type;
                throw ex;
            }

            //
            // TODO: How to utilize CreateContext.config parameter?
            //

            return fact.CreateContext(type, null);
        }
    }
}
