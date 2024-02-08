/********************************************************************************
* UntypedDeserializationContext.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Solti.Utils.Json
{
    using Internals;

    /// <summary>
    /// Context used to create untyped result.
    /// </summary>
    /// <remarks>In untyped result objects are returned as <see cref="IDictionary"/> while lists as <see cref="IList"/>.</remarks>
    public static class UntypedDeserializationContext
    {
        public static DeserializationContext Instance { get; } = new()
        {
            SupportedTypes = JsonDataTypes.Any,

            CreateRawObject = static () => new Dictionary<string, object?>(StringComparer.Ordinal),

            CreateRawList = static () => new List<object?>(),

            GetListItemContext = static _ => Instance! with
            {
                Push = static (object instance, object? val) =>
                {
                    if (instance is not List<object?> lst)
                        throw new NotSupportedException();

                    lst.Add(val);
                }
            },

            GetPropertyContext = static (ReadOnlySpan<char> prop, StringComparison _) =>
            {
                string propStr = prop.AsString();

                return Instance! with
                {
                    Push = (object instance, object? val) =>
                    {
                        if (instance is not Dictionary<string, object?> dict)
                            throw new NotSupportedException();
                        dict[propStr] = val;
                    }
                };
            }
        };
    }
}
