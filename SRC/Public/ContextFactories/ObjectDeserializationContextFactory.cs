/********************************************************************************
* ObjectDeserializationContextFactory.cs                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Json
{
    using Attributes;
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
        private static readonly MethodInfo 
            FValidate = MethodInfoExtractor.Extract<ValidatorAttribute, ICollection<string>>(static (va, errs) => va.Validate(null, out errs)),
            FAddRange = MethodInfoExtractor.Extract<List<string>>(static lst => lst.AddRange(default));

        private static void ProcessProperties(Type type, DelegateCompiler compiler, out StringKeyedDictionary<DeserializationContext> props, out FutureDelegate<VerifyDelegate>? verifyDelegate)
        {
            props = new();
            
            ParameterExpression
                instanceParam = Expression.Parameter(typeof(object), nameof(instanceParam)),
                errorsParam   = Expression.Parameter(typeof(ICollection<string>).MakeByRefType(), nameof(errorsParam)),
                instance      = Expression.Variable(type, nameof(instance)),
                errors        = Expression.Variable(typeof(List<string>), nameof(errors)),
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

                SerializationContextAttribute? fact = prop.GetCustomAttribute<SerializationContextAttribute>(inherit: true);

                DeserializationContext deserializationContext = fact is not null
                    ? fact.ContextFactory.CreateContext(prop.PropertyType, fact.Config)
                    : CreateFor(prop.PropertyType);

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
                Expression<VerifyDelegate> verify = Expression.Lambda<VerifyDelegate>
                (
                    Expression.Block
                    (
                        variables: [instance, errors, errorsSection],
                        validatorBlock
                    ),
                    instanceParam,
                    errorsParam
                );
                Debug.WriteLine(verify.GetDebugView());

                verifyDelegate = compiler.Register(verify);
            }
        }

        private static DeserializationContext? CreateContextCore(Type type) => Cache.GetOrAdd(type, static type =>
        {
            DelegateCompiler compiler = new();

            FutureDelegate<RawObjectFavtoryDelegate> createRaw = compiler.Register
            (
                Expression.Lambda<RawObjectFavtoryDelegate>
                (
                    Expression.New(type)
                )
            );

            ProcessProperties(type, compiler, out StringKeyedDictionary<DeserializationContext> props, out FutureDelegate<VerifyDelegate>? verify);

            compiler.Compile();

            return (DeserializationContext?) new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.Object | JsonDataTypes.Null,
                CreateRawObject = createRaw.Value,
                GetPropertyContext = props.TryGetValue,
                Verify = verify?.Value
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
