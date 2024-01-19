/********************************************************************************
* IJsonReaderContext.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.JSON
{
    public interface IJsonReaderContext
    {
        /// <summary>
        /// Throws if the operation should be cancelled.
        /// </summary>
        void ThrowIfCancellationRequested();

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
        /// Creates a raw list or object according to the actual state.
        /// </summary>
        object CreateRawObject();

        /// <summary>
        /// Updates the given <paramref name="obj"/> according to the actual state. <paramref name="obj"/> must be created by the <see cref="CreateRawObject"/> method.
        /// </summary>
        void SetValue(object obj, object? value);

        /// <summary>
        /// Updates the given <paramref name="obj"/> according to the actual state. <paramref name="obj"/> must be created by the <see cref="CreateRawObject"/> method.
        /// </summary>
        void SetValue(object obj, ReadOnlySpan<char> value);

        /// <summary>
        /// Gets the memory block in a given <paramref name="length"/>.
        /// </summary>
        /// <remarks>The implementation should use the same memory block to solve all the requests so the returned buffer can be resized by calling <see cref="GetBuffer(int)"/> again.</remarks>
        Span<char> GetBuffer(int length);

        int Row { get; set; }

        int Column { get; set; }
    }
}
