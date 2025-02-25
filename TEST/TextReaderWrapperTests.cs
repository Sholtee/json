﻿/********************************************************************************
* TextReaderWrapperTests.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Json.Internals.Tests
{
    [TestFixture]
    public class TextReaderWrapperTests
    {
        [Test]
        public void PeekText_ShouldThrowOnInvalidIndex()
        {
            using TextReaderWrapper textReader = new(new StringReader("cica"));
            Assert.Throws<ArgumentOutOfRangeException>(() => textReader.PeekText(-1));
        }

        [Test]
        public void PeekText_ShouldInvokeTheUnderlyingReaderIfNecessary([Values(1, 5)] int charsToRead)
        {
            const string str = "cicamica";

            Mock<StringReader> mockRdr = new(str);
            mockRdr
                .Setup(r => r.Read(It.IsAny<char[]>(), 0, charsToRead))
                .CallBase();

            using TextReaderWrapper textReader = new(mockRdr.Object);

            string ret = textReader.PeekText(charsToRead).ToString();

            Assert.That(ret, Is.EqualTo(str.Substring(0, charsToRead)));
            mockRdr.Verify(r => r.Read(It.IsAny<char[]>(), 0, charsToRead), Times.Once);
            mockRdr.VerifyNoOtherCalls();

            mockRdr.Reset();

            ret = textReader.PeekText(charsToRead).ToString();
            Assert.That(ret, Is.EqualTo(str.Substring(0, charsToRead)));

            mockRdr.Verify(r => r.Read(It.IsAny<char[]>(), 0, charsToRead), Times.Never);
            mockRdr.VerifyNoOtherCalls();
        }

        [Test]
        public void PeekText_ShouldPreserveTheState()
        {
            using TextReaderWrapper textReader = new(new StringReader("cica"));

            textReader.PeekText(1)[0] = 'm';

            Assert.That(textReader.PeekText(4).ToString(), Is.EqualTo("mica"));
        }

        [Test]
        public void PeekText_ShouldEnlargeTheUnderlyingBuffer()
        {
            using TextReaderWrapper textReader = new(new StringReader("cicamica"), 2);

            Assert.That(textReader.BufferSize, Is.GreaterThanOrEqualTo(2));
            Assert.That(textReader.FreeSpace, Is.GreaterThanOrEqualTo(2));
            Assert.That(textReader.CharsLeft, Is.EqualTo(0));

            Assert.That(textReader.PeekText(1).ToString(), Is.EqualTo("c"));

            Assert.That(textReader.BufferSize, Is.GreaterThanOrEqualTo(2));
            Assert.That(textReader.FreeSpace, Is.GreaterThanOrEqualTo(1));
            Assert.That(textReader.CharsLeft, Is.EqualTo(1));

            Assert.That(textReader.PeekText(3).ToString(), Is.EqualTo("cic"));

            Assert.That(textReader.BufferSize, Is.GreaterThanOrEqualTo(3));
            Assert.That(textReader.FreeSpace, Is.GreaterThanOrEqualTo(0));
            Assert.That(textReader.CharsLeft, Is.EqualTo(3));
        }

        [Test]
        public void PeekText_ShouldOptimizeTheBuffer()
        {
            using TextReaderWrapper textReader = new(new StringReader("cicamica"), 4);

            Assert.That(textReader.BufferSize, Is.GreaterThanOrEqualTo(4));
            Assert.That(textReader.FreeSpace, Is.GreaterThanOrEqualTo(4));
            Assert.That(textReader.CharsLeft, Is.EqualTo(0));

            Assert.That(textReader.PeekText(4).ToString(), Is.EqualTo("cica"));
            textReader.Advance(2);

            Assert.That(textReader.BufferSize, Is.GreaterThanOrEqualTo(4));
            Assert.That(textReader.FreeSpace, Is.GreaterThanOrEqualTo(0));
            Assert.That(textReader.CharsLeft, Is.EqualTo(2));

            Assert.That(textReader.PeekText(4).ToString(), Is.EqualTo("cami"));

            Assert.That(textReader.BufferSize, Is.GreaterThanOrEqualTo(4));
            Assert.That(textReader.FreeSpace, Is.GreaterThanOrEqualTo(0));
            Assert.That(textReader.CharsLeft, Is.EqualTo(4));
        }

        [Test]
        public void Advance_ShouldThrowOnInvalidIndex()
        {
            string input = "cicamica";

            using TextReaderWrapper textReader = new(new StringReader(input));

            Assert.Throws<ArgumentOutOfRangeException>(() => textReader.Advance(-1));

            textReader.PeekText(2);
            Assert.DoesNotThrow(() => textReader.Advance(2));
        }

        [Test]
        public void Advance_ShouldThrowOnInvalidIndex2()
        {
            string input = "cicamica";

            using TextReaderWrapper textReader = new(new StringReader(input));

            Assert.DoesNotThrow(() => textReader.Advance(input.Length));
            Assert.Throws<ArgumentOutOfRangeException>(() => textReader.Advance(1));
        }

        [Test]
        public void PeekChar_ShouldReturnTheFirstChar([Values(0, 1, 2, 5, 10)] int initialBufferSize)
        {
            const string input = "cica";

            using TextReaderWrapper textReader = new(new StringReader(input), initialBufferSize);

            for (int i = 0; i < input.Length; i++)
            {
                Assert.That(textReader.PeekChar(), Is.EqualTo(input[i]));
                textReader.Advance(1);
            }

            Assert.That(textReader.PeekChar(), Is.EqualTo(-1));
        }
    }
}
