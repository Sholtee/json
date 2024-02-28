/********************************************************************************
* ValidatorAttribute.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Json.Attributes
{
    /// <summary>
    /// Validates the given property
    /// </summary>
    /// <remarks>Validations run after the parent object has been parsed successfully.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public abstract class ValidatorAttribute: Attribute
    {
        /// <summary>
        /// The concrete validator to be implemented.
        /// </summary>
        public abstract bool Validate(object? value, out ICollection<string> errors);
    }
}
