/********************************************************************************
* SerializationContext.Untyped.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Solti.Utils.Json
{
    using static Internals.Consts;
    using static Properties.Resources;

    public readonly partial struct SerializationContext
    {
        public static readonly SerializationContext Untyped = new()
        {
            ConvertToString = static (val, _) =>
            (
                val switch
                {
                    bool b => b ? TRUE : FALSE,  // b.ToString() should be lowercased
                    null => NULL,
                    _ => Convert.ToString(val, CultureInfo.InvariantCulture)
                }
            ).AsSpan(),

            GetTypeOf = static val => Convert.GetTypeCode(val) switch
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

        private static IEnumerable<Entry> EnumEntriesImpl(object val)
        {
            switch (val)
            {
                case IList<object?> lst:
                    foreach (object? item in lst)
                    {
                        yield return new Entry(in Untyped, item);
                    }
                    break;
                case IDictionary<string, object?> dict:
                    foreach (KeyValuePair<string, object?> entry in dict)
                    {
                        yield return new Entry(in Untyped, entry.Value, entry.Key);
                    }
                    break;
                default: throw new ArgumentException(INVALID_VALUE, nameof(val));
            }
        }
    }
}
