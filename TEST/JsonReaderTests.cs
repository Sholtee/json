/********************************************************************************
* JsonReaderTests.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Json.Tests
{
    using Internals;

    using JsonTokenValue = (bool IsLiteral, string Value, JsonReaderFlags RequiredFlags);

    [TestFixture]
    public class JsonReaderTests
    {
        private static JsonReader CreateReader(string input, out ITextReader reader, out Mock<IJsonReaderContext> mockContext, JsonReaderFlags flags = JsonReaderFlags.None, JsonDataTypes supportedTypes = JsonDataTypes.String)
        {
            mockContext = new(MockBehavior.Loose);
            mockContext
                .SetupGet(c => c.SupportedTypes)
                .Returns(supportedTypes);

            reader = new StringReader(input);

            return new JsonReader(reader, mockContext.Object, flags, int.MaxValue);
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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);
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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _, flags);

            Assert.That(rdr.Consume(), Is.EqualTo(expected));
        }

        [Test]
        public void Consume_ShouldRejectPartialLiterals([Values("nul", "tru", "fal")] string input)
        {
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

            Assert.That(rdr.Consume(), Is.EqualTo(JsonTokens.Unknown));
        }

        [Test]
        public void ConsumeAndValidate_ShouldValidateTheReturnedToken()
        {
            JsonReader rdr = CreateReader("{", out ITextReader content, out _);

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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _, JsonReaderFlags.AllowSingleQuotedStrings);

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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

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
            JsonReader rdr = CreateReader(input, out _, out _);

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

        //
        // We cannot mock methods having Span<T> parameter =(
        //

        private sealed class CommentParserContext : UntypedJsonReaderContext
        {
            public string LastComment { get; private set; } = null!;
            public override JsonDataTypes SupportedTypes => throw new NotImplementedException();
            public override void CommentParsed(ReadOnlySpan<char> value) => LastComment = value.AsString();
        }

        private static void ParseComment_ShouldConsumeComments(string input, string expected, int charsLeft, int bufferSize)
        {
            CommentParserContext commentParserContext = new();
            ITextReader content = new StringReader(input);

            JsonReader rdr = new(content, commentParserContext, JsonReaderFlags.AllowComments, int.MaxValue);

            Assert.DoesNotThrow(() => rdr.ParseComment(commentParserContext, bufferSize));
            Assert.That(commentParserContext.LastComment, Is.EqualTo(expected));
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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

            object result = rdr.ParseNumber(bufferSize);

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
            JsonReader rdr = CreateReader(input, out _, out _);
            Assert.Throws<FormatException>(() => rdr.ParseNumber(bufferSize));
        }

        [TestCase("[", JsonReaderFlags.None, 1)]
        [TestCase("[,]", JsonReaderFlags.None, 1)]
        [TestCase("[x]", JsonReaderFlags.None, 1)]
        [TestCase("[\"cica\"", JsonReaderFlags.None, 7)]
        [TestCase("[\"cica\",]", JsonReaderFlags.None, 8)]
        [TestCase("[\"cica\", x]", JsonReaderFlags.None, 9)]
        [TestCase("[\"cica\", \"mica\"", JsonReaderFlags.None, 15)]
        [TestCase("[\"cica\", \"mica\",]", JsonReaderFlags.None, 16)]
        [TestCase("[\"cica\", \"mica\", x]", JsonReaderFlags.None, 17)]
        public void ParseList_ShouldThrowOnInvalidList(string input, JsonReaderFlags flags, int errorPos)
        {
            JsonReader rdr = new(new StringReader(input), new UntypedJsonReaderContext(), flags, int.MaxValue);

            Assert.Throws<FormatException>(() => rdr.ParseList(0, new UntypedJsonReaderContext(), default));
            Assert.That(rdr.Column, Is.EqualTo(errorPos));
        }

        public static IEnumerable<object[]> ParseList_ShouldParse_Params
        {
            get
            {
                yield return new object[] { "[]", new List<object?> { } };
                yield return new object[] { "[1]", new List<object?> { 1 } };
                yield return new object[] { "[\"1\"]", new List<object?> { "1" } };
                yield return new object[] { "[\r\n\"1\"\r\n]", new List<object?> { "1" } };
                yield return new object[] { "[null, true, false, 1, \"1\"]", new List<object?> { null, true, false, 1, "1" } };
            }
        }

        [TestCaseSource(nameof(ParseList_ShouldParse_Params))]
        public void ParseList_ShouldParse(string input, List<object?> expected)
        {
            ITextReader content = new StringReader(input);
            JsonReader rdr = new(content, new UntypedJsonReaderContext(), JsonReaderFlags.None, int.MaxValue);

            Assert.That(rdr.ParseList(0, new UntypedJsonReaderContext(), default), Is.EquivalentTo(expected));
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
            JsonReader rdr = CreateReader(input, out ITextReader content, out _, flags, supportedTypes);

            Assert.That(rdr.Parse(default), Is.EqualTo(expected));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
        }

        [Test]
        public void Parse_ShouldBeCaseSensitiveByDefault()
        {
            JsonReader rdr = CreateReader("Null", out ITextReader content, out _);
            Assert.Throws<FormatException>(() => rdr.Parse(default));
            Assert.That(content.CharsLeft, Is.EqualTo(4));
        }

        //
        // We cannot mock methods having Span<T> parameter =(
        //

        private sealed class ValueParserContext : UntypedJsonReaderContext
        {
            public string Comment { get; private set; } = null!;
            public override JsonDataTypes SupportedTypes { get; } = JsonDataTypes.String | JsonDataTypes.Number;
            public override void CommentParsed(ReadOnlySpan<char> value) => Comment = value.ToString();
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
            ITextReader content = new StringReader(input);
            ValueParserContext context = new();
            JsonReader rdr = new(content, context, JsonReaderFlags.AllowComments, int.MaxValue);

            Assert.That(rdr.Parse(default), Is.EqualTo(expected));
            Assert.That(context.Comment, Is.EqualTo(comment));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
        }

        [TestCase("\"cica\"", 0, false)]
        [TestCase("[\"cica\"]", 0, true)]
        [TestCase("[\"cica\"]", 1, false)]
        [TestCase("[[\"cica\"]]", 1, true)]
        public void Parse_ShouldCheckTheDepth(string input, int maxDepth, bool shouldThrow)
        {
            JsonReader rdr = new(new StringReader(input), new UntypedJsonReaderContext(), JsonReaderFlags.AllowComments, maxDepth);

            if (shouldThrow)
                Assert.Throws<InvalidOperationException>(() => rdr.Parse(default));
            else
                Assert.DoesNotThrow(() => rdr.Parse(default));
        }

        [Test]
        public void Parse_ShouldVerify()
        {
            JsonReader rdr = CreateReader("1986", out _, out Mock<IJsonReaderContext> mockContext, supportedTypes: JsonDataTypes.Number);
            mockContext.Setup(c => c.Verify((long) 1986));

            rdr.Parse(default);

            mockContext.Verify(c => c.Verify((long) 1986), Times.Once);
        }
    }
}
