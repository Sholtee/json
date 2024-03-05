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

    using static SerializationContext;
    using static Properties.Resources;

    /// <summary>
    /// Represents a low-level, cancellable JSON writer.
    /// </summary>
    /// <remarks>This class is thread safe.</remarks>
    public sealed class JsonWriter(int maxDepth = 64, byte indent = 2)
    {
        #region Private
        private static readonly char[][] FSpaces = GetAllSpaces(256);

        private readonly char[] FValueSeparator = indent > 0 ? [' '] : [];

        private static char[][] GetAllSpaces(int maxLength)  // slow but will be called only once
        {
            char[][] spaces = new char[maxLength][];
            for (int i = 1 /*0 is handled by GetSpaces()*/; i < maxLength; i++)
            {
                spaces[i] = GetSpacesAr(i);
            }
            return spaces;
        }

        private static char[] GetSpacesAr(int len) => [..Environment.NewLine, ..Enumerable.Repeat(' ', len)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                throw new JsonWriterException(MAX_DEPTH_REACHED);
            return currentDepth;
        }

        /// <summary>
        /// Verifies the given <paramref name="delegate"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T VerifyDelegate<T>(T? @delegate) where T : Delegate => @delegate ?? throw new InvalidOperationException(INVALID_CONTEXT);
        #endregion

        #region Internal
        /// <summary>
        /// Writes a JSON string to the underlying buffer representing the given <paramref name="str"/>.
        /// </summary>
        /// <remarks>If the given <paramref name="str"/> is not a <see cref="string"/> this method tries to convert it first.</remarks>
        internal void WriteString(TextWriter dest, object str, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent)
        {
            ReadOnlySpan<char> s = str is string @string
                ? @string.AsSpan()
                : currentContext.ConvertToString(str, default);
#if NETSTANDARD2_1_OR_GREATER
            Span<char> ordBuffer = stackalloc char[4];
#endif
            dest.Write(explicitIndent ?? GetSpaces(currentDepth));
            dest.Write('"');
    
            for (int pos = 0; pos < s.Length;)
            {
                ReadOnlySpan<char> charsLeft = s.Slice(pos);

                for (int i = 0; i < charsLeft.Length; i++)
                {
                    switch (charsLeft[i])
                    {
                        case '"':
                            dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            dest.Write("\\\"");
                            goto nextChunk;
                        case '\r':
                            dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            dest.Write("\\r");
                            goto nextChunk;
                        case '\n':
                            dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            dest.Write("\\n");
                            goto nextChunk;
                        case '\\':
                            dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            dest.Write("\\\\");
                            goto nextChunk;
                        case '\b':
                            dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            dest.Write("\\b");
                            goto nextChunk;
                        case '\t':
                            dest.Write(charsLeft, 0, i);
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

                            dest.Write(charsLeft);
                            pos += i + escape;

                            for (byte j = 0; j < escape; j++, i++)
                            {
                                int ord = charsLeft[i];
                                dest.Write("\\u");
#if NETSTANDARD2_1_OR_GREATER
                                dest.Write(ord.Format("X4", ordBuffer, CultureInfo.InvariantCulture));
#else
                                dest.Write(ord.ToString("X4", CultureInfo.InvariantCulture));
#endif
                            }

                            goto nextChunk;
                    }
                }

                dest.Write(charsLeft, 0, charsLeft.Length);
                pos += charsLeft.Length;

                nextChunk:
                Debug.Assert(pos <= s.Length, "Miscalculated position");
            }

            dest.Write('"');
        }

        /// <summary>
        /// Writes the given value to the underlying buffer.
        /// </summary>
        internal void WriteValue(TextWriter dest, object? val, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent)
        {
            dest.Write(explicitIndent ?? GetSpaces(currentDepth));
            dest.Write
            (
                currentContext.ConvertToString(val, default)
            );
        }

        /// <summary>
        /// Writes the list value to the underlying buffer.
        /// </summary>
        internal void WriteList(TextWriter dest, object val, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent, in CancellationToken cancellation)
        {
            dest.Write(explicitIndent ?? GetSpaces(currentDepth));
            dest.Write('[');

            bool firstItem = true;
            foreach (Entry entry in VerifyDelegate(currentContext.EnumEntries)(val))
            {
                if (entry.Context.Equals(in Default))
                    continue;

                if (firstItem) firstItem = false;
                else dest.Write(',');

                Write(dest, entry.Value, in entry.Context, Deeper(currentDepth), null, in cancellation);
            }

            dest.Write(GetSpaces(currentDepth));
            if (currentDepth is 0 && indent > 0)
                dest.Write(Environment.NewLine);
            dest.Write(']');
        }

        /// <summary>
        /// Writes the given object to the underlying buffer.
        /// </summary>
        internal void WriteObject(TextWriter dest, object val, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent, in CancellationToken cancellation)
        {
            dest.Write(explicitIndent ?? GetSpaces(currentDepth));
            dest.Write('{');

            bool firstItem = true;
            foreach (Entry entry in VerifyDelegate(currentContext.EnumEntries)(val))
            {
                if (entry.Context.Equals(in Default))
                    continue;

                if (firstItem) firstItem = false;
                else dest.Write(',');

                WriteString(dest, entry.Name!, in entry.Context, Deeper(currentDepth), null);
                dest.Write(':');
                Write
                (
                    dest,
                    entry.Value,
                    in entry.Context,
                    Deeper(currentDepth),
                    FValueSeparator,
                    in cancellation
                );
            }

            dest.Write(GetSpaces(currentDepth));
            if (currentDepth is 0 && indent > 0)
                dest.Write(Environment.NewLine);
            dest.Write('}');
        }

        internal void Write(TextWriter dest, object? val, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent, in CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();

            switch (VerifyDelegate(currentContext.GetTypeOf)(val))
            {
                case JsonDataTypes.Number:
                case JsonDataTypes.Boolean:
                case JsonDataTypes.Null:
                    WriteValue(dest, val, in currentContext, currentDepth, explicitIndent);
                    break;
                case JsonDataTypes.String:
                    WriteString(dest, val!, in currentContext, currentDepth, explicitIndent);
                    break;
                case JsonDataTypes.List:
                    WriteList(dest, val!, in currentContext, currentDepth, explicitIndent, in cancellation);
                    break;
                case JsonDataTypes.Object:
                    WriteObject(dest, val!, in currentContext, currentDepth, explicitIndent, in cancellation);
                    break;
                default:
                    throw new JsonWriterException(NOT_SERIALIZABLE);
            }
        }
#endregion

        public void Write(TextWriter dest, object? val, in SerializationContext context, in CancellationToken cancellation) => Write
        (
            dest ?? throw new ArgumentNullException(nameof(dest)),
            val,
            in context,
            0,
            null,
            in cancellation
        );
    }
}
