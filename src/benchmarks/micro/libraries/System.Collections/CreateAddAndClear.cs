// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class CreateAddAndClear<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private T[] _uniqueValues;

        [GlobalSetup]
        public void Setup() => _uniqueValues = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [BenchmarkCategory(Categories.Runtime)]
        [Benchmark]
        public T[] Array()
        {
            T[] array = new T[_uniqueValues.Length];
            for (int i = 0; i < _uniqueValues.Length; i++)
            {
                array[i] = _uniqueValues[i];
            }
            System.Array.Clear(array, 0, array.Length);
            return array;
        }

        [BenchmarkCategory(Categories.Runtime, Categories.Span)]
        [Benchmark]
        public Span<T> Span()
        {
            Span<T> span = new T[_uniqueValues.Length];
            for (int i = 0; i < _uniqueValues.Length; i++)
            {
                span[i] = _uniqueValues[i];
            }
            span.Clear();
            return span;
        }

        [Benchmark]
        public List<T> List()
        {
            List<T> list = new List<T>();
            foreach (T value in _uniqueValues)
            {
                list.Add(value);
            }
            list.Clear();
            return list;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.Runtime, Categories.Virtual)]
        public ICollection<T> ICollection() => ICollection(new List<T>());

        [MethodImpl(MethodImplOptions.NoInlining)] // we want to prevent from inlining this particular method to make sure that JIT does not find out that ICollection is always List
        private ICollection<T> ICollection(ICollection<T> collection)
        {
            foreach (T value in _uniqueValues)
            {
                collection.Add(value);
            }
            collection.Clear();
            return collection;
        }

        [Benchmark]
        public LinkedList<T> LinkedList()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            foreach (T value in _uniqueValues)
            {
                linkedList.AddLast(value);
            }
            linkedList.Clear();
            return linkedList;
        }

        [Benchmark]
        public HashSet<T> HashSet()
        {
            HashSet<T> hashSet = new HashSet<T>();
            foreach (T value in _uniqueValues)
            {
                hashSet.Add(value);
            }
            hashSet.Clear();
            return hashSet;
        }

        [Benchmark]
        public Dictionary<T, T> Dictionary()
        {
            Dictionary<T, T> dictionary = new Dictionary<T, T>();
            foreach (T value in _uniqueValues)
            {
                dictionary.Add(value, value);
            }
            dictionary.Clear();
            return dictionary;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.Runtime, Categories.Virtual)]
        public IDictionary<T, T> IDictionary() => IDictionary(new Dictionary<T, T>());

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IDictionary<T, T> IDictionary(IDictionary<T, T> dictionary)
        {
            foreach (T value in _uniqueValues)
            {
                dictionary.Add(value, value);
            }
            dictionary.Clear();
            return dictionary;
        }

        [Benchmark]
        public SortedList<T, T> SortedList()
        {
            SortedList<T, T> sortedList = new SortedList<T, T>();
            foreach (T value in _uniqueValues)
            {
                sortedList.Add(value, value);
            }
            sortedList.Clear();
            return sortedList;
        }

        [Benchmark]
        public SortedSet<T> SortedSet()
        {
            SortedSet<T> sortedSet = new SortedSet<T>();
            foreach (T value in _uniqueValues)
            {
                sortedSet.Add(value);
            }
            sortedSet.Clear();
            return sortedSet;
        }

        [Benchmark]
        public SortedDictionary<T, T> SortedDictionary()
        {
            SortedDictionary<T, T> sortedDictionary = new SortedDictionary<T, T>();
            foreach (T value in _uniqueValues)
            {
                sortedDictionary.Add(value, value);
            }
            sortedDictionary.Clear();
            return sortedDictionary;
        }

        [Benchmark]
        public ConcurrentDictionary<T, T> ConcurrentDictionary()
        {
            ConcurrentDictionary<T, T> concurrentDictionary = new ConcurrentDictionary<T, T>();
            foreach (T value in _uniqueValues)
            {
                concurrentDictionary.TryAdd(value, value);
            }
            concurrentDictionary.Clear();
            return concurrentDictionary;
        }

        [Benchmark]
        public Stack<T> Stack()
        {
            Stack<T> stack = new Stack<T>();
            foreach (T value in _uniqueValues)
            {
                stack.Push(value);
            }
            stack.Clear();
            return stack;
        }

        [Benchmark]
        public ConcurrentStack<T> ConcurrentStack()
        {
            ConcurrentStack<T> stack = new ConcurrentStack<T>();
            foreach (T value in _uniqueValues)
            {
                stack.Push(value);
            }
            stack.Clear();
            return stack;
        }

        [Benchmark]
        public Queue<T> Queue()
        {
            Queue<T> queue = new Queue<T>();
            foreach (T value in _uniqueValues)
            {
                queue.Enqueue(value);
            }
            queue.Clear();
            return queue;
        }

