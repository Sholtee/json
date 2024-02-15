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

    public sealed partial record SerializationContext
    {
        public static SerializationContext Untyped { get; } = new()
        {
            ConvertToString = static val => val switch
            {
                bool b => b ? TRUE : FALSE,  // b.ToString() should be lowercased
                null => NULL,
                _ => Convert.ToString(val, CultureInfo.InvariantCulture)
            },

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

            EnumListEntries = EnumListEntriesImpl,

            EnumObjectEntries = EnumObjectEntriesImpl,
        };

        private static IEnumerable<(SerializationContext?, object?)> EnumListEntriesImpl(object val)
        {
            if (val is not IList<object?> lst)
                throw new ArgumentException(INVALID_VALUE, nameof(val));

            foreach (object? item in lst)
            {
                yield return (Untyped, item);
            }
        }

        private static IEnumerable<(SerializationContext?, string, object?)> EnumObjectEntriesImpl(object val)
        {
            if (val is not IDictionary<string, object?> lst)
                throw new ArgumentException(INVALID_VALUE, nameof(val));

            foreach (KeyValuePair<string, object?> item in lst)
            {
                yield return (Untyped, item.Key, item.Value);
            }
        }
    }
}
