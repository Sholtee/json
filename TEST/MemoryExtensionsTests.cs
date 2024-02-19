/********************************************************************************
* MemoryExtensionsTests.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Json.Internals.Tests
{
    [TestFixture]
    public class MemoryExtensionsTests
    {
        [Test]
        public void AsString_ShouldStringify([Values("", "1", "1986", "cica", "😁")] string str) =>
            Assert.That(str.AsSpan().AsString(), Is.EqualTo(str));

        [Test]
        public void GetXxHashCode_ShouldHash([Values("", "1", "1986", "cica", "😁", "loooooooooooooooooooooooooooooooooooooooooong")] string str, [Values(1, 2, 4, 10, 16)] byte bufferSize)
        {
            Assert.AreEqual(str.AsSpan().GetXxHashCode(bufferSize), str.Substring(0).AsSpan().GetXxHashCode(bufferSize));
            Assert.AreNotEqual(str.AsSpan().GetXxHashCode(bufferSize), (str + "_").AsSpan().GetXxHashCode(bufferSize));
        }
    }
}
