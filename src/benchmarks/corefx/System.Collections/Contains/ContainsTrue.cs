using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class ContainsTrue<T>
    {
        private T[] _found;
        
        private T[] _array;
        private List<T> _list;
        private LinkedList<T> _linkedlist;
        private HashSet<T> _hashset;
        private Queue<T> _queue;
        private Stack<T> _stack;
        private SortedSet<T> _sortedset;
        private ImmutableArray<T> _immutablearray;
        private ImmutableHashSet<T> _immutablehashset;
        private ImmutableList<T> _immutablelist;
        private ImmutableSortedSet<T> _immutablesortedset;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _found = UniqueValuesGenerator.GenerateArray<T>(Size);
            _array = _found.ToArray();
            _list = new List<T>(_found);
            _linkedlist = new LinkedList<T>(_found);
            _hashset = new HashSet<T>(_found);
            _queue = new Queue<T>(_found);
            _stack = new Stack<T>(_found);
            _sortedset = new SortedSet<T>(_found);
            _immutablearray = Immutable.ImmutableArray.CreateRange<T>(_found);
            _immutablehashset = Immutable.ImmutableHashSet.CreateRange<T>(_found);
            _immutablelist = Immutable.ImmutableList.CreateRange<T>(_found);
            _immutablesortedset = Immutable.ImmutableSortedSet.CreateRange<T>(_found);
        }
        
        [Benchmark]
        public bool Array()
        {
            bool result = default;
            var collection = _array;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }
        
        [Benchmark]
        public bool List()
        {
            bool result = default;
            var collection = _list;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool LinkedList()
        {
            bool result = default;
            var collection = _linkedlist;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = default;
            var collection = _hashset;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool Queue()
        {
            bool result = default;
            var collection = _queue;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool Stack()
        {
            bool result = default;
            var collection = _stack;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = default;
            var collection = _sortedset;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableArray()
        {
            bool result = default;
            var collection = _immutablearray;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableHashSet()
        {
            bool result = default;
            var collection = _immutablehashset;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableList()
        {
            bool result = default;
            var collection = _immutablelist;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedSet()
        {
            bool result = default;
            var collection = _immutablesortedset;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result = collection.Contains(found[i]);
            return result;
        }
    }
}