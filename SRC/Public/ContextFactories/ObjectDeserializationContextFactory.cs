﻿/********************************************************************************
* ObjectDeserializationContextFactory.cs                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Json
{
    using Internals;
    using Primitives;
    using Properties;

    using static DeserializationContext;

    /// <summary>
    /// Creates context for object deserialization.
    /// </summary>
    public class ObjectDeserializationContextFactory : DeserializationContextFactory
    {
        #region Private
        private static StringKeyedDictionary<DeserializationContext> ProcessProperties(Type type, DelegateCompiler compiler)
        {
            StringKeyedDictionary<DeserializationContext> props = new();
            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!prop.CanWrite)
                    continue;

                //
                // TODO: check if the "prop" has its own context defined by an attribute
                //

                ParameterExpression
                    instance = Expression.Parameter(typeof(object), nameof(instance)),
                    value = Expression.Parameter(typeof(object), nameof(value));

                FutureDelegate<PushDelegate> push = compiler.Register
                (
                    Expression.Lambda<PushDelegate>
                    (
                        Expression.Assign
                        (
                            Expression.Property
                            (
                                Expression.Convert(instance, type),
                                prop
                            ),
                            Expression.Convert(value, prop.PropertyType)
                        ),
                        instance,
                        value
                    )
                );

                props.Add
                (
                    prop.Name,
                    DeserializationContextFactory.CreateFor(prop.PropertyType) with
                    {
                        Push = (inst, val) => push.Value(inst, val)
                    }
                );
            }
            return props;
        }

        private static DeserializationContext? CreateContextCore(Type type) => Cache.GetOrAdd(type, static type =>
        {
            DelegateCompiler compiler = new();

            FutureDelegate<RawObjectFavtoryDelegate> createRaw = compiler.Register
            (
                Expression.Lambda<RawObjectFavtoryDelegate>
                (
                    Expression.New
                    (
                        type.GetConstructor(Type.EmptyTypes)
                    )
                )
            );

            StringKeyedDictionary<DeserializationContext> props = ProcessProperties(type, compiler);

            compiler.Compile();

            return (DeserializationContext?) new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.Object | JsonDataTypes.Null,
                CreateRawObject = () => createRaw.Value(),
                GetPropertyContext = props.TryGetValue
            };
        });
        #endregion

        /// <inheritdoc/>
        protected override DeserializationContext CreateContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateContextCore(type)!.Value;
        }

        /// <inheritdoc/>
        public override bool IsSupported(Type type) =>
            type?.IsClass is true && type.GetConstructor(Type.EmptyTypes) is not null;
    }
}