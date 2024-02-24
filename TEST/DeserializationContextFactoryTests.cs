/********************************************************************************
* JsonReaderTests.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using NUnit.Framework;

namespace Solti.Utils.Json.DeserializationContexts.Tests
{
    public abstract class DeserializationContextFactoryTestsBase<TDescendant> where TDescendant : DeserializationContextFactoryTestsBase<TDescendant>, new()
    {
        public abstract IEnumerable<(Type targetType, object? Config, string Input, object Expected)> ValidCases { get; }

        public abstract IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases { get; }

        public abstract IEnumerable<(Type Type, object? Config)> InvalidConfigs { get; }

        public abstract IEnumerable<Type> InvalidTypes { get; }

        public abstract DeserializationContextFactory Factory { get; }

        public static DeserializationContextFactoryTestsBase<TDescendant> Instance { get; } = new TDescendant();

        // TestCaseSource requires static property
        public static IEnumerable<(Type TargetType, object? Config, string Input, object Expected)> GetValidCases => Instance.ValidCases;

        public static IEnumerable<(Type TargetType, object? Config, string Input)> GetInvalidCases => Instance.InvalidCases;

        public static IEnumerable<(Type Type, object? Config)> GetInvalidConfigs => Instance.InvalidConfigs;

        public static IEnumerable<Type> GetInvalidTypes => Instance.InvalidTypes;

        [TestCaseSource(nameof(GetValidCases))]
        public void TestValidCase((Type TargetType, object? Config, string Input, object Expected) testCase)
        {
            JsonParser parser = new();

            StringReader content = new(testCase.Input);

            Assert.That(Factory.IsSupported(testCase.TargetType));

            object? ret = parser.Parse(content, Factory.CreateContext(testCase.TargetType, testCase.Config), default);

            Assert.That(ret, Is.EqualTo(testCase.Expected));
        }

        [TestCaseSource(nameof(GetInvalidCases))]
        public void TestInvalidCase((Type TargetType, object? Config, string Input) testCase)
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
        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected)> ValidCases
        {
            get
            {
                yield return (typeof(MethodImplOptions), null, "256", MethodImplOptions.AggressiveInlining);
                yield return (typeof(MethodImplOptions), null, "\"AggressiveInlining\"", MethodImplOptions.AggressiveInlining);
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

        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected)> ValidCases
        {
            get
            {
                yield return (typeof(Guid), "D", "\"D6B6D5B5-826E-4362-A19A-219997E6D693\"", TestGuid);
                yield return (typeof(Guid), "N", "\"D6B6D5B5826E4362A19A219997E6D693\"", TestGuid);
                yield return (typeof(Guid), null, "\"D6B6D5B5826E4362A19A219997E6D693\"", TestGuid);
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

        public override IEnumerable<(Type targetType, object? Config, string Input, object Expected)> ValidCases
        {
            get
            {
                yield return (typeof(DateTime), "s", "\"2009-06-15T13:45:30\"", TestDate);
                yield return (typeof(DateTime), "u", "\"2009-06-15 13:45:30Z\"", TestDate);
                yield return (typeof(DateTime), null, "\"2009-06-15T13:45:30\"", TestDate);
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
}
