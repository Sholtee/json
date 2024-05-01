/********************************************************************************
* UntypedContextFactory.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Solti.Utils.Json
{
    using Internals;

    using static Internals.Consts;
    using static Properties.Resources;
    using static SerializationContext;

    /// <summary>
    /// Creates context for untyped result
    /// </summary>
    /// <remarks>In untyped objects are returned as <see cref="IDictionary"/> while lists as <see cref="IList"/>.</remarks>
    public class UntypedContextFactory : ContextFactory
    {
        #region Private
        private static readonly DeserializationContext FUntypedDeserialization = new()
        {
            SupportedTypes = JsonDataTypes.Any,

            ConvertString = static (ReadOnlySpan<char> chars, bool ignoreCase, out object? val) =>
            {
                val = chars.AsString();
                return true;
            },

            ParseNumber = static (ReadOnlySpan<char> value, bool integral, out object parsed) =>
            {
                parsed = null!;
                if (integral)
                {
                    if
                    (
                        long.TryParse
                        (
#if NETSTANDARD2_1_OR_GREATER
                            value,
#else
                            value.AsString(),
#endif
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out long ret
                        )
                    )
                        parsed = ret;
                }
                else
                {
                    if
                    (
                        double.TryParse
                        (
#if NETSTANDARD2_1_OR_GREATER
                            value,
#else
                            value.AsString(),
#endif
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double ret
                        )
                    )
                        parsed = ret;
                }
                return parsed != null;
            },

            CreateRawObject = static () => new Dictionary<string, object?>(StringComparer.Ordinal),

            CreateRawList = static () => new List<object?>(),

            GetListItemContext = static (int _, out DeserializationContext context) =>
            {
                context = FUntypedDeserialization with
                {
                    Push = static (object instance, object? val) =>
                    {
                        if (instance is not List<object?> lst)
                            throw new ArgumentException(INVALID_INSTANCE, nameof(val));
                        lst.Add(val);
                    }
                };
                return true;
            },

            GetPropertyContext = static (ReadOnlySpan<char> prop, bool _, out DeserializationContext context) =>
            {
                string propStr = prop.AsString();

                context = FUntypedDeserialization with
                {
                    Push = (object instance, object? val) =>
                    {
                        if (instance is not Dictionary<string, object?> dict)
                            throw new ArgumentException(INVALID_INSTANCE, nameof(val));
                        dict[propStr] = val;
                    }
                };
                return true;
            }
        };

        private static readonly SerializationContext FUntypedSerialization = new()
        {
            ConvertToString = static (object? val, ref char[] _) =>
            (
                val switch
                {
                    bool b => b ? TRUE : FALSE,  // b.ToString() should be lowercased
                    null => NULL,
                    _ => Convert.ToString(val, CultureInfo.InvariantCulture)
                }
            ).AsSpan(),

            GetTypeOf = static (object? val) => Convert.GetTypeCode(val) switch
            {
                TypeCode.Empty => JsonDataTypes.Null,
                TypeCode.Boolean => JsonDataTypes.Boolean,
                TypeCode.String => JsonDataTypes.String,
                >= TypeCode.SByte and <= TypeCode.Double => JsonDataTypes.Number,
                TypeCode.Object when val is IDictionary<string, object?> => JsonDataTypes.Object,
                TypeCode.Object when val is IList<object?> => JsonDataTypes.List,
                _ => JsonDataTypes.Unkown
            },

            EnumEntries = EnumEntriesImpl
        };

        private static IEnumerable<Entry> EnumEntriesImpl(object val)  // "yield" cannot be in lambda function =(
        {
            switch (val)
            {
                case IList<object?> lst:
                    foreach (object? item in lst)
                    {
                        yield return new Entry(in FUntypedSerialization, item);
                    }
                    break;
                case IDictionary<string, object?> dict:
                    foreach (KeyValuePair<string, object?> entry in dict)
                    {
                        yield return new Entry(in FUntypedSerialization, entry.Value, entry.Key);
                    }
                    break;
                default: throw new ArgumentException(INVALID_VALUE, nameof(val));
            }
        }
        #endregion

        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return FUntypedDeserialization;
        }

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return FUntypedSerialization;
        }

        private static bool IsSupported(Type type) => type == typeof(object);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => IsSupported(type);

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) => IsSupported(type);
    }
}
