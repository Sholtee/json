﻿/********************************************************************************
* DeserializationContext.Default.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Json
{
    public readonly partial struct DeserializationContext
    {
        /// <summary>
        /// Default deserialization context. It doesn't produce any output.
        /// </summary>
        public static DeserializationContext Default { get; } = new DeserializationContext
        {
            SupportedTypes = JsonDataTypes.Any
        };
    }
}