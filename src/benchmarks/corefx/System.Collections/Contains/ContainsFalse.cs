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
    public class ContainsFalse<T>
    {
        private T[] _notFound;
        
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
            var values = UniqueValuesGenerator.GenerateArray<T>(Size * 2);
            _notFound = values.Take(Size).ToArray();
            var secondHalf = values.Skip(Size).Take(Size).ToArray();

            _array = secondHalf;
            _list = new List<T>(secondHalf);
            _linkedlist = new LinkedList<T>(secondHalf);
            _hashset = new HashSet<T>(secondHalf);
            _queue = new Queue<T>(secondHalf);
            _stack = new Stack<T>(secondHalf);
            _sortedset = new SortedSet<T>(secondHalf);
            _immutablearray = Immutable.ImmutableArray.CreateRange<T>(secondHalf);
            _immutablehashset = Immutable.ImmutableHashSet.CreateRange<T>(secondHalf);
            _immutablelist = Immutable.ImmutableList.CreateRange<T>(secondHalf);
            _immutablesortedset = Immutable.ImmutableSortedSet.CreateRange<T>(secondHalf);
        }
        
        [Benchmark]
        public bool Array()
        {
            bool result = default;
            var collection = _array;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }
        
        [Benchmark]
        public bool List()
        {
            bool result = default;
            var collection = _list;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool LinkedList()
        {
            bool result = default;
            var collection = _linkedlist;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = default;
            var collection = _hashset;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool Queue()
        {
            bool result = default;
            var collection = _queue;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool Stack()
        {
            bool result = default;
            var collection = _stack;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = default;
            var collection = _sortedset;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableArray()
        {
            bool result = default;
            var collection = _immutablearray;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableHashSet()
        {
            bool result = default;
            var collection = _immutablehashset;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableList()
        {
            bool result = default;
            var collection = _immutablelist;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedSet()
        {
            bool result = default;
            var collection = _immutablesortedset;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result = collection.Contains(notFound[i]);
            return result;
        }
    }
}