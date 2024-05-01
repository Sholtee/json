/********************************************************************************
* ObjectContextFactory.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Json
{
    using Attributes;
    using Internals;
    using Primitives;

    using static Properties.Resources;
    using static DeserializationContext;
    using static SerializationContext;

    /// <summary>
    /// Creates context for object [de]serialization.
    /// </summary>
    public class ObjectContextFactory : ContextFactory
    {
        #region Private
        private static readonly MethodInfo
            FValidate = MethodInfoExtractor.Extract<ValidatorAttribute, ICollection<string>>(static (va, errs) => va.Validate(null, null, out errs)),
            FAddRange = MethodInfoExtractor.Extract<List<string>>(static lst => lst.AddRange(default));

        private static void ProcessPropertiesForDeserialization(Type type, DelegateCompiler compiler, out StringKeyedDictionary<DeserializationContext> props, out FutureDelegate<VerifyDelegate>? verifyDelegate)
        {
            props = new();

            ParameterExpression
                instanceParam = Expression.Parameter(typeof(object), nameof(instanceParam)),
                errorsParam = Expression.Parameter(typeof(ICollection<string>).MakeByRefType(), nameof(errorsParam)),
                instance = Expression.Variable(type, nameof(instance)),
                errors = Expression.Variable(typeof(List<string>), nameof(errors)),
                errorsSection = Expression.Variable(typeof(ICollection<string>), nameof(errorsSection));

            List<Expression> validatorBlock = [];

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                if (!prop.CanWrite || !prop.CanRead || prop.GetCustomAttribute<IgnoreAttribute>() is not null)
                    continue;

                ParameterExpression
                    rawInstance = Expression.Parameter(typeof(object), nameof(rawInstance)),
                    value = Expression.Parameter(typeof(object), nameof(value));

                FutureDelegate<PushDelegate> push = compiler.Register
                (
                    Expression.Lambda<PushDelegate>
                    (
                        Expression.Assign
                        (
                            Expression.Property
                            (
                                Expression.Convert(rawInstance, type),
                                prop
                            ),
                            Expression.Convert(value, prop.PropertyType)
                        ),
                        rawInstance,
                        value
                    )
                );

                ContextAttribute? contextAttr = prop.GetCustomAttribute<ContextAttribute>(inherit: true);

                DeserializationContext deserializationContext = contextAttr is not null
                    ? contextAttr.ContextFactory.CreateDeserializationContext(prop.PropertyType, contextAttr.Config)
                    : CreateDeserializationContextFor(prop.PropertyType);

                props.Add
                (
                    prop.GetCustomAttribute<AliasAttribute>()?.Name ?? prop.Name,
                    deserializationContext with
                    {
                        Push = (inst, val) => push.Value(inst, val)
                    }
                );

                foreach (ValidatorAttribute validatorAttribute in prop.GetCustomAttributes<ValidatorAttribute>(inherit: true))
                {
                    if (validatorBlock.Count is 0) validatorBlock.AddRange
                    ([
                        Expression.Assign(errors, Expression.Constant(null, typeof(List<string>))),
                        Expression.Assign(instance, Expression.Convert(instanceParam, type))
                    ]);

                    validatorBlock.AddRange
                    ([
                        Expression.IfThen
                        (
                            Expression.Not
                            (
                                Expression.Call
                                (
                                    Expression.Constant(validatorAttribute),
                                    FValidate,
                                    Expression.Convert
                                    (
                                        Expression.Property(instance, prop),
                                        typeof(object)
                                    ),
                                    Expression.Constant(prop.Name),
                                    errorsSection
                                )
                            ),
                            ifTrue: Expression.Block
                            (
                                Expression.IfThen
                                (
                                    Expression.Equal(errors, Expression.Default(errors.Type)),
                                    ifTrue: Expression.Assign(errors, Expression.New(errors.Type))
                                ),
                                Expression.Call(errors, FAddRange, errorsSection)
                            )
                        )

                    ]);
                }
            }

            if (validatorBlock.Count is 0)
                verifyDelegate = null;
            else
            {
                validatorBlock.AddRange
                ([
                    Expression.Assign(errorsParam, errors),
                    Expression.Equal(errors, Expression.Default(errors.Type))  // return errors == null
                ]);

                verifyDelegate = compiler.Register
                (
                    Expression.Lambda<VerifyDelegate>
                    (
                        Expression.Block
                        (
                            variables: [instance, errors, errorsSection],
                            validatorBlock
                        ),
                        instanceParam,
                        errorsParam
                    )
                );
            }
        }

        private static IReadOnlyList<Func<object, Entry>> ProcessPropertiesForSerialization(Type type, DelegateCompiler compiler)
        {
            List<Func<object, Entry>> result = [];
            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy))
            {
                if (!prop.CanRead || prop.GetCustomAttribute<IgnoreAttribute>() is not null)
                    continue;

                ContextAttribute? contextAttr = prop.GetCustomAttribute<ContextAttribute>(inherit: true);
                SerializationContext serializationContext = contextAttr is not null
                    ? contextAttr.ContextFactory.CreateSerializationContext(prop.PropertyType, contextAttr.Config)
                    : CreateSerializationContextFor(prop.PropertyType);

                string name = prop.GetCustomAttribute<AliasAttribute>()?.Name ?? prop.Name;

                ParameterExpression obj = Expression.Parameter(typeof(object), nameof(obj));
                FutureDelegate<Func<object, object?>> getProp = compiler.Register
                (
                    Expression.Lambda<Func<object, object?>>
                    (
                        Expression.Property
                        (
                            Expression.Convert(obj, type),
                            prop
                        ),
                        obj
                    )
                );

                result.Add
                (
                    obj => new Entry
                    (   
                        serializationContext,
                        getProp.Value(obj),
                        name
                    )
                );
            }
            return result;
        }

        private static DeserializationContext CreateDeserializationContextCore(Type type) => (DeserializationContext) Cache.GetOrAdd(type, static type =>
        {
            DelegateCompiler compiler = new();

            FutureDelegate<RawObjectFavtoryDelegate> createRaw = compiler.Register
            (
                Expression.Lambda<RawObjectFavtoryDelegate>
                (
                    Expression.New(type)
                )
            );

            ProcessPropertiesForDeserialization(type, compiler, out StringKeyedDictionary<DeserializationContext> props, out FutureDelegate<VerifyDelegate>? verify);

            compiler.Compile();

            return (object) new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.Object | JsonDataTypes.Null,
                CreateRawObject = createRaw.Value,
                GetPropertyContext = props.TryGetValue,
                Verify = verify?.Value
            };
        });

        private static SerializationContext CreateSerializationContextCore(Type type) => (SerializationContext) Cache.GetOrAdd(type, static type =>
        {
            DelegateCompiler compiler = new();
            IReadOnlyList<Func<object, Entry>> props = ProcessPropertiesForSerialization(type, compiler);
            FutureDelegate<GetTypeDelegate> getTypeOf = DelegateHelpers.ChangeType<GetTypeDelegate>(GetTypeOf<object>, type, compiler);
            FutureDelegate<EnumEntriesDelegate> enumEntries = DelegateHelpers.ChangeType<EnumEntriesDelegate>(EnumEntries<object>, type, compiler);
            compiler.Compile();

            return (object) new SerializationContext
            {
                GetTypeOf = getTypeOf.Value,
                EnumEntries = enumEntries.Value,
                ConvertToString = (object? val, ref char[] _) => val is null
                    ? Consts.NULL.AsSpan()
                    : throw new ArgumentException(INVALID_INSTANCE, nameof(val))
            };

            static JsonDataTypes GetTypeOf<T>(object? val) => val switch
            {
                T => JsonDataTypes.Object,
                null => JsonDataTypes.Null,
                _ => JsonDataTypes.Unkown
            };

            IEnumerable<Entry> EnumEntries<T>(object val)
            {
                if (val is not T)
                    throw new ArgumentException(INVALID_INSTANCE, nameof(val));

                foreach (Func<object, Entry> fact in props)
                {
                    yield return fact(val);
                }
            }
        });
        #endregion

        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateDeserializationContextCore(type);
        }

        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateSerializationContextCore(type);
        }

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => type != typeof(object);

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) =>
            type?.IsAbstract is false &&
            type.GetConstructor(Type.EmptyTypes) is not null &&
            type != typeof(object);
    }
}
