// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class IndexerSet<T>
    {
        [Params(Utils.DefaultCollectionSize)] 
        public int Size;

        private T[] _keys;

        private T[] _array;
        private List<T> _list;
        private Dictionary<T, T> _dictionary;
        private SortedList<T, T> _sortedList;
        private SortedDictionary<T, T> _sortedDictionary;
        private ConcurrentDictionary<T, T> _concurrentDictionary;
#if NET9_0_OR_GREATER
        private OrderedDictionary<T, T> _orderedDictionary;
#endif

        [GlobalSetup(Targets = new[] { nameof(Array), nameof(Span) })]
        public void SetupArray() => _array = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [Benchmark]
        public T[] Array()
        {
            var array = _array;
            for (int i = 0; i < array.Length; i++)
                array[i] = default;
            return array;
        }

        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public T Span()
        {
            T result = default;
            var collection = new Span<T>(_array);
            for (int i = 0; i < collection.Length; i++)
                collection[i] = default;
            return result;
        }

        [GlobalSetup(Targets = new[] { nameof(List), nameof(IList) })]
        public void SetupList() => _list = new List<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public List<T> List()
        {
            var list = _list;
            for (int i = 0; i < list.Count; i++)
                list[i] = default;
            return list;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.Runtime, Categories.Virtual)]
        public IList<T> IList() => Set(_list);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IList<T> Set(IList<T> collection)
        {
            for (int i = 0; i < collection.Count; i++)
                collection[i] = default;
            return collection;
        }

        [GlobalSetup(Target = nameof(Dictionary))]
        public void SetupDictionary()
        {
            _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _dictionary = new Dictionary<T, T>(_keys.ToDictionary(i => i, i => i));
        }

        [Benchmark]
        public Dictionary<T, T> Dictionary()
        {
            var dictionary = _dictionary;
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
                dictionary[keys[i]] = default;
            return dictionary;
        }

        [GlobalSetup(Target = nameof(SortedList))]
        public void SetupSortedList()
        {
            _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _sortedList = new SortedList<T, T>(_keys.ToDictionary(i => i, i => i));
        }

        [Benchmark]
        public SortedList<T, T> SortedList()
        {
            var sortedList = _sortedList;
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
                sortedList[keys[i]] = default;
            return sortedList;
        }

        [GlobalSetup(Target = nameof(SortedDictionary))]
        public void SetupSortedDictionary()
        {
            _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _sortedDictionary = new SortedDictionary<T, T>(_keys.ToDictionary(i => i, i => i));
        }

        [Benchmark]
        public SortedDictionary<T, T> SortedDictionary()
        {
            var dictionary = _sortedDictionary;
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
                dictionary[keys[i]] = default;
            return dictionary;
        }

        [GlobalSetup(Target = nameof(ConcurrentDictionary))]
        public void SetupConcurrentDictionary()
        {
            _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _concurrentDictionary = new ConcurrentDictionary<T, T>(_keys.ToDictionary(i => i, i => i));
        }

        [Benchmark]
        public ConcurrentDictionary<T, T> ConcurrentDictionary()
        {
            var dictionary = _concurrentDictionary;
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
                dictionary[keys[i]] = default;
            return dictionary;
        }

#if NET9_0_OR_GREATER
        [GlobalSetup(Targets = [ nameof(OrderedDictionary), nameof(OrderedDictionary_SetAt) ])]
        public void SetupOrderedDictionary()
        {
            _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _orderedDictionary = new OrderedDictionary<T, T>(_keys.ToDictionary(i => i, i => i));
        }

        [Benchmark]
        public OrderedDictionary<T, T> OrderedDictionary()
        {
            var dictionary = _orderedDictionary;
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
                dictionary[keys[i]] = default;
            return dictionary;
        }

        [Benchmark]
        public OrderedDictionary<T, T> OrderedDictionary_SetAt()
        {
            var dictionary = _orderedDictionary;
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
                dictionary.SetAt(i, keys[i], default);
            return dictionary;
        }
#endif
    }
}