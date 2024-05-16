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
        [TestCase("a")]
        [TestCase("abcd")]
        [TestCase("aábcdeé")]
        [TestCase("12345abcd")]
        public void GetHashCode_ShouldHash(string val)
        {
            Assert.DoesNotThrow(() => val.AsSpan().GetHashCode(false));
            Assert.AreEqual(val.AsSpan().GetHashCode(false), val.AsSpan().GetHashCode(false));
            Assert.AreNotEqual(val.AsSpan().GetHashCode(false), val.ToUpper().AsSpan().GetHashCode(false));
        }

        [TestCase("a")]
        [TestCase("abcd")]
        [TestCase("ABCD")]
        [TestCase("aábcdeé")]
        [TestCase("AÁBCDEÉ")]
        public void GetHashCode_ShouldHashIgnoringCasing(string val)
        {
            Assert.DoesNotThrow(() => val.AsSpan().GetHashCode(true));
            Assert.AreEqual(val.AsSpan().GetHashCode(true), val.AsSpan().GetHashCode(true));
            Assert.AreEqual(val.ToLower().AsSpan().GetHashCode(true), val.ToUpper().AsSpan().GetHashCode(true));
        }
    }
}
