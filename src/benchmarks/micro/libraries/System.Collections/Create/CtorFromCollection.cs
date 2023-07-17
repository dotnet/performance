// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class CtorFromCollection<T>
    {
        private ICollection<T> _collection;
        private IDictionary<T, T> _dictionary;
        private SortedDictionary<T, T> _sortedDictionary;
        
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup(Targets = new[] { nameof(List), nameof(LinkedList), nameof(HashSet), nameof(Queue), nameof(Stack), nameof(SortedSet), nameof(ConcurrentQueue), nameof(ConcurrentStack), 
            nameof(ConcurrentBag), nameof(ImmutableArray), nameof(ImmutableHashSet), nameof(ImmutableList), nameof(ImmutableQueue), nameof(ImmutableStack), nameof(ImmutableSortedSet)})]
        public void SetupCollection() => _collection = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [GlobalSetup(Targets = new[] { nameof(Dictionary), nameof(SortedList), nameof(SortedDictionary), nameof(ConcurrentDictionary), nameof(ImmutableDictionary), nameof(ImmutableSortedDictionary), nameof(FrozenDictionary), nameof(FrozenDictionaryOptimized) })]
        public void SetupDictionary() => _dictionary = ValuesGenerator.Dictionary<T, T>(Size);

        [GlobalSetup(Targets = new[] { nameof(SortedDictionaryDeepCopy) })]
        public void SetupSortedDictionary() => _sortedDictionary = new SortedDictionary<T, T>(ValuesGenerator.Dictionary<T, T>(Size));

        [Benchmark]
        [MemoryRandomization]
        public List<T> List() => new List<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public LinkedList<T> LinkedList() => new LinkedList<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public HashSet<T> HashSet() => new HashSet<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public Dictionary<T, T> Dictionary() => new Dictionary<T, T>(_dictionary);

        [Benchmark]
        [MemoryRandomization]
        public Queue<T> Queue() => new Queue<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public Stack<T> Stack() => new Stack<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public SortedList<T, T> SortedList() => new SortedList<T, T>(_dictionary);

        [Benchmark]
        [MemoryRandomization]
        public SortedSet<T> SortedSet() => new SortedSet<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public SortedDictionary<T, T> SortedDictionary() => new SortedDictionary<T, T>(_dictionary);

        [Benchmark]
        [MemoryRandomization]
        public SortedDictionary<T, T> SortedDictionaryDeepCopy() => new SortedDictionary<T, T>(_sortedDictionary);

        [Benchmark]
        [MemoryRandomization]
        public ConcurrentDictionary<T, T> ConcurrentDictionary() => new ConcurrentDictionary<T, T>(_dictionary);

        [Benchmark]
        [MemoryRandomization]
        public ConcurrentQueue<T> ConcurrentQueue() => new ConcurrentQueue<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public ConcurrentStack<T> ConcurrentStack() => new ConcurrentStack<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public ConcurrentBag<T> ConcurrentBag() => new ConcurrentBag<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public ImmutableArray<T> ImmutableArray() => Immutable.ImmutableArray.CreateRange<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public ImmutableDictionary<T, T> ImmutableDictionary() => Immutable.ImmutableDictionary.CreateRange<T, T>(_dictionary);

        [Benchmark]
        [MemoryRandomization]
        public ImmutableHashSet<T> ImmutableHashSet() => Immutable.ImmutableHashSet.CreateRange<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public ImmutableList<T> ImmutableList() => Immutable.ImmutableList.CreateRange<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public ImmutableQueue<T> ImmutableQueue() => Immutable.ImmutableQueue.CreateRange<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public ImmutableStack<T> ImmutableStack() => Immutable.ImmutableStack.CreateRange<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public ImmutableSortedDictionary<T, T> ImmutableSortedDictionary() => Immutable.ImmutableSortedDictionary.CreateRange<T, T>(_dictionary);

        [Benchmark]
        [MemoryRandomization]
        public ImmutableSortedSet<T> ImmutableSortedSet() => Immutable.ImmutableSortedSet.CreateRange<T>(_collection);

        [Benchmark]
        [MemoryRandomization]
        public FrozenDictionary<T, T> FrozenDictionary() => _dictionary.ToFrozenDictionary();

        [Benchmark]
        [MemoryRandomization]
        public FrozenDictionary<T, T> FrozenDictionaryOptimized() => _dictionary.ToFrozenDictionary();
    }
}