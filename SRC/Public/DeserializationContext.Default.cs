/********************************************************************************
* DeserializationContext.Default.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    public partial record DeserializationContext
    {
        /// <summary>
        /// Default deserialization context. It doesn't produce any valid output.
        /// </summary>
        public static readonly DeserializationContext Default = new()
        {
            SupportedTypes = JsonDataTypes.Any,
            ConvertString = static (ReadOnlySpan<char> chars, bool ignoreCase, out object? val) =>
            {
                val = null!;
                return true;
            },
            ParseNumber = static (ReadOnlySpan<char> value, bool integral, out object parsed) =>
            {
                parsed = null!;
                return true;
            },
            CreateRawObject = static () => null!,
            CreateRawList = static () => null!,
            GetListItemContext = static (int _, out DeserializationContext context) =>
            {
                context = Default!;
                return false;
            },
            GetPropertyContext = static (ReadOnlySpan<char> prop, bool ignoreCase, out DeserializationContext context) =>
            {
                context = Default!;
                return false;
            },
            Push = static (object instance, object? value) => {}
        };
    }
}
