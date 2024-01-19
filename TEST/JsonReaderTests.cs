/********************************************************************************
* JsonReaderTests.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Json.Tests
{
    using Internals;

    [TestFixture]
    public class JsonReaderTests
    {
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
            Mock<JsonReaderContextBase> mockContext = new(MockBehavior.Loose, CancellationToken.None);
            ITextReader content = new StringReader(input);

            JsonReader.SkipSpaces(content, mockContext.Object, bufferSize);
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

        private static void SkipSpaces_ShouldMaintainThePosition(string input, int rows, int bufferSize)
        {
            Mock<JsonReaderContextBase> mockContext = new(MockBehavior.Loose, CancellationToken.None);
            mockContext
                .SetupGet(c => c.Row)
                .CallBase();
            mockContext
                .SetupSet(c => c.Row = It.IsAny<int>())
                .CallBase();
            mockContext
                .SetupGet(c => c.Column)
                .CallBase();
            mockContext
                .SetupSet(c => c.Column = It.IsAny<int>())
                .CallBase();

            for (ITextReader content = new StringReader(input); content.CharsLeft > 0;)
            {
                JsonReader.SkipSpaces(content, mockContext.Object, bufferSize);

                while (content.PeekChar(out char chr) && !char.IsWhiteSpace(chr))
                {
                    content.Advance(1);
                }
            }

            Assert.That(mockContext.Object.Row, Is.EqualTo(rows));
            mockContext.VerifySet(c => c.Row = It.IsAny<int>(), Times.Exactly(rows));
        }

        [TestCaseSource(nameof(SkipSpaces_ShouldMaintainThePosition_Params))]
        public void SkipSpaces_ShouldMaintainThePosition(string input, int rows) => SkipSpaces_ShouldMaintainThePosition(input, rows, 32);

        [TestCaseSource(nameof(SkipSpaces_ShouldMaintainThePosition_Params))]
        public void SkipSpaces_ShouldMaintainThePositionInMultipleIterations(string input, int rows) => SkipSpaces_ShouldMaintainThePosition(input, rows, 32);

        public static IEnumerable<object[]> Consume_ShouldReturnTheCurrectToken_Params
        {
            get
            {
                foreach (JsonTokens token in Enum.GetValues(typeof(JsonTokens)))
                {
                    foreach (MemberInfo member in typeof(JsonTokens).GetMember(token.ToString()))
                    {
                        TokenValueAttribute? val = member.GetCustomAttribute<TokenValueAttribute>();
                        if (val is not null)
                        {
                            if (val.IsLiteral)
                            {
                                yield return new object[] { val.Value.ToUpper(), token, val.RequiredFlag | JsonReaderFlags.CaseInsensitive };
                                yield return new object[] { val.Value.ToUpper(), JsonTokens.Unknown, val.RequiredFlag };
                            }
                            yield return new object[] { val.Value, token, val.RequiredFlag };

                            if (val.IsLiteral)
                            {
                                yield return new object[] { $" {val.Value}", token, val.RequiredFlag | JsonReaderFlags.CaseInsensitive };
                                yield return new object[] { $" {val.Value.ToUpper()}", JsonTokens.Unknown, val.RequiredFlag };
                            }
                            yield return new object[] { $" {val.Value}", token, val.RequiredFlag };
                        }
                    }
                }
            }
        }

        [TestCaseSource(nameof(Consume_ShouldReturnTheCurrectToken_Params))]
        public void Consume_ShouldReturnTheCurrectToken(string input, object expected, JsonReaderFlags flags)
        {
            Mock<JsonReaderContextBase> mockContext = new(MockBehavior.Loose, CancellationToken.None);
            ITextReader content = new StringReader(input);

            JsonReader rdr = new(flags, int.MaxValue);
            Assert.That(rdr.Consume(content, mockContext.Object), Is.EqualTo(expected));
        }
    }
}
