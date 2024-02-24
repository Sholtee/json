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

    public class NumberDeserializationContextFactory : DeserializationContextFactory
    {
        #region Private
        private static ConvertDelegate CreateConvertDelegate(Type type)
        {
            Type expectedType = type == typeof(float) || type == typeof(double) ? typeof(double) : typeof(long);

            ParameterExpression
                value = Expression.Parameter(typeof(object), nameof(value)),
                converted = Expression.Parameter(typeof(object).MakeByRefType(), nameof(converted)),
                tmp = Expression.Variable(expectedType, nameof(tmp));

            LabelTarget exit = Expression.Label(typeof(bool), nameof(exit));

            Expression<ConvertDelegate> expr = Expression.Lambda<ConvertDelegate>
            (
                Expression.Block
                (
                    variables: [tmp],
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
                            Expression.Convert(tmp, type),
                            typeof(object)
                        )
                    ),
                    Expression.Label(exit, Expression.Constant(true))
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
                    type.GetFields(BindingFlags.Public | BindingFlags.Static).Single(f => f.IsLiteral && f.Name == name).GetValue(null)
                ),
                expectedType
            );
        }
        #endregion

        public override DeserializationContext CreateContext(Type type, object? config = null)
        {
            EnsureValidType(type);
            if (config is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.Number,
                Convert = CreateConvertDelegate(type)
            };
        }

        private static readonly HashSet<Type> FSupportedTypes =
        [
            typeof(byte),
            typeof(ushort), typeof(short),
            typeof(uint),   typeof(int),
            typeof(ulong),  typeof(long),
            typeof(float),
            typeof(double)
        ];

        public override bool IsSupported(Type type) => FSupportedTypes.Contains(type);
    }
}
