// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DockerHarness
{
    /// <summary>
    ///   A dictionary-like class with support for evicting the least-recently-used value
    ///   Values are stored in a Heap and a dictionary exists to map keys to indecies
    ///   Note that becuase any access can force reordering, access time is O(log n)
    /// </summary>
    class LruDictionary<TKey, TValue>
    {
        public LruDictionary() { }

        public LruDictionary(IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            foreach (var kv in enumerable)
            {
                Add(kv.Key, kv.Value);
            }
        }

        /// <summary>
        ///   Returns a count of items in the dictionary
        /// </summary>
        public int Count { get => heap.Count; }

        /// <summary>
        ///   Gets or sets a key in the dictionary to a value
        ///   O(log n) time complexity
        /// </summary>
        public TValue this[TKey k] {
            get
            {
                var index = dict[k];
                var result = heap[index].Value;
                Sink(index);
                return result;
            }
            set
            {
                if (dict.TryGetValue(k, out var index))
                {
                    heap[index].Value = value;
                    Sink(index);
                }
                else
                {
                    Add(k, value);
                }
            }
        }

        /// <summary>
        ///   Returns a collection of keys in the dictionary
        ///   O(1) time complexity
        /// </summary>
        public ICollection<TKey> Keys { get => dict.Keys; }

        /// <summary>
        ///   Yields all values in the dictionary
        ///   The access time of each value is adjusted when yielded
        ///   O(nlog n) time complexity
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (TKey k in Keys)
                {
                    yield return this[k];
                }
            }
        }

        /// <summary>
        ///   Adds a new key and value to the dictionary
        ///   Throws an exception if the key already exists in the dictionary
        ///   O(log n) time complexity
        /// </summary>
        public void Add(TKey k, TValue v)
        {
            Push(new Entry(k, v));
        }

        /// <summary>
        ///   Removes a key from the dictionary and returns it's value
        ///   Throws an exception if the key does not exist in the dictionary
        ///   O(log n) time complexity
        /// </summary>
        public TValue Remove(TKey k)
        {
            var index = dict[k];
            return Pop(index).Value;
        }

        /// <summary>
        ///   Removes and returns least-recently-used item in the dictionary
        ///   O(log n) time complexity
        /// </summary>
        public KeyValuePair<TKey, TValue> Evict()
        {
            Entry entry = Pop();
            return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }

        /// <summary>
        ///   Removes all items from the dictionary
        /// </summary>
        public void Clear()
        {
            heap.Clear();
            dict.Clear();
        }

        /// <summary>
        ///   Returns true if the dictionary contains the key
        ///   O(1) time complexity
        /// </summary>
        public bool ContainsKey(TKey k)
        {
            return dict.ContainsKey(k);
        }

