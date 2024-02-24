/********************************************************************************
* DeserializationContextFactoryTests.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using NUnit.Framework;

namespace Solti.Utils.Json.DeserializationContexts.Tests
{
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

            Assert.Throws<InvalidOperationException>(() => parser.Parse(content, Factory.CreateContext(testCase.TargetType, testCase.Config), default));
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
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield return (typeof(MethodImplOptions), null, "255");
                yield return (typeof(MethodImplOptions), null, "256.0");
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
    public class DateTimeDeserializationContextFactoryTests : DeserializationContextFactoryTestsBase<GuidDeserializationContextFactoryTests>
    {
        private static readonly DateTime TestDate = DateTime.ParseExact("2009-06-15T13:45:30", "s", null);

        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected, JsonParserFlags Flags)> ValidCases
        {
            get
            {
                yield return (typeof(DateTime), "s", "\"2009-06-15T13:45:30\"", TestDate, JsonParserFlags.None);
                yield return (typeof(DateTime), "u", "\"2009-06-15 13:45:30Z\"", TestDate, JsonParserFlags.None);
                yield return (typeof(DateTime), null, "\"2009-06-15T13:45:30\"", TestDate, JsonParserFlags.None);
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

        public override DeserializationContextFactory Factory => new GuidDeserializationContextFactory();
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
            }
        }

        public override IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases
        {
            get
            {
                yield return (typeof(byte), null, "1986");
                yield return (typeof(int), null, "1986.1026");
                yield return (typeof(double), null, "1986");  // TBD: we should support this scenario?
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
}
