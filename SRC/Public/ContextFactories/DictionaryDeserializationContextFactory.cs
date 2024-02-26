/********************************************************************************
* DictionaryDeserializationContextFactory.cs                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solti.Utils.Json
{
    using Internals;
    using Primitives;

    using static DeserializationContext;
    using static Properties.Resources;

    /// <summary>
    /// Creates context for <see cref="Dictionary{TKey, TValue}"/> deserialization.
    /// </summary>
    public class DictionaryDeserializationContextFactory : DeserializationContextFactory
    {
        private static DeserializationContext? CreateContextCore(Type type) => Cache.GetOrAdd(type, static type =>
        {
            DeserializationContext itemContext = DeserializationContextFactory.CreateFor(type.GetGenericArguments()[1]);

            return (DeserializationContext?) new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.Object | JsonDataTypes.Null,

                ConvertString = static (ReadOnlySpan<char> input, bool _, out object? value) =>
                {
                    value = input.AsString();
                    return true;
                },

                CreateRawObject = Expression.Lambda<RawObjectFavtoryDelegate>
                (
                    Expression.New(typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments()))
                ).Compile(),

                GetPropertyContext = (ReadOnlySpan<char> prop, bool _, out DeserializationContext context) =>
                {
                    string propStr = prop.AsString();

                    context = itemContext with
                    {
                        Push = (object instance, object? val) =>
                        {
                            if (instance is not IDictionary dict)
                                throw new ArgumentException(INVALID_INSTANCE, nameof(val));
                            dict[propStr] = val; 
                        }
                    };
                    return true;
                }
            };
        });

        /// <inheritdoc/>
        protected override DeserializationContext CreateContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateContextCore(type)!.Value;
        }

        private static readonly HashSet<Type> FSupportedTypes = 
        [
            typeof(Dictionary<,>),
            typeof(IDictionary<,>),
            typeof(IReadOnlyDictionary<,>)
        ];

        /// <inheritdoc/>
        public override bool IsSupported(Type type) =>
            type?.IsConstructedGenericType is true &&
            FSupportedTypes.Contains(type.GetGenericTypeDefinition()) &&
            type.GetGenericArguments()[0] == typeof(string);
    }
}
