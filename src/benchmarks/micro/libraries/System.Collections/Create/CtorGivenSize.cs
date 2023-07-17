// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class CtorGivenSize<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;
        
        [Benchmark]

        [MemoryRandomization]
        public T[] Array() => new T[Size];

        [Benchmark]
        [MemoryRandomization]
        public List<T> List() => new List<T>(Size);

#if !NETFRAMEWORK // API added in .NET Core 2.0
        [Benchmark]
        [MemoryRandomization]
        public HashSet<T> HashSet() => new HashSet<T>(Size);
#endif
        [Benchmark]
        [MemoryRandomization]
        public Dictionary<T, T> Dictionary() => new Dictionary<T, T>(Size);

        [Benchmark]
        [MemoryRandomization]
        public Queue<T> Queue() => new Queue<T>(Size);

        [Benchmark]
        [MemoryRandomization]
        public Stack<T> Stack() => new Stack<T>(Size);

        [Benchmark]
        [MemoryRandomization]
        public SortedList<T, T> SortedList() => new SortedList<T, T>(Size);

        [Benchmark]
        [MemoryRandomization]
        public ConcurrentDictionary<T, T> ConcurrentDictionary() => new ConcurrentDictionary<T, T>(Utils.ConcurrencyLevel, Size);
    }
}