// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public class ContainsTrue<T>
    {
        private T[] _found;
        
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
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _array = _found.ToArray();
            _list = new List<T>(_found);
            _linkedList = new LinkedList<T>(_found);
            _hashSet = new HashSet<T>(_found);
            _queue = new Queue<T>(_found);
            _stack = new Stack<T>(_found);
            _sortedSet = new SortedSet<T>(_found);
            _immutableArray = Immutable.ImmutableArray.CreateRange<T>(_found);
            _immutableHashSet = Immutable.ImmutableHashSet.CreateRange<T>(_found);
            _immutableList = Immutable.ImmutableList.CreateRange<T>(_found);
            _immutableSortedSet = Immutable.ImmutableSortedSet.CreateRange<T>(_found);
        }
        
        [Benchmark]
        public bool Array()
        {
            bool result = default;
            var collection = _array;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }
        
        [Benchmark]
        public bool List()
        {
            bool result = default;
            var collection = _list;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
        public bool ICollection() => Contains(_list);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool Contains(ICollection<T> collection)
        {
            bool result = default;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool LinkedList()
        {
            bool result = default;
            var collection = _linkedList;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = default;
            var collection = _hashSet;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool Queue()
        {
            bool result = default;
            var collection = _queue;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool Stack()
        {
            bool result = default;
            var collection = _stack;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = default;
            var collection = _sortedSet;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableArray()
        {
            bool result = default;
            var collection = _immutableArray;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableHashSet()
        {
            bool result = default;
            var collection = _immutableHashSet;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableList()
        {
            bool result = default;
            var collection = _immutableList;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedSet()
        {
            bool result = default;
            var collection = _immutableSortedSet;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }
    }
}