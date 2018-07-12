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
        private T[] _values;
        
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

        [Params(100)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _values = UniqueValuesGenerator.GenerateArray<T>(Size);
            _array = _values.ToArray();
            _list = new List<T>(_values);
            _linkedlist = new LinkedList<T>(_values);
            _hashset = new HashSet<T>(_values);
            _queue = new Queue<T>(_values);
            _stack = new Stack<T>(_values);
            _sortedset = new SortedSet<T>(_values);
            _immutablearray = Immutable.ImmutableArray.CreateRange<T>(_values);
            _immutablehashset = Immutable.ImmutableHashSet.CreateRange<T>(_values);
            _immutablelist = Immutable.ImmutableList.CreateRange<T>(_values);
            _immutablesortedset = Immutable.ImmutableSortedSet.CreateRange<T>(_values);
        }
        
        [Benchmark]
        public bool Array()
        {
            bool result = default;
            var local = _array;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }
        
        [Benchmark]
        public bool List()
        {
            bool result = default;
            var local = _list;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool LinkedList()
        {
            bool result = default;
            var local = _linkedlist;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = default;
            var local = _hashset;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool Queue()
        {
            bool result = default;
            var local = _queue;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool Stack()
        {
            bool result = default;
            var local = _stack;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = default;
            var local = _sortedset;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableArray()
        {
            bool result = default;
            var local = _immutablearray;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableHashSet()
        {
            bool result = default;
            var local = _immutablehashset;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableList()
        {
            bool result = default;
            var local = _immutablelist;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedSet()
        {
            bool result = default;
            var local = _immutablesortedset;
            var found = _values;
            for (int i = 0; i < found.Length; i++)
                result = local.Contains(found[i]);
            return result;
        }
    }
}