using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections.Concurrent
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class Count<T>
    {
        private ConcurrentDictionary<T, T> _dictionary;
        private ConcurrentQueue<T> _queue;
        private ConcurrentStack<T> _stack;
        private ConcurrentBag<T> _bag;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            var values = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            
            _dictionary = new ConcurrentDictionary<T, T>(values.ToDictionary(v => v, v => v));
            _queue = new ConcurrentQueue<T>(values);
            _stack = new ConcurrentStack<T>(values);
            _bag = new ConcurrentBag<T>(values);
        }

        [Benchmark]
        public int Dictionary() => _dictionary.Count;
        
        [Benchmark]
        public int Queue() => _queue.Count;
        
        [Benchmark]
        public int Stack() => _stack.Count;
        
        [Benchmark]
        public int Bag() => _bag.Count;
    }
}