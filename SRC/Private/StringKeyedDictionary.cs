/********************************************************************************
* StringKeyedDictionary.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Json.Internals
{
    /// <summary>
    /// Simple dictionary that supports <see cref="ReadOnlySpan{char}"/> keys
    /// </summary>
    /// <remarks>Logic of this class was mostly taken from <a href="https://github.com/dotnet/corefxlab/blob/archive/src/Microsoft.Experimental.Collections/Microsoft/Collections/Extensions/DictionarySlim.cs">here</a></remarks>
    internal sealed class StringKeyedDictionary<TValue>
    {
        #region Private
        private delegate int GetHashCodeDelegate(ReadOnlySpan<char> input);

        private static GetHashCodeDelegate GetGetHashCodeDelegate()
        {
            //
            // Modern systems must have this delegate built in
            //

            MethodInfo getHashCode = typeof(string).GetMethod
            (
                nameof(GetHashCode),
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(ReadOnlySpan<char>),typeof(StringComparison)],
                null
            );

            if (getHashCode is null)
                return MemoryExtensions.GetXxHashCode;

            ParameterExpression input = Expression.Parameter(typeof(ReadOnlySpan<char>), nameof(input));
            return Expression.Lambda<GetHashCodeDelegate>
            (
                Expression.Call
                (
                    null,
                    getHashCode,
                    input,
                    Expression.Constant(StringComparison.OrdinalIgnoreCase)
                ),
                input
            ).Compile();
        }

        private static readonly GetHashCodeDelegate FGetHashCodeDelegate = GetGetHashCodeDelegate();
        private static readonly Entry[] FInitialEntries = new Entry[1];
        private static readonly int[] FInitialBuckets = new int[1];

        private int FCount;
        private int[] FBuckets = FInitialBuckets;  // the first add will cause a resize so this assignment is safe
        private Entry[] FEntries = FInitialEntries;  // as is this one

        private struct Entry
        {
            public string Key;
            public TValue Value;

            //
            // 0-based index of next entry in chain: -1 means end of chain
            // also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
            // so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
            //

            public int Next;
        }

        private void Resize()
        {
            Debug.Assert(FEntries.Length == FCount || FEntries.Length == 1);
            int
                count = FCount,
                newSize = FEntries.Length * 2;

            Array.Resize(ref FEntries, newSize);
            FBuckets = new int[newSize];  // will contain 0s only

            while (count-- > 0)
            {
                int bucketIndex = FGetHashCodeDelegate(FEntries[count].Key.AsSpan()) & (FBuckets.Length - 1);
                FEntries[count].Next = FBuckets[bucketIndex] - 1;
                FBuckets[bucketIndex] = count + 1;
            }
        }
        #endregion

        public void Add(string key, TValue value)
        {
            if (FCount == FEntries.Length || FEntries.Length == 1)
                Resize();

            int bucketIndex = FGetHashCodeDelegate(key.AsSpan()) & (FBuckets.Length - 1);

            FEntries[FCount].Key = key;
            FEntries[FCount].Value = value;
            FEntries[FCount].Next = FBuckets[bucketIndex] - 1;
            
            FBuckets[bucketIndex] = ++FCount; 
        }

        public bool TryGetValue(ReadOnlySpan<char> key, bool ignoreCase, out TValue value)
        {
            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            for (int i = FBuckets[FGetHashCodeDelegate(key) & (FBuckets.Length - 1)] - 1; (uint) i < FEntries.Length; i = FEntries[i].Next)
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

        public int Count => FCount;
    }
}
