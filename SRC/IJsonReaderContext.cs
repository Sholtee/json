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
        /// Notifies the implementor about state change (when processing a nested property)
        /// </summary>
        void PushState(ReadOnlySpan<char> property, StringComparison comparison);

        /// <summary>
        /// Reverts the actual state (triggered after a nested property parsed successfully).
        /// </summary>
        void PopState();

        /// <summary>
        /// Creates a new list object taking the actual state into account.
        /// </summary>
        object CreateList();

        /// <summary>
        /// Extends the given list.
        /// </summary>
        /// <remarks>This method may throw for instance if the <paramref name="value"/> is incompatible.</remarks>
        void PushItem(object list, object? value);

        /// <summary>
        /// Creates a new object taking the actual state into account.
        /// </summary>
        object CreateObject();

        /// <summary>
        /// Sets the given property.
        /// </summary>
        /// <remarks>This method may throw for instance if the <paramref name="value"/> is incompatible.</remarks>
        void SetProperty(object obj, ReadOnlySpan<char> property, object? value);
    }
}
