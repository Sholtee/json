/********************************************************************************
* MemoryPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Concurrent;

namespace Solti.Utils.Json.Internals
{
    internal static class MemoryPool<T>
    {
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

        public static void Return(T[] buffer) => FPool.Push(buffer);
    }
}
