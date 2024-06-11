/********************************************************************************
* Memory.cs                                                                     *
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
    public class HashTests
    {
        [Params("", "a", "ab", "abcd", "abcdefgh", "abcdefghijklmnopqrstuvwz")]
        public string Input { get; set; } = null!;

        [Params(true, false)]
        public bool IgnoreCase { get; set; }

        [Benchmark(Baseline = true)]
        public int GetNativeHashCode() => string.GetHashCode(Input, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        [Benchmark]
        public int GetMurmurHashCode() => Input.AsSpan().GetHashCode(IgnoreCase);
    }

    [MemoryDiagnoser]
    public class IndexOfAnyExceptTests
    {
        public static IEnumerable<string> Inputs
        {
            get
            {
                yield return "";
                yield return new string(Consts.FLOATING.Substring(0, 1));
                yield return new string(Consts.FLOATING.Substring(0, 2));
                yield return new string(Consts.FLOATING.Substring(0, 5));
                yield return Consts.FLOATING + Consts.FLOATING + Consts.FLOATING;
            }
        }

        [ParamsSource(nameof(Inputs))]
        public string Input { get; set; } = null!;

        [Benchmark(Baseline = true)]
        public int IndexOfAnyExceptNative() => System.MemoryExtensions.IndexOfAnyExcept(Input.AsSpan(), Consts.FLOATING.AsSpan());

        [Benchmark]
        public int IndexOfAnyExcept() => Internals.MemoryExtensions.IndexOfAnyExcept(Input.AsSpan(), Consts.FLOATING.AsSpan());

        [Benchmark]
        public int IndexOfAnyExceptNotOptimized()
        {
            for (int i = 0; i < Input.Length; i++)
            {
                if (Consts.FLOATING.IndexOf(Input[i]) < 0)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
