/********************************************************************************
* DeserializationContext.Untyped.cs                                             *
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

    using static Properties.Resources;

    public readonly partial struct DeserializationContext
    {
        /// <summary>
        /// Context used to create untyped result.
        /// </summary>
        /// <remarks>In untyped result objects are returned as <see cref="IDictionary"/> while lists as <see cref="IList"/>.</remarks>
        public static readonly DeserializationContext Untyped = new()
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
                context = Untyped with
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

                context = Untyped with
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
    }
}
