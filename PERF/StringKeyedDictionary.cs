/********************************************************************************
* StringKeyedDictionary.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    using Internals;

    [MemoryDiagnoser]
    public class HashHelpersTests
    {
        public static string INPUT = "cica123456789";

        [Benchmark(Baseline = true)]
        public int GetHashCodeNative() => string.GetHashCode(INPUT.AsSpan(), StringComparison.OrdinalIgnoreCase);

        [Benchmark]
        public new int GetHashCode() => HashHelpers.GetHashCode(INPUT.AsSpan());
    }

    [MemoryDiagnoser]
    public class StringComparisonTests
    {
        public static readonly string
            STRING_1 = "cica123456789",
            STRING_2 = new(STRING_1);

        [Benchmark]
        public bool CompareAsString() => STRING_1.Equals(STRING_2, StringComparison.OrdinalIgnoreCase);

        [Benchmark]
        public bool CompareAsSpan() => STRING_1.AsSpan().Equals(STRING_2.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    [MemoryDiagnoser]
    public class StringKeyedDictionaryTests
    {
        [Params(1, 2, 3, 5, 10, 100, 1000)]
        public int EntryCount { get; set; }

        public string[] Keys { get; set; } = null!;

        private StringKeyedDictionary<int> Dict { get; set; } = null!;

        private readonly Random Random = new();

        [GlobalSetup(Target = nameof(Get))]
        public void SetupGet()
        {
            Dict = new StringKeyedDictionary<int>();

            Keys = new string[EntryCount];

            for (int i = 0; i < EntryCount; i++)
            {
                Dict.Add(Keys[i] = Path.GetRandomFileName(), i);
            }
        }

        [Benchmark]
        public void Get() => Dict.TryGetValue(Keys[Random.Next(EntryCount)].AsSpan(), true, out _);

        private Dictionary<string, int> DictNative { get; set; } = null!;

        [GlobalSetup(Target = nameof(GetNative))]
        public void SetupGetNative()
        {
            DictNative = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            Keys = new string[EntryCount];

            for (int i = 0; i < EntryCount; i++)
            {
                Keys[i] = Path.GetRandomFileName();
                DictNative.Add(new string(Keys[i]), i);  // copy the string to avoid by ref comparison
            }
        }

        [Benchmark(Baseline = true)]
        public void GetNative() => DictNative.TryGetValue(Keys[Random.Next(EntryCount)], out _);
    }
}
