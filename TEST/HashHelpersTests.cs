/********************************************************************************
* HashHelpersTests.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Json.Internals.Tests
{
    [TestFixture]
    public class HashHelpersTests
    {
        [Test]
        public void GetXxHashCode_ShouldHash([Values("", "1", "1986", "cica", "😁", "loooooooooooooooooooooooooooooooooooooooooong")] string str)
        {
            Assert.AreEqual(HashHelpers.GetXxHashCode(str.AsSpan()), HashHelpers.GetXxHashCode(str.Substring(0).AsSpan()));
            Assert.AreNotEqual(HashHelpers.GetXxHashCode(str.AsSpan()), HashHelpers.GetXxHashCode((str + "_").AsSpan()));
        }

        [Test]
        public void GetHashCode_ShouldHash([Values("", "1", "1986", "cica", "😁", "loooooooooooooooooooooooooooooooooooooooooong")] string str)
        {
            Assert.AreEqual(HashHelpers.GetHashCode(str.AsSpan()), HashHelpers.GetHashCode(str.Substring(0).AsSpan()));
            Assert.AreNotEqual(HashHelpers.GetHashCode(str.AsSpan()), HashHelpers.GetHashCode((str + "_").AsSpan()));
        }
    }
}
