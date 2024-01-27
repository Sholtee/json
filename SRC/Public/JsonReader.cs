/********************************************************************************
* JsonReader.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.Diagnostics.Debug;

namespace Solti.Utils.Json
{
    using Internals;

    using static Properties.Resources;

    /// <summary>
    /// Represents a generic, cancellable JSON reader.
    /// </summary>
    public sealed class JsonReader(ITextReader input, DeserializationContext context, JsonReaderFlags flags, int maxDepth): IDisposable
    {
        #region Private
        private static readonly string
            TRUE = "true",
            FALSE = "false",
            NULL = "null",
            DOUBLE_SLASH = "//";

        //
        // Do not use ConcurrentBag here as it significantly slower than ConcurrentStack
        //

        private static readonly ConcurrentStack<char[]> FBufferPool = [];

        private char[] FBuffer = FBufferPool.TryPop(out char[] buffer) ? buffer : new char[256];

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
        /// Gets maximum <paramref name="len"/> character(s) from the input without advancing the reader.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> PeekText(int len)
        {
            Span<char> buffer = GetBuffer(len);
            return buffer.Slice(0, input.PeekText(buffer));
        }

        /// <summary>
        /// Advances the underlying <see cref="ITextReader"/>. It assumes that the row remains the same.
        /// </summary>
        /// <param name="len"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance(int len)
        {
            Assert(len >= 0, "Cannot advance in negative direction");
            Assert(len <= input.CharsLeft, "Cannot advance more characters than we have");

            input.Advance(len);
            Column += len;
        }

        /// <summary>
        /// Creates a view to the underlying buffer.
        /// </summary>
        /// <remarks>The method resizes the underlying buffer if necessary.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<char> GetBuffer(int length)
        {
            Assert(length >= 0, "Buffer size must be greater or equal to 0");

            if (length > FBuffer.Length)
                Array.Resize(ref FBuffer, length);

            return FBuffer.AsSpan(0, length);
        }

        void IDisposable.Dispose()
        {
            if (FBuffer is not null)
            {
                FBufferPool.Push(FBuffer);
                FBuffer = null!;
            }
        }
        #endregion

        #region Internal
        /// <summary>
        /// Trims all the leading whitespaces maintaining the row and column count.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SkipSpaces(int bufferSize = 32 /*for tests*/)
        {
            Span<char> buffer = GetBuffer(bufferSize);

            for (int read; (read = input.PeekText(buffer)) > 0;)
            {
                int skip;
                for (skip = 0; skip < read && char.IsWhiteSpace(buffer[skip]); skip++)
                {
                    if (buffer[skip] is '\n')
                    {
                        Row++;
                        Column = 0;
                    }
                    else Column++;
                }

                input.Advance(skip);
                if (skip < read)
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

            return !input.PeekChar(out char chr) ? JsonTokens.Eof : chr switch
            {
                '{' => JsonTokens.CurlyOpen,
                '}' => JsonTokens.CurlyClose,
                '[' => JsonTokens.SquaredOpen,
                ']' => JsonTokens.SquaredClose,
                ',' => JsonTokens.Comma,
                ':' => JsonTokens.Colon,
                '"' => JsonTokens.DoubleQuote,
                '\'' when flags.HasFlag(JsonReaderFlags.AllowSingleQuotedStrings) => JsonTokens.SingleQuote,
                '/' when flags.HasFlag(JsonReaderFlags.AllowComments) && PeekText(DOUBLE_SLASH.Length).Equals(DOUBLE_SLASH.AsSpan(), FComparison) => JsonTokens.DoubleSlash,
                't' or 'T' when PeekText(TRUE.Length).Equals(TRUE.AsSpan(), FComparison) => JsonTokens.True,
                'f' or 'F' when PeekText(FALSE.Length).Equals(FALSE.AsSpan(), FComparison) => JsonTokens.False,
                'n' or 'N' when PeekText(NULL.Length).Equals(NULL.AsSpan(), FComparison) => JsonTokens.Null,
                '-' or (>= '0' and <= '9') => JsonTokens.Number,
                _ => JsonTokens.Unknown
            };
        }

        internal ReadOnlySpan<char> ParseString(int initialBufferSize = 128 /*for debug*/)
        {
            ConsumeAndValidate(JsonTokens.DoubleQuote | JsonTokens.SingleQuote);
            input.PeekChar(out char quote);
            Advance(1);

            for (int bufferSize = initialBufferSize, parsed = 0; ; bufferSize *= 2)
            {
                Span<char>
                    //
                    // Increase the buffer size
                    //

                    buffer = GetBuffer(bufferSize),

                    //
                    // Create a new "working view" to keep the previously parsed characters untouched
                    // 

                    span = buffer.Slice(parsed);

                int returned = input.PeekText(span);
                if (returned is 0)
                    MalformedValue("string", UNTERMINATED_STR);

                int i;
                for (i = 0; i < returned; i++)
                {
                    char c = span[i];

                    if (c == quote)
                    {
                        //
                        // We reached the end of the string
                        //

                        Advance(i + 1);
                        return buffer.Slice(0, parsed);
                    }

                    if (char.IsWhiteSpace(c) && c is not ' ')
                    {
                        //
                        // Unexpected white space
                        //

                        Advance(i + 1);
                        MalformedValue("string", UNEXPECTED_WHITE_SPACE);
                    }

                    if (c == '\\')
                    {
                        if (i + 1 == returned)
                        {
                            if (input.CharsLeft is 1)
                                i++;  // avoid infinite loop 

                            //
                            // We ran out of the characters but there are more
                            //

                            goto readNextChunk;
                        }

                        switch (c = span[++i])
                        {
                            case '\\':
                                buffer[parsed++] = '\\';
                                break;
                            case 'b':
                                buffer[parsed++] = '\b';
                                break;
                            case 't':
                                buffer[parsed++] = '\t';
                                break;
                            case 'r':
                                buffer[parsed++] = '\r';
                                break;
                            case 'n':
                                buffer[parsed++] = '\n';
                                break;
                            case 'u':
                                const int HEX_LEN = 4;

                                if (returned - i <= HEX_LEN)
                                {
                                    //
                                    // We need 4 hex digits
                                    //

                                    if (input.CharsLeft - i > HEX_LEN)
                                    {
                                        //
                                        // We ran out of the characters but there are more
                                        //

                                        i--;
                                        goto readNextChunk;
                                    }

                                    //
                                    // Unterminated HEX digits
                                    //

                                    Advance(i + 1);
                                    MalformedValue("string", MISSING_HEX_DIGIT);
                                }

                                if 
                                (
                                    !ushort.TryParse
                                    (
    #if NETSTANDARD2_1_OR_GREATER
                                        span.Slice(i + 1, HEX_LEN),
    #else
                                        span.Slice(i + 1, HEX_LEN).AsString(),
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

                                    Advance(i + 1);
                                    MalformedValue("string", NOT_HEX_DIGIT);
                                }

                                //
                                // Jump to the last HEX digit
                                //

                                i += HEX_LEN;

                                //
                                // Already unicode so no Encoding.GetChars() call required
                                //

                                buffer[parsed++] = (char) chr;
                                break;
                            default:
                                if (c == quote)
                                    buffer[parsed++] = c;
                                else
                                {
                                    //
                                    // Unknown control character -> Error
                                    //

                                    Advance(i);
                                    MalformedValue("string", UNKNOWN_CTRL);
                                }
                                break;
                        }
                    }
                    else
                        buffer[parsed++] = span[i];
                }

                readNextChunk:
                Advance(i);
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
                buffer = GetBuffer(bufferSize);
                int returned = input.PeekText(buffer);

                for (; parsed < returned; parsed++)
                {
                    char chr = buffer[parsed];

                    if (chr is '.' || char.ToLower(chr) is 'e')
                        isFloating = true;

                    else if ((chr is < '0' or > '9') && chr is not '+' && chr is not '-')
                        goto parse;
                }

                if (input.CharsLeft == parsed)
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
            input.Advance(2);

            Span<char> buffer;

            int parsed = 0;
            for (int bufferSize = initialBufferSize; ; bufferSize *= 2)
            {
                //
                // Increase the buffer size
                //

                buffer = GetBuffer(bufferSize);

                //
                // Create a new "working view" to keep the previously parsed characters untouched
                // 

                Span<char> span = buffer.Slice(parsed);

                int returned = input.PeekText(span);
                if (returned is 0)
                    break;

                int lineEnd = span.Slice(0, returned).IndexOf('\n');
                if (lineEnd >= 0)
                {
                    Advance(lineEnd + 1);
                    parsed += lineEnd + 1;
                    break;
                }

                Advance(returned);
                parsed += returned;
            }

            if (parsed > 0 && buffer[parsed - 1] is '\n')
                parsed--;

            if (parsed > 0 && buffer[parsed - 1] is '\r')
                parsed--;

            currentContext.CommentParser?.Invoke(buffer.Slice(0, parsed));
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
