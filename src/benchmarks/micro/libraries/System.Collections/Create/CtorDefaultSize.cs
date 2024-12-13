// Licensed to the .NET Foundation under one or more agreements.
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

#if NET9_0_OR_GREATER
        [Benchmark]
        public OrderedDictionary<T, T> OrderedDictionary() => new OrderedDictionary<T, T>();
#endif
    }
}