/********************************************************************************
* IJsonReaderContext.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    public enum ObjectKind
    {
        List,
        Object
    }

    public interface IJsonReaderContext
    {
        /// <summary>
        /// Notifies the state machine if a new property is being parsed.
        /// </summary>
        /// <remarks>If the method returns false then the property value won't be parsed and <see cref="PopState"/> won't be called</remarks>
        bool PushState(ReadOnlySpan<char> property, StringComparison comparison);

        /// <summary>
        /// Reverts the actual state (triggered after a nested property parsed successfully).
        /// </summary>
        void PopState();

        /// <summary>
        /// Method to be called when a comment section has been parsed successfully.
        /// </summary>
        /// <remarks>This method gets invoked only when the <see cref="JsonReaderFlags.AllowComments"/> flag is set.</remarks>
        void CommentParsed(ReadOnlySpan<char> value);

        /// <summary>
        /// Creates a raw list or object according to the actual state.
        /// </summary>
        object CreateRawObject(ObjectKind objectKind);

        /// <summary>
        /// Updates the given <paramref name="obj"/> according to the actual state. <paramref name="obj"/> must be created by the <see cref="CreateRawObject(ObjectKind)"/> method.
        /// </summary>
        void SetValue(object obj, object? value);

        /// <summary>
        /// Updates the given <paramref name="obj"/> according to the actual state. <paramref name="obj"/> must be created by the <see cref="CreateRawObject(ObjectKind)"/> method.
        /// </summary>
        void SetValue(object obj, ReadOnlySpan<char> value);
    }
}
