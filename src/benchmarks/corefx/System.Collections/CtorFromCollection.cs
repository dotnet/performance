using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int), typeof(int))] // value type
    [GenericTypeArguments(typeof(string), typeof(string))] // reference type
    public class CtorFromCollection<TKey, TValue>
    {
        private ICollection<TKey> _collection;
        private IDictionary<TKey, TValue> _dictionary;
        
        [Params(100)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _collection = UniqueValuesGenerator.GenerateArray<TKey>(Size);
            _dictionary = UniqueValuesGenerator.GenerateDictionary<TKey, TValue>(Size);
        }
        
        [Benchmark]
        public List<TKey> List() => new List<TKey>(_collection);

        [Benchmark]
        public LinkedList<TKey> LinkedList() => new LinkedList<TKey>(_collection);

        [Benchmark]
        public HashSet<TKey> HashSet() => new HashSet<TKey>(_collection);

        [Benchmark]
        public Dictionary<TKey, TValue> Dictionary() => new Dictionary<TKey, TValue>(_dictionary);

        [Benchmark]
        public Queue<TKey> Queue() => new Queue<TKey>(_collection);

        [Benchmark]
        public Stack<TKey> Stack() => new Stack<TKey>(_collection);

        [Benchmark]
        public SortedList<TKey, TValue> SortedList() => new SortedList<TKey, TValue>(_dictionary);

        [Benchmark]
        public SortedSet<TKey> SortedSet() => new SortedSet<TKey>(_collection);

        [Benchmark]
        public SortedDictionary<TKey, TValue> SortedDictionary() => new SortedDictionary<TKey, TValue>(_dictionary);

        [Benchmark]
        public ConcurrentDictionary<TKey, TValue> ConcurrentDictionary() => new ConcurrentDictionary<TKey, TValue>(_dictionary);

        [Benchmark]
        public ConcurrentQueue<TKey> ConcurrentQueue() => new ConcurrentQueue<TKey>(_collection);

        [Benchmark]
        public ConcurrentStack<TKey> ConcurrentStack() => new ConcurrentStack<TKey>(_collection);

        [Benchmark]
        public ConcurrentBag<TKey> ConcurrentBag() => new ConcurrentBag<TKey>(_collection);

        [Benchmark]
        public ImmutableArray<TKey> ImmutableArray() => Immutable.ImmutableArray.CreateRange<TKey>(_collection);

        [Benchmark]
        public ImmutableDictionary<TKey, TValue> ImmutableDictionary() => Immutable.ImmutableDictionary.CreateRange<TKey, TValue>(_dictionary);

        [Benchmark]
        public ImmutableHashSet<TKey> ImmutableHashSet() => Immutable.ImmutableHashSet.CreateRange<TKey>(_collection);

        [Benchmark]
        public ImmutableList<TKey> ImmutableList() => Immutable.ImmutableList.CreateRange<TKey>(_collection);

        [Benchmark]
        public ImmutableQueue<TKey> ImmutableQueue() => Immutable.ImmutableQueue.CreateRange<TKey>(_collection);

        [Benchmark]
        public ImmutableStack<TKey> ImmutableStack() => Immutable.ImmutableStack.CreateRange<TKey>(_collection);

        [Benchmark]
        public ImmutableSortedDictionary<TKey, TValue> ImmutableSortedDictionary() => Immutable.ImmutableSortedDictionary.CreateRange<TKey, TValue>(_dictionary);

        [Benchmark]
        public ImmutableSortedSet<TKey> ImmutableSortedSet() => Immutable.ImmutableSortedSet.CreateRange<TKey>(_collection);
    }
}