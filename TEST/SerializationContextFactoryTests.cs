/********************************************************************************
* SerializationContextFactoryTestsBase.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using NUnit.Framework;

namespace Solti.Utils.Json.Contexts.Tests
{
    using Attributes;

    public abstract class SerializationContextFactoryTestsBase<TDescendant> where TDescendant : SerializationContextFactoryTestsBase<TDescendant>, new()
    {
        public abstract IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases { get; }

        public abstract IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases { get; }

        public abstract IEnumerable<(Type Type, object? Config)> InvalidConfigs { get; }

        public abstract IEnumerable<Type> InvalidTypes { get; }

        public abstract ContextFactory Factory { get; }

        public static SerializationContextFactoryTestsBase<TDescendant> Instance { get; } = new TDescendant();

        // TestCaseSource requires static property
        public static IEnumerable<(Type TargetType, object? Config, object? Input, string Expected)> GetValidCases => Instance.ValidCases;

        public static IEnumerable<(Type TargetType, object? Config, object? Input)> GetInvalidCases => Instance.InvalidCases;

        public static IEnumerable<(Type Type, object? Config)> GetInvalidConfigs => Instance.InvalidConfigs;

        public static IEnumerable<Type> GetInvalidTypes => Instance.InvalidTypes;

        [TestCaseSource(nameof(GetValidCases))]
        public void Context_ShouldInstructTheWriter((Type TargetType, object? Config, object? Input, string Expected) testCase)
        {
            JsonWriter writer = new();

            StringWriter output = new();

            Assert.That(Factory.IsSerializationSupported(testCase.TargetType));

            writer.Write(output, true, testCase.Input, Factory.CreateSerializationContext(testCase.TargetType, testCase.Config), default);

            Assert.That(output.ToString(), Is.EqualTo(testCase.Expected));
        }

        [TestCaseSource(nameof(GetInvalidCases))]
        public void Context_ShouldInstructTheWriterToValidate((Type TargetType, object? Config, object? Input) testCase)
        {
            JsonWriter writer = new();

            Assert.Throws<JsonWriterException>
            (
                () => writer.Write
                (
                    new StringWriter(),
                    true,
                    testCase.Input,
                    Factory.CreateSerializationContext(testCase.TargetType, testCase.Config),
                    default
                )
            );
        }

        [TestCaseSource(nameof(GetInvalidTypes))]
        public void CreateContext_ShouldThrowOnInvalidType(Type type) =>
            Assert.Throws<ArgumentException>(() => Factory.CreateSerializationContext(type, null));

        [TestCaseSource(nameof(GetInvalidConfigs))]
        public void CreateContext_ShouldThrowOnInvalidConfig((Type Type, object? Config) testCase) =>
            Assert.Throws<ArgumentException>(() => Factory.CreateSerializationContext(testCase.Type, testCase.Config));
    }

    [TestFixture]
    public class EnumSerializationContextFactoryTests : SerializationContextFactoryTestsBase<EnumSerializationContextFactoryTests>
    {
        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                yield return (typeof(MethodImplOptions), null, MethodImplOptions.AggressiveInlining, "\"AggressiveInlining\"");
                yield return (typeof(MethodImplOptions), null, MethodImplOptions.AggressiveInlining | MethodImplOptions.InternalCall, "\"AggressiveInlining, InternalCall\"");

                yield return (typeof(MethodImplOptions?), null, MethodImplOptions.AggressiveInlining, "\"AggressiveInlining\"");
                yield return (typeof(MethodImplOptions?), null, MethodImplOptions.AggressiveInlining | MethodImplOptions.InternalCall, "\"AggressiveInlining, InternalCall\"");
                yield return (typeof(MethodImplOptions?), null, null, "null");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(MethodImplOptions), null, "AggressiveInlining");
                yield return (typeof(MethodImplOptions), null, 256);
                yield return (typeof(MethodImplOptions), null, AttributeTargets.Parameter);
                yield return (typeof(MethodImplOptions), null, null);

                yield return (typeof(MethodImplOptions?), null, "AggressiveInlining");
                yield return (typeof(MethodImplOptions?), null, 256);
                yield return (typeof(MethodImplOptions?), null, AttributeTargets.Parameter);
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(MethodImplOptions), "invalid");
                yield return (typeof(MethodImplOptions), 1);

                yield return (typeof(MethodImplOptions?), "invalid");
                yield return (typeof(MethodImplOptions?), 1);
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(int);
                yield return typeof(string);
            }
        }

        public override ContextFactory Factory => new EnumContextFactory();
    }

    [TestFixture]
    public class GuidSerializationContextFactoryTests : SerializationContextFactoryTestsBase<GuidSerializationContextFactoryTests>
    {
        private static readonly Guid TestGuid = Guid.Parse("D6B6D5B5-826E-4362-A19A-219997E6D693");

        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                yield return (typeof(Guid), "D", TestGuid, "\"d6b6d5b5-826e-4362-a19a-219997e6d693\"");
                yield return (typeof(Guid), "N", TestGuid, "\"d6b6d5b5826e4362a19a219997e6d693\"");
                yield return (typeof(Guid), null, TestGuid, "\"d6b6d5b5-826e-4362-a19a-219997e6d693\"");
                yield return (typeof(Guid?), "D", (Guid?) TestGuid, "\"d6b6d5b5-826e-4362-a19a-219997e6d693\"");
                yield return (typeof(Guid?), "N", (Guid?) TestGuid, "\"d6b6d5b5826e4362a19a219997e6d693\"");
                yield return (typeof(Guid?), null, (Guid?) TestGuid, "\"d6b6d5b5-826e-4362-a19a-219997e6d693\"");
                yield return (typeof(Guid?), null, null, "null");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(Guid), "D", "invalid");
                yield return (typeof(Guid), "N", "invalid");
                yield return (typeof(Guid), "D", 1);
                yield return (typeof(Guid), "N", 1);
                yield return (typeof(Guid), null, null);
                yield return (typeof(Guid?), "D", "invalid");
                yield return (typeof(Guid?), "N", "invalid");
                yield return (typeof(Guid?), "D", 1);
                yield return (typeof(Guid?), "N", 1);
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(Guid), "invalid");
                yield return (typeof(Guid), 1);
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(int);
                yield return typeof(string);
            }
        }

        public override ContextFactory Factory => new GuidContextFactory();
    }

    [TestFixture]
    public class DateTimeSerializationContextFactoryTests : SerializationContextFactoryTestsBase<DateTimeSerializationContextFactoryTests>
    {
        private static readonly DateTime TestDate = DateTime.ParseExact("2009-06-15T13:45:30", "s", null);

        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                yield return (typeof(DateTime), "s", TestDate, "\"2009-06-15T13:45:30\"");
                yield return (typeof(DateTime), "u", TestDate, "\"2009-06-15 13:45:30Z\"");
                yield return (typeof(DateTime), null, TestDate, "\"06/15/2009 13:45:30\"");
                yield return (typeof(DateTime?), "s", (DateTime?) TestDate, "\"2009-06-15T13:45:30\"");
                yield return (typeof(DateTime?), "u", (DateTime?) TestDate, "\"2009-06-15 13:45:30Z\"");
                yield return (typeof(DateTime?), null, (DateTime?) TestDate, "\"06/15/2009 13:45:30\"");
                yield return (typeof(DateTime?), null, null, "null");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(DateTime), "s", 1);
                yield return (typeof(DateTime), "u", 1);
                yield return (typeof(DateTime), null, null);
                yield return (typeof(DateTime?), "s", 1);
                yield return (typeof(DateTime?), "u", 1);
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(DateTime), 1);
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(int);
                yield return typeof(string);
            }
        }

        public override ContextFactory Factory => new DateTimeContextFactory();
    }

    [TestFixture]
    public class StreamSerializationContextFactoryTests : SerializationContextFactoryTestsBase<StreamSerializationContextFactoryTests>
    {
        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                byte[] content = Encoding.UTF8.GetBytes("cica");

                yield return (typeof(Stream), null, new MemoryStream(content), "\"Y2ljYQ==\"");
                yield return (typeof(MemoryStream), null, new MemoryStream(content), "\"Y2ljYQ==\"");
                yield return (typeof(MemoryStream), null, null, "null");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(Stream), null, new object());
                yield return (typeof(MemoryStream), null, "invalid");
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(Stream), 1);
                yield return (typeof(MemoryStream), "invalid");
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(int);
                yield return typeof(string);
            }
        }

        public override ContextFactory Factory => new StreamContextFactory();
    }

    [TestFixture]
    public class NumberSerializationContextFactoryTests : SerializationContextFactoryTestsBase<NumberSerializationContextFactoryTests>
    {
        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                yield return (typeof(byte), null, (byte) 86, "86");
                yield return (typeof(int), null, (int) 1986, "1986");
                yield return (typeof(short), null, (short) 1986, "1986");
                yield return (typeof(long), null, (long) 1986, "1986");

                yield return (typeof(float), null, (float) 1986.1026, ((float) 1986.1026).ToString(CultureInfo.InvariantCulture));
                yield return (typeof(double), null, (double) 1986.1026, "1986.1026");
                yield return (typeof(double), null, (double) 1986, "1986");

                yield return (typeof(byte?), null, (byte?) 86, "86");
                yield return (typeof(int?), null, (int?) 1986, "1986");
                yield return (typeof(short?), null, (short?) 1986, "1986");
                yield return (typeof(long?), null, (long?) 1986, "1986");

                yield return (typeof(float?), null, (float?) 1986.1026, ((float) 1986.1026).ToString(CultureInfo.InvariantCulture));
                yield return (typeof(double?), null, (double?) 1986.1026, "1986.1026");
                yield return (typeof(double?), null, (double?) 1986, "1986");

                yield return (typeof(byte?), null, null!, "null");
                yield return (typeof(int?), null, null!, "null");
                yield return (typeof(short?), null, null!, "null");
                yield return (typeof(long?), null, null!, "null");
                yield return (typeof(float?), null, null!, "null");
                yield return (typeof(double?), null, null!, "null");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(byte), null, "1986");
                yield return (typeof(int), null, "1986.1026");
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(int), 1);
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(string);
            }
        }

        public override ContextFactory Factory => new NumberContextFactory();
    }

    [TestFixture]
    public class StringSerializationContextFactoryTests : SerializationContextFactoryTestsBase<StringSerializationContextFactoryTests>
    {
        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                yield return (typeof(string), null, "cica", "\"cica\"");
                yield return (typeof(string), null, null, "null");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(string), null, 1986);
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(string), 1);
                yield return (typeof(string), "invalid");
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(int);
            }
        }

        public override ContextFactory Factory => new StringContextFactory();
    }

    [TestFixture]
    public class BooleanSerializationContextFactoryTests : SerializationContextFactoryTestsBase<BooleanSerializationContextFactoryTests>
    {
        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                yield return (typeof(bool), null, true, "true");
                yield return (typeof(bool), null, false, "false");
                yield return (typeof(bool?), null, (bool?) true, "true");
                yield return (typeof(bool?), null, (bool?) false, "false");
                yield return (typeof(bool?), null, null, "null");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(bool), null, 1986);
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(bool), 1);
                yield return (typeof(bool), "invalid");
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(int);
            }
        }

        public override ContextFactory Factory => new BooleanContextFactory();
    }

    [TestFixture]
    public class DictionarySerializationContextFactoryTests : SerializationContextFactoryTestsBase<DictionarySerializationContextFactoryTests>
    {
        private record Value
        {
            public int Prop { get; set; }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                yield return (typeof(Dictionary<string, Value>), null, new Dictionary<string, Value> { { "1", new Value { Prop = 1 } }, { "2", new Value { Prop = 2 } } }, $"{{{Environment.NewLine}  \"1\": {{{Environment.NewLine}    \"Prop\": 1{Environment.NewLine}  }},{Environment.NewLine}  \"2\": {{{Environment.NewLine}    \"Prop\": 2{Environment.NewLine}  }}{Environment.NewLine}}}");
                yield return (typeof(Dictionary<string, int>), null, new Dictionary<string, int> { { "1", 1 }, { "2", 2 } }, $"{{{Environment.NewLine}  \"1\": 1,{Environment.NewLine}  \"2\": 2{Environment.NewLine}}}");
                yield return (typeof(Dictionary<string, string>), null, new Dictionary<string, string> { { "1", "1" }, { "2", "2" } }, $"{{{Environment.NewLine}  \"1\": \"1\",{Environment.NewLine}  \"2\": \"2\"{Environment.NewLine}}}");
                yield return (typeof(Dictionary<string, string>), null, null, "null");

                yield return (typeof(IDictionary<string, Value>), null, new Dictionary<string, Value> { { "1", new Value { Prop = 1 } }, { "2", new Value { Prop = 2 } } }, $"{{{Environment.NewLine}  \"1\": {{{Environment.NewLine}    \"Prop\": 1{Environment.NewLine}  }},{Environment.NewLine}  \"2\": {{{Environment.NewLine}    \"Prop\": 2{Environment.NewLine}  }}{Environment.NewLine}}}");
                yield return (typeof(IDictionary<string, int>), null, new Dictionary<string, int> { { "1", 1 }, { "2", 2 } }, $"{{{Environment.NewLine}  \"1\": 1,{Environment.NewLine}  \"2\": 2{Environment.NewLine}}}");
                yield return (typeof(IDictionary<string, string>), null, new Dictionary<string, string> { { "1", "1" }, { "2", "2" } }, $"{{{Environment.NewLine}  \"1\": \"1\",{Environment.NewLine}  \"2\": \"2\"{Environment.NewLine}}}");
                yield return (typeof(IDictionary<string, string>), null, null, "null");

                yield return (typeof(IReadOnlyDictionary<string, Value>), null, new Dictionary<string, Value> { { "1", new Value { Prop = 1 } }, { "2", new Value { Prop = 2 } } }, $"{{{Environment.NewLine}  \"1\": {{{Environment.NewLine}    \"Prop\": 1{Environment.NewLine}  }},{Environment.NewLine}  \"2\": {{{Environment.NewLine}    \"Prop\": 2{Environment.NewLine}  }}{Environment.NewLine}}}");
                yield return (typeof(IReadOnlyDictionary<string, int>), null, new Dictionary<string, int> { { "1", 1 }, { "2", 2 } }, $"{{{Environment.NewLine}  \"1\": 1,{Environment.NewLine}  \"2\": 2{Environment.NewLine}}}");
                yield return (typeof(IReadOnlyDictionary<string, string>), null, new Dictionary<string, string> { { "1", "1" }, { "2", "2" } }, $"{{{Environment.NewLine}  \"1\": \"1\",{Environment.NewLine}  \"2\": \"2\"{Environment.NewLine}}}");
                yield return (typeof(IReadOnlyDictionary<string, string>), null, null, "null");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(IDictionary<string, string>), null, 1986);
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(Dictionary<string, int>), 1);
                yield return (typeof(Dictionary<string, int>), "invalid");
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(int);
                yield return typeof(Dictionary<int, int>);
            }
        }

        public override ContextFactory Factory => new DictionaryContextFactory();
    }

    [TestFixture]
    public class ListSerializationContextFactoryTests : SerializationContextFactoryTestsBase<ListSerializationContextFactoryTests>
    {
        private record Value
        {
            public int Prop { get; set; }
        }

        private class ValueHavingListProp
        {
            public IList<int> Prop { get; set; } = null!;

            public override bool Equals(object? obj) => obj is ValueHavingListProp other && other.Prop.SequenceEqual(Prop);

            public override int GetHashCode() => base.GetHashCode();
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                yield return (typeof(List<ValueHavingListProp>), null, new List<ValueHavingListProp> { new ValueHavingListProp { Prop = [1] }, new ValueHavingListProp { Prop = [2] } }, $"[{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": [{Environment.NewLine}      1{Environment.NewLine}    ]{Environment.NewLine}  }},{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": [{Environment.NewLine}      2{Environment.NewLine}    ]{Environment.NewLine}  }}{Environment.NewLine}]");
                yield return (typeof(List<Value>), null, new List<Value> { new Value { Prop = 1 }, new Value { Prop = 2 } }, $"[{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": 1{Environment.NewLine}  }},{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": 2{Environment.NewLine}  }}{Environment.NewLine}]");
                yield return (typeof(List<int>), null, new List<int> { 1, 2 }, $"[{Environment.NewLine}  1,{Environment.NewLine}  2{Environment.NewLine}]");
                yield return (typeof(List<string>), null, new List<string> { "1", "2" }, $"[{Environment.NewLine}  \"1\",{Environment.NewLine}  \"2\"{Environment.NewLine}]");
                yield return (typeof(List<string>), null, null, "null");

                yield return (typeof(IList<ValueHavingListProp>), null, new List<ValueHavingListProp> { new ValueHavingListProp { Prop = [1] }, new ValueHavingListProp { Prop = [2] } }, $"[{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": [{Environment.NewLine}      1{Environment.NewLine}    ]{Environment.NewLine}  }},{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": [{Environment.NewLine}      2{Environment.NewLine}    ]{Environment.NewLine}  }}{Environment.NewLine}]");
                yield return (typeof(IList<Value>), null, new List<Value> { new Value { Prop = 1 }, new Value { Prop = 2 } }, $"[{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": 1{Environment.NewLine}  }},{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": 2{Environment.NewLine}  }}{Environment.NewLine}]");
                yield return (typeof(IList<int>), null, new List<int> { 1, 2 }, $"[{Environment.NewLine}  1,{Environment.NewLine}  2{Environment.NewLine}]");
                yield return (typeof(IList<string>), null, new List<string> { "1", "2" }, $"[{Environment.NewLine}  \"1\",{Environment.NewLine}  \"2\"{Environment.NewLine}]");
                yield return (typeof(IList<string>), null, null, "null");

                yield return (typeof(ICollection<ValueHavingListProp>), null, new List<ValueHavingListProp> { new ValueHavingListProp { Prop = [1] }, new ValueHavingListProp { Prop = [2] } }, $"[{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": [{Environment.NewLine}      1{Environment.NewLine}    ]{Environment.NewLine}  }},{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": [{Environment.NewLine}      2{Environment.NewLine}    ]{Environment.NewLine}  }}{Environment.NewLine}]");
                yield return (typeof(ICollection<Value>), null, new List<Value> { new Value { Prop = 1 }, new Value { Prop = 2 } }, $"[{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": 1{Environment.NewLine}  }},{Environment.NewLine}  {{{Environment.NewLine}    \"Prop\": 2{Environment.NewLine}  }}{Environment.NewLine}]");
                yield return (typeof(ICollection<int>), null, new List<int> { 1, 2 }, $"[{Environment.NewLine}  1,{Environment.NewLine}  2{Environment.NewLine}]");
                yield return (typeof(ICollection<string>), null, new List<string> { "1", "2" }, $"[{Environment.NewLine}  \"1\",{Environment.NewLine}  \"2\"{Environment.NewLine}]");
                yield return (typeof(ICollection<string>), null, null, "null");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(ICollection<string>), null, 1986);
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(List<int>), 1);
                yield return (typeof(IList<int>), "invalid");
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(int);
            }
        }

        public override ContextFactory Factory => new ListContextFactory();
    }

    [TestFixture]
    public class ObjectSerializationContextFactoryTests : SerializationContextFactoryTestsBase<ObjectSerializationContextFactoryTests>
    {
        private record Nested
        {
            public string? Prop3 { get; set; }
        }

        private record Parent
        {
            public int Prop1 { get; set; }

            public Nested? Prop2 { get; set; }
        }

        private record CustomizedParent : Parent
        {
            [Ignore]
            public int ToBeIgnored { get; set; }

            [Alias(Name = "Alias")]
            public string? Prop3 { get; set; }
#if !NETFRAMEWORK
            [Context<AnyObjectContextFactory>(Config = typeof(MyEnum))]
#else
            [Context(typeof(AnyObjectContextFactory), Config = typeof(MyEnum))]
#endif
            public object? Prop4 { get; set; }

            public Wrapped? Prop5 { get; set; }
        }

        private sealed class AnyObjectContextFactory : ContextFactory
        {
            public override bool IsSerializationSupported(Type type) => true;

            protected override SerializationContext CreateSerializationContextCore(Type type, object? config) => CreateSerializationContextFor((Type) config!);
        }

        private sealed class WrappedObjectContextFactory : ContextFactory
        {
            public override bool IsSerializationSupported(Type type) => type == typeof(Wrapped);

            protected override SerializationContext CreateSerializationContextCore(Type type, object? config) => CreateSerializationContextFor(typeof(string)) with
            {
                ConvertToString = (object? value, Buffer<char> buffer) => value is Wrapped wrapped
                    ? wrapped.Value.AsSpan()
                    : throw new NotSupportedException(),
                GetTypeOf = val =>
                    val is Wrapped ? JsonDataTypes.String : JsonDataTypes.Unkown
            };
        }

        private enum MyEnum
        {
            Default = 0,
            Value1 = 1
        }

#if !NETFRAMEWORK
        [Context<WrappedObjectContextFactory>()]
#else
        [Context(typeof(WrappedObjectContextFactory))]
#endif
        private record Wrapped(string Value)
        {
        }

        private record Empty { }

        public override IEnumerable<(Type targetType, object? Config, object? Input, string Expected)> ValidCases
        {
            get
            {
                yield return (typeof(Parent), null, new Parent { Prop1 = 1986, Prop2 = new Nested { Prop3 = "cica" } }, $"{{{Environment.NewLine}  \"Prop1\": 1986,{Environment.NewLine}  \"Prop2\": {{{Environment.NewLine}    \"Prop3\": \"cica\"{Environment.NewLine}  }}{Environment.NewLine}}}");
                yield return (typeof(CustomizedParent), null, new CustomizedParent { Prop1 = 1986, Prop2 = new Nested { Prop3 = "cica" }, Prop3 = "kutya", Prop4 = MyEnum.Value1, Prop5 = new Wrapped("desznaj") }, $"{{{Environment.NewLine}  \"Alias\": \"kutya\",{Environment.NewLine}  \"Prop4\": \"Value1\",{Environment.NewLine}  \"Prop5\": \"desznaj\",{Environment.NewLine}  \"Prop1\": 1986,{Environment.NewLine}  \"Prop2\": {{{Environment.NewLine}    \"Prop3\": \"cica\"{Environment.NewLine}  }}{Environment.NewLine}}}");
                yield return (typeof(Parent), null, null, "null");
                yield return (typeof(Empty), null, new Empty(), $"{{{Environment.NewLine}}}");
            }
        }

        public override IEnumerable<(Type targetType, object? Config, object? Input)> InvalidCases
        {
            get
            {
                yield return (typeof(Parent), null, 1986);
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(Parent), 1);
                yield return (typeof(Parent), "invalid");
            }
        }

        public override IEnumerable<Type> InvalidTypes
        {
            get
            {
                yield return typeof(object);
            }
        }

        public override ContextFactory Factory => new ObjectContextFactory();
    }
}
