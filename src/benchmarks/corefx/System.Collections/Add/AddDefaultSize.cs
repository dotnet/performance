using System.Collections.Concurrent;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class AddDefaultSize<T>
    {
        private T[] _values;

        [Params(100)]
        public int Count;

        [GlobalSetup]
        public void Setup() => _values = UniqueValuesGenerator.GenerateArray<T>(Count);
        
        [Benchmark]
        public List<T> List()
        {
            var result = new List<T>();
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.Add(values[i]);
            return result;
        }

        [Benchmark]
        public HashSet<T> HashSet()
        {
            var result = new HashSet<T>();
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.Add(values[i]);
            return result;
        }

        [Benchmark]
        public Dictionary<T, T> Dictionary()
        {
            var result = new Dictionary<T, T>();
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.Add(values[i], values[i]);
            return result;
        }

        [Benchmark]
        public SortedList<T, T> SortedList()
        {
            var result = new SortedList<T, T>();
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.Add(values[i], values[i]);
            return result;
        }

        [Benchmark]
        public SortedSet<T> SortedSet()
        {
            var result = new SortedSet<T>();
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.Add(values[i]);
            return result;
        }

        [Benchmark]
        public SortedDictionary<T, T> SortedDictionary()
        {
            var result = new SortedDictionary<T, T>();
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.Add(values[i], values[i]);
            return result;
        }

        [Benchmark]
        public ConcurrentBag<T> ConcurrentBag()
        {
            var result = new ConcurrentBag<T>();
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.Add(values[i]);
            return result;
        }
    }
}