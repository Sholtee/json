/********************************************************************************
* TextWriterExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

namespace Solti.Utils.Json.Internals
{
    internal static class TextWriterExtensions
    {
        public static void Write(this TextWriter self, ReadOnlySpan<char> buffer, int index, int count)
        {
#if NETSTANDARD2_1_OR_GREATER
            self.Write(buffer.Slice(index, count));
#else
            self.Write(buffer.ToArray(), index, count);
#endif
        }

        public static void Write(this TextWriter self, ReadOnlySpan<char> buffer) => self.Write(buffer, 0, buffer.Length);
    }
}
