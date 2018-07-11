using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int), typeof(int))] // value type
    [GenericTypeArguments(typeof(string), typeof(string))] // reference type
    public class CtorDefaultSize<TKey, TValue>
    {
        [Benchmark]
        public TKey[] Array() => new TKey[4]; // array has no default size, List has = 4 so I decided to use 4 here

        [Benchmark]
        public List<TKey> List() => new List<TKey>();

        [Benchmark]
        public LinkedList<TKey> LinkedList() => new LinkedList<TKey>();

        [Benchmark]
        public HashSet<TKey> HashSet() => new HashSet<TKey>();

        [Benchmark]
        public Dictionary<TKey, TValue> Dictionary() => new Dictionary<TKey, TValue>();

        [Benchmark]
        public Queue<TKey> Queue() => new Queue<TKey>();

        [Benchmark]
        public Stack<TKey> Stack() => new Stack<TKey>();

        [Benchmark]
        public SortedList<TKey, TValue> SortedList() => new SortedList<TKey, TValue>();

        [Benchmark]
        public SortedSet<TKey> SortedSet() => new SortedSet<TKey>();

        [Benchmark]
        public SortedDictionary<TKey, TValue> SortedDictionary() => new SortedDictionary<TKey, TValue>();

        [Benchmark]
        public ConcurrentDictionary<TKey, TValue> ConcurrentDictionary() => new ConcurrentDictionary<TKey, TValue>();

        [Benchmark]
        public ConcurrentQueue<TKey> ConcurrentQueue() => new ConcurrentQueue<TKey>();

        [Benchmark]
        public ConcurrentStack<TKey> ConcurrentStack() => new ConcurrentStack<TKey>();

        [Benchmark]
        public ConcurrentBag<TKey> ConcurrentBag() => new ConcurrentBag<TKey>();

        [Benchmark]
        public ImmutableArray<TKey> ImmutableArray() => Immutable.ImmutableArray.Create<TKey>();

        [Benchmark]
        public ImmutableDictionary<TKey, TValue> ImmutableDictionary() => Immutable.ImmutableDictionary.Create<TKey, TValue>();

        [Benchmark]
        public ImmutableHashSet<TKey> ImmutableHashSet() => Immutable.ImmutableHashSet.Create<TKey>();

        [Benchmark]
        public ImmutableList<TKey> ImmutableList() => Immutable.ImmutableList.Create<TKey>();

        [Benchmark]
        public ImmutableQueue<TKey> ImmutableQueue() => Immutable.ImmutableQueue.Create<TKey>();

        [Benchmark]
        public ImmutableStack<TKey> ImmutableStack() => Immutable.ImmutableStack.Create<TKey>();

        [Benchmark]
        public ImmutableSortedDictionary<TKey, TValue> ImmutableSortedDictionary() => Immutable.ImmutableSortedDictionary.Create<TKey, TValue>();

        [Benchmark]
        public ImmutableSortedSet<TKey> ImmutableSortedSet() => Immutable.ImmutableSortedSet.Create<TKey>();
    }
}