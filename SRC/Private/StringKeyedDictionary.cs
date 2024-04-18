/********************************************************************************
* StringKeyedDictionary.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Json.Internals
{
    /// <summary>
    /// Simple dictionary that supports <see cref="ReadOnlySpan{char}"/> keys
    /// </summary>
    /// <remarks>The idea was taken from <a href="https://github.com/dotnet/corefxlab/blob/archive/src/Microsoft.Experimental.Collections/Microsoft/Collections/Extensions/DictionarySlim.cs">here</a></remarks>
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

        private const int INITIAL_SIZE = 16;

        private int FCount;
        private int[] FBuckets = new int[INITIAL_SIZE];
        private Entry[] FEntries = new Entry[INITIAL_SIZE];

        private struct Entry
        {
            public string Key;
            public TValue Value;
            public int Next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucket(ReadOnlySpan<char> key) => ref FBuckets[FGetHashCodeDelegate(key) & (FBuckets.Length - 1)];

        private void Resize()
        {
            Debug.Assert(FEntries.Length == FCount);
            int newSize = FEntries.Length * 2;

            Array.Resize(ref FEntries, newSize);
            FBuckets = new int[newSize];  // will contain 0s only

            for(int i = FCount; i > 0; i--)
            {
                ref Entry entry = ref FEntries[i - 1];
                ref int bucket = ref GetBucket(entry.Key.AsSpan());

                entry.Next = bucket - 1;
                bucket = i;
            }
        }
        #endregion

        public void Add(string key, TValue value)
        {
            if (FCount == FEntries.Length || FEntries.Length == 1)
                Resize();

            ref Entry entry = ref FEntries[FCount];
            ref int bucket = ref GetBucket(key.AsSpan());

            entry.Key = key;
            entry.Value = value;
            entry.Next = bucket - 1;
            
            bucket = ++FCount; 
        }

        public bool TryGetValue(ReadOnlySpan<char> key, bool ignoreCase, out TValue value)
        {
            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            for (int i = GetBucket(key) - 1; (uint) i < FEntries.Length;)
            {
                ref Entry entry = ref FEntries[i];
                if (key.Equals(entry.Key.AsSpan(), comparison))
                {
                    value = entry.Value;
                    return true;
                }
                i = entry.Next;
            }

            value = default!;
            return false;
        }

        public int Count => FCount;
    }
}
