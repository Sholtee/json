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
        private static JsonReader CreateReader(string input, out ITextReader reader, out Mock<JsonReaderContextBase> mockContext, JsonReaderFlags flags = JsonReaderFlags.None)
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
                yield return new object[] { "\"\"", '"', "" };
                yield return new object[] { "''", '\'', "" };
                yield return new object[] { "\"cica\"", '"', "cica" };
                yield return new object[] { "'cica'", '\'', "cica" };
                yield return new object[] { "\"cica mica\"", '"', "cica mica" };
                yield return new object[] { "'cica mica'", '\'', "cica mica" };
            }
        }

        private static void ParseString_ShouldParseSingleStrings(string input, char terminating, string expected, int bufferSize)
        {
            JsonReader rdr = CreateReader(input, out ITextReader content, out _, JsonReaderFlags.AllowSingleQuotedStrings);

            Assert.That(rdr.ParseString(terminating, bufferSize).AsString(), Is.EqualTo(expected));
            Assert.That(content.CharsLeft, Is.EqualTo(0));
        }

        [TestCaseSource(nameof(ParseString_ShouldParseSingleStrings_Params))]
        public void ParseString_ShouldParseSingleStrings(string input, char terminating, string expected) =>
            ParseString_ShouldParseSingleStrings(input, terminating, expected, 128);

        [TestCaseSource(nameof(ParseString_ShouldParseSingleStrings_Params))]
        public void ParseString_ShouldParseSingleStringsInMultipleIterations(string input, char terminating, string expected) =>
            ParseString_ShouldParseSingleStrings(input, terminating, expected, 1);

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

            Assert.That(rdr.ParseString('"', bufferSize).AsString(), Is.EqualTo(expected));
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
                    yield return new object[] { $"\"{chr}\"", 1 };
                    yield return new object[] { $"\"prefix{chr}\"", 7 };
                    yield return new object[] { $"\"{chr}suffix\"", 1 };
                    yield return new object[] { $"\"prefix{chr}suffix\"", 7 };
                }
            }
        }

        private static void ParseString_ShouldThrowOnUnescapedSpace(string input, int position, int bufferSize)
        {
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

            FormatException ex = Assert.Throws<FormatException>(() => rdr.ParseString('"', bufferSize))!;
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
        public void ParseStringShouldThrowOnUnterminatedString([Values("\"", "\"cica", "\"cica\t", /*"\"\\\"",*/ "\"\\")] string input, [Values(1, 128)] int bufferSize)
        {
            JsonReader rdr = CreateReader(input, out ITextReader content, out _);

            Assert.Throws<FormatException>(() => rdr.ParseString('"', bufferSize));
        }
    }
}
