/********************************************************************************
* ListContextFactory.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json
{
    using Internals;
    using Primitives;

    using static DeserializationContext;
    using static SerializationContext;
    using static Properties.Resources;

    /// <summary>
    /// Creates context for <see cref="List{TValue}"/> deserialization.
    /// </summary>
    public class ListContextFactory : ContextFactory
    {
        #region Private
        private static DeserializationContext CreateDeserializationContextCore(Type type) => (DeserializationContext) Cache.GetOrAdd(type, static type =>
        {
            DeserializationContext itemContext = CreateDeserializationContextFor(type.GetGenericArguments()[0]);

            return (object) new DeserializationContext
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
                        Push = static (object instance, object? val) =>
                        {
                            IList lst = instance as IList ?? throw new ArgumentException(INVALID_INSTANCE, nameof(val)); 
                            lst.Add(val); 
                        }
                    };
                    return true;
                }
            };
        });

        protected static SerializationContext CreateSerializationContextCore(Type type) => (SerializationContext) Cache.GetOrAdd(type, static type =>
        {
            Type itemType = type.GetGenericArguments()[0];
            SerializationContext itemContext = CreateSerializationContextFor(itemType);

            DelegateCompiler compiler = new();
            FutureDelegate<GetTypeDelegate> getTypeOf = DelegateHelpers.ChangeType<GetTypeDelegate>(GetTypeOf<object>, type, compiler);
            FutureDelegate<EnumEntriesDelegate> enumEntries = DelegateHelpers.ChangeType<EnumEntriesDelegate>(EnumEntries<object>, itemType, compiler);
            compiler.Compile();

            return (object) new SerializationContext
            {
                GetTypeOf = getTypeOf.Value,
                EnumEntries = enumEntries.Value,
                ConvertToString = (object? val, ref char[] _) => val is null
                    ? Consts.NULL.AsSpan()
                    : throw new ArgumentException(INVALID_INSTANCE, nameof(val))
            };

            static JsonDataTypes GetTypeOf<T>(object? val) => val switch
            {
                T => JsonDataTypes.List,
                null => JsonDataTypes.Null,
                _ => JsonDataTypes.Unkown
            };

            IEnumerable<Entry> EnumEntries<T>(object items)
            {
                if (items is not IEnumerable<T> @enum)
                    throw new ArgumentException(INVALID_INSTANCE, nameof(items));

                foreach (T item in @enum)
                {
                    yield return new Entry(itemContext, item);
                }
            }
        });

        private static readonly HashSet<Type> FSupportedTypes = 
        [
            typeof(List<>),
            typeof(IList<>),
            typeof(IReadOnlyList<>),
            typeof(IEnumerable<>),
            typeof(ICollection<>)
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupported(Type type) =>
            type?.IsConstructedGenericType is true &&
            FSupportedTypes.Contains(type.GetGenericTypeDefinition());
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
