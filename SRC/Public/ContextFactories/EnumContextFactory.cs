﻿/********************************************************************************
* EnumContextFactory.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json
{
    using Internals;
    using Primitives;

    using static Properties.Resources;

    using static DeserializationContext;
    using static SerializationContext;

    /// <summary>
    /// Creates context for <see cref="Enum"/> [de]serialization.
    /// </summary>
    public class EnumContextFactory: ContextFactory
    {
        #region Private
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string SpanToString(ReadOnlySpan<char> span) => span.ToString();

        private delegate string SpanToStringDelegate(ReadOnlySpan<char> span);

        private static ConvertStringDelegate CreateConvertStringDelegate(Type type)
        {
            Type valueType = type.IsConstructedGenericType
                ? type.GetGenericArguments().Single()
                : type;
 
            ParameterExpression
                input      = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(input)),
                ignoreCase = Expression.Parameter(typeof(bool), nameof(ignoreCase)),
                result     = Expression.Parameter(typeof(object).MakeByRefType(), nameof(result)),
                ret        = Expression.Parameter(valueType, nameof(ret));

            MethodCallExpression tryParseExpr = Expression.Call
            (
                MethodInfoExtractor
                    .Extract<int>(static val => Enum.TryParse(default, false, out val))
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(valueType),
                Expression.Invoke
                (
                    Expression.Constant((SpanToStringDelegate) SpanToString),
                    input
                ),
                ignoreCase,
                ret
            );

            Expression<ConvertStringDelegate> convertStringExpr = Expression.Lambda<ConvertStringDelegate>
            (
                Expression.Block
                (
                    type: typeof(bool),
                    [ret],
                    Expression.Condition
                    (
                        tryParseExpr,
                        ifTrue: Expression.Block
                        (
                            type: typeof(bool),
                            Expression.Assign
                            (
                                result,
                                Expression.Convert
                                (
                                    valueType != type ? Expression.Convert(ret, type) : ret,
                                    typeof(object)
                                )
                            ),
                            Expression.Constant(true)
                        ),
                        ifFalse: Expression.Constant(false)
                    )
                ),
                input,
                ignoreCase,
                result
            );

            Debug.WriteLine(convertStringExpr.GetDebugView());
            return convertStringExpr.Compile();
        }

        private static DeserializationContext CreateDeserializationContextCore(Type type) => Cache.GetOrAdd(type, static type =>
        {
            JsonDataTypes supportedType = JsonDataTypes.String;
            if (type.IsConstructedGenericType)
                supportedType |= JsonDataTypes.Null;

            return new DeserializationContext
            {
                SupportedTypes = supportedType,
                ConvertString = CreateConvertStringDelegate(type)
            };
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
            FutureDelegate<GetTypeDelegate> getTypeOf = DelegateHelpers.ChangeType<GetTypeDelegate>(GetTypeOf<Enum>, type, compiler);
            FutureDelegate<ToStringDelegate> toString = DelegateHelpers.ChangeType<ToStringDelegate>(ToString<Enum>, type, compiler);
            compiler.Compile();

            return new SerializationContext
            {
                ConvertToString = toString.Value,
                GetTypeOf = getTypeOf.Value
            };

            JsonDataTypes GetTypeOf<T>(object? val) where T: Enum => val switch
            {
                T => JsonDataTypes.String,
                null when nullable => JsonDataTypes.Null,
                _ => JsonDataTypes.Unkown
            };

            ReadOnlySpan<char> ToString<T>(object? val, Buffer<char> buffer) where T: Enum
            {
                if (val is null && nullable)
                    return Consts.NULL.AsSpan();

                if (val is not T target)
                    throw new ArgumentException(INVALID_VALUE, nameof(val));

                //
                // Works for [Flags] as well
                //

                return target.ToString("G").AsSpan();  
            }
        });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupported(Type type) => type?.IsEnum is true ||
        (
            type?.IsConstructedGenericType is true &&
            type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
            type.GetGenericArguments().Single().IsEnum
        );
        #endregion

        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateDeserializationContextCore(type);
        }

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateSerializationContextCore(type);
        }

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => IsSupported(type);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => IsSupported(type);
    }
}