#if !NETFRAMEWORK // API added in .NET Core 2.0
        [Benchmark]
        public ConcurrentQueue<T> ConcurrentQueue()
        {
            ConcurrentQueue<T> concurrentQueue = new ConcurrentQueue<T>();
            foreach (T value in _uniqueValues)
            {
                concurrentQueue.Enqueue(value);
            }
            concurrentQueue.Clear();
            return concurrentQueue;
        }

        [Benchmark]
        public ConcurrentBag<T> ConcurrentBag()
        {
            ConcurrentBag<T> concurrentBag = new ConcurrentBag<T>();
            foreach (T value in _uniqueValues)
            {
                concurrentBag.Add(value);
            }
            concurrentBag.Clear();
            return concurrentBag;
        }
#endif

        [Benchmark]
        public ImmutableArray<T> ImmutableArray()
        {
            ImmutableArray<T> immutableArray = ImmutableArray<T>.Empty;
            foreach (T value in _uniqueValues)
            {
                immutableArray = immutableArray.Add(value);
            }
            return immutableArray.Clear();
        }

        [Benchmark]
        public ImmutableList<T> ImmutableList()
        {
            ImmutableList<T> immutableList = ImmutableList<T>.Empty;
            foreach (T value in _uniqueValues)
            {
                immutableList = immutableList.Add(value);
            }
            return immutableList.Clear();
        }

        [Benchmark]
        public ImmutableDictionary<T, T> ImmutableDictionary()
        {
            ImmutableDictionary<T, T> immutableDictionary = ImmutableDictionary<T, T>.Empty;
            foreach (T value in _uniqueValues)
            {
                immutableDictionary = immutableDictionary.Add(value, value);
            }
            return immutableDictionary.Clear();
        }

        [Benchmark]
        public ImmutableHashSet<T> ImmutableHashSet()
        {
            ImmutableHashSet<T> immutableHashSet = ImmutableHashSet<T>.Empty;
            foreach (T value in _uniqueValues)
            {
                immutableHashSet = immutableHashSet.Add(value);
            }
            return immutableHashSet.Clear();
        }

        [Benchmark]
        public ImmutableSortedDictionary<T, T> ImmutableSortedDictionary()
        {
            ImmutableSortedDictionary<T, T> immutableSortedDictionary = ImmutableSortedDictionary<T, T>.Empty;
            foreach (T value in _uniqueValues)
            {
                immutableSortedDictionary = immutableSortedDictionary.Add(value, value);
            }
            return immutableSortedDictionary.Clear();
        }

        [Benchmark]
        public ImmutableSortedSet<T> ImmutableSortedSet()
        {
            ImmutableSortedSet<T> immutableSortedSet = ImmutableSortedSet<T>.Empty;
            foreach (T value in _uniqueValues)
            {
                immutableSortedSet = immutableSortedSet.Add(value);
            }
            return immutableSortedSet.Clear();
        }

        [Benchmark]
        public ImmutableStack<T> ImmutableStack()
        {
            ImmutableStack<T> immutableStack = ImmutableStack<T>.Empty;
            foreach (T value in _uniqueValues)
            {
                immutableStack = immutableStack.Push(value);
            }
            return immutableStack.Clear();
        }

        [Benchmark]
        public ImmutableQueue<T> ImmutableQueue()
        {
            ImmutableQueue<T> immutableQueue = ImmutableQueue<T>.Empty;
            foreach (T value in _uniqueValues)
            {
                immutableQueue = immutableQueue.Enqueue(value);
            }
            return immutableQueue.Clear();
        }

#if NET9_0_OR_GREATER
        [Benchmark]
        public OrderedDictionary<T, T> OrderedDictionary()
        {
            OrderedDictionary<T, T> orderedDictionary = new OrderedDictionary<T, T>();
            foreach (T value in _uniqueValues)
            {
                orderedDictionary.Add(value, value);
            }
            orderedDictionary.Clear();
            return orderedDictionary;
        }
#endif
    }
}