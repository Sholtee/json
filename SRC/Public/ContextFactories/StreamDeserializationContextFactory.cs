/********************************************************************************
* StreamDeserializationContextFactory.cs                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

namespace Solti.Utils.Json
{
    using Internals;
    using Properties;

    /// <summary>
    /// Creates context for <see cref="Stream"/> or <see cref="MemoryStream"/> deserialization.
    /// </summary>
    /// <remarks>Input data should be Base64 encoded string</remarks>
    public class StreamDeserializationContextFactory : DeserializationContextFactory
    {
        /// <inheritdoc/>
        public override DeserializationContext CreateContext(Type type, object? config = null)
        {
            EnsureValidType(type);
            if (config is not null)
                throw new ArgumentException(Resources.INVALID_FORMAT_SPECIFIER, nameof(config));

            return new DeserializationContext
            {
                SupportedTypes = JsonDataTypes.String,
                ConvertString = static (ReadOnlySpan<char> input, bool _, out object? val) =>
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
            };
        }

        /// <inheritdoc/>
        public override bool IsSupported(Type type) => type == typeof(Stream) || type == typeof(MemoryStream);
    }
}
