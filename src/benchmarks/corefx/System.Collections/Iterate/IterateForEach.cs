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
    public class IterateForEach<TKey, TValue>
    {
        [Params(100)]
        public int Size;

        private TValue[] _array;
        private List<TValue> _list;
        private LinkedList<TValue> _linkedlist;
        private HashSet<TValue> _hashset;
        private Dictionary<TKey, TValue> _dictionary;
        private Queue<TValue> _queue;
        private Stack<TValue> _stack;
        private SortedList<TKey, TValue> _sortedlist;
        private SortedSet<TValue> _sortedset;
        private SortedDictionary<TKey, TValue> _sorteddictionary;
        private ConcurrentDictionary<TKey, TValue> _concurrentdictionary;
        private ConcurrentQueue<TValue> _concurrentqueue;
        private ConcurrentStack<TValue> _concurrentstack;
        private ConcurrentBag<TValue> _concurrentbag;
        private ImmutableArray<TValue> _immutablearray;
        private ImmutableDictionary<TKey, TValue> _immutabledictionary;
        private ImmutableHashSet<TValue> _immutablehashset;
        private ImmutableList<TValue> _immutablelist;
        private ImmutableQueue<TValue> _immutablequeue;
        private ImmutableStack<TValue> _immutablestack;
        private ImmutableSortedDictionary<TKey, TValue> _immutablesorteddictionary;
        private ImmutableSortedSet<TValue> _immutablesortedset;

        [GlobalSetup(Target = nameof(Array))]
        public void SetupArray() => _array = UniqueValuesGenerator.GenerateArray<TValue>(Size);

        [Benchmark]
        public TValue Array()
        {
            TValue result = default;
            var local = _array;
            foreach (var item in local)
                result = item;
            return result;
        }
        
        [GlobalSetup(Target = nameof(List))]
        public void SetupList() => _list = new List<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue List()
        {
            TValue result = default;
            var local = _list;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(LinkedList))]
        public void SetupLinkedList() => _linkedlist = new LinkedList<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue LinkedList()
        {
            TValue result = default;
            var local = _linkedlist;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(HashSet))]
        public void SetupHashSet() => _hashset = new HashSet<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue HashSet()
        {
            TValue result = default;
            var local = _hashset;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Dictionary))]
        public void SetupDictionary() => _dictionary = new Dictionary<TKey, TValue>(UniqueValuesGenerator.GenerateDictionary<TKey, TValue>(Size));

        [Benchmark]
        public TValue Dictionary()
        {
            TValue result = default;
            var local = _dictionary;
            foreach (var item in local)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(Queue))]
        public void SetupQueue() => _queue = new Queue<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue Queue()
        {
            TValue result = default;
            var local = _queue;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Stack))]
        public void SetupStack() => _stack = new Stack<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue Stack()
        {
            TValue result = default;
            var local = _stack;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(SortedList))]
        public void SetupSortedList() => _sortedlist = new SortedList<TKey, TValue>(UniqueValuesGenerator.GenerateDictionary<TKey, TValue>(Size));

        [Benchmark]
        public TValue SortedList()
        {
            TValue result = default;
            var local = _sortedlist;
            foreach (var item in local)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(SortedSet))]
        public void SetupSortedSet() => _sortedset = new SortedSet<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue SortedSet()
        {
            TValue result = default;
            var local = _sortedset;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(SortedDictionary))]
        public void SetupSortedDictionary() => _sorteddictionary = new SortedDictionary<TKey, TValue>(UniqueValuesGenerator.GenerateDictionary<TKey, TValue>(Size));

        [Benchmark]
        public TValue SortedDictionary()
        {
            TValue result = default;
            var local = _sorteddictionary;
            foreach (var item in local)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(ConcurrentDictionary))]
        public void SetupConcurrentDictionary() => _concurrentdictionary = new ConcurrentDictionary<TKey, TValue>(UniqueValuesGenerator.GenerateDictionary<TKey, TValue>(Size));

        [Benchmark]
        public TValue ConcurrentDictionary()
        {
            TValue result = default;
            var local = _concurrentdictionary;
            foreach (var item in local)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(ConcurrentQueue))]
        public void SetupConcurrentQueue() => _concurrentqueue = new ConcurrentQueue<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ConcurrentQueue()
        {
            TValue result = default;
            var local = _concurrentqueue;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ConcurrentStack))]
        public void SetupConcurrentStack() => _concurrentstack = new ConcurrentStack<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ConcurrentStack()
        {
            TValue result = default;
            var local = _concurrentstack;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ConcurrentBag))]
        public void SetupConcurrentBag() => _concurrentbag = new ConcurrentBag<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ConcurrentBag()
        {
            TValue result = default;
            var local = _concurrentbag;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableArray))]
        public void SetupImmutableArray() => _immutablearray = Immutable.ImmutableArray.CreateRange<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ImmutableArray()
        {
            TValue result = default;
            var local = _immutablearray;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableDictionary))]
        public void SetupImmutableDictionary() => _immutabledictionary = Immutable.ImmutableDictionary.CreateRange<TKey, TValue>(UniqueValuesGenerator.GenerateDictionary<TKey, TValue>(Size));

        [Benchmark]
        public TValue ImmutableDictionary()
        {
            TValue result = default;
            var local = _immutabledictionary;
            foreach (var item in local)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableHashSet))]
        public void SetupImmutableHashSet() => _immutablehashset = Immutable.ImmutableHashSet.CreateRange<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ImmutableHashSet()
        {
            TValue result = default;
            var local = _immutablehashset;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableList))]
        public void SetupImmutableList() => _immutablelist = Immutable.ImmutableList.CreateRange<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ImmutableList()
        {
            TValue result = default;
            var local = _immutablelist;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableQueue))]
        public void SetupImmutableQueue() => _immutablequeue = Immutable.ImmutableQueue.CreateRange<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ImmutableQueue()
        {
            TValue result = default;
            var local = _immutablequeue;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableStack))]
        public void SetupImmutableStack() => _immutablestack = Immutable.ImmutableStack.CreateRange<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ImmutableStack()
        {
            TValue result = default;
            var local = _immutablestack;
            foreach (var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableSortedDictionary))]
        public void SetupImmutableSortedDictionary() => _immutablesorteddictionary = Immutable.ImmutableSortedDictionary.CreateRange<TKey, TValue>(UniqueValuesGenerator.GenerateDictionary<TKey, TValue>(Size));

        [Benchmark]
        public TValue ImmutableSortedDictionary()
        {
            TValue result = default;
            var local = _immutablesorteddictionary;
            foreach (var item in local)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableSortedSet))]
        public void SetupImmutableSortedSet() => _immutablesortedset = Immutable.ImmutableSortedSet.CreateRange<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ImmutableSortedSet()
        {
            TValue result = default;
            var local = _immutablesortedset;
            foreach (var item in local)
                result = item;
            return result;
        }
    }
}