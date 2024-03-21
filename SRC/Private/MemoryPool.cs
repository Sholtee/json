/********************************************************************************
* MemoryPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json.Internals
{
    internal static class MemoryPool<T>
    {
        private static readonly int FSizeOfT = Unsafe.SizeOf<T>();

        //
        // Do not use ConcurrentBag here as it significantly slower than ConcurrentStack
        //

        private static readonly ConcurrentStack<T[]> FPool = [];

        /// <summary>
        /// Gets or creates an array in the given <paramref name="length"/>. 
        /// </summary>
        /// <remarks>If <paramref name="length"/> is less than 0 the size of the returned array is unpredictable.</remarks>
        public static T[] Get(int length = -1)
        {
            if (length > -1)
                return new T[length];

            return FPool.TryPop(out T[] result)
                ? result
                : [];
        }

        public static void Return(T[] buffer)
        {
            //
            // Do not retain buffers bigger than 1MB
            //

            if (buffer.Length * FSizeOfT < 1024 * 1024)
                FPool.Push(buffer);
        }
    }
}
