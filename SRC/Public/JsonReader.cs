/********************************************************************************
* JsonReader.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.Diagnostics.Debug;
using static System.Char;

namespace Solti.Utils.Json
{
    using Internals;

    using static Properties.Resources;

    /// <summary>
    /// Represents a generic, cancellable JSON reader.
    /// </summary>
    public sealed class JsonReader(TextReader content, DeserializationContext context, JsonReaderFlags flags, int maxDepth): IDisposable
    {
        #region Private
        private static readonly string
            TRUE = "true",
            FALSE = "false",
            NULL = "null",
            DOUBLE_SLASH = "//";

        private readonly StringComparison FComparison = flags.HasFlag(JsonReaderFlags.CaseInsensitive)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        private readonly TextReaderWrapper FContent = new(content);

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
        /// Throws a <see cref="FormatException"/>. The exception being thrown is augmented by the actual row and column count.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowFormatException(string msg)
        {
            FormatException ex = new(msg);
            ex.Data["row"] = Row;
            ex.Data["column"] = Column;
            throw ex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MalformedValue(string type, string reason) => ThrowFormatException
        (
            string.Format(MALFORMED, type, Row, Column, reason)
        );

        /// <summary>
        /// Advances the underlying text reader. It assumes that the row remains the same.
        /// </summary>
        /// <param name="len"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance(int len)
        {
            FContent.Advance(len);
            Column += len;
        }

        /// <summary>
        /// Returns true if the input is positioned on the given <paramref name="literal"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsLiteral(string literal) => literal.AsSpan().Equals
        (
            FContent.PeekText(literal.Length),
            FComparison
        );

        void IDisposable.Dispose()
        {
            if (!FContent.Disposed)
                FContent.Dispose();
        }
        #endregion

        #region Internal
        /// <summary>
        /// The input text.
        /// </summary>
        internal TextReaderWrapper Content => FContent;

        /// <summary>
        /// Trims all the leading whitespaces maintaining the row and column count.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SkipSpaces(int bufferSize = 32 /*for tests*/)
        {
            for (Span<char> read; (read = FContent.PeekText(bufferSize)).Length > 0;)
            {
                int skip;
                for (skip = 0; skip < read.Length && char.IsWhiteSpace(read[skip]); skip++)
                {
                    if (read[skip] is '\n')
                    {
                        Row++;
                        Column = 0;
                    }
                    else Column++;
                }

                FContent.Advance(skip);
                if (skip < read.Length)
                    break;
            }
        }

        /// <summary>
        /// Consumes the current token which the reader is positioned on. Throws a <see cref="FormatException"/> if the token is not in the given range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTokens ConsumeAndValidate(JsonTokens expected)
        {
            JsonTokens got = Consume();
            if (!expected.HasFlag(got) || got is JsonTokens.Unknown)
                //
                // Concatenation of "expected" flags are done by the system
                //

                ThrowFormatException(string.Format(MALFORMED_INPUT, expected, got, Row, Column));
            return got;
        }

        /// <summary>
        /// Consumes the current token which the reader is positioned on.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTokens Consume()
        {
            SkipSpaces();

            int chr = FContent.PeekChar();

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
                '/' when flags.HasFlag(JsonReaderFlags.AllowComments) && IsLiteral(DOUBLE_SLASH) => JsonTokens.DoubleSlash,
                't' or 'T' when IsLiteral(TRUE) => JsonTokens.True,
                'f' or 'F' when IsLiteral(FALSE) => JsonTokens.False,
                'n' or 'N' when IsLiteral(NULL) => JsonTokens.Null,
                '-' or (>= '0' and <= '9') => JsonTokens.Number,
                _ => JsonTokens.Unknown
            };
        }

        internal ReadOnlySpan<char> ParseString(int initialBufferSize = 128 /*for debug*/)
        {
            ConsumeAndValidate(JsonTokens.DoubleQuote | JsonTokens.SingleQuote);
            int quote = FContent.PeekChar();
            Advance(1);

            for (int bufferSize = initialBufferSize, parsed = 0, outputSize = 0; ; bufferSize *= 2)
            {
                Span<char> buffer = FContent.PeekText(bufferSize);
                if (parsed == buffer.Length)
                {
                    Advance(parsed);
                    MalformedValue("string", INCOMPLETE_STR);
                }

                for (; parsed < buffer.Length; parsed++)
                {
                    char c = buffer[parsed];

                    if (c == quote)
                    {
                        //
                        // We reached the end of the string
                        //

                        Assert(parsed < FContent.CharsLeft, "Miscalculated 'parsed' value");
                        Advance(parsed + 1);

                        return buffer.Slice(0, outputSize);
                    }

                    if (IsWhiteSpace(c) && c is not ' ')
                    {
                        //
                        // Unexpected white space
                        //

                        Assert(parsed < FContent.CharsLeft, "Miscalculated 'parsed' value");
                        Advance(parsed + 1);

                        MalformedValue("string", UNEXPECTED_WHITE_SPACE);
                    }

                    if (c == '\\')
                    {
                        if (parsed == buffer.Length - 1)
                        {
                            //
                            // We ran out of the characters.
                            //

                            buffer = FContent.PeekText(buffer.Length + 1);
                            if (parsed == buffer.Length - 1)
                            {
                                //
                                // Enexpected end of string
                                //

                                Advance(parsed);
                                MalformedValue("string", INCOMPLETE_STR);
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

                                    buffer = FContent.PeekText(buffer.Length + HEX_LEN);
                                    if (buffer.Length - parsed <= HEX_LEN)
                                    {
                                        //
                                        // Unterminated HEX digits
                                        //

                                        Advance(parsed);
                                        MalformedValue("string", INCOMPLETE_STR);
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

                                    Advance(parsed);
                                    MalformedValue("string", NOT_NUMBER);
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

                                    Advance(parsed);
                                    MalformedValue("string", UNKNOWN_CTRL);
                                }
                                break;
                        }
                    }
                    else
                        buffer[outputSize++] = buffer[parsed];
                }
            }
        }

        //
        // a) 100
        // b) 100.0
        // c) 1.0E+2
        // d) 1E+2
        //

        internal object ParseNumber(DeserializationContext currentContext, int initialBufferSize = 16 /*for debug*/)
        {
            ConsumeAndValidate(JsonTokens.Number);

            Span<char> buffer;
            bool isFloating = false;

            //
            // Copy all promising chars
            //

            int parsed = 0;
            for (int bufferSize = initialBufferSize; ; bufferSize *= 2)
            {
                buffer = FContent.PeekText(bufferSize);

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
                MalformedValue("number", NOT_NUMBER);

            //
            // Advance the reader if everything was all right
            //

            Advance(parsed);

            if (currentContext.ConvertNumber is not null)
                result = currentContext.ConvertNumber(result);

            return result!;
        }

        internal object ParseList(int currentDepth, DeserializationContext currentContext, in CancellationToken cancellation)
        {
            ConsumeAndValidate(JsonTokens.SquaredOpen);
            Advance(1);

            object list = EnsureNotNull(currentContext.CreateRawList).Invoke();

            int i = 0;
            for (JsonTokens token = Consume(); token is not JsonTokens.SquaredClose;)
            {
                DeserializationContext childContext = EnsureNotNull(currentContext.GetListItemContext).Invoke(i++);

                EnsureNotNull(childContext.Push).Invoke(list, Parse(currentDepth, childContext, cancellation));

                //
                // Check if we reached the end of the list or we have a next element.
                //

                token = ConsumeAndValidate(JsonTokens.SquaredClose | JsonTokens.Comma);
                if (token is JsonTokens.Comma)
                {
                    //
                    // Check if we have a trailing comma
                    //

                    Advance(1);
                    token = Consume();

                    if (token is JsonTokens.SquaredClose && !flags.HasFlag(JsonReaderFlags.AllowTrailingComma))
                        MalformedValue("list", MISSING_ITEM);
                }
            }

            Advance(1);

            return list;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T EnsureNotNull<T>(T? val) => val ?? throw new InvalidOperationException(INVALID_CONTEXT);
        }

        internal object ParseObject(int currentDepth, DeserializationContext currentContext, in CancellationToken cancellation)
        {
            ConsumeAndValidate(JsonTokens.CurlyOpen);
            Advance(1);

            object obj = EnsureNotNull(currentContext.CreateRawObject).Invoke();

            for (JsonTokens token = Consume(); token is not JsonTokens.CurlyClose;)
            {
                DeserializationContext childContext = EnsureNotNull(currentContext.GetPropertyContext).Invoke
                (
                    ParseString(),
                    FComparison
                );

                ConsumeAndValidate(JsonTokens.Colon);
                Advance(1);

                EnsureNotNull(childContext.Push).Invoke(obj, Parse(currentDepth, childContext, cancellation));

                //
                // Check if we reached the end of the object or we have a next element.
                //

                token = ConsumeAndValidate(JsonTokens.CurlyClose | JsonTokens.Comma);
                if (token is JsonTokens.Comma)
                {
                    //
                    // Check if we have a trailing comma
                    //

                    Advance(1);
                    token = Consume();

                    if (token is JsonTokens.CurlyClose && !flags.HasFlag(JsonReaderFlags.AllowTrailingComma))
                        MalformedValue("object", MISSING_PROP);
                }
            }

            Advance(1);

            return obj;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static T EnsureNotNull<T>(T? val) => val ?? throw new InvalidOperationException(INVALID_CONTEXT);
        }

        internal void ParseComment(DeserializationContext currentContext, int initialBufferSize = 32 /*for debug*/)
        {
            ConsumeAndValidate(JsonTokens.DoubleSlash);
            Advance(2);

            Span<char> buffer;

            int
                lineEnd,
                parsed = 0;

            for (int bufferSize = initialBufferSize; ; bufferSize *= 2)
            {
                //
                // Increase the buffer size
                //

                buffer = FContent.PeekText(bufferSize);

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

            Assert(parsed <= FContent.CharsLeft, "Cannot advance more character than we have in the buffer");
            Advance(parsed);

            currentContext.CommentParser?.Invoke(buffer.Slice(0, lineEnd));
        }

        internal object? Parse(int currentDepth, DeserializationContext currentContext, in CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            object? result;

            start:
            switch (ConsumeAndValidate((JsonTokens) currentContext.SupportedTypes | JsonTokens.DoubleSlash)) 
            {
                case JsonTokens.CurlyOpen:
                    result = ParseObject(Deeper(currentDepth), currentContext, cancellation);
                    break;
                case JsonTokens.SquaredOpen:
                    result = ParseList(Deeper(currentDepth), currentContext, cancellation);
                    break;
                case JsonTokens.SingleQuote: case JsonTokens.DoubleQuote:
                    result = currentContext.ConvertString(ParseString());
                    break;
                case JsonTokens.DoubleSlash:
                    ParseComment(currentContext);
                    goto start;
                case JsonTokens.Number:
                    result = ParseNumber(currentContext);
                    break;
                case JsonTokens.True:
                    Advance(TRUE.Length);
                    result = true;
                    break;
                case JsonTokens.False:
                    Advance(FALSE.Length);
                    result = false;
                    break;
                case JsonTokens.Null:
                    Advance(NULL.Length);
                    result = null;
                    break;
                default:
                    Fail("Got unexpected token");
                    return null!;
            };

            currentContext.Verify?.Invoke(result);
            return result;
        }
#endregion

        /// <summary>
        /// Gets the current row which is the reader is positioned on
        /// </summary>
        public int Row { get; private set; }

        /// <summary>
        /// Gets the current column which is the reader is positioned on
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// Parses the <see cref="input"/>
        /// </summary>
        public object? Parse(in CancellationToken cancellation)
        {
            object? result = Parse(0, context, cancellation);

            //
            // Parse trailing comments properly.
            //

            while(ConsumeAndValidate(JsonTokens.Eof | JsonTokens.DoubleSlash) is JsonTokens.DoubleSlash)
                ParseComment(context);

            return result;
        }
    }
}
