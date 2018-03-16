using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DockerHarness
{
    class LruDictionary<TKey, TValue>
    {
        private class Entry
        {
            public TValue Value {
                get {
                    LastUsed = Stopwatch.GetTimestamp();
                    return val;
                }
                set {
                    LastUsed = Stopwatch.GetTimestamp();
                    val = value;
                }
            }
            public TKey Key { get; private set; }
            public long LastUsed { get; private set; }

            private TValue val { get; set; }

            public Entry(TKey k, TValue v)
            {
                Key = k;
                val = v;
                LastUsed = Stopwatch.GetTimestamp();
            }
        }

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

        public LruDictionary() { }

        public LruDictionary(IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            foreach (var kv in enumerable)
            {
                Add(kv.Key, kv.Value);
            }
        }

        public int Count { get => heap.Count; }

        public TValue this[TKey k] {
            get {
                var index = dict[k];
                var result = heap[index].Value;
                Sink(index);
                return result;
            }
            set {
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

        public ICollection<TKey> Keys { get => dict.Keys; }
        
        public IEnumerable<TValue> Values {
            get {
                foreach (TKey k in Keys)
                {
                    yield return this[k];
                }
            }
        }

        public void Add(TKey k, TValue v)
        {
            Push(new Entry(k, v));
        }
        
        public TValue Remove(TKey k)
        {
            var index = dict[k];
            return Pop(index).Value;
        }

        public KeyValuePair<TKey, TValue> Evict()
        {
            Entry entry = Pop();
            return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }

        public void Clear()
        {
            heap.Clear();
            dict.Clear();
        }

        public bool ContainsKey(TKey k)
        {
            return dict.ContainsKey(k);
        }

        private void Push(Entry x)
        {
            dict.Add(x.Key, Count - 1);
            heap.Add(x);
            Bubble(Count - 1);
        }

        private Entry Pop(int i=0)
        {
            Entry result = heap[i];
            heap[i] = heap[Count - 1];
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

        public void Sink(int xi)
        {
            Entry x = heap[xi];
            bool modified = false;
            while (xi * 2 + 1 < Count)
            {
                int ai = xi * 2 + 1;
                int bi = xi * 2 + 2;
                int ci = bi < Count && comparer.Compare(heap[bi], heap[ai]) > 0 ? ai : bi;
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
    }

    class LruSet<T>
    {
        private LruDictionary<T, byte> dict = new LruDictionary<T, byte>(); 
        private byte dummy = 0xFE;
        
        public int Count { get => dict.Count; }
        public void Clear() => dict.Clear();
        public bool Contains(T item) => dict.ContainsKey(item);
        
        public bool Add(T item) 
        {
            bool preexisting = dict.ContainsKey(item);
            dict[item] = dummy;
            return !preexisting;
        }
        
        public T Evict()
        {
            return dict.Evict().Key;
        }
        
        public void Remove(T item)
        {
            dict.Remove(item);
        }
    }
}
