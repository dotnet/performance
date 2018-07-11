using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    public class IterateForEachNonGeneric
    {
        [Params(100)]
        public int Size;
        
        private ArrayList _arraylist;
        private Hashtable _hashtable;
        private Queue _queue;
        private Stack _stack;
        private SortedList _sortedlist;

        [GlobalSetup(Target = nameof(ArrayList))]
        public void SetupArrayList() => _arraylist = new ArrayList(UniqueValuesGenerator.GenerateArray<string>(Size));

        [Benchmark]
        public object ArrayList()
        {
            object result = default;
            var local = _arraylist;
            foreach(var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Hashtable))]
        public void SetupHashtable() => _hashtable = new Hashtable(UniqueValuesGenerator.GenerateDictionary<string, string>(Size));

        [Benchmark]
        public object Hashtable()
        {
            object result = default;
            var local = _hashtable;
            foreach(var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Queue))]
        public void SetupQueue() => _queue = new Queue(UniqueValuesGenerator.GenerateArray<string>(Size));

        [Benchmark]
        public object Queue()
        {
            object result = default;
            var local = _queue;
            foreach(var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Stack))]
        public void SetupStack() => _stack = new Stack(UniqueValuesGenerator.GenerateArray<string>(Size));

        [Benchmark]
        public object Stack()
        {
            object result = default;
            var local = _stack;
            foreach(var item in local)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(SortedList))]
        public void SetupSortedList() => _sortedlist = new SortedList(UniqueValuesGenerator.GenerateDictionary<string, string>(Size));

        [Benchmark]
        public object SortedList()
        {
            object result = default;
            var local = _sortedlist;
            foreach(var item in local)
                result = item;
            return result;
        }
    }
}