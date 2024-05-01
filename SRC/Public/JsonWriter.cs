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
    public sealed class JsonWriter(int maxDepth = 64, byte indent = 2, int maxChunkSize = 1024)
    {
        #region Private
        private static readonly char[][] FSpaces = GetAllSpaces(256);

        private readonly char[] FValueSeparator = indent > 0 ? [' '] : [];

        private static char[][] GetAllSpaces(int maxLength)  // slow but will be called only once
        {
            char[][] spaces = new char[maxLength][];
            spaces[0] = [];
            for (int i = 1; i < maxLength; i++)
            {
                spaces[i] = GetSpacesAr(i);
            }
            return spaces;
        }

        private static char[] GetSpacesAr(int len) => [..Environment.NewLine, ..Enumerable.Repeat(' ', len)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char[] GetSpaces(int val) => (val *= indent) < FSpaces.Length
            ? FSpaces[val]
            : GetSpacesAr(val);

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
        internal ref struct Session(TextWriter dest, char[] buffer, in CancellationToken cancellation = default)
        {
            public readonly TextWriter Dest = dest;

            public char[] Buffer = buffer;

            public readonly CancellationToken CancellationToken = cancellation;
        }

        /// <summary>
        /// Writes a JSON string to the underlying buffer representing the given <paramref name="str"/>.
        /// </summary>
        /// <remarks>If the given <paramref name="str"/> is not a <see cref="string"/> this method tries to convert it first.</remarks>
        internal void WriteString(ref Session session, object str, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent)
        {
            ReadOnlySpan<char> s = str is string @string
                ? @string.AsSpan()
                : VerifyDelegate(currentContext.ConvertToString)(str, ref session.Buffer);
#if NETSTANDARD2_1_OR_GREATER
            //
            // "session.Buffer" might be already in use so we need a separate buffer
            //

            Span<char> ordBuffer = stackalloc char[4];
#endif
            session.Dest.Write(explicitIndent ?? GetSpaces(currentDepth));
            session.Dest.Write('"');
    
            for (int pos = 0; pos < s.Length;)
            {
                ReadOnlySpan<char> charsLeft = s.Slice(pos);

                int len = Math.Min(charsLeft.Length, maxChunkSize);

                for (int i = 0; i < len; i++)
                {
                    switch (charsLeft[i])
                    {
                        case '"':
                            session.Dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            session.Dest.Write("\\\"");
                            goto nextChunk;
                        case '\r':
                            session.Dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            session.Dest.Write("\\r");
                            goto nextChunk;
                        case '\n':
                            session.Dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            session.Dest.Write("\\n");
                            goto nextChunk;
                        case '\\':
                            session.Dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            session.Dest.Write("\\\\");
                            goto nextChunk;
                        case '\b':
                            session.Dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            session.Dest.Write("\\b");
                            goto nextChunk;
                        case '\t':
                            session.Dest.Write(charsLeft, 0, i);
                            pos += i + 1;
                            session.Dest.Write("\\t");
                            goto nextChunk;
                        default:
                            byte escape;

                            if (IsControl(charsLeft[i]))
                                escape = 1;
                            else if (i < charsLeft.Length - 1 /*override "len"*/ && IsSurrogatePair(charsLeft[i], charsLeft[i + 1]))
                                escape = 2;
                            else
                                break;

                            session.Dest.Write(charsLeft, 0, i);
                            pos += i + escape;

                            for (byte j = 0; j < escape; j++, i++)
                            {
                                int ord = charsLeft[i];
                                session.Dest.Write("\\u");
#if NETSTANDARD2_1_OR_GREATER
                                session.Dest.Write(ord.Format("X4", ordBuffer, CultureInfo.InvariantCulture));
#else
                                session.Dest.Write(ord.ToString("X4", CultureInfo.InvariantCulture));
#endif
                            }

                            goto nextChunk;
                    }
                }

                session.Dest.Write(charsLeft, 0, len);
                pos += len;

                nextChunk:
                Debug.Assert(pos <= s.Length, "Miscalculated position");
            }

            session.Dest.Write('"');
        }

        /// <summary>
        /// Writes the given value to the underlying buffer.
        /// </summary>
        internal void WriteValue(ref Session session, object? val, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent)
        {
            session.Dest.Write(explicitIndent ?? GetSpaces(currentDepth));
            session.Dest.Write
            (
                VerifyDelegate(currentContext.ConvertToString)(val, ref session.Buffer)
            );
        }

        /// <summary>
        /// Writes the list value to the underlying buffer.
        /// </summary>
        internal void WriteList(ref Session session, object val, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent)
        {
            session.Dest.Write(explicitIndent ?? GetSpaces(currentDepth));
            session.Dest.Write('[');

            bool firstItem = true;
            foreach (Entry entry in VerifyDelegate(currentContext.EnumEntries)(val))
            {
                if (firstItem) firstItem = false;
                else session.Dest.Write(',');

                Write(ref session, entry.Value, in entry.Context, Deeper(currentDepth), null);
            }

            session.Dest.Write(GetSpaces(currentDepth));
            if (currentDepth is 0 && indent > 0)
                session.Dest.Write(Environment.NewLine);
            session.Dest.Write(']');
        }

        /// <summary>
        /// Writes the given object to the underlying buffer.
        /// </summary>
        internal void WriteObject(ref Session session, object val, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent)
        {
            session.Dest.Write(explicitIndent ?? GetSpaces(currentDepth));
            session.Dest.Write('{');

            bool firstItem = true;
            foreach (Entry entry in VerifyDelegate(currentContext.EnumEntries)(val))
            {
                if (firstItem) firstItem = false;
                else session.Dest.Write(',');

                WriteString(ref session, entry.Name!, in entry.Context, Deeper(currentDepth), null);
                session.Dest.Write(':');
                Write(ref session, entry.Value, in entry.Context, Deeper(currentDepth), FValueSeparator);
            }

            session.Dest.Write(GetSpaces(currentDepth));
            if (currentDepth is 0 && indent > 0)
                session.Dest.Write(Environment.NewLine);
            session.Dest.Write('}');
        }

        internal void Write(ref Session session, object? val, in SerializationContext currentContext, int currentDepth, char[]? explicitIndent)
        {
            session.CancellationToken.ThrowIfCancellationRequested();

            switch (VerifyDelegate(currentContext.GetTypeOf)(val))
            {
                case JsonDataTypes.Number:
                case JsonDataTypes.Boolean:
                case JsonDataTypes.Null:
                    WriteValue(ref session, val, in currentContext, currentDepth, explicitIndent);
                    break;
                case JsonDataTypes.String:
                    WriteString(ref session, val!, in currentContext, currentDepth, explicitIndent);
                    break;
                case JsonDataTypes.List:
                    WriteList(ref session, val!, in currentContext, currentDepth, explicitIndent);
                    break;
                case JsonDataTypes.Object:
                    WriteObject(ref session, val!, in currentContext, currentDepth, explicitIndent);
                    break;
                default:
                    throw new JsonWriterException(NOT_SERIALIZABLE);
            }
        }
        #endregion

        /// <summary>
        /// Serializes the given <paramref name="value"/>.
        /// </summary>
        /// <param name="dest">The destination that holds the serialized content.</param>
        /// <param name="closeDest">If set to true, the system disposes the <paramref name="dest"/> before this function would return.</param>
        /// <param name="value">Value to be serialized</param>
        /// <param name="context">Context that instructs the system how to serialize the input.</param>
        /// <param name="cancellation"><see cref="CancellationToken"/></param>
        public void Write(TextWriter dest, bool closeDest, object? value, in SerializationContext context, in CancellationToken cancellation)
        {
            if (dest is null)
                throw new ArgumentNullException(nameof(dest));

            Session session = new(dest, MemoryPool<char>.Get(), in cancellation);
            try
            {
                Write(ref session, value, in context, 0, null);
            }
            finally
            {
                MemoryPool<char>.Return(session.Buffer);
                if (closeDest)
                    dest.Dispose();
            }
        }
    }
}
