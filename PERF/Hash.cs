/********************************************************************************
* Hash.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    using Internals;

    [MemoryDiagnoser]
    public class HashTests
    {
        [Params("", "a", "ab", "abcd", "abcdefgh", "abcdefghijklmnopqrstuvwz")]
        public string Input { get; set; } = null!;

        [Params(true, false)]
        public bool IgnoreCase { get; set; }

        [Benchmark(Baseline = true)]
        public int GetNativeHashCode() => string.GetHashCode(Input, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        [Benchmark]
        public int GetMurmurHash3() => Input.AsSpan().GetHashCode(IgnoreCase);
    }
}
