using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.NonGenericCollections)]
    public class CtorDefaultSizeNonGeneric
    {
        [Benchmark]
        public ArrayList ArrayList() => new ArrayList();

        [Benchmark]
        public Hashtable Hashtable() => new Hashtable();

        [Benchmark]
        public Queue Queue() => new Queue();

        [Benchmark]
        public Stack Stack() => new Stack();

        [Benchmark]
        public SortedList SortedList() => new SortedList();
    }
}