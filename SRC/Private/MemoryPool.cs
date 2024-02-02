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