#region private
        /// <summary>
        ///   An Entry class which holds the stored Value and fills the Heap
        ///   Tracks the Key, Value, and LastUsed time
        /// </summary>
        private class Entry
        {
            /// <summary>
            ///   The value that is held in this entry
            ///   Any access, get or set, will update the LastUsed time
            /// </summary>
            public TValue Value
            {
                get
                {
                    LastUsed = Stopwatch.GetTimestamp();
                    return val;
                }
                set
                {
                    LastUsed = Stopwatch.GetTimestamp();
                    val = value;
                }
            }
            
            /// <summary>
            ///   The key which is mapped to this entry's Value from the users perspective
            /// </summary>
            public TKey Key { get; private set; }
            
            /// <summary>
            ///   A millisecond percision timestamp of the last access time
            ///   Used in eviction decisions
            /// </summary>
            public long LastUsed { get; private set; }

            private TValue val { get; set; }

            public Entry(TKey k, TValue v)
            {
                Key = k;
                val = v;
                LastUsed = Stopwatch.GetTimestamp();
            }
        }

        /// <summary>
        ///   A comparer to establish ordering in the Heap for Entries
        ///   Only looks at the LastUsed time
        /// </summary>
        private class LruComparer : IComparer<Entry>
        {
            public int Compare(Entry a, Entry b)
            {
                return a.LastUsed.CompareTo(b.LastUsed);
            }
        }

        private IDictionary<TKey, int> dict = new Dictionary<TKey, int>();
        private IList<Entry> heap = new List<Entry>();
        private IComparer<Entry> comparer = new LruComparer();

        private void Push(Entry x)
        {
            dict.Add(x.Key, Count);
            heap.Add(x);
            Bubble(Count - 1);
        }

        private Entry Pop(int i=0)
        {
            Entry result = heap[i];
            Entry replacement = heap[Count - 1];
            heap[i] = replacement;
            dict[replacement.Key] = i;
            heap.RemoveAt(Count - 1);
            dict.Remove(result.Key);

            if (i < Count) Sink(i);
            return result;
        }

        private Entry Peek()
        {
            if (Count == 0) throw new InvalidOperationException("Cache heap is empty");
            return heap[0];
        }

        private void Bubble(int xi)
        {
            Entry x = heap[xi];
            bool modified = false;
            while (xi > 0)
            {
                int pi = (xi - 1) / 2;
                Entry p = heap[pi];
                if (comparer.Compare(p, x) > 0)
                {
                    heap[xi] = p;
                    dict[p.Key] = xi;
                    xi = pi;
                    modified = true;
                }
                else break;
            }

            if (modified)
            {
                heap[xi] = x;
                dict[x.Key] = xi;
            }
        }

        private void Sink(int xi)
        {
            Entry x = heap[xi];
            bool modified = false;
            while (xi * 2 + 1 < Count)
            {
                int ai = xi * 2 + 1;
                int bi = xi * 2 + 2;
                int ci = bi < Count && comparer.Compare(heap[bi], heap[ai]) <= 0 ? bi : ai;
                Entry c = heap[ci];

                if (comparer.Compare(x, c) > 0)
                {
                    heap[xi] = c;
                    dict[c.Key] = xi;
                    xi = ci;
                    modified = true;
                }
                else break;
            }

            if (modified)
            {
                heap[xi] = x;
                dict[x.Key] = xi;
            }
        }
#endregion
    }

    /// <summary>
    ///   A set-like class with support for evicting the least-recently-used value
    ///   This class is built as an LruDictionary with dummy values
    /// </summary>
    class LruSet<T>
    {
        private LruDictionary<T, byte> dict = new LruDictionary<T, byte>();
        private byte dummy = 0xFE;

        /// <summary>
        ///   Returns the number of items in the set
        /// </summary>
        public int Count { get => dict.Count; }
        
        /// <summary>
        ///   Removes all elements from the set
        /// </summary>
        public void Clear() => dict.Clear();

        /// <summary>
        ///   Returned true if the item is in the set
        ///   Does not update the item's access time
        ///   O(1) time complexity
        /// </summary>
        public bool Contains(T item) => dict.ContainsKey(item);

        /// <summary>
        ///   Adds and item to the set or updates the access time
        ///   Returns true if the item was not already in the set
        ///   O(log n) time complexity
        /// </summary>
        public bool Add(T item)
        {
            bool preexisting = dict.ContainsKey(item);
            dict[item] = dummy;
            return !preexisting;
        }

        /// <summary>
        ///   If an item is in the set, sets the access time to now
        ///   Returns true if the item is in the set
        ///   O(log n) time complexity
        /// </summary>
        public bool Refresh(T item)
        {
            if (dict.ContainsKey(item))
            {
                dict[item] = dummy;
                return true;
            }
            return false;
        }

        /// <summary>
        ///   Removes and returns the least-recently-used value
        ///   O(log n) time complexity
        /// </summary>
        public T Evict()
        {
            return dict.Evict().Key;
        }

        /// <summary>
        ///   Removes an item from the set
        ///   Returns true if the item was in the set
        ///   O(log n) time complexity
        /// </summary>
        public bool Remove(T item)
        {
            if (dict.ContainsKey(item))
            {
                dict.Remove(item);
                return true;
            }
            return false;  
        }
    }
}
