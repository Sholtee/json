/********************************************************************************
* NumberDeserializationContextFactory.cs                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        private delegate string AsStringDelegate(ReadOnlySpan<char> input);

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

        private static ParseNumberDelegate CreateParseNumberDelegate(Type type)
        {
            Type valueType = type.IsConstructedGenericType
                ? type.GetGenericArguments().Single()
                : type;

            bool legacy = false;
            MethodInfo? tryParse = valueType.GetMethod
            (
                nameof(int.TryParse),
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(ReadOnlySpan<char>), typeof(NumberStyles), typeof(IFormatProvider), valueType.MakeByRefType()],
                null
            );
            if (tryParse is null)
            {
                tryParse = valueType.GetMethod
                (
                    nameof(int.TryParse),
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(string), typeof(NumberStyles), typeof(IFormatProvider), valueType.MakeByRefType()],
                    null
                );
                legacy = true;
            }
            Debug.Assert(tryParse is not null, "Cannot grab the actual parser");

            ParameterExpression
                value     = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(value)),
                integral  = Expression.Parameter(typeof(bool), nameof(integral)), 
                parsed = Expression.Parameter(typeof(object).MakeByRefType(), nameof(parsed)),
                tmp       = Expression.Variable(valueType, nameof(tmp));

            LabelTarget exit = Expression.Label(typeof(bool), nameof(exit));

            Expression<ParseNumberDelegate> expr = Expression.Lambda<ParseNumberDelegate>
            (
                Expression.Block
                (
                    variables: [tmp],
                    Expression.IfThen
                    (
                        Expression.Not
                        (
                            Expression.Call
                            (
                                tryParse,
                                legacy
                                    ? Expression.Invoke
                                    (
                                        Expression.Constant((AsStringDelegate) Internals.MemoryExtensions.AsString),
                                        value
                                    )
                                    : value,
                                Expression.Constant
                                (
                                    valueType == typeof(float) || valueType == typeof(double) 
                                        ? NumberStyles.Float
                                        : NumberStyles.Number
                                ),
                                Expression.Constant(CultureInfo.InvariantCulture),
                                tmp
                            )
                        ),
                        ifTrue: Expression.Goto(exit, Expression.Constant(false))
                    ),
                    Expression.Assign
                    (
                        parsed,
                        Expression.Convert
                        (
                            valueType != type
                                ? Expression.Convert(tmp, type)
                                : tmp,
                            parsed.Type
                        )
                    ),
                    Expression.Label(exit, Expression.Constant(true))
                ),
                value,
                integral,
                parsed
            );

            Debug.WriteLine(expr.GetDebugView());
            return expr.Compile();
        }

        private static DeserializationContext? CreateContextCore(Type type) => Cache.GetOrAdd(type, static type => (DeserializationContext?) new DeserializationContext
        {
            SupportedTypes = type.IsConstructedGenericType
                    ? JsonDataTypes.Number | JsonDataTypes.Null
                    : JsonDataTypes.Number,
            ParseNumber = CreateParseNumberDelegate(type)
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
