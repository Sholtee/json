using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Solti.Utils.JSON
{
    using static Properties.Resources;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="maxDepth"></param>
    public readonly ref struct JsonReader(JsonReaderFlags flags, int maxDepth)
    {
        #region Private
        [Flags]
        private enum JsonTokens
        {
            Unknown = 0,
            Eof = 1 << 0,
            DoubleSlash = 1 << 1,
            CurlyOpen = 1 << 2,
            CurlyClose = 1 << 3,
            SquaredOpen = 1 << 4,
            SquaredClose = 1 << 5,
            Colon = 1 << 6,
            Comma = 1 << 7,
            SingleQuote = 1 << 8,
            DoubleQuote = 1 << 9,
            Number = 1 << 10,
            True = 1 << 11,
            False = 1 << 12,
            Null = 1 << 13
        }

        private readonly ReadOnlySpan<char>
            TRUE = "true".AsSpan(),
            FALSE = "false".AsSpan(),
            NULL = "null".AsSpan(),
            DOUBLE_SLASH = "//".AsSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Deeper(int currentDepth)
        {
            if (++currentDepth > MaxDepth)
                throw new InvalidOperationException(MAX_DEPTH_REACHED);
            return currentDepth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Malformed(string type, ITextReader reader) => throw new FormatException(string.Format(MALFORMED, type, reader.Row, reader.Column));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SkipSpaces(ITextReader input, IJsonReaderContext context)
        {
            Span<char> buffer = context.GetBuffer(32);

            for (int read; (read = input.PeekText(buffer)) > 0;)
            {
                int skip;
                for (skip = 0; skip < read && char.IsWhiteSpace(buffer[skip]); skip++)
                {
                }

                input.Advance(skip);
                if (skip < read)
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> PeekText(ITextReader input, IJsonReaderContext context, int len)
        {
            Span<char> buffer = context.GetBuffer(len);
            return buffer.Slice(0, input.PeekText(buffer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonTokens ConsumeAndValidate(ITextReader input, IJsonReaderContext context, JsonTokens expected)
        {
            JsonTokens got = Consume(input, context);
            if (!expected.HasFlag(got))
                //
                // Concatenation of "exptected" flags are done by the system
                // 

                throw new FormatException(string.Format(MALFORMED_INPUT, expected, got, input.Row, input.Column));
            return got;
        }

        /// <summary>
        /// Consumes the current token which the reader is positioned on.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonTokens Consume(ITextReader input, IJsonReaderContext context)
        {
            SkipSpaces(input, context);

            StringComparison comparison = Flags.HasFlag(JsonReaderFlags.CaseInsensitive)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return !input.PeekChar(out char chr) ? JsonTokens.Eof : chr switch
            {
                '{' => JsonTokens.CurlyOpen,
                '}' => JsonTokens.CurlyClose,
                '[' => JsonTokens.SquaredOpen,
                ']' => JsonTokens.SquaredClose,
                ',' => JsonTokens.CurlyOpen,
                ':' => JsonTokens.Colon,
                '"' => JsonTokens.DoubleQuote,
                '\'' when Flags.HasFlag(JsonReaderFlags.AllowSingleQuotedStrings) => JsonTokens.SingleQuote,
                '/' when Flags.HasFlag(JsonReaderFlags.AllowComments) && DOUBLE_SLASH.Equals(PeekText(input, context, DOUBLE_SLASH.Length), comparison) => JsonTokens.DoubleSlash,
                't' or 'T' when TRUE.Equals(PeekText(input, context, TRUE.Length), comparison) => JsonTokens.True,
                'f' or 'F' when FALSE.Equals(PeekText(input, context, FALSE.Length), comparison) => JsonTokens.False,
                'n' or 'N' when NULL.Equals(PeekText(input, context, NULL.Length), comparison) => JsonTokens.Null,
                '-' or (>= '0' and <= '9') => JsonTokens.Number,
                _ => JsonTokens.Unknown
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if !NETSTANDARD2_1_OR_GREATER
        unsafe
#endif
        private static string ConvertString(ReadOnlySpan<char> chars)
        {
#if NETSTANDARD2_1_OR_GREATER
            return  new string(chars);
#else
            fixed (char* ptr = chars)
            {
                return new string(ptr, 0, chars.Length);
            }
#endif
        }
        #endregion

        #region Internal
        internal ReadOnlySpan<char> ParseString(ITextReader input, IJsonReaderContext context, char quote, int initialBufferSize = 128 /*for debug*/)
        {
            ConsumeAndValidate(input, context, quote is '"' ? JsonTokens.DoubleQuote : JsonTokens.SingleQuote);
            input.Advance(1);

            for (int bufferSize = initialBufferSize, parsed = 0; ; bufferSize *= 2)
            {
                Span<char>
                    //
                    // Increase the buffer size
                    //

                    buffer = context.GetBuffer(bufferSize),

                    //
                    // Create a new "working view" to keep the previously parsed characters untouched
                    // 

                    span = buffer.Slice(parsed);

                int returned = input.PeekText(span);
                if (returned is 0)
                    Malformed("string", input);

                for (int i = 0; i < returned; i++)
                {
                    char c = span[i];

                    if (c == quote)
                        //
                        // We reached the end of the string
                        //

                        return buffer.Slice(parsed);

                    if (c == '\\')
                    {
                        if (++i == returned)
                        {
                            input.Advance(returned);
                            if (input.CharsLeft > 0)
                                //
                                // We ran out of the characters but there are more
                                //

                                break;

                            //
                            // Unterminated string -> Error
                            //
                        
                            Malformed("string", input);
                        }

                        c = span[i];

                        if (c == quote || c == '\\')
                            span[parsed++] = c;
                        else if (c == 'b')
                            span[parsed++] = '\b';
                        else if (c == 't')
                            span[parsed++] = '\t';
                        else if (c == 'n')
                            span[parsed++] = '\n';
                        else if (c == 'r')
                            span[parsed++] = '\r';
                        else if (c == 'u')
                        {
                            input.Advance(i);
                            if (returned - i < 4)
                            {
                                //
                                // We need 4 hex digits
                                //

                                if (input.CharsLeft > 3)
                                    //
                                    // We ran out of the characters but there are more
                                    //

                                    break;

                                //
                                // Unterminated HEX digits
                                //

                                Malformed("string", input);
                            }

                            bool validHex = ushort.TryParse
                            (
#if NETSTANDARD2_1_OR_GREATER
                                span.Slice(i + 1, 4),
#else
                                ConvertString(span.Slice(i + 1, 4)),
#endif
                                NumberStyles.HexNumber,
                                null,
                                out ushort chr
                            );
                            if (!validHex)
                                //
                                // Malformed HEX digits
                                //

                                Malformed("string", input);

                            input.Advance(4);
                            span[parsed++] = (char) chr;
                        }
                        else
                        {
                            //
                            // Unknown control character -> Error
                            //

                            input.Advance(i);
                            Malformed("string", input);
                        }


                    }
                    else
                        span[parsed++] = span[i];
                }
            }
        }

        //
        // a) 100
        // b) 100.0
        // c) 1.0E+2
        // d) 1E+2
        //

        internal object ParseNumber(ITextReader input, IJsonReaderContext context)
        {
            Span<char> buffer;
            bool isFloating = false;

            //
            // Copy all promising chars
            //

            for (int bufferSize = 16, parsed = 0; ; bufferSize *= 2)
            {
                buffer = context.GetBuffer(bufferSize);
                int returned = input.PeekText(buffer);

                for (; parsed < returned; parsed++)
                {
                    char chr = buffer[parsed];

                    if (chr is '.' || char.ToLower(chr) is 'e')
                    {
                        isFloating = true;
                    }

                    else if ((chr is < '0' or > '9') && chr is not '+' && chr is not '-')
                    {
                        buffer = buffer.Slice(0, parsed);
                        goto parse;
                    }
                }

                if (input.CharsLeft == parsed)
                    goto parse;  // We reached the end of the stream
            }

            //
            // Do the actual parse
            //

            parse:
            object? result = null;

            if (isFloating)
            {
#if NETSTANDARD2_1_OR_GREATER
                if (double.TryParse(buffer, NumberStyles.Float, CultureInfo.InvariantCulture, out double ret))
#else
                if (double.TryParse(ConvertString(buffer), NumberStyles.Float, CultureInfo.InvariantCulture, out double ret))
#endif
                    result = ret;
            }
            else
            {
#if NETSTANDARD2_1_OR_GREATER
                if (long.TryParse(buffer, NumberStyles.Number, CultureInfo.InvariantCulture, out long ret))
#else
                if (long.TryParse(ConvertString(buffer), NumberStyles.Number, CultureInfo.InvariantCulture, out long ret))
#endif
                    result = ret;
            }

            if (result is null)
                Malformed("number", input);

            //
            // Advance the reader if everything was all right
            //

            input.Advance(buffer.Length);
            return result!;
        }

        internal object ParseList(ITextReader input, IJsonReaderContext context, int currentDepth)
        {
            ConsumeAndValidate(input, context, JsonTokens.SquaredOpen);
            input.Advance(1);

            object result = context.CreateRawObject();

            while (Consume(input, context) is not JsonTokens.SquaredClose)
            {
                context.SetValue(result, Parse(input, context, currentDepth));

                if (ConsumeAndValidate(input, context, JsonTokens.SquaredClose | JsonTokens.Comma) is JsonTokens.Comma)
                {
                    input.Advance(1);
                    if (Consume(input, context) is JsonTokens.SquaredClose && !Flags.HasFlag(JsonReaderFlags.AllowTrailingComma))
                        Malformed("list", input);
                }
            }

            input.Advance(1);

            return result;
        }

        internal object ParseObject(ITextReader input, IJsonReaderContext context, int currentDepth)
        {
            return new Dictionary<string, object?>();
        }

        internal void ParseComment(ITextReader input, IJsonReaderContext context)
        {           
            for (ReadOnlySpan<char> buffer; !(buffer = PeekText(input, context, 32)).IsEmpty;)
            {
                int lineEnd = buffer.IndexOf('\n');
                if (lineEnd >= 0)
                {
                    input.Advance(lineEnd + 1);
                    break;
                }

                input.Advance(buffer.Length);
            }

            // TODO: introduce a flag if we want to pass the read comment to the context
        }

        internal object? Parse(ITextReader input, IJsonReaderContext context, int currentDepth)
        {
            const JsonTokens EXPECTED =
                JsonTokens.CurlyOpen |
                JsonTokens.SquaredOpen |
                JsonTokens.SingleQuote |
                JsonTokens.DoubleQuote |
                JsonTokens.DoubleSlash |
                JsonTokens.Number |
                JsonTokens.True |
                JsonTokens.False |
                JsonTokens.Null;

            context.ThrowIfCancellationRequested();

            for (;;)
            {
                switch (ConsumeAndValidate(input, context, EXPECTED)) 
                {
                    case JsonTokens.CurlyOpen:
                        return ParseObject(input, context, Deeper(currentDepth));
                    case JsonTokens.SquaredOpen:
                        return ParseList(input, context, Deeper(currentDepth));
                    case JsonTokens.SingleQuote:
                        return ConvertString(ParseString(input, context, '\''));
                    case JsonTokens.DoubleQuote:
                        return ConvertString(ParseString(input, context, '"'));
                    case JsonTokens.DoubleSlash:
                        ParseComment(input, context);
                        continue;
                    case JsonTokens.Number:
                        return ParseNumber(input, context);
                    case JsonTokens.True:
                        input.Advance(TRUE.Length);
                        return true;
                    case JsonTokens.False:
                        input.Advance(FALSE.Length);
                        return false;
                    case JsonTokens.Null:
                        input.Advance(NULL.Length);
                        return null;
                    default:
                        Debug.Fail("We must not get here. Review the EXPECTED constant and the ConsumeAndValidate() method");
                        return null!;
                };
            }
        }
#endregion

        public int MaxDepth { get; } = maxDepth;

        public JsonReaderFlags Flags { get; } = flags;

        public object? Parse(ITextReader input, IJsonReaderContext context)
        {
            object? result = Parse(input, context, 0);

            //
            // Parse trailing comments properly.
            //

            while(ConsumeAndValidate(input, context, JsonTokens.Eof | JsonTokens.DoubleSlash) is JsonTokens.DoubleSlash)
                ParseComment(input, context);

            return result;
        }
    }
}
