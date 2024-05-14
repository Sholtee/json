/********************************************************************************
* Memory.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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

        private static readonly char[] FControlChars = GetControlChars().ToArray();

        private static IEnumerable<char> GetControlChars()
        {
            for (int ctr = 0x00; ctr <= 0xFFFF; ctr++)
            {
                char c = (char)ctr;
                if (char.IsControl(c))
                    yield return c;
            }
        }

        [Benchmark]
        public int FindControl() => Input.AsSpan().IndexOfAny(FControlChars);

        [Benchmark]
        public void FindControlRange()
        {
            _ = Input.AsSpan().IndexOfAnyInRange((char) 0000, (char) 001F);
            _ = Input.AsSpan().IndexOfAnyInRange((char) 007F, (char) 009F);
        }

        [Benchmark]
        public int FindControlShort() => Input.AsSpan().IndexOfAny("\r\n\t");

        [Benchmark]
        public void FindControlUsingIsControl()
        {
            Span<char> src = Input.AsSpan();
            for (int i = 0; i < src.Length; i++)
            {
                _ = char.IsControl(src[i]);
            }
        }
    }
}
