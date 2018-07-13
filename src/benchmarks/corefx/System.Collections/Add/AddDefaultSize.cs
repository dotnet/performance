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
        private T[] _uniqueValues;

        [Params(Utils.DefaultCollectionSize)]
        public int Count;

        [GlobalSetup]
        public void Setup() => _uniqueValues = UniqueValuesGenerator.GenerateArray<T>(Count);

        [Benchmark]
        public List<T> List()
        {
            var collection = new List<T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public HashSet<T> HashSet()
        {
            var collection = new HashSet<T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public Dictionary<T, T> Dictionary()
        {
            var collection = new Dictionary<T, T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i], uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public SortedList<T, T> SortedList()
        {
            var collection = new SortedList<T, T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i], uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public SortedSet<T> SortedSet()
        {
            var collection = new SortedSet<T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public SortedDictionary<T, T> SortedDictionary()
        {
            var collection = new SortedDictionary<T, T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i], uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public ConcurrentBag<T> ConcurrentBag()
        {
            var collection = new ConcurrentBag<T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Add(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public Queue<T> Queue()
        {
            var collection = new Queue<T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Enqueue(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public Stack<T> Stack()
        {
            var collection = new Stack<T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Push(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public ConcurrentQueue<T> ConcurrentQueue()
        {
            var collection = new ConcurrentQueue<T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Enqueue(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public ConcurrentStack<T> ConcurrentStack()
        {
            var collection = new ConcurrentStack<T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.Push(uniqueValues[i]);
            return collection;
        }

        [Benchmark]
        public ConcurrentDictionary<T, T> ConcurrentDictionary()
        {
            var collection = new ConcurrentDictionary<T, T>();
            var uniqueValues = _uniqueValues;
            for (int i = 0; i < uniqueValues.Length; i++)
                collection.TryAdd(uniqueValues[i], uniqueValues[i]);
            return collection;
        }
    }
}