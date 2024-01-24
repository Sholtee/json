/********************************************************************************
* IJsonReaderContext.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    public interface IJsonReaderContext
    {
        /// <summary>
        /// Gets the nested context belongs to the property being parsed.
        /// </summary>
        IJsonReaderContext GetNestedContext(ReadOnlySpan<char> property, StringComparison comparison);

        /// <summary>
        /// GEts the nested context belongs to the list item being parsed.
        /// </summary>
        IJsonReaderContext GetNestedContext(int index);

        /// <summary>
        /// Method to be called when a comment section has been parsed successfully.
        /// </summary>
        /// <remarks>This method gets invoked only when the <see cref="JsonReaderFlags.AllowComments"/> flag is set.</remarks>
        void CommentParsed(ReadOnlySpan<char> value);

        /// <summary>
        /// Creates a raw list or object instance.
        /// </summary>
        object CreateRawObject(JsonDataTypes jsonDataType);

        /// <summary>
        /// Returns the supported data types.
        /// </summary>
        JsonDataTypes SupportedTypes { get; }

        /// <summary>
        /// Runs custom validators against the given <paramref name="value"/>.
        /// </summary>
        /// <remarks>You can implement custom validations here for instance you can check if a value falls in a particular range or an object has all its required properties set.</remarks>
        void Verify(object? value);

        /// <summary>
        /// Updates the given <paramref name="instance"/>.
        /// </summary>
        /// <remarks>This method will never receive <see cref="string"/> <paramref name="value"/>s.</remarks>
        void SetValue(object instance, object? value);

        /// <summary>
        /// Converts the given string <paramref name="value"/> to a user specified type. For instance <see cref="string"/>, <see cref="DateTime"/> or <see cref="Guid"/>.
        /// </summary>
        object ConvertString(ReadOnlySpan<char> value);
    }
}
