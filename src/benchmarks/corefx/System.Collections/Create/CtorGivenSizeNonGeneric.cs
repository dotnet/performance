using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.NonGenericCollections)]
    public class CtorGivenSizeNonGeneric
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;
        
        [Benchmark]
        public ArrayList ArrayList() => new ArrayList(Size);

        [Benchmark]
        public Hashtable Hashtable() => new Hashtable(Size);

        [Benchmark]
        public Queue Queue() => new Queue(Size);

        [Benchmark]
        public Stack Stack() => new Stack(Size);

        [Benchmark]
        public SortedList SortedList() => new SortedList(Size);
    }
}