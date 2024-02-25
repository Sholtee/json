/********************************************************************************
* NumberDeserializationContextFactory.cs                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Json
{
    using Primitives;
    using Properties;

    using static DeserializationContext;

    /// <summary>
    /// Creates context for number (<see cref="byte"/>, <see cref="int"/>, <see cref="float"/>, etc) deserialization.
    /// </summary>
    public class NumberDeserializationContextFactory : DeserializationContextFactory
    {
        #region Private
        private static readonly HashSet<Type> FSupportedTypes =
        [
            typeof(byte),
            typeof(ushort),  typeof(short),
            typeof(uint),    typeof(int),
            typeof(ulong),   typeof(long),
            typeof(float),
            typeof(double),

            typeof(byte?),
            typeof(ushort?), typeof(short?),
            typeof(uint?),   typeof(int?),
            typeof(ulong?),  typeof(long?),
            typeof(float?),
            typeof(double?)
        ];

        private static ConvertDelegate CreateConvertDelegate(Type type)
        {
            Type
                valueType = type.IsConstructedGenericType
                    ? type.GetGenericArguments().Single()
                    : type,
                expectedType = valueType == typeof(float) || valueType == typeof(double)
                    ? typeof(double)
                    : typeof(long);

            ParameterExpression
                value     = Expression.Parameter(typeof(object), nameof(value)),
                converted = Expression.Parameter(typeof(object).MakeByRefType(), nameof(converted)),
                tmp       = Expression.Variable(expectedType, nameof(tmp));

            LabelTarget exit = Expression.Label(typeof(bool), nameof(exit));

            List<Expression> block = [];

            if (valueType != type) block.Add
            (
                Expression.IfThen
                (
                    Expression.Equal(value, Expression.Default(typeof(object))),
                    ifTrue: Expression.Block
                    (
                        Expression.Assign(converted, value),
                        Expression.Goto(exit, Expression.Constant(true))
                    )
                )
            );

            block.AddRange
            ([
                Expression.IfThen
                (
                    Expression.Not
                    (
                        Expression.TypeIs(value, expectedType)
                    ),
                    ifTrue: Expression.Goto(exit, Expression.Constant(false))
                ),
                Expression.Assign
                (
                    tmp,
                    Expression.Convert
                    (
                        value,
                        expectedType
                    )
                ),
                Expression.IfThen
                (
                    Expression.Or
                    (
                        Expression.LessThan
                        (
                            tmp,
                            GetConstant(nameof(int.MinValue))
                        ),
                        Expression.GreaterThan
                        (
                            tmp,
                            GetConstant(nameof(int.MaxValue))
                        )
                    ),
                    ifTrue: Expression.Goto(exit, Expression.Constant(false))
                ),
                Expression.Assign
                (
                    converted,
                    Expression.Convert
                    (
                        valueType != type
                            ? Expression.Convert
                            (
                                Expression.Convert(tmp, valueType),
                                type
                            )
                            : Expression.Convert(tmp, valueType),
                        typeof(object)
                    )
                ),
                Expression.Label(exit, Expression.Constant(true))
            ]);

            Expression<ConvertDelegate> expr = Expression.Lambda<ConvertDelegate>
            (
                Expression.Block
                (
                    variables: [tmp],
                    block
                ),
                value,
                converted
            );

            Debug.WriteLine(expr.GetDebugView());
            return expr.Compile();

            Expression GetConstant(string name) => Expression.Convert
            (
                Expression.Constant
                (
                    valueType
                        .GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Single(f => f.IsLiteral && f.Name == name)
                        .GetValue(null)
                ),
                expectedType
            );
        }

        private static DeserializationContext? CreateContextCore(Type type) => Cache.GetOrAdd(type, static type => (DeserializationContext?) new DeserializationContext
        {
            SupportedTypes = type.IsConstructedGenericType
                    ? JsonDataTypes.Number | JsonDataTypes.Null
                    : JsonDataTypes.Number,
            Convert = CreateConvertDelegate(type)
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
        public override bool IsSupported(Type type) => FSupportedTypes.Contains(type);
    }
}
