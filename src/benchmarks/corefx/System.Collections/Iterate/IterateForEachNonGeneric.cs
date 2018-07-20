using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.NonGenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type (it shows how bad idea is to use non-generic collections for value types)
    [GenericTypeArguments(typeof(string))] // reference type
    public class IterateForEachNonGeneric<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;
        
        private ArrayList _arraylist;
        private Hashtable _hashtable;
        private Queue _queue;
        private Stack _stack;
        private SortedList _sortedlist;

        [GlobalSetup(Target = nameof(ArrayList))]
        public void SetupArrayList() => _arraylist = new ArrayList(UniqueValuesGenerator.GenerateArray<T>(Size));

        [Benchmark]
        public object ArrayList()
        {
            object result = default;
            var collection = _arraylist;
            foreach(var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Hashtable))]
        public void SetupHashtable() => _hashtable = new Hashtable(UniqueValuesGenerator.GenerateDictionary<T, T>(Size));

        [Benchmark]
        public object Hashtable()
        {
            object result = default;
            var collection = _hashtable;
            foreach(var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Queue))]
        public void SetupQueue() => _queue = new Queue(UniqueValuesGenerator.GenerateArray<T>(Size));

        [Benchmark]
        public object Queue()
        {
            object result = default;
            var collection = _queue;
            foreach(var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(Stack))]
        public void SetupStack() => _stack = new Stack(UniqueValuesGenerator.GenerateArray<T>(Size));

        [Benchmark]
        public object Stack()
        {
            object result = default;
            var collection = _stack;
            foreach(var item in collection)
                result = item;
            return result;
        }

        [GlobalSetup(Target = nameof(SortedList))]
        public void SetupSortedList() => _sortedlist = new SortedList(UniqueValuesGenerator.GenerateDictionary<T, T>(Size));

        [Benchmark]
        public object SortedList()
        {
            object result = default;
            var collection = _sortedlist;
            foreach(var item in collection)
                result = item;
            return result;
        }
    }
}