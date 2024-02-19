/********************************************************************************
* StringKeyedDictionary.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

namespace Solti.Utils.Json.Internals
{
    /// <summary>
    /// Simple dictionary that supports <see cref="ReadOnlySpan{char}"/> keys
    /// </summary>
    /// <remarks>Logic of this class was mostly taken from <a href="https://github.com/dotnet/corefxlab/blob/archive/src/Microsoft.Experimental.Collections/Microsoft/Collections/Extensions/DictionarySlim.cs">here</a></remarks>
    internal sealed class StringKeyedDictionary<TValue>
    {
        private static readonly Entry[] FInitialEntries = new Entry[1];
        private static readonly int[] FInitialBuckets = new int[1];

        private int FCount;
        private int FFreeList = -1;  // -1 means empty
        private int[] FBuckets = FInitialBuckets;  // the first add will cause a resize so this assignment is safe
        private Entry[] FEntries = FInitialEntries;  // as is this one

        private struct Entry
        {
            public string Key;
            public TValue Value;
            // 0-based index of next entry in chain: -1 means end of chain
            // also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
            // so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
            public int Next;
        }

        private void Resize()
        {
            Debug.Assert(FEntries.Length == FCount || FEntries.Length == 1);
            int count = FCount;
            int newSize = FEntries.Length * 2;

            Array.Resize(ref FEntries, newSize);
            FBuckets = new int[newSize];  // will contain 0s only

            while (count-- > 0)
            {
                int bucketIndex = FEntries[count].Key.AsSpan().GetXxHashCode() & (FBuckets.Length - 1);
                FEntries[count].Next = FBuckets[bucketIndex] - 1;
                FBuckets[bucketIndex] = count + 1;
            }
        }

        public void Add(string key, TValue value)
        {
            int bucketIndex = key.AsSpan().GetXxHashCode() & (FBuckets.Length - 1);

            int entryIndex;
            if (FFreeList != -1)
            {
                entryIndex = FFreeList;
                FFreeList = -3 - FEntries[FFreeList].Next;
            }
            else
            {
                if (FCount == FEntries.Length || FEntries.Length == 1)
                {
                    Resize();
                    bucketIndex = key.AsSpan().GetXxHashCode() & (FBuckets.Length - 1);
                    // entry indexes were not changed by Resize
                }
                entryIndex = FCount;
            }

            FEntries[entryIndex].Key = key;
            FEntries[entryIndex].Value = value;
            FEntries[entryIndex].Next = FBuckets[bucketIndex] - 1;
            
            FBuckets[bucketIndex] = entryIndex + 1;

            FCount++;       
        }

        public bool TryGetValue(ReadOnlySpan<char> key, bool ignoreCase, out TValue value)
        {
            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            for (int i = FBuckets[key.GetXxHashCode() & (FBuckets.Length - 1)] - 1; (uint) i < FEntries.Length; i = FEntries[i].Next)
            {
                if (key.Equals(FEntries[i].Key.AsSpan(), comparison))
                {
                    value = FEntries[i].Value;
                    return true;
                }
            }

            value = default!;
            return false;
        }
    }
}
