using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    public class CtorGivenSizeNonGeneric
    {
        [Params(100)]
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