/********************************************************************************
* MemoryPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;

namespace Solti.Utils.Json.Internals
{
    internal static class MemoryPool<T>
    {
        //
        // Do not use ConcurrentBag here as it significantly slower than ConcurrentStack
        //

        private static readonly ConcurrentStack<T[]> FPool = [];

        public static T[] Get(int length)
        {
            if (FPool.TryPop(out T[] result))
            {
                if (result.Length < length)
                    Array.Resize(ref result, length);
            }
            else result = new T[length];

            return result;
        }

        public static void Return(T[] buffer) => FPool.Push(buffer);
    }
}
