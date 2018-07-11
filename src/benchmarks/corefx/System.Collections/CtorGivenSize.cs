using System.Collections.Concurrent;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections)]
    [GenericTypeArguments(typeof(int), typeof(int))] // value type
    [GenericTypeArguments(typeof(string), typeof(string))] // reference type
    public class CtorGiventSize<TKey, TValue>
    {
        private const int ConcurrencyLevel = 4;
        
        [Params(100)]
        public int Size;
        
        [Benchmark]
        public TKey[] Array() => new TKey[Size];

        [Benchmark]
        public List<TKey> List() => new List<TKey>(Size);

#if !NET461 // API added in .NET Core 2.0
        [Benchmark]
        public HashSet<TKey> HashSet() => new HashSet<TKey>(Size);
#endif
        [Benchmark]
        public Dictionary<TKey, TValue> Dictionary() => new Dictionary<TKey, TValue>(Size);

        [Benchmark]
        public Queue<TKey> Queue() => new Queue<TKey>(Size);

        [Benchmark]
        public Stack<TKey> Stack() => new Stack<TKey>(Size);

        [Benchmark]
        public SortedList<TKey, TValue> SortedList() => new SortedList<TKey, TValue>(Size);

        [Benchmark]
        public ConcurrentDictionary<TKey, TValue> ConcurrentDictionary() => new ConcurrentDictionary<TKey, TValue>(ConcurrencyLevel, Size);
    }
}