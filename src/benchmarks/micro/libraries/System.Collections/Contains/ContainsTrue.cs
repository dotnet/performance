// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Frozen;
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
    public class ContainsTrue<T>
        where T : IEquatable<T>
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
        private FrozenSet<T> _frozenSet;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup(Targets = new[] { nameof(Array), "Span" })]
        public void SetupArray()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _array = _found.ToArray();
        }

        [Benchmark]
        public bool Array()
        {
            bool result = default;
            T[] collection = _array;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

#if !NETFRAMEWORK
        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public bool Span()
        {
            bool result = default;
            Span<T> collection = new Span<T>(_array);
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }
#endif
        [GlobalSetup(Targets = new[] { nameof(List), nameof(ICollection) })]
        public void SetupList()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _list = new List<T>(_found);
        }

        [Benchmark]
        public bool List()
        {
            bool result = default;
            List<T> collection = _list;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.Runtime, Categories.Virtual)]
        public bool ICollection() => Contains(_list);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool Contains(ICollection<T> collection)
        {
            bool result = default;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(LinkedList))]
        public void SetupLinkedList()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _linkedList = new LinkedList<T>(_found);
        }

        [Benchmark]
        public bool LinkedList()
        {
            bool result = default;
            LinkedList<T> collection = _linkedList;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(HashSet))]
        public void SetupHashSet()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _hashSet = new HashSet<T>(_found);
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = default;
            HashSet<T> collection = _hashSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(Queue))]
        public void SetupQueue()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _queue = new Queue<T>(_found);
        }

        [Benchmark]
        public bool Queue()
        {
            bool result = default;
            Queue<T> collection = _queue;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(Stack))]
        public void SetupStack()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _stack = new Stack<T>(_found);
        }

        [Benchmark]
        public bool Stack()
        {
            bool result = default;
            Stack<T> collection = _stack;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(SortedSet))]
        public void SetupSortedSet()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _sortedSet = new SortedSet<T>(_found);
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = default;
            SortedSet<T> collection = _sortedSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableArray))]
        public void SetupImmutableArray()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _immutableArray = Immutable.ImmutableArray.CreateRange<T>(_found);
        }

        [Benchmark]
        public bool ImmutableArray()
        {
            bool result = default;
            ImmutableArray<T> collection = _immutableArray;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableHashSet))]
        public void SetupImmutableHashSet()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _immutableHashSet = Immutable.ImmutableHashSet.CreateRange<T>(_found);
        }

        [Benchmark]
        public bool ImmutableHashSet()
        {
            bool result = default;
            ImmutableHashSet<T> collection = _immutableHashSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableList))]
        public void SetupImmutableList()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _immutableList = Immutable.ImmutableList.CreateRange<T>(_found);
        }

        [Benchmark]
        public bool ImmutableList()
        {
            bool result = default;
            ImmutableList<T> collection = _immutableList;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableSortedSet))]
        public void SetupImmutableSortedSet()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _immutableSortedSet = Immutable.ImmutableSortedSet.CreateRange<T>(_found);
        }

        [Benchmark]
        public bool ImmutableSortedSet()
        {
            bool result = default;
            ImmutableSortedSet<T> collection = _immutableSortedSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(FrozenSet))]
        public void SetupFrozenSet()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _frozenSet = _found.ToFrozenSet(optimizeForReading: false);
        }

        [Benchmark]
        public bool FrozenSet() => FrozenSetInternal();

        [GlobalSetup(Target = nameof(FrozenSetOptimized))]
        public void SetupFrozenSetOptimized()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _frozenSet = _found.ToFrozenSet(optimizeForReading: true);
        }

        [Benchmark]
        public bool FrozenSetOptimized() => FrozenSetInternal();

        private bool FrozenSetInternal()
        {
            bool result = default;
            FrozenSet<T> collection = _frozenSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }
    }
}