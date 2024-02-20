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
        public void GetXxHashCode_ShouldHash([Values("", "1", "1986", "cica", "😁", "loooooooooooooooooooooooooooooooooooooooooong")] string str)
        {
            Assert.AreEqual(str.AsSpan().GetXxHashCode(), str.Substring(0).AsSpan().GetXxHashCode());
            Assert.AreNotEqual(str.AsSpan().GetXxHashCode(), (str + "_").AsSpan().GetXxHashCode());
        }
    }
}
