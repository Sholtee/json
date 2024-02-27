/********************************************************************************
* AliasAttribute.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json.Attributes
{
    /// <summary>
    /// Specifies the alternate name for the marked property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class AliasAttribute : Attribute
    {
        /// <summary>
        /// The name to be used
        /// </summary>
        public required string Name { get; init; }
    }
}
