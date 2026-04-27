// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class IterateForEach<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private T[] _array;
        private IEnumerable<T> _ienumerable;
        private List<T> _list;
        private LinkedList<T> _linkedlist;
        private HashSet<T> _hashset;
        private Dictionary<T, T> _dictionary;
        private Queue<T> _queue;
        private Stack<T> _stack;
        private SortedList<T, T> _sortedlist;
        private SortedSet<T> _sortedset;
        private SortedDictionary<T, T> _sorteddictionary;
        private ConcurrentDictionary<T, T> _concurrentdictionary;
        private ConcurrentQueue<T> _concurrentqueue;
        private ConcurrentStack<T> _concurrentstack;
        private ConcurrentBag<T> _concurrentbag;
        private ImmutableArray<T> _immutablearray;
        private ImmutableDictionary<T, T> _immutabledictionary;
        private ImmutableHashSet<T> _immutablehashset;
        private ImmutableList<T> _immutablelist;
        private ImmutableQueue<T> _immutablequeue;
        private ImmutableStack<T> _immutablestack;
        private ImmutableSortedDictionary<T, T> _immutablesorteddictionary;
        private ImmutableSortedSet<T> _immutablesortedset;
        private FrozenDictionary<T, T> _frozenDictionary;
        private FrozenSet<T> _frozenset;
#if NET9_0_OR_GREATER
        private OrderedDictionary<T, T> _orderedDictionary;
