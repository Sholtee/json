/********************************************************************************
* DeserializationContextFactoryTests.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using NUnit.Framework;

namespace Solti.Utils.Json.DeserializationContexts.Tests
{
    using Attributes;
    using Properties;

    public abstract class DeserializationContextFactoryTestsBase<TDescendant> where TDescendant : DeserializationContextFactoryTestsBase<TDescendant>, new()
    {
        public abstract IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases { get; }

        public abstract IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases { get; }

        public abstract IEnumerable<(Type Type, object? Config)> InvalidConfigs { get; }

        public abstract IEnumerable<Type> InvalidTypes { get; }

        public abstract DeserializationContextFactory Factory { get; }

        protected virtual bool Compare(object a, object b) => EqualityComparer<object>.Default.Equals(a, b);

        public static DeserializationContextFactoryTestsBase<TDescendant> Instance { get; } = new TDescendant();

        // TestCaseSource requires static property
        public static IEnumerable<(Type TargetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> GetValidCases => Instance.ValidCases;

        public static IEnumerable<(Type TargetType, object? Config, string Input)> GetInvalidCases => Instance.InvalidCases;

        public static IEnumerable<(Type Type, object? Config)> GetInvalidConfigs => Instance.InvalidConfigs;

        public static IEnumerable<Type> GetInvalidTypes => Instance.InvalidTypes;

        [TestCaseSource(nameof(GetValidCases))]
        public void Context_ShouldInstructTheParser((Type TargetType, object? Config, string Input, object Expected, JsonParserFlags Flags) testCase)
        {
            JsonParser parser = new(testCase.Flags);

            StringReader content = new(testCase.Input);

            Assert.That(Factory.IsSupported(testCase.TargetType));

            object? ret = parser.Parse(content, Factory.CreateContext(testCase.TargetType, testCase.Config), default);

            Assert.That(ret, Is.EqualTo(testCase.Expected).Using<object>(Compare));
        }

        [TestCaseSource(nameof(GetInvalidCases))]
        public void Context_ShouldInstructTheParserToValidate((Type TargetType, object? Config, string Input) testCase)
        {
            JsonParser parser = new();

            StringReader content = new(testCase.Input);

            Assert.Throws<JsonParserException>(() => parser.Parse(content, Factory.CreateContext(testCase.TargetType, testCase.Config), default));
        }

        [TestCaseSource(nameof(GetInvalidTypes))]
        public void CreateContext_ShouldThrowOnInvalidType(Type type) =>
            Assert.Throws<ArgumentException>(() => Factory.CreateContext(type, null));

        [TestCaseSource(nameof(GetInvalidConfigs))]
        public void CreateContext_ShouldThrowOnInvalidConfig((Type Type, object? Config) testCase) =>
            Assert.Throws<ArgumentException>(() => Factory.CreateContext(testCase.Type, testCase.Config));
    }

    [TestFixture]
    public class EnumDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<EnumDeserializationContextFactoryTests>
    {
        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(MethodImplOptions), null, "256", MethodImplOptions.AggressiveInlining, JsonParserFlags.None);
                yield return (typeof(MethodImplOptions), null, "\"AggressiveInlining\"", MethodImplOptions.AggressiveInlining, JsonParserFlags.None);

                yield return (typeof(MethodImplOptions?), null, "256", (MethodImplOptions?) MethodImplOptions.AggressiveInlining, JsonParserFlags.None);
                yield return (typeof(MethodImplOptions?), null, "\"AggressiveInlining\"", (MethodImplOptions?) MethodImplOptions.AggressiveInlining, JsonParserFlags.None);

                yield return (typeof(MethodImplOptions?), null, "null", null!, JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield return (typeof(MethodImplOptions), null, "255");
                yield return (typeof(MethodImplOptions), null, "256.1");
                yield return (typeof(MethodImplOptions), null, "\"AggressiveInlining_wrong\"");
            }
        }

        public override IEnumerable<(Type Type, object? Config)> InvalidConfigs
        {
            get
            {
                yield return (typeof(MethodImplOptions), "invalid");
                yield return (typeof(MethodImplOptions), 1);
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

        public override DeserializationContextFactory Factory => new EnumDeserializationContextFactory();
    }

    [TestFixture]
    public class GuidDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<GuidDeserializationContextFactoryTests>
    {
        private static readonly Guid TestGuid = Guid.Parse("D6B6D5B5-826E-4362-A19A-219997E6D693");

        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(Guid), "D", "\"D6B6D5B5-826E-4362-A19A-219997E6D693\"", TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid), "N", "\"D6B6D5B5826E4362A19A219997E6D693\"", TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid), "D", "\"d6b6d5b5-826e-4362-a19a-219997e6d693\"", TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid), "N", "\"d6b6d5b5826e4362a19a219997e6d693\"", TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid), null, "\"D6B6D5B5826E4362A19A219997E6D693\"", TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid?), "D", "\"D6B6D5B5-826E-4362-A19A-219997E6D693\"", (Guid?) TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid?), "N", "\"D6B6D5B5826E4362A19A219997E6D693\"", (Guid?) TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid?), "D", "\"d6b6d5b5-826e-4362-a19a-219997e6d693\"", (Guid?) TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid?), "N", "\"d6b6d5b5826e4362a19a219997e6d693\"", (Guid?) TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid?), null, "\"D6B6D5B5826E4362A19A219997E6D693\"", (Guid?) TestGuid, JsonParserFlags.None);
                yield return (typeof(Guid?), null, "null", null!, JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield return (typeof(Guid), "D", "\"invalid\"");
                yield return (typeof(Guid), "N", "\"invalid\"");
                yield return (typeof(Guid), null, "\"invalid\"");
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

        public override DeserializationContextFactory Factory => new GuidDeserializationContextFactory();
    }

    [TestFixture]
    public class DateTimeDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<DateTimeDeserializationContextFactoryTests>
    {
        private static readonly DateTime TestDate = DateTime.ParseExact("2009-06-15T13:45:30", "s", null);

        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(DateTime), "s", "\"2009-06-15T13:45:30\"", TestDate, JsonParserFlags.None);
                yield return (typeof(DateTime), "u", "\"2009-06-15 13:45:30Z\"", TestDate, JsonParserFlags.None);
                yield return (typeof(DateTime), null, "\"2009-06-15T13:45:30\"", TestDate, JsonParserFlags.None);
                yield return (typeof(DateTime?), "s", "\"2009-06-15T13:45:30\"", (DateTime?) TestDate, JsonParserFlags.None);
                yield return (typeof(DateTime?), "u", "\"2009-06-15 13:45:30Z\"", (DateTime?) TestDate, JsonParserFlags.None);
                yield return (typeof(DateTime?), null, "\"2009-06-15T13:45:30\"", (DateTime?) TestDate, JsonParserFlags.None);
                yield return (typeof(DateTime?), null, "null", null!, JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield return (typeof(DateTime), "s", "\"invalid\"");
                yield return (typeof(DateTime), "u", "\"invalid\"");
                yield return (typeof(DateTime), null, "\"invalid\"");
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

        public override DeserializationContextFactory Factory => new DateTimeDeserializationContextFactory();
    }

    [TestFixture]
    public class StreamDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<StreamDeserializationContextFactoryTests>
    {
        private static readonly Stream TestStream = new MemoryStream(Encoding.UTF8.GetBytes("cica"));

        protected override bool Compare(object a, object b)
        {
            if (a is Stream s1 && b is Stream s2 && s1.Length == s2.Length)
            {
                byte[]
                    content1 = new byte[s1.Length],
                    content2 = new byte[s2.Length];

                return content1.SequenceEqual(content2);
            }
            return false;
        }

        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(Stream), null, "\"Y2ljYQ==\"", TestStream, JsonParserFlags.None);
                yield return (typeof(MemoryStream), null, "\"Y2ljYQ==\"", TestStream, JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield return (typeof(Stream), null, "\"invalid\"");
                yield return (typeof(MemoryStream), null, "\"invalid\"");
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

        public override DeserializationContextFactory Factory => new StreamDeserializationContextFactory();
    }

    [TestFixture]
    public class NumberDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<NumberDeserializationContextFactoryTests>
    {
        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(byte), null, "86", (byte) 86, JsonParserFlags.None);
                yield return (typeof(int), null, "1986", (int) 1986, JsonParserFlags.None);
                yield return (typeof(short), null, "1986", (short) 1986, JsonParserFlags.None);
                yield return (typeof(long), null, "1986", (long) 1986, JsonParserFlags.None);

                yield return (typeof(float), null, "1986.1026", (float) 1986.1026, JsonParserFlags.None);
                yield return (typeof(double), null, "1986.1026", (double) 1986.1026, JsonParserFlags.None);
                yield return (typeof(double), null, "1986", (double) 1986, JsonParserFlags.None); 

                yield return (typeof(byte?), null, "86", (byte?) 86, JsonParserFlags.None);
                yield return (typeof(int?), null, "1986", (int?) 1986, JsonParserFlags.None);
                yield return (typeof(short?), null, "1986", (short?) 1986, JsonParserFlags.None);
                yield return (typeof(long?), null, "1986", (long?) 1986, JsonParserFlags.None);

                yield return (typeof(float?), null, "1986.1026", (float?) 1986.1026, JsonParserFlags.None);
                yield return (typeof(double?), null, "1986.1026", (double?) 1986.1026, JsonParserFlags.None);
                yield return (typeof(double?), null, "1986", (double?) 1986, JsonParserFlags.None);

                yield return (typeof(int?), null, "null", null!, JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
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

        public override DeserializationContextFactory Factory => new NumberDeserializationContextFactory();
    }

    [TestFixture]
    public class StringDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<StringDeserializationContextFactoryTests>
    {
        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(string), null, "\"cica\"", "cica", JsonParserFlags.None);
                yield return (typeof(string), null, "null", null!, JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield break;
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

        public override DeserializationContextFactory Factory => new StringDeserializationContextFactory();
    }

    [TestFixture]
    public class BooleanDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<BooleanDeserializationContextFactoryTests>
    {
        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(bool), null, "true", true, JsonParserFlags.None);
                yield return (typeof(bool), null, "false", false, JsonParserFlags.None);
                yield return (typeof(bool?), null, "true", (bool?) true, JsonParserFlags.None);
                yield return (typeof(bool?), null, "false", (bool?) false, JsonParserFlags.None);
                yield return (typeof(bool?), null, "null", null!, JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield break;
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

        public override DeserializationContextFactory Factory => new BooleanDeserializationContextFactory();
    }

    [TestFixture]
    public class ObjectDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<ObjectDeserializationContextFactoryTests>
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
            [SerializationContext<AnyObjectDeserializationContextFactory>(Config = typeof(MyEnum))]
#else
            [SerializationContext(typeof(AnyObjectDeserializationContextFactory), Config = typeof(MyEnum))]
#endif
            public object? Prop4 { get; set; }

            [NotNull]
            public Wrapped? Prop5 { get; set; }
        }

        private sealed class NotNullAttribute : ValidatorAttribute
        {
            public override bool Validate(object? value, string? name, out ICollection<string> errors)
            {
                if (value is null)
                {
                    errors = [$"value of \"{name}\" cannot be null"];
                    return false;
                }

                errors = null!;
                return true;
            }
        }

        private sealed class AnyObjectDeserializationContextFactory : DeserializationContextFactory
        {
            public override bool IsSupported(Type type) => true;

            protected override DeserializationContext CreateContextCore(Type type, object? config) => CreateFor((Type) config!);
        }

        private sealed class WrappedObjectDeserializationContextFactory : DeserializationContextFactory
        {
            public override bool IsSupported(Type type) => type == typeof(Wrapped);

            protected override DeserializationContext CreateContextCore(Type type, object? config) => CreateFor(typeof(string)) with
            {
                Convert = (object? value, out object? converted) =>
                {
                    converted = value is string str ? new Wrapped(str) : value;
                    return true;
                }
            };
        }

        private enum MyEnum
        {
            Default = 0,
            Value1 = 1
        }

#if !NETFRAMEWORK
        [SerializationContext<WrappedObjectDeserializationContextFactory>()]
#else
        [SerializationContext(typeof(WrappedObjectDeserializationContextFactory))]
#endif
        private record Wrapped(string Value)
        {
        }

        private record Empty { }

        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(Parent), null, "{\"ToBeIgnored\": 0, \"Prop1\": 1986, \"Prop2\": {\"Prop3\": \"cica\"} }", new Parent { Prop1 = 1986, Prop2 = new Nested { Prop3 = "cica" } }, JsonParserFlags.None);
                yield return (typeof(Parent), null, "{\"tobeignored\": 0, \"prop1\": 1986, \"prop2\": {\"prop3\": \"cica\"} }", new Parent { Prop1 = 1986, Prop2 = new Nested { Prop3 = "cica" } }, JsonParserFlags.CaseInsensitive);
                yield return (typeof(CustomizedParent), null, "{\"ToBeIgnored\": 0, \"Alias\": \"kutya\", \"Prop1\": 1986, \"Prop2\": {\"Prop3\": \"cica\"}, \"Prop4\": 1, \"Prop5\": \"desznaj\" }", new CustomizedParent { Prop1 = 1986, Prop2 = new Nested { Prop3 = "cica" }, Prop3 = "kutya", Prop4 = MyEnum.Value1, Prop5 = new Wrapped("desznaj") }, JsonParserFlags.None);
                yield return (typeof(CustomizedParent), null, "{\"tobeignored\": 0, \"alias\": \"kutya\", \"prop1\": 1986, \"prop2\": {\"prop3\": \"cica\"}, \"prop4\": 1, \"prop5\": \"desznaj\" }", new CustomizedParent { Prop1 = 1986, Prop2 = new Nested { Prop3 = "cica" }, Prop3 = "kutya", Prop4 = MyEnum.Value1, Prop5 = new Wrapped("desznaj") }, JsonParserFlags.CaseInsensitive);
                yield return (typeof(Parent), null, "null", null!, JsonParserFlags.None);
                yield return (typeof(Empty), null, "{}", new Empty(), JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield break;
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
                yield return typeof(int);
            }
        }

        public override DeserializationContextFactory Factory => new ObjectDeserializationContextFactory();

        [Test]
        public void Context_ShouldInstructTheParserToRunTheValidators()
        {
            JsonParser parser = new();

            StringReader content = new("{\"ToBeIgnored\": 0, \"Alias\": \"kutya\", \"Prop1\": 1986, \"Prop2\": {\"Prop3\": \"cica\"}, \"Prop4\": 1, \"Prop5\": null }");

            JsonParserException ex = Assert.Throws<JsonParserException>(() => parser.Parse(content, Factory.CreateContext(typeof(CustomizedParent), null), default))!;
            Assert.That(ex.Message, Does.Contain("value of \"Prop5\" cannot be null"));
        }
    }

    [TestFixture]
    public class DictionaryDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<DictionaryDeserializationContextFactoryTests>
    {
        private record Value
        {
            public int Prop { get; set; }
        }

        protected override bool Compare(object a, object b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is not IDictionary dictA || b is not IDictionary dictB)
                return false;

            if (dictA.Count != dictB.Count)
                return false;

            foreach (object? key in dictA.Keys)
            {
                if (!dictB.Contains(key!))
                    return false;

                if (!dictA[key!]!.Equals(dictB[key!]))
                    return false;
            }

            return true;
        }

        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(Dictionary<string, Value>), null, "{\"1\": {\"Prop\": 1}, \"2\": {\"Prop\": 2}}", new Dictionary<string, Value> { {"1", new Value { Prop = 1 } }, { "2", new Value { Prop = 2 } } }, JsonParserFlags.None);
                yield return (typeof(Dictionary<string, int>), null, "{\"1\": 1, \"2\": 2}", new Dictionary<string, int> { {"1", 1}, {"2", 2} }, JsonParserFlags.None);
                yield return (typeof(Dictionary<string, string>), null, "{\"1\": \"1\", \"2\": \"2\"}", new Dictionary<string, string> { { "1", "1" }, { "2", "2" } }, JsonParserFlags.None);
                yield return (typeof(Dictionary<string, string>), null, "null", null!, JsonParserFlags.None);

                yield return (typeof(IDictionary<string, Value>), null, "{\"1\": {\"Prop\": 1}, \"2\": {\"Prop\": 2}}", new Dictionary<string, Value> { { "1", new Value { Prop = 1 } }, { "2", new Value { Prop = 2 } } }, JsonParserFlags.None);
                yield return (typeof(IDictionary<string, int>), null, "{\"1\": 1, \"2\": 2}", new Dictionary<string, int> { { "1", 1 }, { "2", 2 } }, JsonParserFlags.None);
                yield return (typeof(IDictionary<string, string>), null, "{\"1\": \"1\", \"2\": \"2\"}", new Dictionary<string, string> { { "1", "1" }, { "2", "2" } }, JsonParserFlags.None);
                yield return (typeof(IDictionary<string, string>), null, "null", null!, JsonParserFlags.None);

                yield return (typeof(IReadOnlyDictionary<string, Value>), null, "{\"1\": {\"Prop\": 1}, \"2\": {\"Prop\": 2}}", new Dictionary<string, Value> { { "1", new Value { Prop = 1 } }, { "2", new Value { Prop = 2 } } }, JsonParserFlags.None);
                yield return (typeof(IReadOnlyDictionary<string, int>), null, "{\"1\": 1, \"2\": 2}", new Dictionary<string, int> { { "1", 1 }, { "2", 2 } }, JsonParserFlags.None);
                yield return (typeof(IReadOnlyDictionary<string, string>), null, "{\"1\": \"1\", \"2\": \"2\"}", new Dictionary<string, string> { { "1", "1" }, { "2", "2" } }, JsonParserFlags.None);
                yield return (typeof(IReadOnlyDictionary<string, string>), null, "null", null!, JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield break;
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

        public override DeserializationContextFactory Factory => new DictionaryDeserializationContextFactory();
    }

    [TestFixture]
    public class ListDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<ListDeserializationContextFactoryTests>
    {
        private record Value
        {
            public int Prop { get; set; }
        }

        private class ValueHavingListProp
        {
            public IList<int>? Prop { get; set; }

            public override bool Equals(object obj) => obj is ValueHavingListProp other && other.Prop.SequenceEqual(Prop);

            public override int GetHashCode() => base.GetHashCode();
        }

        protected override bool Compare(object a, object b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is not IList lstA || b is not IList lstB)
                return false;

            if (lstA.Count != lstB.Count)
                return false;

            for (int i = 0; i < lstA.Count; i++)
            {
                if (!lstA[i].Equals(lstB[i]))
                    return false;
            }

            return true;
        }

        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(List<ValueHavingListProp>), null, "[{\"Prop\": [1]}, {\"Prop\": [2]}]", new List<ValueHavingListProp> { new ValueHavingListProp { Prop = [1] }, new ValueHavingListProp { Prop = [2] } }, JsonParserFlags.None);
                yield return (typeof(List<Value>), null, "[{\"Prop\": 1}, {\"Prop\": 2}]", new List<Value> { new Value { Prop = 1 }, new Value { Prop = 2 } }, JsonParserFlags.None);
                yield return (typeof(List<int>), null, "[1, 2]", new List<int> { 1, 2 }, JsonParserFlags.None);
                yield return (typeof(List<string>), null, "[\"1\", \"2\"]", new List<string> { "1", "2" }, JsonParserFlags.None);
                yield return (typeof(List<string>), null, "null", null!, JsonParserFlags.None);

                yield return (typeof(IList<ValueHavingListProp>), null, "[{\"Prop\": [1]}, {\"Prop\": [2]}]", new List<ValueHavingListProp> { new ValueHavingListProp { Prop = [1] }, new ValueHavingListProp { Prop = [2] } }, JsonParserFlags.None);
                yield return (typeof(IList<Value>), null, "[{\"Prop\": 1}, {\"Prop\": 2}]", new List<Value> { new Value { Prop = 1 }, new Value { Prop = 2 } }, JsonParserFlags.None);
                yield return (typeof(IList<int>), null, "[1, 2]", new List<int> { 1, 2 }, JsonParserFlags.None);
                yield return (typeof(IList<string>), null, "[\"1\", \"2\"]", new List<string> { "1", "2" }, JsonParserFlags.None);
                yield return (typeof(IList<string>), null, "null", null!, JsonParserFlags.None);

                yield return (typeof(ICollection<ValueHavingListProp>), null, "[{\"Prop\": [1]}, {\"Prop\": [2]}]", new List<ValueHavingListProp> { new ValueHavingListProp { Prop = [1] }, new ValueHavingListProp { Prop = [2] } }, JsonParserFlags.None);
                yield return (typeof(ICollection<Value>), null, "[{\"Prop\": 1}, {\"Prop\": 2}]", new List<Value> { new Value { Prop = 1 }, new Value { Prop = 2 } }, JsonParserFlags.None);
                yield return (typeof(ICollection<int>), null, "[1, 2]", new List<int> { 1, 2 }, JsonParserFlags.None);
                yield return (typeof(ICollection<string>), null, "[\"1\", \"2\"]", new List<string> { "1", "2" }, JsonParserFlags.None);
                yield return (typeof(ICollection<string>), null, "null", null!, JsonParserFlags.None);
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield break;
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

        public override DeserializationContextFactory Factory => new ListDeserializationContextFactory();
    }

}
