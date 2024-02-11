/********************************************************************************
* DeserializationContext.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Json
{
    using Internals;

    public sealed partial record DeserializationContext
    {
        #region Delegates
        public delegate DeserializationContext? GetPropertyContextDelegate(ReadOnlySpan<char> property, StringComparison comparison);

        /// <summary>
        /// Gets the nested context belongs to the list item being parsed. If returns null the item being processed will be skipped.
        /// </summary>
        public delegate DeserializationContext? GetListItemContextDelegate(int index);

        public delegate void CommentParserDelegate(ReadOnlySpan<char> value);

        public delegate object RawObjectFavtoryDelegate();

        public delegate IEnumerable<string>? VerifyDelegate(object? value);

        public delegate void PushDelegate(object instance, object? value);

        public delegate object? ConvertStringDelegate(ReadOnlySpan<char> value);

        public delegate object? ConvertNumberDelegate(object? value);
        #endregion

        /// <summary>
        /// If applicable, gets the nested context belongs to the property being parsed.
        /// </summary>
        public GetPropertyContextDelegate? GetPropertyContext { get; init; }

        /// <summary>
        /// If applicable, gets the nested context belongs to the list item being parsed.
        /// </summary>
        public GetListItemContextDelegate? GetListItemContext { get; init; }

        /// <summary>
        /// Method to be called when a comment section has been parsed successfully.
        /// </summary>
        /// <remarks>This method gets invoked only when the <see cref="JsonReaderFlags.AllowComments"/> flag is set.</remarks>
        public CommentParserDelegate? CommentParser { get; init; }

        /// <summary>
        /// Creates a rawobject instance, if supported.
        /// </summary>
        /// <remarks>If the value of this property is not null <see cref="GetPropertyContext"/> also need to be provided</remarks>
        public RawObjectFavtoryDelegate? CreateRawObject { get; init; }

        /// <summary>
        /// Creates a raw list instance, if supported.
        /// </summary>
        /// <remarks>If the value of this property is not null <see cref="GetListItemContext"/> also need to be provided</remarks>
        public RawObjectFavtoryDelegate? CreateRawList { get; init; }

        /// <summary>
        /// Returns the supported data types.
        /// </summary>
        public required JsonDataTypes SupportedTypes { get; init; }

        /// <summary>
        /// Runs custom validators against the given <paramref name="value"/>. If the delegate returns any errors the system will throw a validation exception.
        /// </summary>
        /// <remarks>You can check if a value falls in a particular range or an object has all its required properties set.</remarks>
        public VerifyDelegate? Verify { get; init; }

        /// <summary>
        /// If supported, updates the given instance created by the parent context.
        /// </summary>
        public PushDelegate? Push { get; init;}

        /// <summary>
        /// Converts the given <see cref="ReadOnlySpan{char}"/> to a user specified type, for instance <see cref="string"/>, <see cref="DateTime"/> or <see cref="Guid"/>.
        /// </summary>
        public ConvertStringDelegate ConvertString { get; init; } = static chars => chars.AsString();

        /// <summary>
        /// Converts the given value to a user specified type. For instance you can implement <see cref="int"/> to <see cref="DateTime"/> conversation here.
        /// </summary>
        public ConvertNumberDelegate? ConvertNumber { get; init; }  // TBD: universal Convert() instead?
    }
}
