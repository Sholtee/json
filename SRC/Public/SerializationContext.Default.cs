/********************************************************************************
* SerializationContext.Default.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Json
{
    public readonly partial struct SerializationContext
    {
        /// <summary>
        /// Default context. Using this context instructs the system to skip the fragment of object tree on which the writer is positioned.
        /// </summary>
        public static readonly SerializationContext Default = default;
    }
}
