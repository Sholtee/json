﻿/********************************************************************************
* JsonParser.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.Diagnostics.Debug;
using static System.Char;

namespace Solti.Utils.Json
{
    using Internals;

    using static DeserializationContext;
    using static Internals.Consts; 
    using static Properties.Resources;

    /// <summary>
    /// Represents a low-level, cancellable JSON parser.
    /// </summary>
    /// <remarks>This class is thread safe.</remarks>
    public sealed class JsonParser(JsonParserFlags flags = JsonParserFlags.None, int maxDepth = 64)
    {
        #region Private
        /// <summary>
        /// Validates then increases the <paramref name="currentDepth"/>. Throws an <see cref="InvalidOperationException"/> if the current depth reached the <see cref="maxDepth"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Deeper(in Session session, int currentDepth)
        {
            if (++currentDepth > maxDepth)
                Throw(in session, MAX_DEPTH_REACHED);
            return currentDepth;
        }

        /// <summary>
        /// Throws a <see cref="JsonParserException"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Throw(in Session session, string message) => throw new JsonParserException
        (
            message,
            session.Column,
            session.Row
        );

        /// <summary>
        /// Throws a <see cref="JsonParserException"/> if the JSON input cannot be parsed.
        /// </summary>
        /// <remarks>When throwing this exception the reader is supposed to be positioned before the improper value.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvalidInput(in Session session, string type, string reason) => Throw
        (
            in session,
            string.Format(Culture, INVALID_INPUT, type, reason)
        );

        /// <summary>
        /// Throws a <see cref="JsonParserException"/> if a parsed value is invalid (for instance a validation or conversion failed)
        /// </summary>
        /// <remarks>When throwing this exception the reader is supposed to be positioned after the improper value.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InvalidValue(in Session session, params string[] errors) => Throw
        (
            in session,
            string.Format(Culture, INVALID_VALUE, string.Join(", ", errors))
        );

        /// <summary>
        /// Advances the underlying text reader. It assumes that the row remains the same.
        /// </summary>
        /// <param name="len"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Advance(ref Session session, int len)
        {
            session.Content.Advance(len);
            session.Column += len;
        }

        /// <summary>
        /// Returns true if the input is positioned on the given <paramref name="literal"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsLiteral(in Session session, string literal) => literal.AsSpan().Equals
        (
            session.Content.PeekText(literal.Length),
            flags.HasFlag(JsonParserFlags.CaseInsensitive)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal
        );

        /// <summary>
        /// Verifies the given <paramref name="delegate"/> taken from the actual context and throws an <see cref="InvalidOperationException"/> if it is null
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T VerifyDelegate<T>(T? @delegate, string handler) where T : Delegate => @delegate ?? throw new InvalidOperationException
        (
            string.Format(Culture, INVALID_CONTEXT, handler)
        );
        #endregion

        #region Internal
        internal ref struct Session(TextReaderWrapper content, in CancellationToken cancellation = default)
        {
            public readonly TextReaderWrapper Content = content;
            public readonly CancellationToken Cancellation = cancellation;
            public int Row;
            public int Column;
        }

        /// <summary>
        /// Trims all the leading whitespaces maintaining the row and column count.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SkipSpaces(ref Session session, int bufferSize = 32 /*for tests*/)
        {
            for (Span<char> read; (read = session.Content.PeekText(bufferSize)).Length > 0;)
            {
                int skip;
                for (skip = 0; skip < read.Length && IsWhiteSpace(read[skip]); skip++)
                {
                    if (read[skip] is '\n')
                    {
                        session.Row++;
                        session.Column = 0;
                    }
                    else session.Column++;
                }

                session.Content.Advance(skip);
                if (skip < read.Length)
                    break;
            }
        }

        /// <summary>
        /// Consumes the current token which the reader is positioned on.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTokens Consume(ref Session session)
        {
            SkipSpaces(ref session);

            int chr = session.Content.PeekChar();

            return chr is -1 ? JsonTokens.Eof : chr switch
            {
                '{' => JsonTokens.CurlyOpen,
                '}' => JsonTokens.CurlyClose,
                '[' => JsonTokens.SquaredOpen,
                ']' => JsonTokens.SquaredClose,
                ',' => JsonTokens.Comma,
                ':' => JsonTokens.Colon,
                '"' => JsonTokens.DoubleQuote,
                '\'' when flags.HasFlag(JsonParserFlags.AllowSingleQuotedStrings) => JsonTokens.SingleQuote,
                '/' when flags.HasFlag(JsonParserFlags.AllowComments) && IsLiteral(in session, DOUBLE_SLASH) => JsonTokens.DoubleSlash,
                't' or 'T' when IsLiteral(in session, TRUE) => JsonTokens.True,
                'f' or 'F' when IsLiteral(in session, FALSE) => JsonTokens.False,
                'n' or 'N' when IsLiteral(in session, NULL) => JsonTokens.Null,
                '-' or (>= '0' and <= '9') => JsonTokens.Number,
                _ => JsonTokens.Unknown
            };
        }

        /// <summary>
        /// Consumes the current token which the reader is positioned on. Throws a <see cref="FormatException"/> if the token is not in the given range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTokens Consume(ref Session session, JsonTokens expected)
        {
            JsonTokens got = Consume(ref session);
            if (!expected.HasFlag(got) || got is JsonTokens.Unknown)
                //
                // Concatenation of "expected" flags are done by the system
                //

                Throw
                (
                    in session,
                    string.Format(UNEXPECTED_TOKEN, expected, got)
                );
            return got;
        }

        /// <summary>
        /// Consumes the current token which the reader is positioned on. Throws a <see cref="FormatException"/> if the token is not in the given range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTokens Consume(ref Session session, JsonTokens expected, in DeserializationContext currentContext)
        {
            if (!flags.HasFlag(JsonParserFlags.AllowComments))
                //
                // MALFORMED_INPUT can be confusing if it says we expect a DoubleSlash as well while
                // "flags" doesn't allow comments
                //

                return Consume(ref session, expected);

            start:
            JsonTokens got = Consume(ref session, expected | JsonTokens.DoubleSlash);

            if (got is JsonTokens.DoubleSlash)
            {
                ParseComment(ref session, in currentContext);
                goto start;
            }

            return got;
        }

        /// <summary>
        /// Parses the string which the reader is positioned on. The returned <see cref="ReadOnlySpan{char}"/> is valid until the next <see cref="Consume(ref Session)"/> call. 
        /// </summary>
        internal static ReadOnlySpan<char> ParseString(ref Session session, in DeserializationContext currentContext, int initialBufferSize = 128 /*for debug*/)
        {
            const string STRING_ID = "string";

            int quote = session.Content.PeekChar();
            Advance(ref session, 1);

            for (int bufferSize = initialBufferSize, parsed = 0, outputSize = 0; ; bufferSize *= 2)
            {
                Span<char> buffer = session.Content.PeekText(bufferSize);
                if (parsed == buffer.Length)
                {
                    Advance(ref session, parsed);
                    InvalidInput(in session, STRING_ID, INCOMPLETE_STR);
                }

                for (; parsed < buffer.Length; parsed++)
                {
                    char c = buffer[parsed];

                    if (c == quote)
                    {
                        //
                        // We reached the end of the string
                        //

                        Assert(parsed < session.Content.CharsLeft, "Miscalculated 'parsed' value");
                        Advance(ref session, parsed + 1);

                        return buffer.Slice(0, outputSize);
                    }

                    if (IsControl(c))
                    {
                        //
                        // Unexpected white space
                        //

                        Assert(parsed < session.Content.CharsLeft, "Miscalculated 'parsed' value");
                        Advance(ref session, parsed + 1);

                        InvalidInput(in session, STRING_ID, UNEXPECTED_CONTROL);
                    }

                    if (c == '\\')
                    {
                        if (parsed == buffer.Length - 1)
                        {
                            //
                            // We ran out of the characters.
                            //

                            buffer = session.Content.PeekText(buffer.Length + 1);
                            if (parsed == buffer.Length - 1)
                            {
                                //
                                // Enexpected end of string
                                //

                                Advance(ref session, parsed);
                                InvalidInput(in session, STRING_ID, INCOMPLETE_STR);
                            }
                            bufferSize = buffer.Length;
                        }

                        switch (c = buffer[++parsed])
                        {
                            case '\\':
                                buffer[outputSize++] = '\\';
                                break;
                            case 'b':
                                buffer[outputSize++] = '\b';
                                break;
                            case 't':
                                buffer[outputSize++] = '\t';
                                break;
                            case 'r':
                                buffer[outputSize++] = '\r';
                                break;
                            case 'n':
                                buffer[outputSize++] = '\n';
                                break;
                            case 'u':
                                const int HEX_LEN = 4;

                                if (buffer.Length - parsed <= HEX_LEN)
                                {
                                    //
                                    // We need 4 hex digits, try to enlarge the buffer
                                    //

                                    buffer = session.Content.PeekText(buffer.Length + HEX_LEN);
                                    if (buffer.Length - parsed <= HEX_LEN)
                                    {
                                        //
                                        // Unterminated HEX digits
                                        //

                                        Advance(ref session, parsed);
                                        InvalidInput(in session, STRING_ID, INCOMPLETE_STR);
                                    }
                                    bufferSize = buffer.Length;
                                }

                                if 
                                (
                                    !ushort.TryParse
                                    (
#if NETSTANDARD2_1_OR_GREATER
                                        buffer.Slice(parsed + 1, HEX_LEN),
#else
                                        buffer.Slice(parsed + 1, HEX_LEN).AsString(),
#endif
                                        NumberStyles.HexNumber,
                                        CultureInfo.InvariantCulture,
                                        out ushort chr
                                    )
                                )
                                {
                                    //
                                    // Malformed HEX digits
                                    //

                                    Advance(ref session, parsed);
                                    InvalidInput(in session, STRING_ID, CANNOT_PARSE);
                                }

                                //
                                // Jump to the last HEX digit
                                //

                                parsed += HEX_LEN;

                                //
                                // Already unicode so no Encoding.GetChars() call required
                                //

                                buffer[outputSize++] = (char) chr;
                                break;
                            default:
                                if (c == quote)
                                    buffer[outputSize++] = c;
                                else
                                {
                                    //
                                    // Unknown control character -> Error
                                    //

                                    Advance(ref session, parsed);
                                    InvalidInput(in session, STRING_ID, UNKNOWN_CTRL);
                                }
                                break;
                        }
                    }
                    else
                        buffer[outputSize++] = buffer[parsed];
                }
            }
        }

        /// <summary>
        /// Parses the number which the reader is positioned on. The number can be signed integer or floating as well:
        /// <list type="bullet">
        /// <item>-100</item>
        /// <item>-100.0</item>
        /// <item>100</item>
        /// <item>100.0</item>
        /// <item>1.0E+2</item>
        /// <item>1E+2</item>
        /// </list>
        /// </summary>
        internal static object ParseNumber(ref Session session, in DeserializationContext currentContext, int initialBufferSize = 16 /*for debug*/)
        {
            const string NUMBER_ID = "number";

            Span<char> buffer;
            bool isFloating = false;

            //
            // Take all the promising chars
            //

            int parsed = 0;
            for (int bufferSize = initialBufferSize; ; bufferSize *= 2)
            {
                buffer = session.Content.PeekText(bufferSize);

                for (; parsed < buffer.Length; parsed++)
                {
                    char chr = buffer[parsed];

                    if (chr is '.' || ToLower(chr) is 'e')
                        isFloating = true;

                    else if ((chr is < '0' or > '9') && chr is not '+' && chr is not '-')
                        goto parse;
                }

                if (buffer.Length < bufferSize)
                    goto parse;  // We reached the end of the stream
            }

            //
            // Do the actual parse
            //

            parse:
            if (!VerifyDelegate(currentContext.ParseNumber, nameof(currentContext.ParseNumber))(buffer.Slice(0, parsed), !isFloating, out object result))
                InvalidInput(in session, NUMBER_ID, CANNOT_PARSE);

            //
            // Advance the reader if everything was all right
            //

            Advance(ref session, parsed);
            return result!;
        }

        /// <summary>
        /// Parses the list which the reader is positioned on. Returns null if the whole list had to be ingored.
        /// </summary>
        internal object? ParseList(ref Session session, int currentDepth, in DeserializationContext currentContext)
        {
            const string LIST_ID = "list";

            Advance(ref session, 1);

            //
            // If CreateRawList is null OR returns null we won't deserialize the list at all
            //

            object list = VerifyDelegate(currentContext.CreateRawList, nameof(currentContext.CreateRawList))();

            GetListItemContextDelegate getListItemContext = VerifyDelegate(currentContext.GetListItemContext, nameof(currentContext.GetListItemContext));

            int i = 0;

            for (JsonTokens token = Consume(ref session, (JsonTokens) JsonDataTypes.Any | JsonTokens.SquaredClose, currentContext); token is not JsonTokens.SquaredClose; i++)
            {
                if (!getListItemContext(i, out DeserializationContext childContext))
                {
                    if (flags.HasFlag(JsonParserFlags.ThrowOnUnknownListItem))
                        InvalidInput(in session, LIST_ID, UNEXPECTED_LIST_ITEM);

                    childContext = DeserializationContext.Default;
                }

                object? val = Parse(ref session, currentDepth, in childContext);
                VerifyDelegate(childContext.Push, nameof(childContext.Push))(list, val);

                //
                // Check if we reached the end of the list or we have a next element.
                //

                token = Consume(ref session, JsonTokens.SquaredClose | JsonTokens.Comma, in currentContext);
                if (token is JsonTokens.Comma)
                {
                    //
                    // Check if we have a trailing comma
                    //

                    Advance(ref session, 1);
                    token = Consume(ref session, (JsonTokens) JsonDataTypes.Any | JsonTokens.SquaredClose, in currentContext);

                    if (token is JsonTokens.SquaredClose && !flags.HasFlag(JsonParserFlags.AllowTrailingComma))
                        InvalidInput(in session, LIST_ID, MISSING_ITEM);
                }
            }

            Advance(ref session, 1);

            return list;
        }

        /// <summary>
        /// Parses the object which the reader is positioned on. Returns null if the whole object had to be ingored.
        /// </summary>
        internal object? ParseObject(ref Session session, int currentDepth, in DeserializationContext currentContext)
        {
            const string OBJECT_ID = "object";

            Advance(ref session, 1);

            object obj = VerifyDelegate(currentContext.CreateRawObject, nameof(currentContext.CreateRawObject))();

            GetPropertyContextDelegate getPropertyContext = VerifyDelegate(currentContext.GetPropertyContext, nameof(currentContext.GetPropertyContext));
        
            for (JsonTokens token = Consume(ref session, JsonTokens.CurlyClose | (JsonTokens) JsonDataTypes.String, currentContext); token is not JsonTokens.CurlyClose;)
            {
                Consume(ref session, (JsonTokens) JsonDataTypes.String, in currentContext);  // ensure we have a string
                ReadOnlySpan<char> propertyName = ParseString(ref session, in currentContext);

                if (!getPropertyContext(propertyName, flags.HasFlag(JsonParserFlags.CaseInsensitive), out DeserializationContext childContext))
                {
                    if (flags.HasFlag(JsonParserFlags.ThrowOnUnknownProperty))
                        InvalidInput(in session, OBJECT_ID, UNEXPECTED_PROPERTY);

                    childContext = DeserializationContext.Default;
                }

                Consume(ref session, JsonTokens.Colon, in currentContext);
                Advance(ref session, 1);

                object? val = Parse(ref session, currentDepth, in childContext);
                VerifyDelegate(childContext.Push, nameof(currentContext.Push))(obj, val);

                //
                // Check if we reached the end of the object or we have a next element.
                //

                token = Consume(ref session, JsonTokens.CurlyClose | JsonTokens.Comma, in currentContext);
                if (token is JsonTokens.Comma)
                {
                    //
                    // Check if we have a trailing comma
                    //

                    Advance(ref session, 1);
                    token = Consume(ref session, JsonTokens.CurlyClose | (JsonTokens) JsonDataTypes.String, in currentContext);

                    if (token is JsonTokens.CurlyClose && !flags.HasFlag(JsonParserFlags.AllowTrailingComma))
                        InvalidInput(in session, OBJECT_ID, MISSING_PROP);
                }
            }

            Advance(ref session, 1);

            return obj;
        }

        /// <summary>
        /// Parses the comment which the reader is positioned on. This method is also responsible for invoking the corresponding comment processor function.   
        /// </summary>
        internal static void ParseComment(ref Session session, in DeserializationContext currentContext, int initialBufferSize = 32 /*for debug*/)
        {
            Advance(ref session, 2);

            Span<char> buffer;

            int
                lineEnd,
                parsed = 0;

            for (int bufferSize = initialBufferSize; ; bufferSize *= 2)
            {
                //
                // Increase the buffer size
                //

                buffer = session.Content.PeekText(bufferSize);

                //
                // Deal with the "fresh" characters only
                // 

                Span<char> freshChars = buffer.Slice(parsed);
                if (freshChars.Length is 0)
                    break;

                lineEnd = freshChars.IndexOf('\n');
                if (lineEnd >= 0)
                {
                    parsed += lineEnd + 1;
                    break;
                }

                parsed += freshChars.Length;
            }

            lineEnd = parsed;

            if (lineEnd > 0 && buffer[lineEnd - 1] is '\n')
                lineEnd--;

            if (lineEnd > 0 && buffer[lineEnd - 1] is '\r')
                lineEnd--;

            Assert(parsed <= session.Content.CharsLeft, "Cannot advance more character than we have in the buffer");
            Advance(ref session, parsed);

            currentContext.ParseComment?.Invoke(buffer.Slice(0, lineEnd));
        }

        /// <summary>
        /// Parses the input then validates the result.
        /// </summary>
        internal object? Parse(ref Session session, int currentDepth, in DeserializationContext currentContext)
        {
            session.Cancellation.ThrowIfCancellationRequested();

            object? result;

            switch (Consume(ref session, (JsonTokens) currentContext.SupportedTypes | JsonTokens.DoubleSlash, in currentContext)) 
            {
                case JsonTokens.CurlyOpen:
                    result = ParseObject(ref session, Deeper(in session, currentDepth), in currentContext);
                    break;
                case JsonTokens.SquaredOpen:
                    result = ParseList(ref session, Deeper(in session, currentDepth), in currentContext);
                    break;
                case JsonTokens.SingleQuote: case JsonTokens.DoubleQuote:
                    if 
                    (
                        !VerifyDelegate(currentContext.ConvertString, nameof(currentContext.ConvertString))
                        (
                            ParseString(ref session, in currentContext),
                            flags.HasFlag(JsonParserFlags.CaseInsensitive),
                            out result
                        )
                    )
                        InvalidValue(in session, NOT_CONVERTIBLE);
                    break;
                case JsonTokens.Number:
                    result = ParseNumber(ref session, in currentContext);
                    break;
                case JsonTokens.True:
                    Advance(ref session, TRUE.Length);
                    result = true;
                    break;
                case JsonTokens.False:
                    Advance(ref session, FALSE.Length);
                    result = false;
                    break;
                case JsonTokens.Null:
                    Advance(ref session, NULL.Length);
                    result = null;
                    break;
                default:
                    Fail("Got unexpected token");
                    return null!;
            };

            if (currentContext.Convert is not null && !currentContext.Convert(result, out result))
                InvalidValue(in session, NOT_CONVERTIBLE);

            if (currentContext.Verify?.Invoke(result, out ICollection<string> errors) is false)
                InvalidValue(in session, [..errors]);

            return result;
        }
        #endregion

        /// <summary>
        /// Parses the input
        /// </summary>
        public object? Parse(TextReader content, in DeserializationContext context, in CancellationToken cancellation)
        {
            using TextReaderWrapper contentWrapper = content ?? throw new ArgumentNullException(nameof(content));

            Session session = new(contentWrapper, in cancellation);

            object? result = Parse(ref session, 0, in context);

            //
            // Parse trailing comments properly.
            //

            Consume(ref session, JsonTokens.Eof, in context);

            return result;
        }
    }
}
