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

            EnumEntries = static val =>
            {
                switch (val)
                {
                    case IList<object?> lst:
                    {
                        IEnumerator<object?>? values = lst.GetEnumerator(); 
                        return (out Entry entry) =>
                        {
                            if (values is not null)
                            {
                                if (values.MoveNext())
                                {
                                    entry = new Entry(in Untyped, values.Current);
                                    return true;
                                }
                                values.Dispose();
                                values = null;
                            }
                            entry = default;
                            return false;
                        };
                    }
                    case IDictionary<string, object?> dict:
                    {
                        IEnumerator<string>? keys = dict.Keys.GetEnumerator();
                        return (out Entry entry) =>
                        {
                            if (keys is not null)
                            {
                                if (keys.MoveNext())
                                {
                                    entry = new Entry(in Untyped, dict[keys.Current], keys.Current);
                                    return true;
                                }
                                keys.Dispose();
                                keys = null;
                            }
                            entry = default;
                            return false;
                        };
                    }
                    default: throw new ArgumentException(INVALID_VALUE, nameof(val));
                }
            }!
        };
    }
}
