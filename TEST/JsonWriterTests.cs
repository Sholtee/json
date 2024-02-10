/********************************************************************************
* JsonWriterTests.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

namespace Solti.Utils.Json.Tests
{
    [TestFixture]
    public class JsonWriterTests
    {
        public static IEnumerable<object[]> WriteString_ShouldEscape_Params
        {
            get
            {
                foreach ((string Input, string Expected) in ToBeEscaped())
                {
                    yield return new object[] { Input, Expected };
                    yield return new object[] { "prefix-" + Input, "prefix-" + Expected };
                    yield return new object[] { Input + "-suffix", Expected + "-suffix" };
                    yield return new object[] { "prefix-" + Input + "-suffix", "prefix-" + Expected + "-suffix" };
                }

                static IEnumerable<(string, string)> ToBeEscaped() =>
                [
                    ("\r", "\\r"),
                    ("\n", "\\n"),
                    ("\b", "\\b"),
                    ("\t", "\\t"),
                    ("\\", "\\\\"),
                    ("\v", "\\u000B"),
                    ("\"", "\\\""),
                    ("😁", "\\uD83D\\uDE01")
                ];
            }
        }

        [TestCaseSource(nameof(WriteString_ShouldEscape_Params))]
        public void WriteString_ShouldEscape(string input, string expected)
        {
            StringWriter store = new();  // will be closed by the writer
            using JsonWriter writer = new(store);

            writer.WriteString(input, SerializationContext.Untyped);

            Assert.That(store.ToString(), Is.EqualTo($"\"{expected}\""));
        }

        [Test]
        public void WriteString_ShouldHandleRegularStrings([Values("", "c", "cica", "1986")] string input)
        {
            StringWriter store = new();  // will be closed by the writer
            using JsonWriter writer = new(store);

            writer.WriteString(input, SerializationContext.Untyped);

            Assert.That(store.ToString(), Is.EqualTo($"\"{input}\""));
        }

        [Test]
        public void WriteString_ShouldThrowOnInvalidInput()
        {
            using JsonWriter writer = new(new StringWriter());
            Assert.Throws<InvalidCastException>(() => writer.WriteString(new object(), SerializationContext.Untyped));
        }

        [Test]
        public void WriteString_ShouldConvert()
        {
            StringWriter store = new();
            using JsonWriter writer = new(store);

            writer.WriteString(1986, SerializationContext.Untyped with { GetTypeOf = _ => JsonDataTypes.String });

            Assert.That(store.ToString(), Is.EqualTo($"\"1986\""));
        }

        [TestCase(1986, "1986")]
        [TestCase(1986.1026, "1986.1026")]
        public void WriteNumber_ShouldStringifyTheGivenNumber(object input, string expected)
        {
            StringWriter store = new();
            using JsonWriter writer = new(store);

            writer.WriteNumber(input, SerializationContext.Untyped);

            Assert.That(store.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void WriteNumber_ShouldThrowOnInvalidNumber()
        {
            using JsonWriter writer = new(new StringWriter());

            Assert.Throws<InvalidCastException>(() => writer.WriteNumber("invalid", SerializationContext.Untyped));
        }

        [TestCase(null, "null")]
        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void WriteLiteral_ShouldStringifyTheGivenValue(object input, string expected)
        {
            StringWriter store = new();
            using JsonWriter writer = new(store);

            writer.WriteLiteral(input, SerializationContext.Untyped);

            Assert.That(store.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void WriteLiteral_ShouldThrowOnInvalidValue()
        {
            using JsonWriter writer = new(new StringWriter());

            Assert.Throws<InvalidCastException>(() => writer.WriteLiteral("invalid", SerializationContext.Untyped));
        }
    }
}
