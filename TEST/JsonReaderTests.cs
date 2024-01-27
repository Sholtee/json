/********************************************************************************
* JsonReaderTests.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Json.Tests
{
    using Internals;

    using JsonTokenValue = (bool IsLiteral, string Value, JsonReaderFlags RequiredFlags);

    [TestFixture]
    public class JsonReaderTests
    {
        private static JsonReader CreateReader(string input, out ITextReader reader, JsonReaderFlags flags = JsonReaderFlags.None, JsonDataTypes supportedTypes = JsonDataTypes.String)
        {
            reader = new StringReader(input);

            return new JsonReader(reader, UntypedDeserializationContext.Instance with { SupportedTypes = supportedTypes }, flags, int.MaxValue);
        }

        public static IEnumerable<object[]> SkipSpaces_ShouldSkipWhiteSpaces_Params
        {
            get
            {
                yield return new object[] { "", 0 };
                yield return new object[] { "cica", 0 };
                yield return new object[] { " cica", 1 };
                yield return new object[] { "  cica", 2 };
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
            using JsonReader rdr = CreateReader(input, out ITextReader content);
            rdr.SkipSpaces(bufferSize);

            Assert.That(content.CharsLeft, Is.EqualTo(input.Length - shouldSkip));
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
            using JsonReader rdr = CreateReader(input, out ITextReader content);

            for (; content.CharsLeft > 0;)
            {
                rdr.SkipSpaces(bufferSize);

                while (content.PeekChar(out char chr) && !char.IsWhiteSpace(chr))
                {
                    content.Advance(1);
                }
            }

            Assert.That(rdr.Row, Is.EqualTo(rows));
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
                    { JsonTokens.Comma, (IsLiteral: false, ",", JsonReaderFlags.None) },
                    { JsonTokens.DoubleSlash, (IsLiteral: false, "//", JsonReaderFlags.AllowComments) },
                    { JsonTokens.CurlyOpen, (IsLiteral: false, "{", JsonReaderFlags.None) },
                    { JsonTokens.CurlyClose, (IsLiteral: false, "}", JsonReaderFlags.None) },
                    { JsonTokens.SquaredOpen, (IsLiteral: false, "[", JsonReaderFlags.None) },
                    { JsonTokens.SquaredClose, (IsLiteral: false, "]", JsonReaderFlags.None) },
                    { JsonTokens.Colon, (IsLiteral: false, ":", JsonReaderFlags.None) },
                    { JsonTokens.SingleQuote, (IsLiteral: false, "'", JsonReaderFlags.AllowSingleQuotedStrings) },
                    { JsonTokens.DoubleQuote, (IsLiteral: false, "\"", JsonReaderFlags.None) },
                    { JsonTokens.True, (IsLiteral: true, "true", JsonReaderFlags.None) },
                    { JsonTokens.False, (IsLiteral: true, "false", JsonReaderFlags.None) },
                    { JsonTokens.Null, (IsLiteral: true, "null", JsonReaderFlags.None) }
                };

                foreach (KeyValuePair<JsonTokens, JsonTokenValue> token in tokens)
                {
                    if (token.Value.IsLiteral)
                    {
                        yield return new object[] { token.Value.Value.ToUpper(), token.Key, token.Value.RequiredFlags | JsonReaderFlags.CaseInsensitive };
                        yield return new object[] { token.Value.Value.ToUpper(), JsonTokens.Unknown, token.Value.RequiredFlags };
                    }
                    yield return new object[] { token.Value.Value, token.Key, token.Value.RequiredFlags };

                    if (token.Value.IsLiteral)
                    {
                        yield return new object[] { $" {token.Value.Value.ToUpper()}", token.Key, token.Value.RequiredFlags | JsonReaderFlags.CaseInsensitive };
                        yield return new object[] { $" {token.Value.Value.ToUpper()}", JsonTokens.Unknown, token.Value.RequiredFlags };
                    }
                    yield return new object[] { $" {token.Value.Value}", token.Key, token.Value.RequiredFlags };
                }

                yield return new object[] { $"-", JsonTokens.Number, JsonReaderFlags.None };
                yield return new object[] { $"5", JsonTokens.Number, JsonReaderFlags.None };
            }
        }

        [TestCaseSource(nameof(Consume_ShouldReturnTheCurrectToken_Params))]
        public void Consume_ShouldReturnTheCurrectToken(string input, object expected, JsonReaderFlags flags)
        {
            using JsonReader rdr = CreateReader(input, out _, flags);

            Assert.That(rdr.Consume(), Is.EqualTo(expected));
        }

        [Test]
        public void Consume_ShouldRejectPartialLiterals([Values("nul", "tru", "fal")] string input)
        {
            using JsonReader rdr = CreateReader(input, out _);

            Assert.That(rdr.Consume(), Is.EqualTo(JsonTokens.Unknown));
        }

        [Test]
        public void ConsumeAndValidate_ShouldValidateTheReturnedToken()
        {
            using JsonReader rdr = CreateReader("{", out _);

            Assert.That(rdr.ConsumeAndValidate(JsonTokens.Eof | JsonTokens.CurlyOpen), Is.EqualTo(JsonTokens.CurlyOpen));
            Assert.Throws<FormatException>(() => rdr.ConsumeAndValidate(JsonTokens.Eof));
        }

        public static IEnumerable<object[]> ParseString_ShouldParseSingleStrings_Params
        {
            get
            {
                yield return new object[] { "\"\"", "" };
                yield return new object[] { "''", "" };
                yield return new object[] { "\"cica\"", "cica" };
                yield return new object[] { "'cica'", "cica" };
                yield return new object[] { "\"cica mica\"", "cica mica" };
                yield return new object[] { "'cica mica'", "cica mica" };
            }
        }

        private static void ParseString_ShouldParseSingleStrings(string input, string expected, int bufferSize)
        {
            using JsonReader rdr = CreateReader(input, out ITextReader content, JsonReaderFlags.AllowSingleQuotedStrings);

            Assert.That(rdr.ParseString(bufferSize).AsString(), Is.EqualTo(expected));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
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
                }
            }
        }

        private static void ParseString_ShouldProcessSingleControlCharacters(string input, string expected, int bufferSize)
        {
            using JsonReader rdr = CreateReader(input, out ITextReader content);

            Assert.That(rdr.ParseString(bufferSize).AsString(), Is.EqualTo(expected));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
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
            using JsonReader rdr = CreateReader(input, out ITextReader content);

            Assert.Throws<FormatException>(() => rdr.ParseString('"'));
        }

        public static IEnumerable<object[]> ParseString_ShouldThrowOnUnescapedSpace_Params
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

        private static void ParseString_ShouldThrowOnUnescapedSpace(string input, int position, int bufferSize)
        {
            using JsonReader rdr = CreateReader(input, out ITextReader content);

            FormatException ex = Assert.Throws<FormatException>(() => rdr.ParseString(bufferSize))!;
            Assert.That(ex.Data.Contains("column"));
            Assert.That(ex.Data["row"], Is.EqualTo(0));
            Assert.That(ex.Data["column"], Is.EqualTo(position));
        }

        [TestCaseSource(nameof(ParseString_ShouldThrowOnUnescapedSpace_Params))]
        public void ParseString_ShouldThrowOnUnescapedSpace(string input, int position) =>
            ParseString_ShouldThrowOnUnescapedSpace(input, position, 128);

        [TestCaseSource(nameof(ParseString_ShouldThrowOnUnescapedSpace_Params))]
        public void ParseString_ShouldThrowOnUnescapedSpaceInMultipleIterations(string input, int position) =>
            ParseString_ShouldThrowOnUnescapedSpace(input, position, 1);

        [Test]
        public void ParseString_ShouldThrowOnUnterminatedString([Values("\"", "\"cica", "\"cica\t", /*"\"\\\"",*/ "\"\\")] string input, [Values(1, 128)] int bufferSize)
        {
            using JsonReader rdr = CreateReader(input, out ITextReader content);

            Assert.Throws<FormatException>(() => rdr.ParseString(bufferSize));
        }

        public static IEnumerable<object[]> ParseString_ShouldParseEscapedUnicodeCharacters_Params
        {
            get
            {
                yield return new object[] { "\"\\u00E1\"", "á" };
                yield return new object[] { "\"\\u00E1bc\"", "ábc" };
                yield return new object[] { "\"cb\\u00E1\"", "cbá" };
                yield return new object[] { "\"123\\u00E1bc\"", "123ábc" };
                yield return new object[] { "\"\\uD83D\\uDE01\"", "😁" };
            }
        }

        private static void ParseString_ShouldParseEscapedUnicodeCharacters(string input, string expected, int bufferSize)
        {
            using JsonReader rdr = CreateReader(input, out ITextReader content);

            Assert.That(rdr.ParseString(bufferSize).AsString(), Is.EqualTo(expected));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
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
                yield return new object[] { "\"\\uX0E1\"", 3 };
                yield return new object[] { "\"\\u0XE1bc\"", 3 };
                yield return new object[] { "\"cb\\u00X1\"", 5 };
                yield return new object[] { "\"123\\u00EXbc\"", 6 };
                yield return new object[] { "\"\\uD83D\\uD 01\"", 9 };

                // unterminated
                yield return new object[] { "\"\\u00E\"", 3 };
                yield return new object[] { "\"\\u00E", 3 };
                yield return new object[] { "\"cb\\u00E\"", 5 };
                yield return new object[] { "\"cb\\u00E", 5 };
            }
        }

        private static void ParseString_ShouldThrowOnInvalidHex(string input, int col, int bufferSize)
        {
            using JsonReader rdr = CreateReader(input, out _);

            Assert.Throws<FormatException>(() => rdr.ParseString(bufferSize));
            Assert.That(rdr.Column, Is.EqualTo(col));
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
                SupportedTypes = JsonDataTypes.None,
                CommentParser = chars => lastComment = chars.AsString()
            };
            ITextReader content = new StringReader(input);

            using JsonReader rdr = new(content, ctx, JsonReaderFlags.AllowComments, int.MaxValue);

            Assert.DoesNotThrow(() => rdr.ParseComment(ctx, bufferSize));
            Assert.That(lastComment, Is.EqualTo(expected));
            Assert.That(content.CharsLeft, Is.EqualTo(charsLeft));
        }

        [TestCaseSource(nameof(ParseComment_ShouldConsumeComments_Params))]
        public void ParseComment_ShouldConsumeComments(string input, string expected, int charsLeft) =>
            ParseComment_ShouldConsumeComments(input, expected, charsLeft, 32);

        [TestCaseSource(nameof(ParseComment_ShouldConsumeComments_Params))]
        public void ParseComment_ShouldConsumeCommentsInMultipleIterations(string input, string expected, int charsLeft) =>
            ParseComment_ShouldConsumeComments(input, expected, charsLeft, 1);

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
            using JsonReader rdr = CreateReader(input, out ITextReader content);

            object result = rdr.ParseNumber(UntypedDeserializationContext.Instance, bufferSize);

            Assert.That(result.GetType(), Is.EqualTo(expected.GetType()));
            Assert.That(result, Is.EqualTo(expected).Within(expected is double ? 0.1 : 0).Percent);
            Assert.That(content.CharsLeft, Is.EqualTo(charsLeft));
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
            using JsonReader rdr = CreateReader(input, out _);
            Assert.Throws<FormatException>(() => rdr.ParseNumber(UntypedDeserializationContext.Instance, bufferSize));
        }

        [Test]
        public void ParseNumber_ShouldInvokeTheConverterIfPassed()
        {
            Mock<DeserializationContext.ConvertNumberDelegate> mockConverter = new(MockBehavior.Strict);
            mockConverter
                .Setup(c => c.Invoke((long) 1986))
                .Returns(1991);

            using JsonReader rdr = CreateReader("1986", out _);
            object ret = rdr.ParseNumber(UntypedDeserializationContext.Instance with { ConvertNumber = mockConverter.Object });

            Assert.That(ret, Is.EqualTo(1991));
            mockConverter.Verify(c => c.Invoke((long) 1986), Times.Once);
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
            using JsonReader rdr = CreateReader(input, out _);

            Assert.Throws<FormatException>(() => rdr.ParseList(0, UntypedDeserializationContext.Instance, default));
            Assert.That(rdr.Column, Is.EqualTo(errorPos));
        }

        [Test]
        public void ParseList_ShouldThrowOnInvalidContext()
        {
            using JsonReader rdr = CreateReader("[]", out _);
            Assert.Throws<InvalidOperationException>(() => rdr.ParseList(0, new DeserializationContext { SupportedTypes = JsonDataTypes.None }, default));
        }

        public static IEnumerable<object[]> ParseList_ShouldParse_Params
        {
            get
            {
                yield return new object[] { "[]", new List<object?> { }, JsonReaderFlags.None };
                yield return new object[] { "[1]", new List<object?> { 1 }, JsonReaderFlags.None };
                yield return new object[] { "[\"1\"]", new List<object?> { "1" }, JsonReaderFlags.None };
                yield return new object[] { "[1,]", new List<object?> { 1 }, JsonReaderFlags.AllowTrailingComma };
                yield return new object[] { "[\"1\",]", new List<object?> { "1" }, JsonReaderFlags.AllowTrailingComma };
                yield return new object[] { "[\r\n\"1\"\r\n]", new List<object?> { "1" }, JsonReaderFlags.None };
                yield return new object[] { "[null, true, false, 1, \"1\"]", new List<object?> { null, true, false, 1, "1" }, JsonReaderFlags.None };
                yield return new object[] { "[[]]", new List<object?> { new List<object?> { } }, JsonReaderFlags.None };
            }
        }

        [TestCaseSource(nameof(ParseList_ShouldParse_Params))]
        public void ParseList_ShouldParse(string input, List<object?> expected, JsonReaderFlags flags)
        {
            using JsonReader rdr = CreateReader(input, out ITextReader content, flags);

            Assert.That(rdr.ParseList(0, UntypedDeserializationContext.Instance, default), Is.EquivalentTo(expected));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
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
            using JsonReader rdr = CreateReader(input, out _);

            Assert.Throws<FormatException>(() => rdr.ParseObject(0, UntypedDeserializationContext.Instance, default));
            Assert.That(rdr.Column, Is.EqualTo(errorPos));
        }

        [Test]
        public void ParseObject_ShouldThrowOnInvalidContext()
        {
            using JsonReader rdr = CreateReader("{}", out _);
            Assert.Throws<InvalidOperationException>(() => rdr.ParseObject(0, UntypedDeserializationContext.Instance with { CreateRawObject = null }, default));
        }

        public static IEnumerable<object[]> ParseObject_ShouldParse_Params
        {
            get
            {
                yield return new object[] { "{}", new Dictionary<string, object?> { }, JsonReaderFlags.None };
                yield return new object[] { "{\"cica\": 1}", new Dictionary<string, object?> { { "cica", 1 } }, JsonReaderFlags.None };
                yield return new object[] { "{\"cica\": \"1\"}", new Dictionary<string, object?> { { "cica", "1" } }, JsonReaderFlags.None };
                yield return new object[] { "{\"cica\": \"1\", \"cica\": 1986}", new Dictionary<string, object?> { { "cica", 1986 } }, JsonReaderFlags.None };
                yield return new object[] { "{\"cica\": 1,}", new Dictionary<string, object?> { { "cica", 1 } }, JsonReaderFlags.AllowTrailingComma };
                yield return new object[] { "{\"cica\": \"1\",}", new Dictionary<string, object?> { { "cica", "1" } }, JsonReaderFlags.AllowTrailingComma };
                yield return new object[] { "{\r\n  \"cica\": 1\r\n}", new Dictionary<string, object?> { { "cica", 1 } }, JsonReaderFlags.None };
                yield return new object[] { "{\"nested\": {}}", new Dictionary<string, object?> { { "nested", new Dictionary<string, object?> { } } }, JsonReaderFlags.None };
            }
        }

        [TestCaseSource(nameof(ParseObject_ShouldParse_Params))]
        public void ParseObject_ShouldParse(string input, Dictionary<string, object?> expected, JsonReaderFlags flags)
        {
            using JsonReader rdr = CreateReader(input, out ITextReader content, flags);

            Assert.That(rdr.ParseObject(0, UntypedDeserializationContext.Instance, default), Is.EquivalentTo(expected));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
        }

        [TestCase("true", JsonReaderFlags.None, JsonDataTypes.Boolean, true)]
        [TestCase("false", JsonReaderFlags.None, JsonDataTypes.Boolean, false)]
        [TestCase("null", JsonReaderFlags.None, JsonDataTypes.Null, null)]
        [TestCase("True", JsonReaderFlags.CaseInsensitive, JsonDataTypes.Boolean, true)]
        [TestCase("False", JsonReaderFlags.CaseInsensitive, JsonDataTypes.Boolean, false)]
        [TestCase("Null", JsonReaderFlags.CaseInsensitive, JsonDataTypes.Null, null)]
        public void Parse_ShouldParseLiterals(string input, JsonReaderFlags flags, JsonDataTypes supportedTypes, object expected)
        {
            using JsonReader rdr = CreateReader(input, out ITextReader content, flags, supportedTypes);

            Assert.That(rdr.Parse(default), Is.EqualTo(expected));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
        }

        [Test]
        public void Parse_ShouldBeCaseSensitiveByDefault()
        {
            using JsonReader rdr = CreateReader("Null", out ITextReader content);
            Assert.Throws<FormatException>(() => rdr.Parse(default));
            Assert.That(content.CharsLeft, Is.EqualTo(4));
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
        public void Parse_ShouldParseValues(string input, object expected, string? comment)
        {
            string? got = null;

            ITextReader content = new StringReader(input);
            using JsonReader rdr = new(content, UntypedDeserializationContext.Instance with { CommentParser = chars => got = chars.AsString() }, JsonReaderFlags.AllowComments, 0);

            Assert.That(rdr.Parse(default), Is.EqualTo(expected));
            Assert.That(got, Is.EqualTo(comment));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
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
            using JsonReader rdr = new(new StringReader(input), UntypedDeserializationContext.Instance, JsonReaderFlags.None, maxDepth);

            if (shouldThrow)
                Assert.Throws<InvalidOperationException>(() => rdr.Parse(default));
            else
                Assert.DoesNotThrow(() => rdr.Parse(default));
        }

        [Test]
        public void Parse_ShouldVerify()
        {
            Mock<DeserializationContext.VerifyDelegate> mockValidator = new(MockBehavior.Strict);
            mockValidator
                .Setup(v => v.Invoke((long) 1986));

            using JsonReader rdr = new(new StringReader("1986"), new DeserializationContext { SupportedTypes = JsonDataTypes.Number, Verify = mockValidator.Object }, JsonReaderFlags.None, 0);

            rdr.Parse(default);

            mockValidator.Verify(v => v.Invoke((long) 1986), Times.Once);
        }

        [Test]
        public void Parse_ShouldHandleLargeInput()
        {
            using JsonReader rdr = new
            (
                // TODO: implement StreamReader
                new StringReader
                (
                    File.ReadAllText
                    (
                        Path.Combine
                        (
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                            "large.json"
                        )
                    )
                ),
                UntypedDeserializationContext.Instance,
                JsonReaderFlags.None,
                int.MaxValue
            );

            IList? result = rdr.Parse(default) as IList;
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Count, Is.EqualTo(5000));
        }
    }
}
