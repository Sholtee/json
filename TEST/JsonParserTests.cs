/********************************************************************************
* JsonParserTests.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Json.Tests
{
    using Internals;

    using JsonTokenValue = (bool IsLiteral, string Value, JsonParserFlags RequiredFlags);

    using static JsonParser;

    [TestFixture]
    public class JsonParserTests
    {
        private static int CharsLeft(TextReaderWrapper content)
        {
            int i = 0;
            while (content.PeekText(1).Length > 0)
            {
                content.Advance(1);
                i++;
            }
            return i;
        }

        public static IEnumerable<object[]> SkipSpaces_ShouldSkipWhiteSpaces_Params
        {
            get
            {
                yield return new object[] { "", 0 };
                yield return new object[] { "cica", 0 };
                yield return new object[] { " cica", 1 };
                yield return new object[] { "  cica", 2 };
                yield return new object[] { "\n", 1 };
                yield return new object[] { "\r\n", 2 };
                yield return new object[] { "\ncica", 1 };
                yield return new object[] { "\r\ncica", 2 };
                yield return new object[] { " \ncica", 2 };
                yield return new object[] { " \r\ncica", 3 };
                yield return new object[] { "  \ncica", 3 };
                yield return new object[] { "  \r\ncica", 4 };
            }
        }

        private static void SkipSpaces_ShouldSkipWhiteSpaces(string input, int shouldSkip, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            SkipSpaces(ref session, bufferSize);

            Assert.That(CharsLeft(content), Is.EqualTo(input.Length - shouldSkip));
        }

        [TestCaseSource(nameof(SkipSpaces_ShouldSkipWhiteSpaces_Params))]
        public void SkipSpaces_ShouldSkipWhiteSpaces(string input, int shouldSkip) => SkipSpaces_ShouldSkipWhiteSpaces(input, shouldSkip, 32);

        [TestCaseSource(nameof(SkipSpaces_ShouldSkipWhiteSpaces_Params))]
        public void SkipSpaces_ShouldSkipWhiteSpacesInMultipleIterations(string input, int shouldSkip) => SkipSpaces_ShouldSkipWhiteSpaces(input, shouldSkip, 1);

        public static IEnumerable<object[]> SkipSpaces_ShouldMaintainThePosition_Params
        {
            get
            {
                yield return new object[] { "", 0 };
                yield return new object[] { "cica", 0 };
                yield return new object[] { " cica", 0 };
                yield return new object[] { "cica\n", 1 };
                yield return new object[] { "cica\n\n", 2 };
                yield return new object[] { "cica\r\n", 1 };
                yield return new object[] { "cica\r\n\r\n", 2 };
                yield return new object[] { "cica\nmica", 1 };
                yield return new object[] { "cica\n\nmcia", 2 };
                yield return new object[] { "cica\r\nmica", 1 };
                yield return new object[] { "cica\r\n\r\nmica", 2 };
                yield return new object[] { "\nmica", 1 };
                yield return new object[] { "\n\nmcia", 2 };
                yield return new object[] { "\r\nmica", 1 };
                yield return new object[] { "\r\n\r\nmica", 2 };
            }
        }

        private static void SkipSpaces_ShouldMaintainTheRowIndex(string input, int rows, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            for (; content.PeekChar() is not -1;)
            {
                SkipSpaces(ref session, bufferSize);

                while (content.PeekChar() is not -1 && !char.IsWhiteSpace((char) content.PeekChar()))
                {
                    content.Advance(1);
                }
            }

            Assert.That(session.Row, Is.EqualTo(rows));
        }

        [TestCaseSource(nameof(SkipSpaces_ShouldMaintainThePosition_Params))]
        public void SkipSpaces_ShouldMaintainTheRowIndex(string input, int rows) => SkipSpaces_ShouldMaintainTheRowIndex(input, rows, 32);

        [TestCaseSource(nameof(SkipSpaces_ShouldMaintainThePosition_Params))]
        public void SkipSpaces_ShouldMaintainTheRowIndexInMultipleIterations(string input, int rows) => SkipSpaces_ShouldMaintainTheRowIndex(input, rows, 1);

        public static IEnumerable<object[]> Consume_ShouldReturnTheCurrectToken_Params
        {
            get
            {
                Dictionary<JsonTokens, JsonTokenValue> tokens = new()
                {
                    { JsonTokens.Comma, (IsLiteral: false, ",", JsonParserFlags.None) },
                    { JsonTokens.DoubleSlash, (IsLiteral: false, "//", JsonParserFlags.AllowComments) },
                    { JsonTokens.CurlyOpen, (IsLiteral: false, "{", JsonParserFlags.None) },
                    { JsonTokens.CurlyClose, (IsLiteral: false, "}", JsonParserFlags.None) },
                    { JsonTokens.SquaredOpen, (IsLiteral: false, "[", JsonParserFlags.None) },
                    { JsonTokens.SquaredClose, (IsLiteral: false, "]", JsonParserFlags.None) },
                    { JsonTokens.Colon, (IsLiteral: false, ":", JsonParserFlags.None) },
                    { JsonTokens.SingleQuote, (IsLiteral: false, "'", JsonParserFlags.AllowSingleQuotedStrings) },
                    { JsonTokens.DoubleQuote, (IsLiteral: false, "\"", JsonParserFlags.None) },
                    { JsonTokens.True, (IsLiteral: true, "true", JsonParserFlags.None) },
                    { JsonTokens.False, (IsLiteral: true, "false", JsonParserFlags.None) },
                    { JsonTokens.Null, (IsLiteral: true, "null", JsonParserFlags.None) }
                };

                foreach (KeyValuePair<JsonTokens, JsonTokenValue> token in tokens)
                {
                    if (token.Value.IsLiteral)
                    {
                        yield return new object[] { token.Value.Value.ToUpper(), token.Key, token.Value.RequiredFlags | JsonParserFlags.CaseInsensitive };
                        yield return new object[] { token.Value.Value.ToUpper(), JsonTokens.Unknown, token.Value.RequiredFlags };
                    }
                    yield return new object[] { token.Value.Value, token.Key, token.Value.RequiredFlags };

                    if (token.Value.IsLiteral)
                    {
                        yield return new object[] { $" {token.Value.Value.ToUpper()}", token.Key, token.Value.RequiredFlags | JsonParserFlags.CaseInsensitive };
                        yield return new object[] { $" {token.Value.Value.ToUpper()}", JsonTokens.Unknown, token.Value.RequiredFlags };
                    }
                    yield return new object[] { $" {token.Value.Value}", token.Key, token.Value.RequiredFlags };
                }

                yield return new object[] { $"-", JsonTokens.Number, JsonParserFlags.None };
                yield return new object[] { $"5", JsonTokens.Number, JsonParserFlags.None };
            }
        }

        [TestCaseSource(nameof(Consume_ShouldReturnTheCurrectToken_Params))]
        public void Consume_ShouldReturnTheCurrectToken(string input, object expected, JsonParserFlags flags)
        {
            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            JsonParser parser = new(flags);

            Assert.That(parser.Consume(ref session), Is.EqualTo(expected));
        }

        [Test]
        public void Consume_ShouldRejectPartialLiterals([Values("nul", "tru", "fal")] string input)
        {
            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            JsonParser parser = new();

            Assert.That(parser.Consume(ref session), Is.EqualTo(JsonTokens.Unknown));
        }

        [Test]
        public void Consume_ShouldValidateTheReturnedToken()
        {
            using TextReaderWrapper content = new StringReader("{");
 
            JsonParser parser = new();

            Assert.DoesNotThrow(() =>
            {
                Session session = new(content);
                Assert.That(parser.Consume(ref session, JsonTokens.Eof | JsonTokens.CurlyOpen), Is.EqualTo(JsonTokens.CurlyOpen));
            });

            Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                parser.Consume(ref session, JsonTokens.Eof);
            });
        }

        public static IEnumerable<object[]> ParseString_ShouldParseSingleStrings_Params
        {
            get
            {
                yield return new object[] { "\"\"", "" };
                yield return new object[] { "''", "" };
                yield return new object[] { "\"cica\"", "cica" };
                yield return new object[] { "'cica'", "cica" };
                yield return new object[] { "\"'cica'\"", "'cica'" };
                yield return new object[] { "'\"cica\"'", "\"cica\"" };
                yield return new object[] { "\"cica mica\"", "cica mica" };
                yield return new object[] { "'cica mica'", "cica mica" };
            }
        }

        private static void ParseString_ShouldParseSingleStrings(string input, string expected, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            Assert.That(ParseString(ref session, bufferSize).ToString(), Is.EqualTo(expected));
            Assert.That(content.PeekChar(), Is.EqualTo(-1));
        }

        [TestCaseSource(nameof(ParseString_ShouldParseSingleStrings_Params))]
        public void ParseString_ShouldParseSingleStrings(string input, string expected) =>
            ParseString_ShouldParseSingleStrings(input, expected, 128);

        [TestCaseSource(nameof(ParseString_ShouldParseSingleStrings_Params))]
        public void ParseString_ShouldParseSingleStringsInMultipleIterations(string input, string expected) =>
            ParseString_ShouldParseSingleStrings(input, expected, 1);

        public static IEnumerable<object[]> ParseString_ShouldProcessSingleControlCharacters_Params
        {
            get
            {
                foreach ((char Escape, char Value) in new (char Escape, char Value)[] { ('t', '\t'), ('b', '\b'), ('r', '\r'), ('n', '\n'), ('\\', '\\'), ('"', '"') })
                {
                    yield return new object[] { $"\"\\{Escape}\"", $"{Value}" };
                    yield return new object[] { $"\"\\{Escape}suffix\"", $"{Value}suffix" };
                    yield return new object[] { $"\"prefix\\{Escape}\"", $"prefix{Value}" };
                    yield return new object[] { $"\"prefix\\{Escape}suffix\"", $"prefix{Value}suffix" };
                    yield return new object[] { $"\"has \\{Escape} space\"", $"has {Value} space" };

                    yield return new object[] { $"\"\\{Escape}\\{Escape}\"", $"{Value}{Value}" };
                    yield return new object[] { $"\"\\{Escape}\\{Escape}suffix\"", $"{Value}{Value}suffix" };
                    yield return new object[] { $"\"prefix\\{Escape}\\{Escape}\"", $"prefix{Value}{Value}" };
                    yield return new object[] { $"\"prefix\\{Escape}\\{Escape}suffix\"", $"prefix{Value}{Value}suffix" };
                    yield return new object[] { $"\"has \\{Escape}\\{Escape} space\"", $"has {Value}{Value} space" };

                    yield return new object[] { $"\"\\{Escape}middle\\{Escape}\"", $"{Value}middle{Value}" };
                    yield return new object[] { $"\"\\{Escape}middle\\{Escape}suffix\"", $"{Value}middle{Value}suffix" };
                    yield return new object[] { $"\"prefix\\{Escape}middle\\{Escape}\"", $"prefix{Value}middle{Value}" };
                    yield return new object[] { $"\"prefix\\{Escape}middle\\{Escape}suffix\"", $"prefix{Value}middle{Value}suffix" };
                    yield return new object[] { $"\"has \\{Escape}middle\\{Escape} space\"", $"has {Value}middle{Value} space" };
                }
            }
        }

        private static void ParseString_ShouldProcessSingleControlCharacters(string input, string expected, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            Assert.That(ParseString(ref session, bufferSize).ToString(), Is.EqualTo(expected));
            Assert.That(content.PeekChar(), Is.EqualTo(-1));
        }

        [TestCaseSource(nameof(ParseString_ShouldProcessSingleControlCharacters_Params))]
        public void ParseString_ShouldProcessSingleControlCharacters(string input, string expected) =>
            ParseString_ShouldProcessSingleControlCharacters(input, expected, 128);

        [TestCaseSource(nameof(ParseString_ShouldProcessSingleControlCharacters_Params))]
        public void ParseString_ShouldProcessSingleControlCharactersInMultipleIterations(string input, string expected) =>
            ParseString_ShouldProcessSingleControlCharacters(input, expected, 1);

        [Test]
        public void ParseString_ShouldThrowOnUnknownControlCharacter([Values("\"\\x\"", "\"\\\t\"", "\"prefix\\x\"", "\"\\\tsuffix\"")] string input)
        {
            using TextReaderWrapper content = new StringReader(input);

            Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                ParseString(ref session);
            });
        }

        public static IEnumerable<object[]> ParseString_ShouldThrowOnUnescapedControl_Params
        {
            get
            {
                foreach (char chr in new char[] { '\t', '\r', '\n' })
                {
                    yield return new object[] { $"\"{chr}\"", 2 };
                    yield return new object[] { $"\"prefix{chr}\"", 8 };
                    yield return new object[] { $"\"{chr}suffix\"", 2 };
                    yield return new object[] { $"\"prefix{chr}suffix\"", 8 };
                }
            }
        }

        private static void ParseString_ShouldThrowOnUnescapedControl(string input, int position, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            JsonParserException ex = Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                ParseString(ref session, bufferSize);
            })!;
            Assert.That(ex.Row, Is.EqualTo(0));
            Assert.That(ex.Column, Is.EqualTo(position));
        }

        [TestCaseSource(nameof(ParseString_ShouldThrowOnUnescapedControl_Params))]
        public void ParseString_ShouldThrowOnUnescapedControl(string input, int position) =>
            ParseString_ShouldThrowOnUnescapedControl(input, position, 128);

        [TestCaseSource(nameof(ParseString_ShouldThrowOnUnescapedControl_Params))]
        public void ParseString_ShouldThrowOnUnescapedControlInMultipleIterations(string input, int position) =>
            ParseString_ShouldThrowOnUnescapedControl(input, position, 1);

        public static IEnumerable<object[]> ParseString_ShouldThrowOnUnterminatedString_Params
        {
            get
            {
                yield return new object[] { "\"", 1 };
                yield return new object[] { "\"cica", 5 };
                yield return new object[] { "\"cica\t", 6 };
                yield return new object[] { "\"\\", 1 };
                yield return new object[] { "\"\\\"", 3 };
            }
        }

        private static void ParseString_ShouldThrowOnUnterminatedString(string input, int col, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            JsonParserException ex = Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                ParseString(ref session, bufferSize);
            })!;
            Assert.That(ex.Row, Is.EqualTo(0));
            Assert.That(ex.Column, Is.EqualTo(col));
        }

        [TestCaseSource(nameof(ParseString_ShouldThrowOnUnterminatedString_Params))]
        public void ParseString_ShouldThrowOnUnterminatedString(string input, int col) =>
            ParseString_ShouldThrowOnUnterminatedString(input, col, 128);

        [TestCaseSource(nameof(ParseString_ShouldThrowOnUnterminatedString_Params))]
        public void ParseString_ShouldThrowOnUnterminatedStringInMultipleIterations(string input, int col) =>
            ParseString_ShouldThrowOnUnterminatedString(input, col, 1);

        public static IEnumerable<object[]> ParseString_ShouldParseEscapedUnicodeCharacters_Params
        {
            get
            {
                foreach ((string Input, string Expected) @case in new (string, string)[] { ("\\u00E1", "á"), ("\\u00E1\\u00E1", "áá"), ("\\u00E1middle\\u00E1", "ámiddleá"), ("\\uD83D\\uDE01", "😁") })
                {
                    yield return new object[] { $"\"{@case.Input}\"", @case.Expected };
                    yield return new object[] { $"\"prefix{@case.Input}\"", $"prefix{@case.Expected}" };
                    yield return new object[] { $"\"{@case.Input}suffix\"", $"{@case.Expected}suffix" };
                    yield return new object[] { $"\"prefix{@case.Input}suffix\"", $"prefix{@case.Expected}suffix" };
                }
            }
        }

        private static void ParseString_ShouldParseEscapedUnicodeCharacters(string input, string expected, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            Assert.That(ParseString(ref session, bufferSize).ToString(), Is.EqualTo(expected));
            Assert.That(content.PeekChar(), Is.EqualTo(-1));
        }

        [TestCaseSource(nameof(ParseString_ShouldParseEscapedUnicodeCharacters_Params))]
        public void ParseString_ShouldParseEscapedUnicodeCharacters(string input, string expected) =>
            ParseString_ShouldParseEscapedUnicodeCharacters(input, expected, 128);

        [TestCaseSource(nameof(ParseString_ShouldParseEscapedUnicodeCharacters_Params))]
        public void ParseString_ShouldParseEscapedUnicodeCharactersInMultipleIterations(string input, string expected) =>
            ParseString_ShouldParseEscapedUnicodeCharacters(input, expected, 1);

        public static IEnumerable<object[]> ParseString_ShouldThrowOnInvalidHex_Params
        {
            get
            {
                // invalid
                yield return new object[] { "\"\\uX0E1\"", 2 };
                yield return new object[] { "\"\\u0XE1bc\"", 2 };
                yield return new object[] { "\"cb\\u00X1\"", 4 };
                yield return new object[] { "\"123\\u00EXbc\"", 5 };
                yield return new object[] { "\"\\uD83D\\uD 01\"", 8 };

                // unterminated
                yield return new object[] { "\"\\u00E\"", 2 };
                yield return new object[] { "\"\\u00E", 2 };
                yield return new object[] { "\"cb\\u00E\"", 4 };
                yield return new object[] { "\"cb\\u00E", 4 };
            }
        }

        private static void ParseString_ShouldThrowOnInvalidHex(string input, int col, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            JsonParserException ex = Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                ParseString(ref session, bufferSize);
            })!;
            Assert.That(ex.Row, Is.EqualTo(0));
            Assert.That(ex.Column, Is.EqualTo(col));
        }

        [TestCaseSource(nameof(ParseString_ShouldThrowOnInvalidHex_Params))]
        public void ParseString_ShouldThrowOnInvalidHex(string input, int col) =>
            ParseString_ShouldThrowOnInvalidHex(input, col, 128);

        [TestCaseSource(nameof(ParseString_ShouldThrowOnInvalidHex_Params))]
        public void ParseString_ShouldThrowOnInvalidHexInMultipleIterations(string input, int col) =>
            ParseString_ShouldThrowOnInvalidHex(input, col, 1);

        public static IEnumerable<object[]> ParseComment_ShouldConsumeComments_Params
        {
            get
            {
                string[] inputs = ["", "cica"];

                foreach (string input in inputs)
                {
                    yield return new object[] { $"//{input}", input, 0 };
                    yield return new object[] { $"//{input}\n", input, 0 };
                    yield return new object[] { $"//{input}\r\n", input, 0 };
                    yield return new object[] { $"//{input}\n{{", input, 1 };
                    yield return new object[] { $"//{input}\r\n{{", input, 1 };
                }
            }
        }

        private static void ParseComment_ShouldConsumeComments(string input, string expected, int charsLeft, int bufferSize)
        {
            //
            // We cannot mock methods having Span<T> parameter =(
            //

            string lastComment = null!;

            DeserializationContext ctx = new()
            {
                SupportedTypes = JsonDataTypes.Unkown,
                ParseComment = chars => lastComment = chars.ToString()
            };

            using TextReaderWrapper content = new StringReader(input);

            Assert.DoesNotThrow(() =>
            {
                Session session = new(content);
                ParseComment(ref session, ctx, bufferSize);
            });
            Assert.That(lastComment, Is.EqualTo(expected));
            Assert.That(CharsLeft(content), Is.EqualTo(charsLeft));        
        }

        [TestCaseSource(nameof(ParseComment_ShouldConsumeComments_Params))]
        public void ParseComment_ShouldConsumeComments(string input, string expected, int charsLeft) =>
            ParseComment_ShouldConsumeComments(input, expected, charsLeft, 32);

        [TestCaseSource(nameof(ParseComment_ShouldConsumeComments_Params))]
        public void ParseComment_ShouldConsumeCommentsInMultipleIterations(string input, string expected, int charsLeft) =>
            ParseComment_ShouldConsumeComments(input, expected, charsLeft, 1);

        public static IEnumerable<object[]> ParseMultilineComment_ShouldConsumeComments_Params
        {
            get
            {
                (string, int, int)[] inputs = [("", 0, 0), ("cica", 0, 4), ("cica*", 0, 5), ("cica\n*", 1, 1), ("cica/", 0, 5), ("cica\n/", 1, 1), ("cica\nmica", 1, 4), ("cica\nmica00", 1, 6), ("cica\r\nmica", 1, 4), ("cica\r\nmica\r\n", 2, 0)];

                foreach ((string Str, int Row, int Column) input in inputs)
                {
                    yield return new object[] { $"/*{input.Str}*/", input.Str, 0, input.Row, input.Column + (input.Row > 0 ? 2 : 4) };
                    yield return new object[] { $"/*{input.Str}*/\n", input.Str, 1, input.Row, input.Column + (input.Row > 0 ? 2 : 4) };
                    yield return new object[] { $"/*{input.Str}*/cica", input.Str, 4, input.Row, input.Column + (input.Row > 0 ? 2 : 4) };
                }
            }
        }

        private static void ParseMultilineComment_ShouldConsumeComments(string input, string expected, int charsLeft, int rowCount, int columnCount, int bufferSize)
        {
            //
            // We cannot mock methods having Span<T> parameter =(
            //

            string lastComment = null!;

            DeserializationContext ctx = new()
            {
                SupportedTypes = JsonDataTypes.Unkown,
                ParseComment = chars => lastComment = chars.ToString()
            };

            using TextReaderWrapper content = new StringReader(input);

            Assert.DoesNotThrow(() =>
            {
                Session session = new(content);
                ParseMultilineComment(ref session, ctx, bufferSize);
                Assert.That(session.Row, Is.EqualTo(rowCount));
                Assert.That(session.Column, Is.EqualTo(columnCount));
            });
            Assert.That(lastComment, Is.EqualTo(expected));
            Assert.That(CharsLeft(content), Is.EqualTo(charsLeft));
        }

        [TestCaseSource(nameof(ParseMultilineComment_ShouldConsumeComments_Params))]
        public void ParseMultilineComment_ShouldConsumeComments(string input, string expected, int charsLeft, int rowCount, int columnCount) =>
            ParseMultilineComment_ShouldConsumeComments(input, expected, charsLeft, rowCount, columnCount, 32);

        [TestCaseSource(nameof(ParseMultilineComment_ShouldConsumeComments_Params))]
        public void ParseMultilineComment_ShouldConsumeCommentsInMultipleIterations(string input, string expected, int charsLeft, int rowCount, int columnCount) =>
            ParseMultilineComment_ShouldConsumeComments(input, expected, charsLeft, rowCount, columnCount, 1);

        public static IEnumerable<object[]> ParseMultilineComment_ShouldThrowOnInvalidComment_Params
        {
            get
            {
                yield return new object[] { "/*", 0, 2 };
                yield return new object[] { "/*/", 0, 3 };
                yield return new object[] { "/**", 0, 3 };
                yield return new object[] { "/*\n", 1, 0 };
                yield return new object[] { "/*\n/", 1, 1 };
                yield return new object[] { "/*\n*", 1, 1 };
                yield return new object[] { "/*cica\n", 1, 0 };
                yield return new object[] { "/*cica\n/", 1, 1 };
                yield return new object[] { "/*cica\n*", 1, 1 };
                yield return new object[] { "/*\ncica", 1, 4 };
                yield return new object[] { "/*\ncica/", 1, 5 };
                yield return new object[] { "/*\ncica*", 1, 5 };
            }
        }

        private void ParseMultilineComment_ShouldThrowOnInvalidComment(string input, int rowCount, int columnCount, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                ParseMultilineComment(ref session, DeserializationContext.For(typeof(object)), bufferSize);
                Assert.That(session.Row, Is.EqualTo(rowCount));
                Assert.That(session.Column, Is.EqualTo(columnCount));
            });

            Assert.That(CharsLeft(content), Is.EqualTo(0));
        }

        [TestCaseSource(nameof(ParseMultilineComment_ShouldThrowOnInvalidComment_Params))]
        public void ParseMultilineComment_ShouldThrowOnInvalidComment(string input, int rowCount, int columnCount) =>
            ParseMultilineComment_ShouldThrowOnInvalidComment(input, rowCount, columnCount, 32);

        [TestCaseSource(nameof(ParseMultilineComment_ShouldThrowOnInvalidComment_Params))]
        public void ParseMultilineComment_ShouldThrowOnInvalidCommentInMultipleIterations(string input, int rowCount, int columnCount) =>
            ParseMultilineComment_ShouldThrowOnInvalidComment(input, rowCount, columnCount, 1);

        public static IEnumerable<object[]> ParseNumber_ShouldReturnTheAppropriateValue_Params
        {
            get
            {
                yield return new object[] { "0", (long)0, 0 };
                yield return new object[] { "1", (long)1, 0 };
                yield return new object[] { "100", (long)100, 0 };
                yield return new object[] { "100.0", (double)100.0, 0 };
                yield return new object[] { "1.0E+2", (double)100.0, 0 };
                yield return new object[] { "1.0E-2", (double)0.01, 0 };
                yield return new object[] { "1E+2", (double)100.0, 0 };
                yield return new object[] { "1E-2", (double)0.01, 0 };
                yield return new object[] { "-0", (long)0, 0 };
                yield return new object[] { "-1", (long)-1, 0 };
                yield return new object[] { "-100", (long)-100, 0 };
                yield return new object[] { "-100.0", (double)-100.0, 0 };
                yield return new object[] { "-1.0E+2", (double)-100.0, 0 };
                yield return new object[] { "-1.0E-2", (double)-0.01, 0 };
                yield return new object[] { "-1E+2", (double)-100.0, 0 };
                yield return new object[] { "-1E-2", (double)-0.01, 0 };

                yield return new object[] { "0,", (long)0, 1 };
                yield return new object[] { "1,", (long)1, 1 };
                yield return new object[] { "100,", (long)100, 1 };
                yield return new object[] { "100.0,", (double)100.0, 1 };
                yield return new object[] { "1.0E+2,", (double)100.0, 1 };
                yield return new object[] { "1.0E-2,", (double)0.01, 1 };
                yield return new object[] { "1E+2,", (double)100.0, 1 };
                yield return new object[] { "1E-2,", (double)0.01, 1 };
                yield return new object[] { "-0,", (long)0, 1 };
                yield return new object[] { "-1,", (long)-1, 1 };
                yield return new object[] { "-100,", (long)-100, 1 };
                yield return new object[] { "-100.0,", (double)-100.0, 1 };
                yield return new object[] { "-1.0E+2,", (double)-100.0, 1 };
                yield return new object[] { "-1.0E-2,", (double)-0.01, 1 };
                yield return new object[] { "-1E+2,", (double)-100.0, 1 };
                yield return new object[] { "-1E-2,", (double)-0.01, 1 };
            }
        }

        private static void ParseNumber_ShouldReturnTheAppropriateValue(string input, object expected, int charsLeft, int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            object result = ParseNumber(ref session, DeserializationContext.For(typeof(object)), bufferSize);

            Assert.That(result.GetType(), Is.EqualTo(expected.GetType()));
            Assert.That(result, Is.EqualTo(expected).Within(expected is double ? 0.1 : 0).Percent);
            Assert.That(CharsLeft(content), Is.EqualTo(charsLeft));
        }

        [TestCaseSource(nameof(ParseNumber_ShouldReturnTheAppropriateValue_Params))]
        public void ParseNumber_ShouldReturnTheAppropriateValue(string input, object expected, int charsLeft) =>
            ParseNumber_ShouldReturnTheAppropriateValue(input, expected, charsLeft, 16);

        [TestCaseSource(nameof(ParseNumber_ShouldReturnTheAppropriateValue_Params))]
        public void ParseNumber_ShouldReturnTheAppropriateValueInMultipleIterations(string input, object expected, int charsLeft) =>
            ParseNumber_ShouldReturnTheAppropriateValue(input, expected, charsLeft, 1);

        [Test]
        public void ParseNumber_ShouldThrowOnInvalidValue([Values("1E-")] string input, [Values(1, 16)] int bufferSize)
        {
            using TextReaderWrapper content = new StringReader(input);

            Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                ParseNumber(ref session, DeserializationContext.For(typeof(object)), bufferSize);
            });
        }

        [TestCase("[", 1)]
        [TestCase("[,]", 1)]
        [TestCase("[x]", 1)]
        [TestCase("[\"cica\"", 7)]
        [TestCase("[\"cica\",]", 8)]
        [TestCase("[\"cica\", x]", 9)]
        [TestCase("[\"cica\", \"mica\"", 15)]
        [TestCase("[\"cica\", \"mica\",]", 16)]
        [TestCase("[\"cica\", \"mica\", x]", 17)]
        public void ParseList_ShouldThrowOnInvalidList(string input, int errorPos)
        {
            JsonParser parser = new();

            using TextReaderWrapper content = new StringReader(input);

            JsonParserException ex = Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                parser.ParseList(ref session, 0, DeserializationContext.For(typeof(object)));
            })!;
            Assert.That(ex.Column, Is.EqualTo(errorPos));
        }

        [Test]
        public void ParseList_ShouldThrowOnInvalidContext()
        {
            JsonParser parser = new();

            using TextReaderWrapper content = new StringReader("[]");

            Assert.Throws<InvalidOperationException>(() =>
            {
                Session session = new(content);
                parser.ParseList(ref session, 0, DeserializationContext.For(typeof(object)) with { GetListItemContext = null });
            });
        }

        public static IEnumerable<object[]> ParseList_ShouldParse_Params
        {
            get
            {
                yield return new object[] { "[]", new List<object?> { }, JsonParserFlags.None };
                yield return new object[] { "[1]", new List<object?> { 1 }, JsonParserFlags.None };
                yield return new object[] { "[\"1\"]", new List<object?> { "1" }, JsonParserFlags.None };
                yield return new object[] { "[1,]", new List<object?> { 1 }, JsonParserFlags.AllowTrailingComma };
                yield return new object[] { "[\"1\",]", new List<object?> { "1" }, JsonParserFlags.AllowTrailingComma }; 
                yield return new object[] { "[\r\n\"1\"\r\n]", new List<object?> { "1" }, JsonParserFlags.None };
                yield return new object[] { "[null, true, false, 1, \"1\"]", new List<object?> { null, true, false, 1, "1" }, JsonParserFlags.None };
                yield return new object[] { "[[]]", new List<object?> { new List<object?> { } }, JsonParserFlags.None };

                foreach (string comment in new string[] { "//comment\r\n", "/*comment*/" })
                {
                    yield return new object[] { $"[\"1\",{comment}]", new List<object?> { "1" }, JsonParserFlags.AllowTrailingComma | JsonParserFlags.AllowComments };
                    yield return new object[] { $"[\r\n{comment}\"1\"\r\n]", new List<object?> { "1" }, JsonParserFlags.AllowComments };
                    yield return new object[] { $"[\r\n\"1\"\r\n{comment}]", new List<object?> { "1" }, JsonParserFlags.AllowComments };
                }
            }
        }

        [TestCaseSource(nameof(ParseList_ShouldParse_Params))]
        public void ParseList_ShouldParse(string input, List<object?> expected, JsonParserFlags flags)
        {
            JsonParser parser = new(flags);

            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            Assert.That(parser.ParseList(ref session, 0, DeserializationContext.For(typeof(object))), Is.EquivalentTo(expected));
            Assert.That(content.PeekChar(), Is.EqualTo(-1));
        }

        [Test]
        public void ParseList_ShouldSkipUnknownItems([Values("[1, 2, 3]", "[1, [2], 3]", "[1, [[2]], 3]", "[1, {}, 3]", "[1, {\"a\": []}, 3]")] string input)
        {
            JsonParser parser = new();

            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            DeserializationContext untypedCtx = DeserializationContext.For(typeof(object));

            Assert.That
            (
                parser.ParseList
                (
                    ref session,
                    0,
                    untypedCtx with
                    {
                        GetListItemContext = (int i, out DeserializationContext ctx) =>
                        {
                            untypedCtx.GetListItemContext!(i, out ctx);
                            return i != 1;
                        }
                    }
                ), Is.EquivalentTo(new List<object?> { 1, 3 })
            );
            Assert.That(content.PeekChar(), Is.EqualTo(-1));
        }

        [Test]
        public void ParseList_ShouldThrowOnUnknownItems([Values("[1, 2, 3]", "[1, [2], 3]", "[1, [[2]], 3]", "[1, {}, 3]", "[1, {\"a\": []}, 3]")] string input)
        {
            JsonParser parser = new(JsonParserFlags.ThrowOnUnknownListItem);

            using TextReaderWrapper content = new StringReader(input);

            DeserializationContext untypedCtx = DeserializationContext.For(typeof(object));

            JsonParserException ex = Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                parser.ParseList
                (
                    ref session,
                    0,
                    untypedCtx with
                    {
                        GetListItemContext = (int i, out DeserializationContext ctx) =>
                        {
                            untypedCtx.GetListItemContext!(i, out ctx);
                            return i != 1;
                        }
                    }
                );
            })!;
            Assert.That(ex.Column, Is.EqualTo(4));
        }

        [TestCase("{", 1)]
        [TestCase("{,}", 1)]
        [TestCase("{x: 1}", 1)]
        [TestCase("{\"cica\"", 7)]
        [TestCase("{\"cica\"}", 7)]
        [TestCase("{\"cica\": }", 9)]
        [TestCase("{\"cica\": x}", 9)]
        [TestCase("{\"cica\": 1,}", 11)]
        public void ParseObject_ShouldThrowOnInvalidObject(string input, int errorPos)
        {
            JsonParser parser = new();

            using TextReaderWrapper content = new StringReader(input);

            JsonParserException ex = Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                parser.ParseObject(ref session, 0, DeserializationContext.For(typeof(object)));
            })!;
            Assert.That(ex.Column, Is.EqualTo(errorPos));
        }

        [Test]
        public void ParseObject_ShouldThrowOnInvalidContext()
        {
            JsonParser parser = new();

            using TextReaderWrapper content = new StringReader("{}");

            Assert.Throws<InvalidOperationException>(() =>
            {
                Session session = new(content);
                parser.ParseObject(ref session, 0, DeserializationContext.For(typeof(object)) with { GetPropertyContext = null });
            });
        }

        public static IEnumerable<object[]> ParseObject_ShouldParse_Params
        {
            get
            {
                yield return new object[] { "{}", new Dictionary<string, object?> { }, JsonParserFlags.None };
                yield return new object[] { "{\"cica\": 1}", new Dictionary<string, object?> { { "cica", 1 } }, JsonParserFlags.None };
                yield return new object[] { "{\"cica\": \"1\"}", new Dictionary<string, object?> { { "cica", "1" } }, JsonParserFlags.None };
                yield return new object[] { "{\"cica\": \"1\", \"cica\": 1986}", new Dictionary<string, object?> { { "cica", 1986 } }, JsonParserFlags.None };
                yield return new object[] { "{\"cica\": 1,}", new Dictionary<string, object?> { { "cica", 1 } }, JsonParserFlags.AllowTrailingComma };
                yield return new object[] { "{\"cica\": \"1\",}", new Dictionary<string, object?> { { "cica", "1" } }, JsonParserFlags.AllowTrailingComma };
                yield return new object[] { "{\r\n  \"cica\": 1\r\n}", new Dictionary<string, object?> { { "cica", 1 } }, JsonParserFlags.None };
                yield return new object[] { "{\"nested\": {}}", new Dictionary<string, object?> { { "nested", new Dictionary<string, object?> { } } }, JsonParserFlags.None };

                foreach (string comment in new string[] { "//comment\r\n", "/*comment*/" })
                {
                    yield return new object[] { $"{{\r\n  {comment}  \"cica\": 1\r\n}}", new Dictionary<string, object?> { { "cica", 1 } }, JsonParserFlags.AllowComments };
                    yield return new object[] { $"{{\r\n  \"cica\": 1 {comment}}}", new Dictionary<string, object?> { { "cica", 1 } }, JsonParserFlags.AllowComments };
                    yield return new object[] { $"{{\r\n  \"cica\": 1, {comment}}}", new Dictionary<string, object?> { { "cica", 1 } }, JsonParserFlags.AllowComments | JsonParserFlags.AllowTrailingComma };
                }
            }
        }

        [TestCaseSource(nameof(ParseObject_ShouldParse_Params))]
        public void ParseObject_ShouldParse(string input, Dictionary<string, object?> expected, JsonParserFlags flags)
        {
            JsonParser parser = new(flags);

            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            Assert.That(parser.ParseObject(ref session, 0, DeserializationContext.For(typeof(object))), Is.EquivalentTo(expected));
            Assert.That(content.PeekChar(), Is.EqualTo(-1));
        }

        [Test]
        public void ParseObject_ShouldSkipUnknownItems([Values("{\"1\": 1, \"2\": 2, \"3\": 3}", "{\"1\": 1, \"2\": [2], \"3\": 3}", "{\"1\": 1, \"2\": [[2]], \"3\": 3}", "{\"1\": 1, \"2\": {}, \"3\": 3}", "{\"1\": 1, \"2\": {\"2\": {}}, \"3\": 3}")] string input)
        {
            JsonParser parser = new();

            using TextReaderWrapper content = new StringReader(input);

            Session session = new(content);

            DeserializationContext untypedCtx = DeserializationContext.For(typeof(object));

            Assert.That
            (
                parser.ParseObject
                (
                    ref session,
                    0,
                    untypedCtx with
                    {
                        GetPropertyContext = (ReadOnlySpan<char> name, bool ignoreCase, out DeserializationContext ctx) =>
                        {
                            untypedCtx.GetPropertyContext!(name, ignoreCase, out ctx);
                            return !name.ToString().Equals("2", ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                        }
                    }
                ),
                Is.EquivalentTo(new Dictionary<string, object?> { { "1", 1 }, { "3", 3 } })
            );
            Assert.That(content.PeekChar(), Is.EqualTo(-1));
        }

        [Test]
        public void ParseObject_ShouldThrowOnUnknownItems([Values("{\"1\": 1, \"2\": 2, \"3\": 3}", "{\"1\": 1, \"2\": [2], \"3\": 3}", "{\"1\": 1, \"2\": [[2]], \"3\": 3}", "{\"1\": 1, \"2\": {}, \"3\": 3}", "{\"1\": 1, \"2\": {\"2\": {}}, \"3\": 3}")] string input)
        {
            JsonParser parser = new(JsonParserFlags.ThrowOnUnknownProperty);

            using TextReaderWrapper content = new StringReader(input);

            DeserializationContext untypedCtx = DeserializationContext.For(typeof(object));

            JsonParserException ex = Assert.Throws<JsonParserException>(() =>
            {
                Session session = new(content);
                parser.ParseObject
                (
                    ref session,
                    0,
                    untypedCtx with
                    {
                        GetPropertyContext = (ReadOnlySpan<char> name, bool ignoreCase, out DeserializationContext ctx) =>
                        {
                            untypedCtx.GetPropertyContext!(name, ignoreCase, out ctx);
                            return !name.ToString().Equals("2", ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                        }
                    }
                );
            })!;
            Assert.That(ex.Column, Is.EqualTo(12));
        }

        [TestCase("true", JsonParserFlags.None, JsonDataTypes.Boolean, true)]
        [TestCase("false", JsonParserFlags.None, JsonDataTypes.Boolean, false)]
        [TestCase("null", JsonParserFlags.None, JsonDataTypes.Null, null)]
        [TestCase("True", JsonParserFlags.CaseInsensitive, JsonDataTypes.Boolean, true)]
        [TestCase("False", JsonParserFlags.CaseInsensitive, JsonDataTypes.Boolean, false)]
        [TestCase("Null", JsonParserFlags.CaseInsensitive, JsonDataTypes.Null, null)]
        public void Parse_ShouldParseLiterals(string input, JsonParserFlags flags, JsonDataTypes supportedTypes, object expected)
        {
            JsonParser parser = new(flags);

            StringReader content = new(input);

            Assert.That(parser.Parse(content, DeserializationContext.For(typeof(object)) with { SupportedTypes = supportedTypes }, default), Is.EqualTo(expected));
            Assert.That(content.Peek(), Is.EqualTo(-1));
        }

        [Test]
        public void Parse_ShouldBeCaseSensitiveByDefault()
        {
            JsonParser parser = new();

            StringReader content = new("Null");

            JsonParserException ex = Assert.Throws<JsonParserException>(() => parser.Parse(content, DeserializationContext.For(typeof(object)), default))!;
            Assert.That(ex.Column, Is.EqualTo(0));
        }

        [TestCase("\"cica\"", "cica", null)]
        [TestCase("1986", 1986, null)]
        [TestCase("  \"cica\"", "cica", null)]
        [TestCase("  1986", 1986, null)]
        [TestCase("  \"cica\"  ", "cica", null)]
        [TestCase("  1986  ", 1986, null)]

        [TestCase("//comment\n1986  ", 1986, "comment")]
        [TestCase("  \"cica\"  //comment", "cica", "comment")]
        [TestCase("  1986\r\n//comment", 1986, "comment")]
        [TestCase("  1986\r\n//comment\r\n  ", 1986, "comment")]

        [TestCase("/*comment*/1986  ", 1986, "comment")]
        [TestCase("  \"cica\"  /*comment*/", "cica", "comment")]
        [TestCase("  1986\r\n/*comment*/", 1986, "comment")]
        [TestCase("  1986\r\n/*comment*/\r\n  ", 1986, "comment")]

        [TestCase("/*comment\r\ncomment2*/1986  ", 1986, "comment\r\ncomment2")]
        [TestCase("  \"cica\"  /*comment\r\ncomment2*/", "cica", "comment\r\ncomment2")]
        [TestCase("  1986\r\n/*comment\r\ncomment2*/", 1986, "comment\r\ncomment2")]
        [TestCase("  1986\r\n/*comment\r\ncomment2*/\r\n  ", 1986, "comment\r\ncomment2")]
        public void Parse_ShouldParseValues(string input, object expected, string? comment)
        {
            string? got = null;

            JsonParser parser = new(JsonParserFlags.AllowComments, 0);

            StringReader content = new(input);

            Assert.That(parser.Parse(content, DeserializationContext.For(typeof(object)) with { ParseComment = chars => got = chars.ToString() }, default), Is.EqualTo(expected));
            Assert.That(got, Is.EqualTo(comment));
            Assert.That(content.Peek(), Is.EqualTo(-1));
        }

        [TestCase("\"cica\"", 0, false)]
        [TestCase("[]", 0, true)]
        [TestCase("[\"cica\"]", 0, true)]
        [TestCase("[\"cica\"]", 1, false)]
        [TestCase("[[\"cica\"]]", 1, true)]
        [TestCase("{}", 0, true)]
        [TestCase("{\"cica\": 1986}", 0, true)]
        [TestCase("{\"cica\": 1986}", 1, false)]
        [TestCase("{\"cica\": [1986]}", 1, true)]
        [TestCase("{\"cica\": [1986]}", 2, false)]
        public void Parse_ShouldCheckTheDepth(string input, int maxDepth, bool shouldThrow)
        {
            JsonParser parser = new(maxDepth: maxDepth);

            StringReader content = new(input);

            if (shouldThrow)
                Assert.Throws<JsonParserException>(() => parser.Parse(content, DeserializationContext.For(typeof(object)), default));
            else
                Assert.DoesNotThrow(() => parser.Parse(content, DeserializationContext.For(typeof(object)), default));
        }

        [Test]
        public void Parse_CanBeCancelled([Values("{}", "5", "\"cica\"")] string input)
        {
            JsonParser parser = new();

            StringReader content = new(input);

            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            Assert.Throws<OperationCanceledException>(() => parser.Parse(content, DeserializationContext.For(typeof(object)), cancellationTokenSource.Token));
        }

        [Test]
        public void Parse_ShouldVerify()
        {
            ICollection<string> errors;

            Mock<DeserializationContext.VerifyDelegate> mockValidator = new(MockBehavior.Strict);
            mockValidator
                .Setup(v => v.Invoke((long) 1986, out errors))
                .Returns(true);

            JsonParser parser = new(JsonParserFlags.None, 0);

            StringReader content = new("1986");

            parser.Parse(content, DeserializationContext.For(typeof(object)) with { SupportedTypes = JsonDataTypes.Number, Verify = mockValidator.Object }, default);

            mockValidator.Verify(v => v.Invoke((long) 1986, out errors), Times.Once);
        }

        [Test]
        public void Parse_ShouldConvert()
        {
            object? ret;

            Mock<DeserializationContext.ConvertDelegate> mockConverter = new(MockBehavior.Strict);
            mockConverter
                .Setup(c => c.Invoke((long) 1986, out ret))
                .Returns((object? _, out object? ret) => { ret = 1991; return true; });

            JsonParser parser = new(JsonParserFlags.None, 0);

            StringReader content = new("1986");

            ret = parser.Parse(content, DeserializationContext.For(typeof(object)) with { Convert = mockConverter.Object }, default);

            Assert.That(ret, Is.EqualTo(1991));
            mockConverter.Verify(c => c.Invoke((long) 1986, out ret), Times.Once);
        }

        [Test]
        public void Parse_ShouldThrowIfConversationFailed()
        {
            JsonParser parser = new(JsonParserFlags.None, 0);

            StringReader content = new("1986");

            Assert.Throws<JsonParserException>(() => parser.Parse(content, DeserializationContext.For(typeof(object)) with { Convert = (object? _, out object? ret) => { ret = null; return false; } }, default));
        }

        [Test]
        public void Parse_ShouldThrowIfStringConversationFailed()
        {
            JsonParser parser = new(JsonParserFlags.None, 0);

            StringReader content = new("\"1986\"");

            Assert.Throws<JsonParserException>(() => parser.Parse(content, DeserializationContext.For(typeof(object)) with { ConvertString = (ReadOnlySpan<char> _, bool _, out object? ret) => { ret = null; return false; } }, default));
        }

        [Test]
        public void Parse_ShouldThrowIfVerificationFailed()
        {
            DeserializationContext ctx = DeserializationContext.For(typeof(object)) with
            {
                SupportedTypes = JsonDataTypes.Number,
                Verify = (object? val, out ICollection<string> errors) =>
                {
                    errors = ["some error"];
                    return false;
                }
            };

            JsonParser parser = new(JsonParserFlags.None, 0);

            StringReader content = new("1986");

            JsonParserException ex = Assert.Throws<JsonParserException>(() => parser.Parse(content, ctx, default))!;
            Assert.That(ex.Message, Does.Contain("some error"));
        }

        [TestCase("large1.json", 5000)]
        [TestCase("large2.json", 11351)]
        public void Parse_ShouldHandleLargeInput(string file, int expectedLength)
        {
            JsonParser parser = new();

            using StreamReader content = new
            (
                Path.Combine
                (
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                    file
                )
            );

            IList? result = parser.Parse(content, DeserializationContext.For(typeof(object)), default) as IList;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Count, Is.EqualTo(expectedLength));
        }

        [Test]
        public void Parse_ShouldBeNullChecked() =>
            Assert.Throws<ArgumentNullException>(() => new JsonParser().Parse(null!, DeserializationContext.For(typeof(object)), default));
    }
}
