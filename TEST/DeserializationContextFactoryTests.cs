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

namespace Solti.Utils.Json.Tests
{
    public abstract class DeserializationContextFactoryTestsBase<TDescendant> where TDescendant: DeserializationContextFactoryTestsBase<TDescendant>, new()
    {
        public abstract IEnumerable<(Type targetType, object? Config, string Input, object Expected)> ValidCases { get; }

        public abstract IEnumerable<(Type targetType, object? Config, string Input)> InvalidCases { get; }

        public abstract DeserializationContextFactory Factory { get; }

        public static DeserializationContextFactoryTestsBase<TDescendant> Instance { get; } = new TDescendant();

        // TestCaseSource requires static property
        public static IEnumerable<(Type targetType, object? Config, string Input, object Expected)> GetValidCases => Instance.ValidCases;

        public static IEnumerable<(Type targetType, object? Config, string Input)> GetInvalidCases => Instance.InvalidCases;

        [TestCaseSource(nameof(GetValidCases))]
        public void TestValidCase((Type TargetType, object? Config, string Input, object Expected) testCase)
        {
            JsonReader rdr = new();

            StringReader content = new(testCase.Input);

            object? ret = rdr.Parse(content, Factory.CreateContext(testCase.TargetType, testCase.Config), default);

            Assert.That(ret, Is.EqualTo(testCase.Expected));
        }

        [TestCaseSource(nameof(GetInvalidCases))]
        public void TestInvalidCase((Type TargetType, object? Config, string Input) testCase)
        {
            JsonReader rdr = new();

            StringReader content = new(testCase.Input);

            Assert.Throws<InvalidOperationException>(() => rdr.Parse(content, Factory.CreateContext(testCase.TargetType, testCase.Config), default));
        }
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

        public override DeserializationContextFactory Factory => new EnumDeserializationContextFactory();    
    }
}
