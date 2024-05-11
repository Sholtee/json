/********************************************************************************
* SerializationContext.Default.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Json
{
    public partial record SerializationContext
    {
        /// <summary>
        /// Default context. Using this context instructs the system to skip the fragment of object tree on which the writer is positioned.
        /// </summary>
        public static readonly SerializationContext Default = new()
        {
            GetTypeOf = _ => JsonDataTypes.Unkown
        };
    }
}
