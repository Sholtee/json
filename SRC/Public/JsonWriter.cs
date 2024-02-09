/********************************************************************************
* JsonWriter.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using static System.Char;

namespace Solti.Utils.Json
{
    using Internals;

    public sealed class JsonWriter(TextWriter dest) : IDisposable
    {
        #region Private
        private static readonly IReadOnlyDictionary<int, char[]> FSpaces = GetSpacesDict(256);

        private static IReadOnlyDictionary<int, char[]> GetSpacesDict(int maxLength)  // slow but will be called only once
        {
            Dictionary<int, char[]> spaces = new(maxLength);
            for (int i = 0; i < maxLength; i++)
            {
                spaces[i] = GetSpacesAr(i);
            }
            return spaces;
        }

        private static char[] GetSpacesAr(int len) => Enumerable.Repeat(' ', len).ToArray();

        private static char[] GetSpaces(int currentDepth)
        {
            int required = currentDepth * 2; // TODO: "2" from config

            if (FSpaces.TryGetValue(required, out char[] spaces))
                return spaces;

            return GetSpacesAr(required);
        }
        #endregion

        /// <summary>
        /// Writes the gven number to the underlying buffer.
        /// </summary>
        internal void WriteNumber(object num, SerializationContext currentContext)
        {
            if (currentContext.GetTypeOf(num) is not JsonDataTypes.Number)
                throw new InvalidCastException();

            //dest.Write(GetSpaces(currentDepth));  // TODO: move to Write(obj)          
            dest.Write
            (
                currentContext.ConvertToString(num)
            );
        }

        /// <summary>
        /// Writes a JSON string to the underlying buffer representing the given <paramref name="str"/>.
        /// </summary>
        /// <remarks>If the given <paramref name="str"/> is not a <see cref="string"/> this method tries to convert it first.</remarks>
        internal void WriteString(object str, SerializationContext currentContext)
        {
            if (currentContext.GetTypeOf(str) is not JsonDataTypes.String)
                throw new InvalidCastException();

            if (str is not string s)
                s = currentContext.ConvertToString(str);

            dest.Write('"');
    
            for (int pos = 0; pos < s.Length;)
            {
                ReadOnlySpan<char> charsLeft = s.AsSpan(pos);

                for (int i = 0; i < charsLeft.Length; i++)
                {
                    switch (charsLeft[i])
                    {
                        case '"':
                            Flush(charsLeft.Slice(0, i), dest);
                            pos += i + 1;
                            dest.Write("\\\"");
                            goto nextChunk;
                        case '\r':
                            Flush(charsLeft.Slice(0, i), dest);
                            pos += i + 1;
                            dest.Write("\\r");
                            goto nextChunk;
                        case '\n':
                            Flush(charsLeft.Slice(0, i), dest);
                            pos += i + 1;
                            dest.Write("\\n");
                            goto nextChunk;
                        case '\\':
                            Flush(charsLeft.Slice(0, i), dest);
                            pos += i + 1;
                            dest.Write("\\\\");
                            goto nextChunk;
                        case '\b':
                            Flush(charsLeft.Slice(0, i), dest);
                            pos += i + 1;
                            dest.Write("\\b");
                            goto nextChunk;
                        case '\t':
                            Flush(charsLeft.Slice(0, i), dest);
                            pos += i + 1;
                            dest.Write("\\t");
                            goto nextChunk;
                        default:
                            byte escape;

                            if (IsControl(charsLeft[i]))
                                escape = 1;
                            else if (i < charsLeft.Length - 1 && IsSurrogatePair(charsLeft[i], charsLeft[i + 1]))
                                escape = 2;
                            else
                                break;

                            Flush(charsLeft.Slice(0, i), dest);
                            pos += i + escape;

                            for (byte j = 0; j < escape; j++, i++)
                            {
                                int ord = charsLeft[i];
                                dest.Write("\\u");
                                dest.Write(ord.ToString("X4", CultureInfo.InvariantCulture));
                            }

                            goto nextChunk;
                    }
                }

                Flush(charsLeft, dest);
                pos += charsLeft.Length;

                nextChunk:
                Debug.Assert(pos <= s.Length, "Miscalculated position");
            }

            dest.Write('"');

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Flush(ReadOnlySpan<char> span, TextWriter dest)
            {
                if (span.Length > 0) dest.Write
                (
                    span
#if !NETSTANDARD2_1_OR_GREATER
                        .AsString()
#endif
                );
            }
        }

        /// <summary>
        /// Writes the given literal to the underlying buffer.
        /// </summary>
        internal void WriteLiteral(object? val, SerializationContext currentContext)
        {
            switch (val)
            {
                case null:
                    dest.Write("null");
                    break;
                case bool b:
                    dest.Write(b ? "true" : "false");
                    break;
                default:
                    throw new InvalidCastException();
            }
        }

        void IDisposable.Dispose()
        {
            if (dest is not null)
            {
                dest.Dispose();
                dest = null!;
            }
        }
    }
}
