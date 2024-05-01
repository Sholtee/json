/********************************************************************************
* DelegateHelpers.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Json.Internals
{
    using Primitives;

    internal static class DelegateHelpers
    {
        public static FutureDelegate<TDelegate> ChangeType<TDelegate>(this TDelegate del, Type t, DelegateCompiler compiler) where TDelegate : Delegate
        {
            MethodInfo m = del
                .Method
                .GetGenericMethodDefinition()
                .MakeGenericMethod(t);

            ParameterExpression[] paramz = m
                .GetParameters()
                .Select(static p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();

            return compiler.Register
            (
                Expression.Lambda<TDelegate>
                (
                    Expression.Call
                    (
                        del.Target is not null
                            ? Expression.Constant(del.Target, del.Target.GetType())
                            : null,
                        m,
                        paramz
                    ),
                    paramz
                )
            );
        }
    }
}
