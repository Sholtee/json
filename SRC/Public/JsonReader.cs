/********************************************************************************
* JsonReader.cs                                                                 *
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
    /// Represents a generic, cancellable JSON reader.
    /// </summary>
    /// <remarks>This class is thread safe.</remarks>
    public sealed class JsonReader(DeserializationContext context, JsonReaderFlags flags, int maxDepth)
    {
        #region Private
        private readonly StringComparison FComparison = flags.HasFlag(JsonReaderFlags.CaseInsensitive)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        /// <summary>
        /// Validates then increases the <paramref name="currentDepth"/>. Throws an <see cref="InvalidOperationException"/> if the current depth reached the <see cref="maxDepth"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Deeper(int currentDepth)
        {
            if (++currentDepth > maxDepth)
                throw new InvalidOperationException(MAX_DEPTH_REACHED);
            return currentDepth;
        }

        /// <summary>
        /// Throws the given <see cref="Exception"/> extending the <see cref="Exception.Data"/> with reader's position
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Throw(in Session session, Exception ex)
        {
            ex.Data["row"] = session.Row;
            ex.Data["column"] = session.Column;
            throw ex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MalformedValue(in Session session, string type, string reason) => Throw
        (
            session,
            new FormatException
            (
                string.Format(MALFORMED, type, session.Row, session.Column, reason)
            )
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
            FComparison
        );

        /// <summary>
        /// Verifies the given <paramref name="delegate"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T VerifyDelegate<T>(T? @delegate) where T : Delegate => @delegate ?? throw new InvalidOperationException(INVALID_CONTEXT);

        /// <summary>
        /// Skips the value on which the reader is positioned.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Skip(ref Session session, int currentDepth, in CancellationToken cancellation) => Parse(ref session, currentDepth, Default, cancellation);
        #endregion

        #region Internal
        internal ref struct Session
        {
            public required TextReaderWrapper Content { get; init; }
            public int Row { get; set; }
            public int Column { get; set; }
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
                '\'' when flags.HasFlag(JsonReaderFlags.AllowSingleQuotedStrings) => JsonTokens.SingleQuote,
                '/' when flags.HasFlag(JsonReaderFlags.AllowComments) && IsLiteral(session, DOUBLE_SLASH) => JsonTokens.DoubleSlash,
                't' or 'T' when IsLiteral(session, TRUE) => JsonTokens.True,
                'f' or 'F' when IsLiteral(session, FALSE) => JsonTokens.False,
                'n' or 'N' when IsLiteral(session, NULL) => JsonTokens.Null,
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
                    session,
                    new FormatException
                    (
                        string.Format(MALFORMED_INPUT, expected, got, session.Row, session.Column)
                    )
                );
            return got;
        }

        /// <summary>
        /// Consumes the current token which the reader is positioned on. Throws a <see cref="FormatException"/> if the token is not in the given range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTokens Consume(ref Session session, JsonTokens expected, DeserializationContext currentContext)
        {
            if (!flags.HasFlag(JsonReaderFlags.AllowComments))
                //
                // MALFORMED_INPUT can be confusing if it says we expect a DoubleSlash as well while
                // "flags" doesn't allow comments
                //

                return Consume(ref session, expected);

            start:
            JsonTokens got = Consume(ref session, expected | JsonTokens.DoubleSlash);

            if (got is JsonTokens.DoubleSlash)
            {
                ParseComment(ref session, currentContext);
                goto start;
            }

            return got;
        }

        /// <summary>
        /// Parses the string which the reader is positioned on. The returned <see cref="ReadOnlySpan{char}"/> is valid until the next <see cref="Consume()"/> call. 
        /// </summary>
        internal static ReadOnlySpan<char> ParseString(ref Session session, DeserializationContext currentContext, int initialBufferSize = 128 /*for debug*/)
        {
            int quote = session.Content.PeekChar();
            Advance(ref session, 1);

            for (int bufferSize = initialBufferSize, parsed = 0, outputSize = 0; ; bufferSize *= 2)
            {
                Span<char> buffer = session.Content.PeekText(bufferSize);
                if (parsed == buffer.Length)
                {
                    Advance(ref session, parsed);
                    MalformedValue(session, "string", INCOMPLETE_STR);
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

                    if (IsWhiteSpace(c) && c is not ' ')
                    {
                        //
                        // Unexpected white space
                        //

                        Assert(parsed < session.Content.CharsLeft, "Miscalculated 'parsed' value");
                        Advance(ref session, parsed + 1);

                        MalformedValue(session, "string", UNEXPECTED_WHITE_SPACE);
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
                                MalformedValue(session, "string", INCOMPLETE_STR);
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
                                        MalformedValue(session, "string", INCOMPLETE_STR);
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
                                    MalformedValue(session, "string", NOT_NUMBER);
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
                                    MalformedValue(session, "string", UNKNOWN_CTRL);
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
        internal static object ParseNumber(ref Session session, DeserializationContext currentContext, int initialBufferSize = 16 /*for debug*/)
        {
            Span<char> buffer;
            bool isFloating = false;

            //
            // Copy all promising chars
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
            buffer = buffer.Slice(0, parsed);
            object? result = null;

            if (isFloating)
            {
                if 
                (
                    double.TryParse
                    (
#if NETSTANDARD2_1_OR_GREATER
                        buffer,
#else
                        buffer.AsString(),
#endif
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double ret
                    )
                )
                    result = ret;
            }
            else
            {
                if 
                (
                    long.TryParse
                    (
#if NETSTANDARD2_1_OR_GREATER
                        buffer,
#else
                        buffer.AsString(),
#endif
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out long ret
                    )
                )
                    result = ret;
            }

            if (result is null)
                MalformedValue(session, "number", NOT_NUMBER);

            //
            // Advance the reader if everything was all right
            //

            Advance(ref session, parsed);

            if (currentContext.ConvertNumber is not null)
                result = currentContext.ConvertNumber(result);

            return result!;
        }

        /// <summary>
        /// Parses the list which the reader is positioned on. Returns null if the whole list had to be ingored.
        /// </summary>
        internal object? ParseList(ref Session session, int currentDepth, DeserializationContext currentContext, in CancellationToken cancellation)
        {
            Advance(ref session, 1);

            //
            // If CreateRawList is null OR returns null we won't deserialize the list at all
            //

            object? list = currentContext.CreateRawList?.Invoke();

            GetListItemContextDelegate? getListItemContext = list is not null
                ? VerifyDelegate(currentContext.GetListItemContext)
                : null;

            int i = 0;

            for (JsonTokens token = Consume(ref session, (JsonTokens) JsonDataTypes.Any | JsonTokens.SquaredClose, currentContext); token is not JsonTokens.SquaredClose; i++)
            {
                DeserializationContext? childContext = null;
                if (getListItemContext is not null)
                {
                    childContext = getListItemContext(i);
                    if (childContext is null && flags.HasFlag(JsonReaderFlags.ThrowOnUnknownListItem))
                        MalformedValue(session, "list", UNEXPECTED_LIST_ITEM);
                }

                if (childContext is null)
                    Skip(ref session, currentDepth, cancellation);                   
                else
                    VerifyDelegate(childContext.Push)(list!, Parse(ref session, currentDepth, childContext, cancellation));

                //
                // Check if we reached the end of the list or we have a next element.
                //

                token = Consume(ref session, JsonTokens.SquaredClose | JsonTokens.Comma, currentContext);
                if (token is JsonTokens.Comma)
                {
                    //
                    // Check if we have a trailing comma
                    //

                    Advance(ref session, 1);
                    token = Consume(ref session, (JsonTokens) JsonDataTypes.Any | JsonTokens.SquaredClose, currentContext);

                    if (token is JsonTokens.SquaredClose && !flags.HasFlag(JsonReaderFlags.AllowTrailingComma))
                        MalformedValue(session, "list", MISSING_ITEM);
                }
            }

            Advance(ref session, 1);

            return list;
        }

        /// <summary>
        /// Parses the object which the reader is positioned on. Returns null if the whole object had to be ingored.
        /// </summary>
        internal object? ParseObject(ref Session session, int currentDepth, DeserializationContext currentContext, in CancellationToken cancellation)
        {
            Advance(ref session, 1);

            object? obj = currentContext.CreateRawObject?.Invoke();

            GetPropertyContextDelegate? getPropertyContext = obj is not null
                ? VerifyDelegate(currentContext.GetPropertyContext)
                : null;
        
            for (JsonTokens token = Consume(ref session, JsonTokens.CurlyClose | (JsonTokens) JsonDataTypes.String, currentContext); token is not JsonTokens.CurlyClose;)
            {
                Consume(ref session, (JsonTokens) JsonDataTypes.String, currentContext);  // ensure we have a string
                ReadOnlySpan<char> propertyName = ParseString(ref session, currentContext);

                DeserializationContext? childContext = null;
                if (getPropertyContext is not null)
                {
                    childContext = getPropertyContext(propertyName, FComparison);
                    if (childContext is null && flags.HasFlag(JsonReaderFlags.ThrowOnUnknownProperty))
                        MalformedValue(session, "object", UNEXPECTED_PROPERTY);
                }

                Consume(ref session, JsonTokens.Colon, currentContext);
                Advance(ref session, 1);

                if (childContext is null)
                    Skip(ref session, currentDepth, cancellation);
                else
                    VerifyDelegate(childContext.Push)(obj!, Parse(ref session, currentDepth, childContext, cancellation));

                //
                // Check if we reached the end of the object or we have a next element.
                //

                token = Consume(ref session, JsonTokens.CurlyClose | JsonTokens.Comma, currentContext);
                if (token is JsonTokens.Comma)
                {
                    //
                    // Check if we have a trailing comma
                    //

                    Advance(ref session, 1);
                    token = Consume(ref session, JsonTokens.CurlyClose | (JsonTokens) JsonDataTypes.String, currentContext);

                    if (token is JsonTokens.CurlyClose && !flags.HasFlag(JsonReaderFlags.AllowTrailingComma))
                        MalformedValue(session, "object", MISSING_PROP);
                }
            }

            Advance(ref session, 1);

            return obj;
        }

        /// <summary>
        /// Parses the comment which the reader is positioned on. This method is also responsible for invoking the corresponding comment processor function.   
        /// </summary>
        internal static void ParseComment(ref Session session, DeserializationContext currentContext, int initialBufferSize = 32 /*for debug*/)
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

            currentContext.CommentParser?.Invoke(buffer.Slice(0, lineEnd));
        }

        /// <summary>
        /// Parses the input then validates the result.
        /// </summary>
        internal object? Parse(ref Session session, int currentDepth, DeserializationContext currentContext, in CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            object? result;

            switch (Consume(ref session, (JsonTokens) currentContext.SupportedTypes | JsonTokens.DoubleSlash, currentContext)) 
            {
                case JsonTokens.CurlyOpen:
                    result = ParseObject(ref session, Deeper(currentDepth), currentContext, cancellation);
                    break;
                case JsonTokens.SquaredOpen:
                    result = ParseList(ref session, Deeper(currentDepth), currentContext, cancellation);
                    break;
                case JsonTokens.SingleQuote: case JsonTokens.DoubleQuote:
                    result = currentContext.ConvertString(ParseString(ref session, currentContext));
                    break;
                case JsonTokens.Number:
                    result = ParseNumber(ref session, currentContext);
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

            IEnumerable<string>? errors = currentContext.Verify?.Invoke(result);
            if (errors is not null)
            {
                ICollection<string> coll = errors as ICollection<string> ?? new List<string>(errors);
                if (coll.Count > 0)
                {
                    InvalidOperationException ex = new(VALIDATION_FAILED);
                    ex.Data[nameof(errors)] = coll;
                    Throw(session, ex);
                }
            }

            return result;
        }
        #endregion

        /// <summary>
        /// Parses the input
        /// </summary>
        public object? Parse(TextReader content, in CancellationToken cancellation)
        {
            using TextReaderWrapper contentWrapper = content;

            Session session = new() { Content = contentWrapper };

            object? result = Parse(ref session, 0, context, cancellation);

            //
            // Parse trailing comments properly.
            //

            Consume(ref session, JsonTokens.Eof, context);

            return result;
        }
    }
}
