/********************************************************************************
* StringKeyedDictionaryTests.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Json.Internals.Tests
{
    [TestFixture]
    public class StringKeyedDictionaryTests
    {
        [Test]
        public void TryGetValue_ShouldReturnTheAppropritateEntry()
        {
            StringKeyedDictionary<int> dict = new();
            dict.Add("a", 10);
            dict.Add("b", 20);
            Assert.That(dict.TryGetValue("b".AsSpan(), false, out int val));
            Assert.That(val, Is.EqualTo(20));
        }

        [Test]
        public void TryGetValue_ShouldReturnFalseIfTheRequestedEntryNotFound()
        {
            StringKeyedDictionary<int> dict = new();
            dict.Add("a", 10);
            dict.Add("b", 20);
            Assert.That(dict.TryGetValue("z".AsSpan(), false, out int val), Is.False);
            Assert.That(val, Is.EqualTo(default(int)));
        }

        [Test]
        public void TryGetValue_ShouldTakeCaseIntoAccount()
        {
            StringKeyedDictionary<int> dict = new();
            dict.Add("a", 10);
            dict.Add("b", 20);
            Assert.That(dict.TryGetValue("B".AsSpan(), ignoreCase: false, out _), Is.False);
            Assert.That(dict.TryGetValue("B".AsSpan(), ignoreCase: true, out int val));
            Assert.That(val, Is.EqualTo(20));
        }

        [Test]
        public void ManyEntry_Test([Values(1, 2, 3, 5, 10, 20, 100, 1000)] int count)
        {
            StringKeyedDictionary<int> dict = new();

            for (int i = 0; i < count; i++)
            {
                dict.Add(i.ToString(), i);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.That(dict.TryGetValue(i.ToString().AsSpan(), ignoreCase: true, out int val));
                Assert.That(val, Is.EqualTo(i));
            }
        }
    }
}
