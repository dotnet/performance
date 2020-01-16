// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
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
        
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _collection = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _dictionary = ValuesGenerator.Dictionary<T, T>(Size);
        }
        
        [Benchmark]
        public List<T> List() => new List<T>(_collection);

        [Benchmark]
        public LinkedList<T> LinkedList() => new LinkedList<T>(_collection);

        [Benchmark]
        public HashSet<T> HashSet() => new HashSet<T>(_collection);

        [Benchmark]
        public Dictionary<T, T> Dictionary() => new Dictionary<T, T>(_dictionary);

        [Benchmark]
        public Queue<T> Queue() => new Queue<T>(_collection);

        [Benchmark]
        public Stack<T> Stack() => new Stack<T>(_collection);

        [Benchmark]
        public SortedList<T, T> SortedList() => new SortedList<T, T>(_dictionary);

        [Benchmark]
        public SortedSet<T> SortedSet() => new SortedSet<T>(_collection);

        [Benchmark]
        public SortedDictionary<T, T> SortedDictionary() => new SortedDictionary<T, T>(_dictionary);

        [Benchmark]
        public ConcurrentDictionary<T, T> ConcurrentDictionary() => new ConcurrentDictionary<T, T>(_dictionary);

        [Benchmark]
        public ConcurrentQueue<T> ConcurrentQueue() => new ConcurrentQueue<T>(_collection);

        [Benchmark]
        public ConcurrentStack<T> ConcurrentStack() => new ConcurrentStack<T>(_collection);

        [Benchmark]
        public ConcurrentBag<T> ConcurrentBag() => new ConcurrentBag<T>(_collection);

        [Benchmark]
        public ImmutableArray<T> ImmutableArray() => Immutable.ImmutableArray.CreateRange<T>(_collection);

        [Benchmark]
        public ImmutableDictionary<T, T> ImmutableDictionary() => Immutable.ImmutableDictionary.CreateRange<T, T>(_dictionary);

        [Benchmark]
        public ImmutableHashSet<T> ImmutableHashSet() => Immutable.ImmutableHashSet.CreateRange<T>(_collection);

        [Benchmark]
        public ImmutableList<T> ImmutableList() => Immutable.ImmutableList.CreateRange<T>(_collection);

        [Benchmark]
        public ImmutableQueue<T> ImmutableQueue() => Immutable.ImmutableQueue.CreateRange<T>(_collection);

        [Benchmark]
        public ImmutableStack<T> ImmutableStack() => Immutable.ImmutableStack.CreateRange<T>(_collection);

        [Benchmark]
        public ImmutableSortedDictionary<T, T> ImmutableSortedDictionary() => Immutable.ImmutableSortedDictionary.CreateRange<T, T>(_dictionary);

        [Benchmark]
        public ImmutableSortedSet<T> ImmutableSortedSet() => Immutable.ImmutableSortedSet.CreateRange<T>(_collection);
    }
}