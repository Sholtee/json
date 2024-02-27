/********************************************************************************
* IgnoreAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json.Attributes
{
    /// <summary>
    /// Marks a property to be skipped during serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class IgnoreAttribute: Attribute
    {
    }
}
