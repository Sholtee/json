/********************************************************************************
* StreamContextFactory.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

namespace Solti.Utils.Json
{
    using Internals;

    using static Properties.Resources;

    /// <summary>
    /// Creates context for <see cref="Stream"/> and <see cref="MemoryStream"/> [de]serialization.
    /// </summary>
    /// <remarks>Input data should be Base64 encoded string</remarks>
    public class StreamContextFactory : ContextFactory
    {
        /// <inheritdoc/>
        protected override DeserializationContext CreateDeserializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.String,
                ConvertString = ConvertString
            }; 
            
            static bool ConvertString(ReadOnlySpan<char> input, bool _, out object? val)
            {
                byte[] content;
                int length;

#if NETSTANDARD2_1_OR_GREATER
                content = new byte[input.Length];
                if (!Convert.TryFromBase64Chars(input, content.AsSpan(), out length))
#else
                try
                {
                    content = Convert.FromBase64String(input.AsString());
                    length = content.Length;
                }
                catch (FormatException)
#endif
                {
                    val = null;
                    return false;
                }

                //
                // MemoryStream constructor doesn't copy the content:
                // https://github.com/dotnet/runtime/blob/427bc3d9d13e17cb366e700ed894c23cf5c9a5fd/src/libraries/System.Private.CoreLib/src/System/IO/MemoryStream.cs#L90
                //

                val = new MemoryStream(content, 0, length);
                return true;
            }
        }

        /// <inheritdoc/>
        protected override SerializationContext CreateSerializationContextCore(Type type, object? config)
        {
            if (config is not null)
                throw new ArgumentException(INVALID_FORMAT_SPECIFIER, nameof(config));

            return new SerializationContext
            {
                GetTypeOf = static val => val switch
                { 
                    Stream => JsonDataTypes.String,
                    null => JsonDataTypes.Null,
                    _ => JsonDataTypes.Unkown
                },
                ConvertToString = ToString
            };

            static ReadOnlySpan<char> ToString(object? val, ref char[] buffer)
            {
                if (val is null)
                    return Consts.NULL.AsSpan();

                if (val is not Stream stm)
                    throw new ArgumentException(INVALID_INSTANCE, nameof(val));

                //
                // Only MemoryStream has the GetBuffer() method
                //

                if (stm is not MemoryStream memStm || !memStm.TryGetBuffer(out ArraySegment<byte> content))
                {
                    memStm = new MemoryStream();
                    stm.CopyTo(memStm);
                    memStm.TryGetBuffer(out content);
                }

                //
                // base64 len = (bytes / 3) * 4
                //

                int requiredLength = (int) (Math.Ceiling((double) memStm.Length / 3) * 4);
                if (buffer.Length < requiredLength)
                    buffer = new char[requiredLength];

                //
                // GetBuffer() does not copy the underlying data
                //

                int charsWritten = Convert.ToBase64CharArray(content.Array, 0, (int) memStm.Length, buffer, 0);
                return buffer.AsSpan(0, charsWritten);
            }
        }

        /// <inheritdoc/>
        public override bool IsDeserializationSupported(Type type) =>
            //
            // We allways create a MemoryStream instance during deserialization
            //

            type == typeof(Stream) || type == typeof(MemoryStream);

        /// <inheritdoc/>
        public override bool IsSerializationSupported(Type type) => type is not null && typeof(Stream).IsAssignableFrom(type);
    }
}
