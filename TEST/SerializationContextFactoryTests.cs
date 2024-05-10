/********************************************************************************
* SerializationContextFactoryTestsBase.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
}
