// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class ContainsFalse<T> 
        where T : IEquatable<T>
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
            T[] collection = _array;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

#if !NETFRAMEWORK && !NETCOREAPP2_1
        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public bool Span()
        {
            bool result = default;
            Span<T> collection = new Span<T>(_array);
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }
#endif

        [Benchmark]
        public bool List()
        {
            bool result = default;
            List<T> collection = _list;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.Runtime, Categories.Virtual)]
        public bool ICollection() => Contains(_list);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool Contains(ICollection<T> collection)
        {
            bool result = default;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool LinkedList()
        {
            bool result = default;
            LinkedList<T> collection = _linkedList;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = default;
            HashSet<T> collection = _hashSet;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool Queue()
        {
            bool result = default;
            Queue<T> collection = _queue;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool Stack()
        {
            bool result = default;
            Stack<T> collection = _stack;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = default;
            SortedSet<T> collection = _sortedSet;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableArray()
        {
            bool result = default;
            ImmutableArray<T> collection = _immutableArray;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableHashSet()
        {
            bool result = default;
            ImmutableHashSet<T> collection = _immutableHashSet;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableList()
        {
            bool result = default;
            ImmutableList<T> collection = _immutableList;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedSet()
        {
            bool result = default;
            ImmutableSortedSet<T> collection = _immutableSortedSet;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }
    }
}