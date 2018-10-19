using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

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
        private LinkedList<T> _linkedList;
        private HashSet<T> _hashSet;
        private Queue<T> _queue;
        private Stack<T> _stack;
        private SortedSet<T> _sortedSet;
        private ImmutableArray<T> _immutableArray;
        private ImmutableHashSet<T> _immutableHashSet;
        private ImmutableList<T> _immutableList;
        private ImmutableSortedSet<T> _immutableSortedSet;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            var values = ValuesGenerator.ArrayOfUniqueValues<T>(Size * 2);
            _notFound = values.Take(Size).ToArray();
            var secondHalf = values.Skip(Size).Take(Size).ToArray();

            _array = secondHalf;
            _list = new List<T>(secondHalf);
            _linkedList = new LinkedList<T>(secondHalf);
            _hashSet = new HashSet<T>(secondHalf);
            _queue = new Queue<T>(secondHalf);
            _stack = new Stack<T>(secondHalf);
            _sortedSet = new SortedSet<T>(secondHalf);
            _immutableArray = Immutable.ImmutableArray.CreateRange<T>(secondHalf);
            _immutableHashSet = Immutable.ImmutableHashSet.CreateRange<T>(secondHalf);
            _immutableList = Immutable.ImmutableList.CreateRange<T>(secondHalf);
            _immutableSortedSet = Immutable.ImmutableSortedSet.CreateRange<T>(secondHalf);
        }
        
        [Benchmark]
        public bool Array()
        {
            bool result = default;
            var collection = _array;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }
        
        [Benchmark]
        public bool List()
        {
            bool result = default;
            var collection = _list;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
        public bool ICollection() => Contains(_list);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool Contains(ICollection<T> collection)
        {
            bool result = default;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool LinkedList()
        {
            bool result = default;
            var collection = _linkedList;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = default;
            var collection = _hashSet;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool Queue()
        {
            bool result = default;
            var collection = _queue;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool Stack()
        {
            bool result = default;
            var collection = _stack;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = default;
            var collection = _sortedSet;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableArray()
        {
            bool result = default;
            var collection = _immutableArray;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableHashSet()
        {
            bool result = default;
            var collection = _immutableHashSet;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableList()
        {
            bool result = default;
            var collection = _immutableList;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedSet()
        {
            bool result = default;
            var collection = _immutableSortedSet;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }
    }
}