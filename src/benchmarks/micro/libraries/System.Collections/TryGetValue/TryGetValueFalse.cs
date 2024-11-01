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
    public class TryGetValueFalse<TKey, TValue>
    {
        private TKey[] _notFound;
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
            var values = ValuesGenerator.ArrayOfUniqueValues<TKey>(Size * 2);
            _notFound = values.Take(Size).ToArray();

            _source = values.Skip(Size).Take(Size).ToDictionary(item => item, item => (TValue)(object)item);
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
            Dictionary<TKey, TValue> collection = _dictionary;
            TKey[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.TryGetValue(notFound[i], out _);
            return result;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.Runtime, Categories.Virtual)]
        public bool IDictionary() => TryGetValue(_dictionary);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryGetValue(IDictionary<TKey, TValue> collection)
        {
            bool result = default;
            TKey[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.TryGetValue(notFound[i], out _);
            return result;
        }

        [Benchmark]
        public bool SortedList()
        {
            bool result = default;
            SortedList<TKey, TValue> collection = _sortedList;
            TKey[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.TryGetValue(notFound[i], out _);
            return result;
        }

        [Benchmark]
        public bool SortedDictionary()
        {
            bool result = default;
            SortedDictionary<TKey, TValue> collection = _sortedDictionary;
            TKey[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.TryGetValue(notFound[i], out _);
            return result;
        }

        [Benchmark]
        public bool ConcurrentDictionary()
        {
            bool result = default;
            ConcurrentDictionary<TKey, TValue> collection = _concurrentDictionary;
            TKey[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.TryGetValue(notFound[i], out _);
            return result;
        }

        [Benchmark]
        public bool ImmutableDictionary()
        {
            bool result = default;
            ImmutableDictionary<TKey, TValue> collection = _immutableDictionary;
            TKey[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.TryGetValue(notFound[i], out _);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedDictionary()
        {
            bool result = default;
            ImmutableSortedDictionary<TKey, TValue> collection = _immutableSortedDictionary;
            TKey[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.TryGetValue(notFound[i], out _);
            return result;
        }

        [Benchmark(Description = "FrozenDictionary")]
        public bool FrozenDictionaryOptimized() // we kept the old name on purpose to avoid loosing historical data
        {
            bool result = default;
            FrozenDictionary<TKey, TValue> collection = _frozenDictionary;
            TKey[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.TryGetValue(notFound[i], out _);
            return result;
        }

#if NET9_0_OR_GREATER
        [Benchmark]
        public bool OrderedDictionary()
        {
            bool result = default;
            OrderedDictionary<TKey, TValue> collection = _orderedDictionary;
            TKey[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.TryGetValue(notFound[i], out _);
            return result;
        }
#endif
    }
}