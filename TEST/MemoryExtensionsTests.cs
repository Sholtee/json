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
    }
}
