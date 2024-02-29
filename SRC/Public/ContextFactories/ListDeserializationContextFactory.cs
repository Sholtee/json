/********************************************************************************
* ListDeserializationContextFactory.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solti.Utils.Json
{
    using Primitives;

    using static DeserializationContext;
    using static Properties.Resources;

    /// <summary>
    /// Creates context for <see cref="Dictionary{TKey, TValue}"/> deserialization.
    /// </summary>
    public class ListDeserializationContextFactory : DeserializationContextFactory
    {
        private static DeserializationContext? CreateContextCore(Type type) => Cache.GetOrAdd(type, static type =>
        {
            DeserializationContext itemContext = DeserializationContextFactory.CreateFor(type.GetGenericArguments()[0]);

            return (DeserializationContext?) new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.List | JsonDataTypes.Null,

                CreateRawList = Expression.Lambda<RawObjectFavtoryDelegate>
                (
                    Expression.New(typeof(List<>).MakeGenericType(type.GetGenericArguments()))
                ).Compile(),

                GetListItemContext = (int _, out DeserializationContext context) =>
                {
                    context = itemContext with
                    {
                        Push = (object instance, object? val) =>
                        {
                            if (instance is not IList lst)
                                throw new ArgumentException(INVALID_INSTANCE, nameof(val));
                             lst.Add(val); 
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
            typeof(List<>),
            typeof(IList<>),
            typeof(IReadOnlyList<>),
            typeof(IEnumerable<>),
            typeof(ICollection<>)
        ];

        /// <inheritdoc/>
        public override bool IsSupported(Type type) =>
            type?.IsConstructedGenericType is true &&
            FSupportedTypes.Contains(type.GetGenericTypeDefinition());
    }
}
