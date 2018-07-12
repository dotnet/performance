using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class AddGivenSize<T>
    {
        private T[] _values;

        [Params(100)]
        public int Count;

        [GlobalSetup]
        public void Setup() => _values = UniqueValuesGenerator.GenerateArray<T>(Count);

        [Benchmark]
        public List<T> List()
        {
            var result = new List<T>(Count);
            var values = _values;
            for (int i = 0; i < values.Length; i++)
                result.Add(values[i]);
            return result;
        }

#if !NET461 // API added in .NET Core 2.0
        [Benchmark]
        public HashSet<T> HashSet()
        {
            var result = new HashSet<T>(Count);
            var values = _values;
            for(int i = 0; i < values.Length; i++)
                result.Add(values[i]);
            return result;
        }
#endif

        [Benchmark]
        public Dictionary<T, T> Dictionary()
        {
            var result = new Dictionary<T, T>(Count);
            var values = _values;
            for (int i = 0; i < values.Length; i++)
                result.Add(values[i], values[i]);
            return result;
        }

        [Benchmark]
        public SortedList<T, T> SortedList()
        {
            var result = new SortedList<T, T>(Count);
            var values = _values;
            for (int i = 0; i < values.Length; i++)
                result.Add(values[i], values[i]);
            return result;
        }

        [Benchmark]
        public Queue<T> Queue()
        {
            var result = new Queue<T>(Count);
            var values = _values;
            for (int i = 0; i < values.Length; i++)
                result.Enqueue(values[i]);
            return result;
        }

        [Benchmark]
        public Stack<T> Stack()
        {
            var result = new Stack<T>(Count);
            var values = _values;
            for (int i = 0; i < values.Length; i++)
                result.Push(values[i]);
            return result;
        }
    }
}