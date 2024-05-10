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
                yield return (typeof(MethodImplOptions), null, MethodImplOptions.AggressiveInlining, "AggressiveInlining");
                yield return (typeof(MethodImplOptions), null, MethodImplOptions.AggressiveInlining | MethodImplOptions.InternalCall, "AggressiveInlining, InternalCall");

                yield return (typeof(MethodImplOptions?), null, MethodImplOptions.AggressiveInlining, "AggressiveInlining");
                yield return (typeof(MethodImplOptions?), null, MethodImplOptions.AggressiveInlining | MethodImplOptions.InternalCall, "AggressiveInlining, InternalCall");
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
                yield return (typeof(Guid), null, TestGuid, "\"d6b6d5b5826e4362a19a219997e6d693\"");
                yield return (typeof(Guid?), "D", (Guid?) TestGuid, "\"d6b6d5b5-826e-4362-a19a-219997e6d693\"");
                yield return (typeof(Guid?), "N", (Guid?) TestGuid, "\"d6b6d5b5826e4362a19a219997e6d693\"");
                yield return (typeof(Guid?), null, (Guid?) TestGuid, "\"d6b6d5b5826e4362a19a219997e6d693\"");
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
                yield return (typeof(DateTime), null, TestDate, "\"2009-06-15T13:45:30\"");
                yield return (typeof(DateTime?), "s", (DateTime?) TestDate, "\"2009-06-15T13:45:30\"");
                yield return (typeof(DateTime?), "u", (DateTime?) TestDate, "\"2009-06-15 13:45:30Z\"");
                yield return (typeof(DateTime?), null, (DateTime?) TestDate, "\"2009-06-15T13:45:30\"");
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

                yield return (typeof(int?), null, null!, "null");
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

}
