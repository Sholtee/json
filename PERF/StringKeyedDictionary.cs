/********************************************************************************
* StringKeyedDictionary.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    using Internals;

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
                Dict.Add(Keys[i] = i.ToString(), i);
            }
        }

        [Benchmark]
        public void Get() => Dict.TryGetValue(Keys[Random.Next(EntryCount)].AsSpan(), false, out _);

        private Dictionary<string, int> DictNative { get; set; } = null!;

        [GlobalSetup(Target = nameof(GetNative))]
        public void SetupGetNative()
        {
            DictNative = new Dictionary<string, int>();

            Keys = new string[EntryCount];

            for (int i = 0; i < EntryCount; i++)
            {
                DictNative.Add(Keys[i] = i.ToString(), i);
            }
        }

        [Benchmark(Baseline = true)]
        public void GetNative() => DictNative.TryGetValue(Keys[Random.Next(EntryCount)], out _);
    }
}
