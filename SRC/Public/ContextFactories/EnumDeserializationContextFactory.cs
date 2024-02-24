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

    public class EnumDeserializationContextFactory: DeserializationContextFactory
    {
        #region Private
        private delegate string AsStringDelegate(ReadOnlySpan<char> input);

        private static ConvertStringDelegate CreateConvertStringDelegate(Type type)
        {
            ParameterExpression
                input = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(input)),
                ignoreCase = Expression.Parameter(typeof(bool), nameof(ignoreCase)),
                result = Expression.Parameter(typeof(object).MakeByRefType(), nameof(result)),
                ret = Expression.Parameter(type, nameof(ret));

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
                    tryParse.MakeGenericMethod(type),
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
                    .MakeGenericMethod(type);

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
                                Expression.Convert(ret, typeof(object))
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

        private static Func<object, object> CreateConvertDelegate(Type type)
        {

            ParameterExpression num = Expression.Parameter(typeof(object), nameof(num));
            Expression<Func<object, object>> convertExpr = Expression.Lambda<Func<object, object>>
            (
                Expression.Convert
                (
                    Expression.Convert
                    (
                        num,
                        type
                    ),
                    typeof(object)
                ),
                num
            );
            Debug.WriteLine(convertExpr.GetDebugView());
            return convertExpr.Compile();
        }
        #endregion

        public override DeserializationContext CreateContext(Type type, object? config = null)
        {
            EnsureValidType(type);

            if (config is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            ConvertStringDelegate convertString = CreateConvertStringDelegate(type);

            Func<object, object> convert = CreateConvertDelegate(type);

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.String | JsonDataTypes.Number,
                ConvertString = convertString,
                Convert = (object? input, out object? ret) =>
                {
                    if (input is Enum)
                    {
                        //
                        // ConvertString already did the task
                        //

                        ret = input;
                        return true;
                    }

                    if (input is not long lng || !Enum.IsDefined(type, (int) lng))
                    {
                        ret = null;
                        return false;
                    }

                    ret = convert((int) lng);
                    return true;
                }
            };
        }

        public override bool IsSupported(Type type) => type?.IsEnum is true;
    }
}
