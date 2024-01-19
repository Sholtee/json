/********************************************************************************
* JsonReaderTests.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
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
    }
}
