using BenchmarkDotNet.Attributes;

namespace System.Collections
{
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