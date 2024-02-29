/********************************************************************************
* JsonException.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Json
{
    /// <summary>
    /// Base class of JSON related exceptions
    /// </summary>
    public class JsonException(string message) : Exception(message) { }

    /// <summary>
    /// Exception thrown by the <see cref="JsonParser"/>.
    /// </summary>
    public class JsonParserException(string message, int column, int row) : JsonException(message)
    {
        /// <summary>
        /// The actual column on which the <see cref="JsonParser"/> is positioned
        /// </summary>
        public int Column { get; } = column;

        /// <summary>
        /// The actual row on which the <see cref="JsonParser"/> is positioned
        /// </summary>
        public int Row { get; } = row;
    }

    /// <summary>
    /// Exception thrown by the <see cref="JsonWriter"/>.
    /// </summary>
    public class JsonWriterException(string message) : JsonException(message) { }
}
