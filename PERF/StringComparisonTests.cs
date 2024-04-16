/********************************************************************************
* StringComparisonTests.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    [MemoryDiagnoser]
    public class StringComparisonTests
    {
        private string
            FStr1 = null!,
            FStr2 = null!;

        [GlobalSetup]
        public void Setup()
        {
            FStr1 = Path.GetRandomFileName();
            FStr2 = new string(FStr1);  // avoid by ref comparison
        }

        [Params(StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase)]
        public StringComparison Comparison { get; set; }

        [Benchmark]
        public int StringCompare() => string.Compare(FStr1, FStr2, Comparison);

        [Benchmark]
        public int SpanCompare() => FStr1.AsSpan().CompareTo(FStr2, Comparison);
    }
}
