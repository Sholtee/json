/********************************************************************************
* JsonReader.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json
{
    using Internals;
    using static Properties.Resources;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="maxDepth"></param>
    public sealed class JsonReader(JsonReaderFlags flags, int maxDepth)
    {
        #region Private
        private static readonly string
            TRUE = "true",
            FALSE = "false",
            NULL = "null",
            DOUBLE_SLASH = "//";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Deeper(int currentDepth)
        {
            if (++currentDepth > MaxDepth)
                throw new InvalidOperationException(MAX_DEPTH_REACHED);
            return currentDepth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowFormatException(string msg, IJsonReaderContext context)
        {
            FormatException ex = new(msg);
            ex.Data["row"] = context.Row;
            ex.Data["column"] = context.Column;
            throw ex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MalformedValue(string type, string reason, IJsonReaderContext context) =>
            ThrowFormatException(string.Format(MALFORMED, type, context.Row, context.Column, reason), context);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> PeekText(ITextReader input, IJsonReaderContext context, int len)
        {
            Span<char> buffer = context.GetBuffer(len);
            return buffer.Slice(0, input.PeekText(buffer));
        }
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SkipSpaces(ITextReader input, IJsonReaderContext context, int bufferSize = 32 /*for tests*/)
        {
            Span<char> buffer = context.GetBuffer(bufferSize);

            for (int read; (read = input.PeekText(buffer)) > 0;)
            {
                int skip;
                for (skip = 0; skip < read && char.IsWhiteSpace(buffer[skip]); skip++)
                {
                    if (buffer[skip] is '\n')
                    {
                        context.Row++;
                        context.Column = 0;
                    }
                    else context.Column++;
                }

                input.Advance(skip);
                if (skip < read)
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTokens ConsumeAndValidate(ITextReader input, IJsonReaderContext context, JsonTokens expected)
        {
            JsonTokens got = Consume(input, context);
            if (!expected.HasFlag(got))
                //
                // Concatenation of "exptected" flags are done by the system
                //

                ThrowFormatException(string.Format(MALFORMED_INPUT, expected, got, context.Row, context.Column), context);
            return got;
        }

        /// <summary>
        /// Consumes the current token which the reader is positioned on.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal JsonTokens Consume(ITextReader input, IJsonReaderContext context)
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
                ',' => JsonTokens.Comma,
                ':' => JsonTokens.Colon,
                '"' => JsonTokens.DoubleQuote,
                '\'' when Flags.HasFlag(JsonReaderFlags.AllowSingleQuotedStrings) => JsonTokens.SingleQuote,
                '/' when Flags.HasFlag(JsonReaderFlags.AllowComments) && PeekText(input, context, DOUBLE_SLASH.Length).Equals(DOUBLE_SLASH.AsSpan(), comparison) => JsonTokens.DoubleSlash,
                't' or 'T' when PeekText(input, context, TRUE.Length).Equals(TRUE.AsSpan(), comparison) => JsonTokens.True,
                'f' or 'F' when PeekText(input, context, FALSE.Length).Equals(FALSE.AsSpan(), comparison) => JsonTokens.False,
                'n' or 'N' when PeekText(input, context, NULL.Length).Equals(NULL.AsSpan(), comparison) => JsonTokens.Null,
                '-' or (>= '0' and <= '9') => JsonTokens.Number,
                _ => JsonTokens.Unknown
            };
        }

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
                    MalformedValue("string", "unterminated string", context);

                int i;
                for (i = 0; i < returned; i++)
                {
                    char c = span[i];

                    if (c == quote)
                    {
                        //
                        // We reached the end of the string
                        //

                        input.Advance(i + 1);

                        return buffer.Slice(0, parsed);
                    }

                    if (char.IsWhiteSpace(c) && c is not ' ')
                    {
                        //
                        // Unexpected white space
                        //

                        input.Advance(i + 1);
                        MalformedValue("string", "unexpected white space", context);
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

                            break;
                        }

                        c = span[++i];

                        if (c == quote || c == '\\')
                            buffer[parsed++] = c;
                        else if (c == 'b')
                            buffer[parsed++] = '\b';
                        else if (c == 't')
                            buffer[parsed++] = '\t';
                        else if (c == 'n')
                            buffer[parsed++] = '\n';
                        else if (c == 'r')
                            buffer[parsed++] = '\r';
                        else if (c == 'u')
                        {
                            if (returned - i < 4)
                            {
                                //
                                // We need 4 hex digits
                                //

                                if (input.CharsLeft - i > 3)
                                    //
                                    // We ran out of the characters but there are more
                                    //

                                    break;

                                //
                                // Unterminated HEX digits
                                //

                                input.Advance(i);
                                MalformedValue("string", "missing HEX digits", context);
                            }

                            bool validHex = ushort.TryParse
                            (
#if NETSTANDARD2_1_OR_GREATER
                                span.Slice(i, 4),
#else
                                span.Slice(i, 4).AsString(),
#endif
                                NumberStyles.HexNumber,
                                null,
                                out ushort chr
                            );
                            if (!validHex)
                                //
                                // Malformed HEX digits
                                //

                                MalformedValue("string", "not a HEX", context);

                            i += 4;
                            buffer[parsed++] = (char) chr;
                        }
                        else
                        {
                            //
                            // Unknown control character -> Error
                            //

                            input.Advance(i);
                            MalformedValue("string", "unknown control character", context);
                        }
                    }
                    else
                        buffer[parsed++] = span[i];
                }

                input.Advance(i);
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
                if (double.TryParse(buffer.AsString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double ret))
#endif
                    result = ret;
            }
            else
            {
#if NETSTANDARD2_1_OR_GREATER
                if (long.TryParse(buffer, NumberStyles.Number, CultureInfo.InvariantCulture, out long ret))
#else
                if (long.TryParse(buffer.AsString(), NumberStyles.Number, CultureInfo.InvariantCulture, out long ret))
#endif
                    result = ret;
            }

            if (result is null)
                MalformedValue("number", "not a number", context);

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
                        MalformedValue("list", "trailing comma not allowed", context);
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
                    input.Advance(lineEnd);
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
                        return ParseString(input, context, '\'').AsString();
                    case JsonTokens.DoubleQuote:
                        return ParseString(input, context, '"').AsString();
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
