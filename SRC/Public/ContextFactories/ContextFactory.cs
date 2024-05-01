/********************************************************************************
* ContextFactory.cs                                                             *
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
    /// Base class of serialization context factories.
    /// </summary>
    public abstract class ContextFactory
    {
        /// <summary>
        /// Creates the concrete <see cref="DeserializationContext"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which we want to create the context.</param>
        /// <param name="config">User provided custom configuration</param>
        protected virtual DeserializationContext CreateDeserializationContextCore(Type type, object? config) =>
            throw new NotImplementedException();

        /// <summary>
        /// Determines if the given <paramref name="type"/> is supported for deserialization.
        /// </summary>
        public virtual bool IsDeserializationSupported(Type type) => false;

        /// <summary>
        /// Creates the concrete <see cref="SerializationContext"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which we want to create the context.</param>
        /// <param name="config">User provided custom configuration</param>
        protected virtual SerializationContext CreateSerializationContextCore(Type type, object? config) =>
            throw new NotImplementedException();

        /// <summary>
        /// Determines if the given <paramref name="type"/> is supported for serialization.
        /// </summary>
        public virtual bool IsSerializationSupported(Type type) => false;

        /// <summary>
        /// Creates the concrete <see cref="DeserializationContext"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which we want to create the context.</param>
        /// <param name="config">User provided custom configuration</param>
        public DeserializationContext CreateDeserializationContext(Type type, object? config = null)
        {
            if (!IsDeserializationSupported(type))
                throw new ArgumentException(INVALID_TYPE, nameof(type));
            return CreateDeserializationContextCore(type, config);
        }

        /// <summary>
        /// Creates the concrete <see cref="SerializationContext"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which we want to create the context.</param>
        /// <param name="config">User provided custom configuration</param>
        public SerializationContext CreateSerializationContext(Type type, object? config = null)
        {
            if (!IsSerializationSupported(type))
                throw new ArgumentException(INVALID_TYPE, nameof(type));
            return CreateSerializationContextCore(type, config);
        }

        private static TContext CreateContextFor<TContext>(Type type, object? config, Func<Type, object?, ContextFactory, TContext> createContext, Func<Type, ContextFactory, bool> isSupported)
        {
            ContextAttribute? attr = type.GetCustomAttribute<ContextAttribute>(inherit: true);
            if (attr is not null)
                return createContext(type, config ?? attr.Config, attr.ContextFactory);

            ContextFactory? fact = Globals.First(fact => isSupported(type, fact));
            if (fact is null)
            {
                NotSupportedException ex = new(NOT_SERIALIZABLE);
                ex.Data[nameof(type)] = type;
                throw ex;
            }

            return createContext(type, config, fact);
        }

        /// <summary>
        /// Creates the <see cref="DeserializationContext"/> for a particular <paramref name="type"/>.
        /// </summary>
        public static DeserializationContext CreateDeserializationContextFor(Type type, object? config = null) => CreateContextFor
        (
            type,
            config,
            static (type, conf, fact) => fact.CreateDeserializationContext(type, conf),
            static (type, fact) => fact.IsDeserializationSupported(type)
        );

        /// <summary>
        /// Creates the <see cref="SerializationContext"/> for a particular <paramref name="type"/>.
        /// </summary>
        public static SerializationContext CreateSerializationContextFor(Type type, object? config = null) => CreateContextFor
        (
            type,
            config,
            static (type, conf, fact) => fact.CreateSerializationContext(type, conf),
            static (type, fact) => fact.IsSerializationSupported(type)
        );

        /// <summary>
        /// Built in factories. You can tweak this list to customize global serialization behaviors.
        /// </summary>
        /// <remarks>To provide local serialization behavior, use the <see cref="ContextAttribute"/> class.</remarks>
        public static IList<ContextFactory> Globals { get; } =
        [
            new StringContextFactory(),
            new NumberContextFactory(),
            new BooleanContextFactory(),
            new EnumContextFactory(),
            new DateTimeContextFactory(),
            new GuidContextFactory(),
            new DictionaryContextFactory(),
            new StreamContextFactory(),
            new ListContextFactory(),
            new ObjectContextFactory(),
            new UntypedContextFactory()
        ];
    }
}
