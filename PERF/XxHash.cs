/********************************************************************************
* XxHash.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    using Internals;

    [MemoryDiagnoser]
    public class XxHashTests
    {
        [Params("a", "ab", "abcd", "abcdefgh", "abcdefghijklmnopqrstuvwz")]
        public string Input { get; set; } = null!;

        [Benchmark()]
        public int GetXxHashCode() => Input.AsSpan().GetXxHashCode();
    }
}