#endif

        [GlobalSetup(Targets = new [] { nameof(Array), nameof(Span), nameof(ReadOnlySpan)})]
        public void SetupArray() => _array = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [Benchmark]
        public T Array()
        {
            T result = default;
            var collection = _array;
            foreach (var item in collection)
                result = item;
            return result;
        }
        
        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public T Span()
        {
            T result = default;
            var collection = new System.Span<T>(_array);
            foreach (var item in collection)
                result = item;
            return result;
        }
        
        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public T ReadOnlySpan()
        {
            T result = default;
            var collection = new System.ReadOnlySpan<T>(_array);
            foreach (var item in collection)
                result = item;
            return result;
        }
        
        [GlobalSetup(Target = nameof(IEnumerable))]
        public void SetupIEnumerable() => _ienumerable = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [Benchmark]
        [BenchmarkCategory(Categories.Runtime, Categories.Virtual)]
        public T IEnumerable() => Get(_ienumerable);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T Get(IEnumerable<T> collection)
        {
            T result = default;
            foreach (var item in collection)
                result = item;
            return result;
        }
        
        [GlobalSetup(Target = nameof(List))]
        public void SetupList() => _list = new List<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T List()
        {
            T result = default;
            var collection = _list;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(LinkedList))]
        public void SetupLinkedList() => _linkedlist = new LinkedList<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T LinkedList()
        {
            T result = default;
            var collection = _linkedlist;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(HashSet))]
        public void SetupHashSet() => _hashset = new HashSet<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T HashSet()
        {
            T result = default;
            var collection = _hashset;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Dictionary))]
        public void SetupDictionary() => _dictionary = new Dictionary<T, T>(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        public T Dictionary()
        {
            T result = default;
            var collection = _dictionary;
            foreach (var item in collection)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(Queue))]
        public void SetupQueue() => _queue = new Queue<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T Queue()
        {
            T result = default;
            var collection = _queue;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Stack))]
        public void SetupStack() => _stack = new Stack<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T Stack()
        {
            T result = default;
            var collection = _stack;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(SortedList))]
        public void SetupSortedList() => _sortedlist = new SortedList<T, T>(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        public T SortedList()
        {
            T result = default;
            var collection = _sortedlist;
            foreach (var item in collection)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(SortedSet))]
        public void SetupSortedSet() => _sortedset = new SortedSet<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T SortedSet()
        {
            T result = default;
            var collection = _sortedset;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(SortedDictionary))]
        public void SetupSortedDictionary() => _sorteddictionary = new SortedDictionary<T, T>(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        public T SortedDictionary()
        {
            T result = default;
            var collection = _sorteddictionary;
            foreach (var item in collection)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(ConcurrentDictionary))]
        public void SetupConcurrentDictionary() => _concurrentdictionary = new ConcurrentDictionary<T, T>(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        public T ConcurrentDictionary()
        {
            T result = default;
            var collection = _concurrentdictionary;
            foreach (var item in collection)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(ConcurrentQueue))]
        public void SetupConcurrentQueue() => _concurrentqueue = new ConcurrentQueue<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ConcurrentQueue()
        {
            T result = default;
            var collection = _concurrentqueue;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ConcurrentStack))]
        public void SetupConcurrentStack() => _concurrentstack = new ConcurrentStack<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ConcurrentStack()
        {
            T result = default;
            var collection = _concurrentstack;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ConcurrentBag))]
        public void SetupConcurrentBag() => _concurrentbag = new ConcurrentBag<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ConcurrentBag()
        {
            T result = default;
            var collection = _concurrentbag;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableArray))]
        public void SetupImmutableArray() => _immutablearray = Immutable.ImmutableArray.CreateRange<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ImmutableArray()
        {
            T result = default;
            var collection = _immutablearray;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableDictionary))]
        public void SetupImmutableDictionary() => _immutabledictionary = Immutable.ImmutableDictionary.CreateRange<T, T>(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        public T ImmutableDictionary()
        {
            T result = default;
            var collection = _immutabledictionary;
            foreach (var item in collection)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableHashSet))]
        public void SetupImmutableHashSet() => _immutablehashset = Immutable.ImmutableHashSet.CreateRange<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ImmutableHashSet()
        {
            T result = default;
            var collection = _immutablehashset;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableList))]
        public void SetupImmutableList() => _immutablelist = Immutable.ImmutableList.CreateRange<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ImmutableList()
        {
            T result = default;
            var collection = _immutablelist;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableQueue))]
        public void SetupImmutableQueue() => _immutablequeue = Immutable.ImmutableQueue.CreateRange<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ImmutableQueue()
        {
            T result = default;
            var collection = _immutablequeue;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableStack))]
        public void SetupImmutableStack() => _immutablestack = Immutable.ImmutableStack.CreateRange<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ImmutableStack()
        {
            T result = default;
            var collection = _immutablestack;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableSortedDictionary))]
        public void SetupImmutableSortedDictionary() => _immutablesorteddictionary = Immutable.ImmutableSortedDictionary.CreateRange<T, T>(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        public T ImmutableSortedDictionary()
        {
            T result = default;
            var collection = _immutablesorteddictionary;
            foreach (var item in collection)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableSortedSet))]
        public void SetupImmutableSortedSet() => _immutablesortedset = Immutable.ImmutableSortedSet.CreateRange<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ImmutableSortedSet()
        {
            T result = default;
            var collection = _immutablesortedset;
            foreach (var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(FrozenDictionary))]
        public void SetupFrozenDictionary() => _frozenDictionary = ValuesGenerator.Dictionary<T, T>(Size).ToFrozenDictionary();

        [Benchmark]
        public T FrozenDictionary()
        {
            T result = default;
            var collection = _frozenDictionary;
            foreach (var item in collection)
                result = item.Value;
            return result;
        }

        [GlobalSetup(Target = nameof(FrozenSet))]
        public void SetupFrozenSet() => _frozenset = ValuesGenerator.ArrayOfUniqueValues<T>(Size).ToFrozenSet();

        [Benchmark]
        public T FrozenSet()
        {
            T result = default;
            var collection = _frozenset;
            foreach (var item in collection)
                result = item;
            return result;
        }

#if NET9_0_OR_GREATER
        [GlobalSetup(Target = nameof(OrderedDictionary))]
        public void SetupOrderedDictionary() => _orderedDictionary = new OrderedDictionary<T, T>(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        public T OrderedDictionary()
        {
            T result = default;
            var collection = _orderedDictionary;
            foreach (var item in collection)
                result = item.Value;
            return result;
        }
#endif
    }
}