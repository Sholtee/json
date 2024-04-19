/********************************************************************************
* HashHelpers.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Json.Internals
{
    internal static class HashHelpers
    {
        public delegate int GetHashCodeDelegate(ReadOnlySpan<char> input);

        public static new readonly GetHashCodeDelegate GetHashCode = GetGetHashCodeDelegate();

        private static GetHashCodeDelegate GetGetHashCodeDelegate()
        {
            //
            // Modern systems must have this delegate built in
            //

            MethodInfo getHashCode = typeof(string).GetMethod
            (
                nameof(GetHashCode),
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(ReadOnlySpan<char>), typeof(StringComparison)],
                null
            );

            if (getHashCode is null)
                return MemoryExtensions.GetXxHashCode;

            ParameterExpression input = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(input));
            return Expression.Lambda<GetHashCodeDelegate>
            (
                Expression.Call
                (
                    null,
                    getHashCode,
                    input,
                    Expression.Constant(StringComparison.OrdinalIgnoreCase)
                ),
                input
            ).Compile();
        }
    }
}
