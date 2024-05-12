/********************************************************************************
* DictionaryContextFactory.cs                                                   *
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
    /// Creates context for <see cref="Dictionary{TKey, TValue}"/> [de]serialization.
    /// </summary>
    public class DictionaryContextFactory : ContextFactory
    {
        #region Private
        private static DeserializationContext CreateDeserializationContextCore(Type type) => Cache.GetOrAdd(type, static type =>
        {
            DeserializationContext itemContext = CreateDeserializationContextFor(type.GetGenericArguments()[1]);

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.Object | JsonDataTypes.Null,

                ConvertString = static (ReadOnlySpan<char> input, bool _, out object? value) =>
                {
                    value = input.ToString();
                    return true;
                },

                CreateRawObject = Expression.Lambda<RawObjectFavtoryDelegate>
                (
                    Expression.New(typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments()))
                ).Compile(),

                GetPropertyContext = (ReadOnlySpan<char> prop, bool _, out DeserializationContext context) =>
                {
                    string propStr = prop.ToString();

                    context = itemContext with
                    {
                        Push = (object instance, object? val) =>
                        {
                            IDictionary dict = instance as IDictionary ?? throw new ArgumentException(INVALID_INSTANCE, nameof(val));
                            dict[propStr] = val; 
                        }
                    };
                    return true;
                }
            };
        });

        private static SerializationContext CreateSerializationContextCore(Type type) => Cache.GetOrAdd(type, static type =>
        {
            Type itemType = type.GetGenericArguments()[1];
            SerializationContext itemContext = CreateSerializationContextFor(itemType);

            DelegateCompiler compiler = new();
            FutureDelegate<GetTypeDelegate> getTypeOf = DelegateHelpers.ChangeType<GetTypeDelegate>(GetTypeOf<object>, type, compiler);
            FutureDelegate<EnumEntriesDelegate> enumEntries = DelegateHelpers.ChangeType<EnumEntriesDelegate>(EnumEntries<object>, itemType, compiler);
            compiler.Compile();

            return new SerializationContext
            {
                GetTypeOf = getTypeOf.Value,
                EnumEntries = enumEntries.Value,
                ConvertToString = (object? val, ref char[] _) => val is null
                    ? Consts.NULL.AsSpan()
                    : throw new ArgumentException(INVALID_INSTANCE, nameof(val))
            };

            static JsonDataTypes GetTypeOf<T>(object? val) => val switch
            {
                T => JsonDataTypes.Object,
                null => JsonDataTypes.Null,
                _ => JsonDataTypes.Unkown
            };

            IEnumerable<Entry> EnumEntries<T>(object items)
            {
                if (items is not IEnumerable<KeyValuePair<string, T>> @enum)
                    throw new ArgumentException(INVALID_INSTANCE, nameof(items));

                foreach (KeyValuePair<string, T> item in @enum)
                {
                    yield return new Entry(itemContext, item.Value, item.Key);
                }
            }
        });

        private static readonly HashSet<Type> FSupportedTypes =
        [
            typeof(Dictionary<,>),
            typeof(IDictionary<,>),
            typeof(IReadOnlyDictionary<,>)
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSupported(Type type) =>
            type?.IsConstructedGenericType is true &&
            FSupportedTypes.Contains(type.GetGenericTypeDefinition()) &&
            type.GetGenericArguments()[0] == typeof(string);
        #endregion

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateSerializationContextCore(type);
        }

        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return CreateDeserializationContextCore(type);
        }

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => IsSupported(type);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => IsSupported(type);
    }
}
