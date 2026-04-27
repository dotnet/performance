// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int), typeof(int))] // value type
    [GenericTypeArguments(typeof(string), typeof(string))] // reference type
    public class ContainsKeyTrue<TKey, TValue>
    {
        private TKey[] _found;
        private Dictionary<TKey, TValue> _source;
        
        private Dictionary<TKey, TValue> _dictionary;
        private SortedList<TKey, TValue> _sortedList;
        private SortedDictionary<TKey, TValue> _sortedDictionary;
        private ConcurrentDictionary<TKey, TValue> _concurrentDictionary;
        private ImmutableDictionary<TKey, TValue> _immutableDictionary;
        private ImmutableSortedDictionary<TKey, TValue> _immutableSortedDictionary;
        private FrozenDictionary<TKey, TValue> _frozenDictionary;
#if NET9_0_OR_GREATER
        private OrderedDictionary<TKey, TValue> _orderedDictionary;
#endif

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<TKey>(Size);
            _source = _found.ToDictionary(item => item, item => (TValue)(object)item);
            _dictionary = new Dictionary<TKey, TValue>(_source);
            _sortedList = new SortedList<TKey, TValue>(_source);
            _sortedDictionary = new SortedDictionary<TKey, TValue>(_source);
            _concurrentDictionary = new ConcurrentDictionary<TKey, TValue>(_source);
            _immutableDictionary = Immutable.ImmutableDictionary.CreateRange<TKey, TValue>(_source);
            _immutableSortedDictionary = Immutable.ImmutableSortedDictionary.CreateRange<TKey, TValue>(_source);
            _frozenDictionary = _source.ToFrozenDictionary();
#if NET9_0_OR_GREATER
            _orderedDictionary = new OrderedDictionary<TKey, TValue>(_source);
#endif
        }

        [Benchmark]
        public bool Dictionary()
        {
            bool result = default;
            var collection = _dictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.Runtime, Categories.Virtual)]
        public bool IDictionary() => ContainsKey(_dictionary);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool ContainsKey(IDictionary<TKey, TValue> collection)
        {
            bool result = default;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedList()
        {
            bool result = default;
            var collection = _sortedList;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedDictionary()
        {
            bool result = default;
            var collection = _sortedDictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool ConcurrentDictionary()
        {
            bool result = default;
            var collection = _concurrentDictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableDictionary()
        {
            bool result = default;
            var collection = _immutableDictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedDictionary()
        {
            bool result = default;
            var collection = _immutableSortedDictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool FrozenDictionary()
        {
            bool result = default;
            var collection = _frozenDictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

#if NET9_0_OR_GREATER
        [Benchmark]
        public bool OrderedDictionary()
        {
            bool result = default;
            var collection = _orderedDictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }
#endif
    }
}