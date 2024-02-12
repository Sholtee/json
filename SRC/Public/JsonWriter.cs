/********************************************************************************
* JsonWriter.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.Char;

namespace Solti.Utils.Json
{
    using Internals;

    using static Properties.Resources;

    public sealed class JsonWriter(TextWriter dest, int maxDepth = 64, byte indent = 2) : IDisposable
    {
        #region Private
        private static readonly char[][] FSpaces = GetSpacesDict(256);

        private static char[][] GetSpacesDict(int maxLength)  // slow but will be called only once
        {
            char[][] spaces = new char[maxLength][];
            for (int i = 0; i < maxLength; i++)
            {
                spaces[i] = GetSpacesAr(i);
            }
            return spaces;
        }

        private static char[] GetSpacesAr(int len) => [..Environment.NewLine, ..Enumerable.Repeat(' ', len)];

        private char[] GetSpaces(int currentDepth)
        {
            int required = currentDepth * indent;
            if (required is 0)
                return [];

            return required < FSpaces.Length
                ? GetSpacesAr(required)
                : FSpaces[required];
        }

        /// <summary>
        /// Validates then increases the <paramref name="currentDepth"/>. Throws an <see cref="InvalidOperationException"/> if the current depth reached the <see cref="maxDepth"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Deeper(int currentDepth)
        {
            if (++currentDepth > maxDepth)
                throw new InvalidOperationException(MAX_DEPTH_REACHED);
            return currentDepth;
        }


        /// <summary>
        /// Verifies the given <paramref name="delegate"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T VerifyDelegate<T>(T? @delegate) where T : Delegate => @delegate ?? throw new InvalidOperationException(INVALID_CONTEXT);

        void IDisposable.Dispose()
        {
            if (dest is not null)
            {
                dest.Dispose();
                dest = null!;
            }
        }
        #endregion

        /// <summary>
        /// Writes a JSON string to the underlying buffer representing the given <paramref name="str"/>.
        /// </summary>
        /// <remarks>If the given <paramref name="str"/> is not a <see cref="string"/> this method tries to convert it first.</remarks>
        internal void WriteString(object str, SerializationContext currentContext, int currentDepth)
        {
            if (str is not string s)
                s = currentContext.ConvertToString(str);

            dest.Write(GetSpaces(currentDepth));
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
        /// Writes the given value to the underlying buffer.
        /// </summary>
        internal void WriteValue(object? val, SerializationContext currentContext, int currentDepth)
        {
            dest.Write(GetSpaces(currentDepth));
            dest.Write
            (
                currentContext.ConvertToString(val)
            );
        }

        internal void WriteList(object val, SerializationContext currentContext, int currentDepth, in CancellationToken cancellation)
        {
            char[] spaces = GetSpaces(currentDepth);

            dest.Write(spaces);
            dest.Write('[');

            bool firstItem = true;
            foreach ((SerializationContext? childContext, object? item) in VerifyDelegate(currentContext.EnumValues)(val))
            {
                if (firstItem) firstItem = false;
                else dest.Write(",");

                if (childContext is not null)
                    Write(item, childContext, Deeper(currentDepth), cancellation);
            }

            dest.Write(spaces);
            dest.Write(']');
        }

        internal void Write(object? val, SerializationContext currentContext, int currentDepth, in CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            switch (VerifyDelegate(currentContext.GetTypeOf)(val))
            {
                case JsonDataTypes.Number:
                case JsonDataTypes.Boolean:
                case JsonDataTypes.Null:
                    WriteValue(val, currentContext, currentDepth);
                    break;
                case JsonDataTypes.String:
                    WriteString(val!, currentContext, currentDepth);
                    break;
                case JsonDataTypes.List:
                    WriteList(val!, currentContext, currentDepth, cancellation);
                    break;
                default:
                    throw new NotSupportedException(NOT_SERIALIZABLE);
            }             
        }
    }
}
