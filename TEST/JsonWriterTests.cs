/********************************************************************************
* JsonWriterTests.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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

            new JsonWriter().WriteString(store, input, SerializationContext.Untyped, 0, default);

            Assert.That(store.ToString(), Is.EqualTo($"\"{expected}\""));
        }

        [Test]
        public void WriteString_ShouldHandleRegularStrings([Values("", "c", "cica", "1986")] string input)
        {
            StringWriter store = new();  // will be closed by the writer
 
            new JsonWriter().WriteString(store, input, SerializationContext.Untyped, 0, default);

            Assert.That(store.ToString(), Is.EqualTo($"\"{input}\""));
        }

        [Test]
        public void WriteString_ShouldConvert()
        {
            StringWriter store = new();

            new JsonWriter().WriteString(store, 1986, SerializationContext.Untyped with { GetTypeOf = _ => JsonDataTypes.String }, 0, default);

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

            new JsonWriter().WriteValue(store, input, SerializationContext.Untyped, 0, default);

            Assert.That(store.ToString(), Is.EqualTo(expected));
        }

        public static IEnumerable<object[]> WriteList_ShouldStringifyTheGivenList_Params
        {
            get
            {
                yield return new object[] { new List<object> { }, (byte) 0, "[]" };
                yield return new object[] { new List<object> { }, (byte) 2, $"[{Environment.NewLine}]" };
                yield return new object[] { new List<object> { 1 }, (byte) 0, "[1]" };
                yield return new object[] { new List<object> { 1 }, (byte) 2, $"[{Environment.NewLine}  1{Environment.NewLine}]" };
                yield return new object[] { new List<object> { 1, 2 }, (byte) 0, "[1,2]" };
                yield return new object[] { new List<object> { 1, 2 }, (byte) 2, $"[{Environment.NewLine}  1,{Environment.NewLine}  2{Environment.NewLine}]" };
                yield return new object[] { new List<object> { 1, new Dictionary<string, object> { { "prop", "val" } }, 2 }, (byte) 0, "[1,{\"prop\":\"val\"},2]" };
                yield return new object[] { new List<object> { 1, new Dictionary<string, object> { { "prop", "val" } }, 2 }, (byte) 2, $"[{Environment.NewLine}  1,{Environment.NewLine}  {{{Environment.NewLine}    \"prop\": \"val\"{Environment.NewLine}  }},{Environment.NewLine}  2{Environment.NewLine}]" };
            }
        }

        [TestCaseSource(nameof(WriteList_ShouldStringifyTheGivenList_Params))]
        public void WriteList_ShouldStringifyTheGivenList(IList<object> input, byte spaces, string expected)
        {
            StringWriter store = new();

            new JsonWriter(indent: spaces).WriteList(store, input, SerializationContext.Untyped, 0, default, default);

            Assert.That(store.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void WriteList_ShouldThrowIfWeReachTheMaxDepth()
        {
            JsonWriter writer = new(maxDepth: 1);

            Assert.DoesNotThrow(() => writer.WriteList(new StringWriter(), new object[] { 1 }, SerializationContext.Untyped, 0, default, default));
            Assert.Throws<InvalidOperationException>(() => writer.WriteList(new StringWriter(), new object[] { new object[] { 1 } }, SerializationContext.Untyped, 0, default, default));
        }

        public static IEnumerable<object[]> WriteObject_ShouldStringifyTheGivenObject_Params
        {
            get
            {
                yield return new object[] { new Dictionary<string, object>(), (byte) 0, "{}" };
                yield return new object[] { new Dictionary<string, object>(), (byte) 2, $"{{{Environment.NewLine}}}" };
                yield return new object[] { new Dictionary<string, object> { { "prop_1", "val_1"} }, (byte) 0, "{\"prop_1\":\"val_1\"}" };
                yield return new object[] { new Dictionary<string, object> { { "prop_1", "val_1" } }, (byte) 2, $"{{{Environment.NewLine}  \"prop_1\": \"val_1\"{Environment.NewLine}}}" };
                yield return new object[] { new Dictionary<string, object> { { "prop_1", "val_1" }, { "prop_2", "val_2" } }, (byte) 0, "{\"prop_1\":\"val_1\",\"prop_2\":\"val_2\"}" };
                yield return new object[] { new Dictionary<string, object> { { "prop_1", "val_1" }, { "prop_2", "val_2" } }, (byte) 2, $"{{{Environment.NewLine}  \"prop_1\": \"val_1\",{Environment.NewLine}  \"prop_2\": \"val_2\"{Environment.NewLine}}}" };
                yield return new object[] { new Dictionary<string, object> { { "prop_1", new Dictionary<string, object> { {"prop_2", 1986 } } } }, (byte) 0, "{\"prop_1\":{\"prop_2\":1986}}" };
                yield return new object[] { new Dictionary<string, object> { { "prop_1", new Dictionary<string, object> { { "prop_2", 1986 } } } }, (byte) 2, $"{{{Environment.NewLine}  \"prop_1\": {{{Environment.NewLine}    \"prop_2\": 1986{Environment.NewLine}  }}{Environment.NewLine}}}" };
            }
        }

        [TestCaseSource(nameof(WriteObject_ShouldStringifyTheGivenObject_Params))]
        public void WriteObject_ShouldStringifyTheGivenObject(IDictionary<string, object?> input, byte spaces, string expected)
        {
            StringWriter store = new();

            new JsonWriter(indent: spaces).WriteObject(store, input, SerializationContext.Untyped, 0, default, default);

            Assert.That(store.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void Write_ShouldThrowIfTheInputIsNotSerializable()
        {
            Assert.Throws<NotSupportedException>(() => new JsonWriter().Write(new StringWriter(), new { }, SerializationContext.Untyped, default));
        }

        public static IEnumerable<object[]> Write_ShouldStringify_Params
        {
            get
            {
                yield return new object[] { 1, "1" };
                yield return new object[] { true, "true" };
                yield return new object[] { null!, "null" };
                yield return new object[] { "1", "\"1\"" };
                yield return new object[] { new List<object>(), $"[{Environment.NewLine}]" };
            }
        }

        [TestCaseSource(nameof(Write_ShouldStringify_Params))]
        public void Write_ShouldStringify(object? input, string expected)
        {
            StringWriter store = new();

            new JsonWriter().Write(store, input, SerializationContext.Untyped, default);

            Assert.That(store.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void Write_ShouldHandleLargeContent()
        {
            JsonReader rdr = new
            (
                DeserializationContext.Untyped,
                JsonReaderFlags.None,
                int.MaxValue
            );

            using StreamReader content = new
            (
                Path.Combine
                (
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                    "large.json"
                )
            );

            IList? input = rdr.Parse(content, default) as IList;

            content.BaseStream.Position = 0;

            StringWriter store = new();

            new JsonWriter().Write(store, input, SerializationContext.Untyped, default);

            string
                serialized = store.ToString().Replace("\r", string.Empty),
                expected = content.ReadToEnd().Substring(1).Replace("\r", string.Empty);

            Assert.That(serialized, Is.EqualTo(expected));
        }
    }
}
