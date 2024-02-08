/********************************************************************************
* UntypedSerializationContext.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Solti.Utils.Json
{
    public static class UntypedSerializationContext
    {
        public static SerializationContext Instance { get; } = new()
        {
            ConvertToString = static val => Convert.ToString(val, CultureInfo.InvariantCulture),

            GetTypeOf = static val => val switch
            {
                IDictionary<string, object?> => JsonDataTypes.Object,
                IList<object?> => JsonDataTypes.List,
                null => JsonDataTypes.Null,
                bool => JsonDataTypes.Boolean,
                string => JsonDataTypes.String,
                ValueType when Convert.GetTypeCode(val) is >= TypeCode.SByte and <= TypeCode.Double => JsonDataTypes.Number,
                _ => JsonDataTypes.Unkown
            }
        }; 
    }
}
