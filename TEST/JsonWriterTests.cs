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

            new JsonWriter().WriteString(store, input, SerializationContext.Untyped, 0);

            Assert.That(store.ToString(), Is.EqualTo($"\"{expected}\""));
        }

        [Test]
        public void WriteString_ShouldHandleRegularStrings([Values("", "c", "cica", "1986")] string input)
        {
            StringWriter store = new();  // will be closed by the writer
 
            new JsonWriter().WriteString(store, input, SerializationContext.Untyped, 0);

            Assert.That(store.ToString(), Is.EqualTo($"\"{input}\""));
        }

        [Test]
        public void WriteString_ShouldConvert()
        {
            StringWriter store = new();

            new JsonWriter().WriteString(store, 1986, SerializationContext.Untyped with { GetTypeOf = _ => JsonDataTypes.String }, 0);

            Assert.That(store.ToString(), Is.EqualTo($"\"1986\""));
        }

        [TestCase(null, "null")]
        [TestCase(true, "true")]
        [TestCase(false, "false")]
        [TestCase(1986, "1986")]
        [TestCase(1986.1026, "1986.1026")]
        public void WriteValue_ShouldStringifyTheGivenValue(object input, string expected)
        {
            StringWriter store = new();

            new JsonWriter().WriteValue(store, input, SerializationContext.Untyped, 0);

            Assert.That(store.ToString(), Is.EqualTo(expected));
        }

        [TestCase(new object[] { }, 0, "[]")]
        [TestCase(new object[] { }, 2, "[]")]
        [TestCase(new object[] { 1 }, 0, "[1]")]
        [TestCase(new object[] { 1 }, 2, "[\r\n  1]")]
        [TestCase(new object[] { 1, 2 }, 0, "[1,2]")]
        [TestCase(new object[] { 1, 2 }, 2, "[\r\n  1,\r\n  2]")]
        public void WriteList_ShouldStringifyTheGivenList(object[] input, byte spaces, string expected)
        {
            StringWriter store = new();

            new JsonWriter(indent: spaces).WriteList(store, input, SerializationContext.Untyped, 0, default);

            Assert.That(store.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void WriteList_ShouldThrowIfWeReachTheMaxDepth()
        {
            JsonWriter writer = new(maxDepth: 1);

            Assert.DoesNotThrow(() => writer.WriteList(new StringWriter(), new object[] { 1 }, SerializationContext.Untyped, 0, default));
            Assert.Throws<InvalidOperationException>(() => writer.WriteList(new StringWriter(), new object[] { new object[] { 1 } }, SerializationContext.Untyped, 0, default));
        }

        [Test]
        public void Write_ShouldThrowIfTheInputIsNotSerializable()
        {
            Assert.Throws<NotSupportedException>(() => new JsonWriter().Write(new StringWriter(), new { }, SerializationContext.Untyped, 0, default));
        }

        [TestCase(1, "1")]
        [TestCase(true, "true")]
        [TestCase(null, "null")]
        [TestCase("1", "\"1\"")]
        [TestCase(new object[] { }, "[]")]
        public void Write_ShouldStringify(object? input, string expected)
        {
            StringWriter store = new();

            new JsonWriter().Write(store, input, SerializationContext.Untyped, 0, default);

            Assert.That(store.ToString(), Is.EqualTo(expected));
        }
    }
}
