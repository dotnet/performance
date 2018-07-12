using System.Collections.Generic;
using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class TryAddDefaultSize<T>
    {
        private T[] _values;

        [Params(100)]
        public int Count;

        [GlobalSetup]
        public void Setup() => _values = UniqueValuesGenerator.GenerateArray<T>(Count);

#if !NET461 // API added in .NET Core 2.0
        [Benchmark]
        public Dictionary<T, T> Dictionary()
        {
            var result = new Dictionary<T, T>();
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.TryAdd(values[i], values[i]);
            return result;
        }
#endif

        [Benchmark]
        public ConcurrentDictionary<T, T> ConcurrentDictionary()
        {
            var result = new ConcurrentDictionary<T, T>();
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.TryAdd(values[i], values[i]);
            return result;
        }
    }
}