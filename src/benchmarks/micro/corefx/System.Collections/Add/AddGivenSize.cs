// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class AddGivenSize<T>
    {
        private T[] _uniqueValues;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup() => _uniqueValues = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [Benchmark]
        public List<T> List()
        {
            var collection = new List<T>(Size);
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
        public ICollection<T> ICollection() => AddToICollection(new List<T>(Size));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ICollection<T> AddToICollection(ICollection<T> collection)
        {
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i]);
            return collection;
        }

#if !NETFRAMEWORK // API added in .NET Core 2.0
        [Benchmark]
        public HashSet<T> HashSet()
        {
            var collection = new HashSet<T>(Size);
            var uniqueValues = _uniqueValues;
            for(int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i]);
            return collection;
        }
#endif

        [Benchmark]
        public Dictionary<T, T> Dictionary()
        {
            var collection = new Dictionary<T, T>(Size);
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i], uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
        public IDictionary<T, T> IDictionary() => AddToIDictionary(new Dictionary<T, T>(Size));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IDictionary<T, T> AddToIDictionary(IDictionary<T, T> collection)
        {
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i], uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public SortedList<T, T> SortedList()
        {
            var collection = new SortedList<T, T>(Size);
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i], uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public Queue<T> Queue()
        {
            var collection = new Queue<T>(Size);
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Enqueue(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public Stack<T> Stack()
        {
            var collection = new Stack<T>(Size);
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Push(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public ConcurrentDictionary<T, T> ConcurrentDictionary()
        {
            var collection = new ConcurrentDictionary<T, T>(Utils.ConcurrencyLevel, Size);
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.TryAdd(uniqueValues[i], uniqueValues[i]);
            return collection;
        }
    }
}