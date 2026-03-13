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
    public class ContainsValueFalse<TKey, TValue>
    {
        private TValue[] _notFound;
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
            var values = ValuesGenerator.ArrayOfUniqueValues<TValue>(Size * 2);
            _notFound = values.Take(Size).ToArray();

            _source = values.Skip(Size).Take(Size).ToDictionary(item => (TKey)(object)item, item => item);
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
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.ContainsValue(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool SortedList()
        {
            bool result = default;
            var collection = _sortedList;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.ContainsValue(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool SortedDictionary()
        {
            bool result = default;
            var collection = _sortedDictionary;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.ContainsValue(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableDictionary()
        {
            bool result = default;
            var collection = _immutableDictionary;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.ContainsValue(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedDictionary()
        {
            bool result = default;
            var collection = _immutableSortedDictionary;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.ContainsValue(notFound[i]);
            return result;
        }
    }
}