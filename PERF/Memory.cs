/********************************************************************************
* Memory.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

using System;
using System.Linq;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Json.Perf
{
    [MemoryDiagnoser]
    public class MemoryTests
    {
        private static readonly char[]
            Input = Enumerable.Repeat(' ', 1024 * 5).ToArray(),
            Dest = new char[Input.Length];

        [Benchmark]
        public void SingleCopy()
        {
            Span<char>
                src = Input.AsSpan(),
                dst = Dest.AsSpan();

            for (int i = 0; i < Input.Length; i++)
            {
                dst[i] = src[i];
            }
        }

        [Benchmark]
        public void SpanCopy()
        {
            Span<char>
                src = Input.AsSpan(),
                dst = Dest.AsSpan();
            src.CopyTo(dst);
        }

        [Benchmark]
        public void Enumerate()
        {
            Span<char> src = Input.AsSpan();
            foreach (char c in src)
            {
                _ = c;
            }
        }

        [Benchmark]
        public void Iterate()
        {
            Span<char> src = Input.AsSpan();
            for (int i = 0; i < src.Length; i++)
            {
                _ = src[i];
            }
        }
    }
}
