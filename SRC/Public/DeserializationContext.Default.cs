/********************************************************************************
* DeserializationContext.Default.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Json
{
    public sealed partial record DeserializationContext
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
