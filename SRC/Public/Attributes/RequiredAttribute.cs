/********************************************************************************
* RequiredAttribute.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Json.Attributes
{
    /// <summary>
    /// Checks if the given property is not null
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class RequiredAttribute : ValidatorAttribute
    {
        /// <summary>
        /// The validator logic
        /// </summary>
        public override bool Validate(object? value, string? name, out ICollection<string> errors)
        {
            if (value is null)
            {
                errors = [$"Value of \"{name}\" cannot be null"];
                return false;
            }

            errors = null!;
            return true;
        }
    }
}
