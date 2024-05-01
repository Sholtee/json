/********************************************************************************
* DeserializationContext.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Json
{
    public readonly partial struct DeserializationContext // TODO: convert record type
    {
        #region Delegates
        /// <summary>
        /// Gets the nested context belongs to the property being parsed. If returns false the property and all its children won't be processed.
        /// </summary>
        public delegate bool GetPropertyContextDelegate(ReadOnlySpan<char> property, bool ignoreCase, out DeserializationContext context);

        /// <summary>
        /// Gets the nested context belongs to the list item being parsed. If returns false the list item and all its children won't be processed.
        /// </summary>
        public delegate bool GetListItemContextDelegate(int index, out DeserializationContext context);

        public delegate void ParseCommentDelegate(ReadOnlySpan<char> value);

        public delegate object RawObjectFavtoryDelegate();

        public delegate bool VerifyDelegate(object? value, out ICollection<string> errors);

        public delegate void PushDelegate(object instance, object? value);

        public delegate bool ParseNumberDelegate(ReadOnlySpan<char> value, bool integral, out object parsed);

        public delegate bool ConvertStringDelegate(ReadOnlySpan<char> value, bool ignoreCase, out object? converted);

        public delegate bool ConvertDelegate(object? value, out object? converted);
        #endregion

        /// <summary>
        /// If supported, gets the nested context belongs to the property being parsed. If returns null the property and all its children won't be processed.
        /// </summary>
        public GetPropertyContextDelegate? GetPropertyContext { get; init; }

        /// <summary>
        /// If supported, gets the nested context belongs to the list item being parsed. If returns null the list item and all its children won't be processed.
        /// </summary>
        public GetListItemContextDelegate? GetListItemContext { get; init; }

        /// <summary>
        /// Method to be called when a comment section has been parsed successfully.
        /// </summary>
        /// <remarks>This method gets invoked only when the <see cref="JsonParserFlags.AllowComments"/> flag is set.</remarks>
        public ParseCommentDelegate? ParseComment { get; init; }

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
        public ConvertStringDelegate? ConvertString { get; init; }

        /// <summary>
        /// Parses the given number
        /// </summary>
        public ParseNumberDelegate? ParseNumber { get; init; }

        /// <summary>
        /// Converts the given value to a user specified type. For instance you can implement <see cref="int"/> to <see cref="DateTime"/> conversation here.
        /// </summary>
        public ConvertDelegate? Convert { get; init; }

        /// <summary>
        /// Shortcut for <see cref="ContextFactory.CreateDeserializationContext(Type, object?)"/>
        /// </summary>
        public static DeserializationContext For(Type type, object? config = null) => ContextFactory.CreateDeserializationContextFor(type, config);
    }
}
