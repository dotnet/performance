using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    public class CtorFromCollectionNonGeneric
    {
        private ICollection _collection;
        private IDictionary _dictionary;
        
        [Params(100)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _collection = UniqueValuesGenerator.GenerateArray<string>(Size);
            _dictionary = UniqueValuesGenerator.GenerateDictionary<string, string>(Size);
        }
        
        [Benchmark]
        public ArrayList ArrayList() => new ArrayList(_collection);

        [Benchmark]
        public Hashtable Hashtable() => new Hashtable(_dictionary);

        [Benchmark]
        public Queue Queue() => new Queue(_collection);

        [Benchmark]
        public Stack Stack() => new Stack(_collection);

        [Benchmark]
        public SortedList SortedList() => new SortedList(_dictionary);
    }
}