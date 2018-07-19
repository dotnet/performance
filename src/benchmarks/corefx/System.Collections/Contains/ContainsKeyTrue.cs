using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int), typeof(int))] // value type
    [GenericTypeArguments(typeof(string), typeof(string))] // reference type
    public class ContainsKeyTrue<TKey, TValue>
    {
        private TKey[] _found;
        private Dictionary<TKey, TValue> _source;
        
        private Dictionary<TKey, TValue> _dictionary;
        private SortedList<TKey, TValue> _sortedlist;
        private SortedDictionary<TKey, TValue> _sorteddictionary;
        private ConcurrentDictionary<TKey, TValue> _concurrentdictionary;
        private ImmutableDictionary<TKey, TValue> _immutabledictionary;
        private ImmutableSortedDictionary<TKey, TValue> _immutablesorteddictionary;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _found = UniqueValuesGenerator.GenerateArray<TKey>(Size);
            _source = _found.ToDictionary(item => item, item => (TValue)(object)item);
            _dictionary = new Dictionary<TKey, TValue>(_source);
            _sortedlist = new SortedList<TKey, TValue>(_source);
            _sorteddictionary = new SortedDictionary<TKey, TValue>(_source);
            _concurrentdictionary = new ConcurrentDictionary<TKey, TValue>(_source);
            _immutabledictionary = Immutable.ImmutableDictionary.CreateRange<TKey, TValue>(_source);
            _immutablesorteddictionary = Immutable.ImmutableSortedDictionary.CreateRange<TKey, TValue>(_source);
        }

        [Benchmark]
        public bool Dictionary()
        {
            bool result = default;
            var collection = _dictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
        public bool IDictionary() => ContainsKey(_dictionary);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool ContainsKey(IDictionary<TKey, TValue> collection)
        {
            bool result = default;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedList()
        {
            bool result = default;
            var collection = _sortedlist;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedDictionary()
        {
            bool result = default;
            var collection = _sorteddictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool ConcurrentDictionary()
        {
            bool result = default;
            var collection = _concurrentdictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableDictionary()
        {
            bool result = default;
            var collection = _immutabledictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedDictionary()
        {
            bool result = default;
            var collection = _immutablesorteddictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.ContainsKey(found[i]);
            return result;
        }
    }
}