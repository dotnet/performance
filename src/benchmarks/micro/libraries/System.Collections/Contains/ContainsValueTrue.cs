// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
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
    public class ContainsValueTrue<TKey, TValue>
    {
        private TValue[] _found;
        private Dictionary<TKey, TValue> _source;

        private Dictionary<TKey, TValue> _dictionary;
        private SortedList<TKey, TValue> _sortedList;
        private SortedDictionary<TKey, TValue> _sortedDictionary;
        private ImmutableDictionary<TKey, TValue> _immutableDictionary;
        private ImmutableSortedDictionary<TKey, TValue> _immutableSortedDictionary;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<TValue>(Size);
            _source = _found.ToDictionary(item => (TKey)(object)item, item => item);
            _dictionary = new Dictionary<TKey, TValue>(_source);
            _sortedList = new SortedList<TKey, TValue>(_source);
            _sortedDictionary = new SortedDictionary<TKey, TValue>(_source);
            _immutableDictionary = Immutable.ImmutableDictionary.CreateRange<TKey, TValue>(_source);
            _immutableSortedDictionary = Immutable.ImmutableSortedDictionary.CreateRange<TKey, TValue>(_source);
        }

        [Benchmark]
        public bool Dictionary()
        {
            bool result = default;
            var collection = _dictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsValue(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedList()
        {
            bool result = default;
            var collection = _sortedList;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsValue(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedDictionary()
        {
            bool result = default;
            var collection = _sortedDictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsValue(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableDictionary()
        {
            bool result = default;
            var collection = _immutableDictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsValue(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedDictionary()
        {
            bool result = default;
            var collection = _immutableSortedDictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsValue(found[i]);
            return result;
        }
    }
}