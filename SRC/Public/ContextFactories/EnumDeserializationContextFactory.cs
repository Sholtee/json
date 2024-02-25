/********************************************************************************
* EnumDeserializationContextFactory.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
    /// Creates context for <see cref="Enum"/> deserialization.
    /// </summary>
    public class EnumDeserializationContextFactory: DeserializationContextFactory
    {
        #region Private
        private delegate string AsStringDelegate(ReadOnlySpan<char> input);

        private delegate bool CheckValidDelegate(object? input, ref int @int);

        private static FutureDelegate<ConvertStringDelegate> CreateConvertStringDelegate(Type type, DelegateCompiler compiler)
        {
            Type valueType = type.IsConstructedGenericType
                ? type.GetGenericArguments().Single()
                : type;
 
            ParameterExpression
                input      = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(input)),
                ignoreCase = Expression.Parameter(typeof(bool), nameof(ignoreCase)),
                result     = Expression.Parameter(typeof(object).MakeByRefType(), nameof(result)),
                ret        = Expression.Parameter(valueType, nameof(ret));

            MethodCallExpression tryParseExpr;

            MethodInfo? tryParse = typeof(Enum)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault
                (
                    static m =>
                    {
                        if (m.Name != nameof(Enum.TryParse) || !m.ContainsGenericParameters)
                            return false;

                        ParameterInfo[] paramz = m.GetParameters();
                        if (paramz.Length != 3)
                            return false;

                        return
                            paramz[0].ParameterType == typeof(ReadOnlySpan<char>) &&
                            paramz[1].ParameterType == typeof(bool) &&
                            paramz[2].ParameterType == m.GetGenericArguments()[0].MakeByRefType();
                    }
                );
            if (tryParse is not null)
            {
                tryParseExpr = Expression.Call
                (
                    tryParse.MakeGenericMethod(valueType),
                    input,
                    ignoreCase,
                    ret
                );
            }
            else
            {
                tryParse = MethodInfoExtractor
                    .Extract<int>(static val => Enum.TryParse(default, false, out val))
                    .GetGenericMethodDefinition()
                    .MakeGenericMethod(valueType);

                tryParseExpr = Expression.Call
                (
                    tryParse,
                    Expression.Invoke
                    (
                        Expression.Constant((AsStringDelegate)Internals.MemoryExtensions.AsString),
                        input
                    ),
                    ignoreCase,
                    ret
                );
            }

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
            return compiler.Register(convertStringExpr);
        }

        private static FutureDelegate<ConvertDelegate> CreateConvertDelegate(Type type, DelegateCompiler compiler)
        {
            Type valueType = type.IsConstructedGenericType
                ? type.GetGenericArguments().Single()
                : type;

            ParameterExpression
                input = Expression.Parameter(typeof(object), nameof(input)),
                converted = Expression.Parameter(typeof(object).MakeByRefType(), nameof(converted)),
                @int = Expression.Variable(typeof(int), nameof(@int));

            LabelTarget exit = Expression.Label(typeof(bool), nameof(exit));

            Expression
                convert = Expression.Convert(@int, valueType),
                returnImmediately = Expression.TypeIs(input, valueType);

            if (valueType != type)
            {
                convert = Expression.Convert(convert, type);
                returnImmediately = Expression.Or
                (
                    returnImmediately,
                    Expression.Or
                    (
                        Expression.Equal
                        (
                            input,
                            Expression.Default(typeof(object))
                        ),
                        Expression.TypeIs(input, type)
                    )
                );
            }

            Expression<ConvertDelegate> expr = Expression.Lambda<ConvertDelegate>
            (
                Expression.Block
                (
                    variables: [@int],
                    Expression.IfThen
                    (
                        returnImmediately,
                        ifTrue: Expression.Block
                        (
                            Expression.Assign(converted, input),
                            Expression.Goto(exit, Expression.Constant(true))
                        )
                    ),
                    Expression.IfThen
                    (
                        Expression.Not
                        (
                            Expression.Call
                            (
                                ((CheckValidDelegate) (CheckValid<int>)).Method.GetGenericMethodDefinition().MakeGenericMethod(valueType),
                                input,
                                @int
                            )
                        ),
                        ifTrue: Expression.Goto(exit, Expression.Constant(false))
                    ),
                    Expression.Assign(converted, Expression.Convert(convert, converted.Type)),
                    Expression.Label(exit, Expression.Constant(true))
                ),
                input,
                converted
            );

            Debug.WriteLine(expr.GetDebugView());
            return compiler.Register(expr);

            static bool CheckValid<TEnum>(object? input, ref int @int) => 
                input is long lng &&
                lng <= int.MaxValue &&
                lng >= int.MinValue &&
                Enum.IsDefined(typeof(TEnum), @int = (int) lng);
        }

        private static DeserializationContext? CreateContextCore(Type type) => Cache.GetOrAdd(type, static type =>
        {
            DelegateCompiler compiler = new();

            FutureDelegate<ConvertStringDelegate> convertString = CreateConvertStringDelegate(type, compiler);

            FutureDelegate<ConvertDelegate> convert = CreateConvertDelegate(type, compiler);

            compiler.Compile();

            JsonDataTypes supportedType = JsonDataTypes.String | JsonDataTypes.Number;
            if (type.IsConstructedGenericType)
                supportedType |= JsonDataTypes.Null;

            return (DeserializationContext?) new DeserializationContext
            {
                SupportedTypes = supportedType,
                ConvertString = convertString.Value,
                Convert = convert.Value
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
            type?.IsEnum is true || 
            (
                type?.IsConstructedGenericType is true &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                type.GetGenericArguments().Single().IsEnum
            );
    }
}
