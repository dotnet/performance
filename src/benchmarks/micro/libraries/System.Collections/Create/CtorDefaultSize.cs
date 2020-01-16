﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class CtorDefaultSize<T>
    {
        [Benchmark]
        public List<T> List() => new List<T>();

        [Benchmark]
        public LinkedList<T> LinkedList() => new LinkedList<T>();

        [Benchmark]
        public HashSet<T> HashSet() => new HashSet<T>();

        [Benchmark]
        public Dictionary<T, T> Dictionary() => new Dictionary<T, T>();

        [Benchmark]
        public Queue<T> Queue() => new Queue<T>();

        [Benchmark]
        public Stack<T> Stack() => new Stack<T>();

        [Benchmark]
        public SortedList<T, T> SortedList() => new SortedList<T, T>();

        [Benchmark]
        public SortedSet<T> SortedSet() => new SortedSet<T>();

        [Benchmark]
        public SortedDictionary<T, T> SortedDictionary() => new SortedDictionary<T, T>();

        [Benchmark]
        public ConcurrentDictionary<T, T> ConcurrentDictionary() => new ConcurrentDictionary<T, T>();

        [Benchmark]
        public ConcurrentQueue<T> ConcurrentQueue() => new ConcurrentQueue<T>();

        [Benchmark]
        public ConcurrentStack<T> ConcurrentStack() => new ConcurrentStack<T>();

        [Benchmark]
        public ConcurrentBag<T> ConcurrentBag() => new ConcurrentBag<T>();

        [Benchmark]
        public ImmutableArray<T> ImmutableArray() => Immutable.ImmutableArray.Create<T>();

        [Benchmark]
        public ImmutableDictionary<T, T> ImmutableDictionary() => Immutable.ImmutableDictionary.Create<T, T>();

        [Benchmark]
        public ImmutableHashSet<T> ImmutableHashSet() => Immutable.ImmutableHashSet.Create<T>();

        [Benchmark]
        public ImmutableList<T> ImmutableList() => Immutable.ImmutableList.Create<T>();

        [Benchmark]
        public ImmutableQueue<T> ImmutableQueue() => Immutable.ImmutableQueue.Create<T>();

        [Benchmark]
        public ImmutableStack<T> ImmutableStack() => Immutable.ImmutableStack.Create<T>();

        [Benchmark]
        public ImmutableSortedDictionary<T, T> ImmutableSortedDictionary() => Immutable.ImmutableSortedDictionary.Create<T, T>();

        [Benchmark]
        public ImmutableSortedSet<T> ImmutableSortedSet() => Immutable.ImmutableSortedSet.Create<T>();
    }
}