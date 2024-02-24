/********************************************************************************
* DeserializationContext.Untyped.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

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
        public static readonly DeserializationContext Untyped = Default with
        {
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
