// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.NonGenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type (it shows how bad idea is to use non-generic collections for value types)
    [GenericTypeArguments(typeof(string))] // reference type
    public class CtorFromCollectionNonGeneric<T>
    {
        private ICollection _collection;
        private IDictionary _dictionary;
        
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _collection = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _dictionary = ValuesGenerator.Dictionary<T, T>(Size);
        }
        
        [Benchmark]
        public ArrayList ArrayList() => new ArrayList(_collection);

        [Benchmark]
        public Hashtable Hashtable() => new Hashtable(_dictionary);

        [Benchmark]
        public Queue Queue() => new Queue(_collection);

        [Benchmark]
        public Stack Stack() => new Stack(_collection);

        [Benchmark]
        public SortedList SortedList() => new SortedList(_dictionary);
    }
}