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
        private static JsonReader CreateReader(string input, out ITextReader reader, out Mock<IJsonReaderContext> mockContext, JsonReaderFlags flags = JsonReaderFlags.None)
        {
            mockContext = new(MockBehavior.Loose);
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
        // Cannot mock methods having Span<T> parameter
        //

        private sealed class CommentParserContext : IJsonReaderContext
        {
            public string LastComment { get; private set; } = null!;
            public void CommentParsed(ReadOnlySpan<char> value) => LastComment  = value.AsString();
            public object CreateRawObject(ObjectKind objectKind) => throw new NotImplementedException();
            public void PopState() => throw new NotImplementedException();
            public bool PushState(ReadOnlySpan<char> property, StringComparison comparison) => throw new NotImplementedException();
            public void SetValue(object obj, object? value) => throw new NotImplementedException();
            public void SetValue(object obj, ReadOnlySpan<char> value) => throw new NotImplementedException();
        }

        private static void ParseComment_ShouldConsumeComments(string input, string expected, int charsLeft, int bufferSize)
        {
            CommentParserContext commentParserContext = new();
            ITextReader content = new StringReader(input);

            JsonReader rdr = new(content, commentParserContext, JsonReaderFlags.AllowComments, int.MaxValue);

            Assert.DoesNotThrow(() => rdr.ParseComment(bufferSize));
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
                yield return new object[] { "0", (long) 0, 0 };
                yield return new object[] { "1", (long) 1, 0 };
                yield return new object[] { "100", (long) 100, 0 };
                yield return new object[] { "100.0", (double) 100.0, 0 };
                yield return new object[] { "1.0E+2", (double) 100.0, 0 };
                yield return new object[] { "1.0E-2", (double) 0.01, 0 };
                yield return new object[] { "1E+2", (double) 100.0, 0 };
                yield return new object[] { "1E-2", (double) 0.01, 0 };
                yield return new object[] { "-0", (long) 0, 0 };
                yield return new object[] { "-1", (long) -1, 0 };
                yield return new object[] { "-100", (long) -100, 0 };
                yield return new object[] { "-100.0", (double) -100.0, 0 };
                yield return new object[] { "-1.0E+2", (double) -100.0, 0 };
                yield return new object[] { "-1.0E-2", (double) -0.01, 0 };
                yield return new object[] { "-1E+2", (double) -100.0, 0 };
                yield return new object[] { "-1E-2", (double) -0.01, 0 };

                yield return new object[] { "0,", (long) 0, 1 };
                yield return new object[] { "1,", (long) 1, 1 };
                yield return new object[] { "100,", (long) 100, 1 };
                yield return new object[] { "100.0,", (double) 100.0, 1 };
                yield return new object[] { "1.0E+2,", (double) 100.0, 1 };
                yield return new object[] { "1.0E-2,", (double) 0.01, 1 };
                yield return new object[] { "1E+2,", (double) 100.0, 1 };
                yield return new object[] { "1E-2,", (double) 0.01, 1 };
                yield return new object[] { "-0,", (long) 0, 1 };
                yield return new object[] { "-1,", (long) -1, 1 };
                yield return new object[] { "-100,", (long) -100, 1 };
                yield return new object[] { "-100.0,", (double) -100.0, 1 };
                yield return new object[] { "-1.0E+2,", (double) -100.0, 1 };
                yield return new object[] { "-1.0E-2,", (double) -0.01, 1 };
                yield return new object[] { "-1E+2,", (double) -100.0, 1 };
                yield return new object[] { "-1E-2,", (double) -0.01, 1 };
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

        //
        // Cannot mock methods having Span<T> parameter
        //

        private sealed class ListParserContext : IJsonReaderContext
        {
            public void CommentParsed(ReadOnlySpan<char> value) => throw new NotImplementedException();
            public object CreateRawObject(ObjectKind objectKind) => new List<object?>();
            public void PopState() => throw new NotImplementedException();
            public bool PushState(ReadOnlySpan<char> property, StringComparison comparison) => throw new NotImplementedException();
            public void SetValue(object obj, object? value) => ((List<object?>) obj).Add(value);
            public void SetValue(object obj, ReadOnlySpan<char> value) => ((List<object?>) obj).Add(value.ToString());
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
            JsonReader rdr = new(new StringReader(input), new ListParserContext(), flags, int.MaxValue);

            Assert.Throws<FormatException>(() => rdr.ParseList(0, default));
            Assert.That(rdr.Column, Is.EqualTo(errorPos));
        }
    }
}
