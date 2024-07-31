/********************************************************************************
* NumberContextFactory.cs                                                       *
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
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json
{
    using Internals;
    using Primitives;
    using Properties;

    using static DeserializationContext;
    using static SerializationContext;

    /// <summary>
    /// Creates context for number (<see cref="byte"/>, <see cref="int"/>, <see cref="float"/>, etc) [de]serialization.
    /// </summary>
    public class NumberContextFactory : ContextFactory
    {
        #region Private
#if !NETSTANDARD2_1_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string SpanToString(ReadOnlySpan<char> span) => span.ToString();
#endif
        private delegate string SpanToStringDelegate(ReadOnlySpan<char> span);

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

            MethodInfo tryParse = valueType.GetMethod
            (
                nameof(int.TryParse),
                BindingFlags.Public | BindingFlags.Static,
                null,
                [
#if NETSTANDARD2_1_OR_GREATER
                    typeof(ReadOnlySpan<char>),
#else
                    typeof(string),
#endif
                    typeof(NumberStyles),
                    typeof(IFormatProvider),
                    valueType.MakeByRefType()
                ],
                null
            );
            Debug.Assert(tryParse is not null, "Cannot grab the actual parser");

            ParameterExpression
                value    = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(value)),
                integral = Expression.Parameter(typeof(bool), nameof(integral)), 
                parsed   = Expression.Parameter(typeof(object).MakeByRefType(), nameof(parsed)),
                tmp      = Expression.Variable(valueType, nameof(tmp));

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
#if NETSTANDARD2_1_OR_GREATER
                                value,
#else
                                Expression.Invoke
                                (
                                    Expression.Constant((SpanToStringDelegate) SpanToString),
                                    value
                                ),
#endif
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

        private static DeserializationContext CreateDeserializationContextCore(Type type) => Cache.GetOrAdd(type, static type => new DeserializationContext
        {
            SupportedTypes = type.IsConstructedGenericType
                ? JsonDataTypes.Number | JsonDataTypes.Null
                : JsonDataTypes.Number,
            ParseNumber = CreateParseNumberDelegate(type)
        });

        private static SerializationContext CreateSerializationContextCore(Type type) => Cache.GetOrAdd(type, static type =>
        {
            bool nullable = false;
            if (type.IsConstructedGenericType)
            {
                type = type.GetGenericArguments().Single();
                nullable = true;
            }

            DelegateCompiler compiler = new();
            FutureDelegate<GetTypeDelegate> getTypeOf = DelegateHelpers.ChangeType<GetTypeDelegate>(GetTypeOf<int>, type, compiler);
            FutureDelegate<ToStringDelegate> toString = DelegateHelpers.ChangeType<ToStringDelegate>(ToString<int>, type, compiler);
            compiler.Compile();

            return new SerializationContext
            {
                GetTypeOf = getTypeOf.Value,
                ConvertToString = toString.Value,
            };

            JsonDataTypes GetTypeOf<T>(object? val) => val switch
            {
                T => JsonDataTypes.Number,
                null when nullable => JsonDataTypes.Null,
                _ => JsonDataTypes.Unkown
            };

            ReadOnlySpan<char> ToString<T>(object? val, Buffer<char> buffer) where T : IFormattable
            {
                if (val is null && nullable)
                    return Consts.NULL.AsSpan();

                if (val is not T target)
                    throw new ArgumentException(Resources.INVALID_VALUE, nameof(val));

                return target.ToString("G", CultureInfo.InvariantCulture).AsSpan();
            }
        });
#endregion

        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateDeserializationContextCore(type);
        }

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateSerializationContextCore(type);
        }

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => FSupportedTypes.Contains(type);

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => FSupportedTypes.Contains(type);
    }
}
