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
    public class StringKeyedDictionaryTests
    {
        [Params(1, 2, 3, 5, 10, 100, 1000)]
        public int EntryCount { get; set; }

        [Params(true, false)]
        public bool IgnoreCase { get; set; }

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

        [Benchmark(OperationsPerInvoke = 100)]
        public void Get()
        {
            ReadOnlySpan<char> key = Keys[Random.Next(EntryCount)];

            for (int i = 0; i < 100; i++)
            {
                Dict.TryGetValue(key, IgnoreCase, out _);
            }
        }

        private Dictionary<string, int> DictNative { get; set; } = null!;

        [GlobalSetup(Target = nameof(GetNative))]
        public void SetupGetNative()
        {
            DictNative = new Dictionary<string, int>(IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            Keys = new string[EntryCount];

            for (int i = 0; i < EntryCount; i++)
            {
                Keys[i] = Path.GetRandomFileName();
                DictNative.Add(new string(Keys[i]), i);  // copy the string to avoid by ref comparison
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = 100)]
        public void GetNative()
        {
            string key = Keys[Random.Next(EntryCount)];

            for (int i = 0; i < 100; i++)
            {
                DictNative.TryGetValue(key, out _);
            }
        }
    }
}
