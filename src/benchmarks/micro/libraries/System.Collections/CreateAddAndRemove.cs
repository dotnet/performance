// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Collections
{
    // the concurrent collections are covered with benchmarks in Add_Remove_SteadyState.cs
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class CreateAddAndRemove<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private T[] _keys;

        [GlobalSetup]
        public void Setup() => _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [Benchmark]
        public List<T> List()
        {
            List<T> list = new List<T>();
            foreach (T uniqueKey in _keys)
            {
                list.Add(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                list.Remove(uniqueKey);
            }
            return list;
        }

        [Benchmark]
        public LinkedList<T> LinkedList()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            foreach (T item in _keys)
            {
                linkedList.AddLast(item);
            }
            foreach (T item in _keys)
            {
                linkedList.Remove(item);
            }
            return linkedList;
        }

        [Benchmark]
        public HashSet<T> HashSet()
        {
            HashSet<T> hashSet = new HashSet<T>();
            foreach (T uniqueKey in _keys)
            {
                hashSet.Add(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                hashSet.Remove(uniqueKey);
            }
            return hashSet;
        }

        [Benchmark]
        public Dictionary<T, T> Dictionary()
        {
            Dictionary<T, T> dictionary = new Dictionary<T, T>();
            foreach (T uniqueKey in _keys)
            {
                dictionary.Add(uniqueKey, uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                dictionary.Remove(uniqueKey);
            }
            return dictionary;
        }

        [Benchmark]
        public SortedList<T, T> SortedList()
        {
            SortedList<T, T> sortedList = new SortedList<T, T>();
            foreach (T uniqueKey in _keys)
            {
                sortedList.Add(uniqueKey, uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                sortedList.Remove(uniqueKey);
            }
            return sortedList;
        }

        [Benchmark]
        public SortedSet<T> SortedSet()
        {
            SortedSet<T> sortedSet = new SortedSet<T>();
            foreach (T uniqueKey in _keys)
            {
                sortedSet.Add(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                sortedSet.Remove(uniqueKey);
            }
            return sortedSet;
        }

        [Benchmark]
        public SortedDictionary<T, T> SortedDictionary()
        {
            SortedDictionary<T, T> sortedDictionary = new SortedDictionary<T, T>();
            foreach (T uniqueKey in _keys)
            {
                sortedDictionary.Add(uniqueKey, uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                sortedDictionary.Remove(uniqueKey);
            }
            return sortedDictionary;
        }

        [Benchmark]
        public Stack<T> Stack()
        {
            Stack<T> stack = new Stack<T>();
            foreach (T uniqueKey in _keys)
            {
                stack.Push(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                stack.Pop();
            }
            return stack;
        }

        [Benchmark]
        public Queue<T> Queue()
        {
            Queue<T> queue = new Queue<T>();
            foreach (T uniqueKey in _keys)
            {
                queue.Enqueue(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                queue.Dequeue();
            }
            return queue;
        }

        [Benchmark]
        public ImmutableArray<T> ImmutableArray()
        {
            ImmutableArray<T> immutableArray = ImmutableArray<T>.Empty;
            foreach (T uniqueKey in _keys)
            {
                immutableArray = immutableArray.Add(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                immutableArray = immutableArray.Remove(uniqueKey);
            }
            return immutableArray;
        }

        [Benchmark]
        public ImmutableList<T> ImmutableList()
        {
            ImmutableList<T> immutableList = ImmutableList<T>.Empty;
            foreach (T uniqueKey in _keys)
            {
                immutableList = immutableList.Add(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                immutableList = immutableList.Remove(uniqueKey);
            }
            return immutableList;
        }

        [Benchmark]
        public ImmutableHashSet<T> ImmutableHashSet()
        {
            ImmutableHashSet<T> immutableHashSet = ImmutableHashSet<T>.Empty;
            foreach (T uniqueKey in _keys)
            {
                immutableHashSet = immutableHashSet.Add(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                immutableHashSet = immutableHashSet.Remove(uniqueKey);
            }
            return immutableHashSet;
        }

        [Benchmark]
        public ImmutableSortedSet<T> ImmutableSortedSet()
        {
            ImmutableSortedSet<T> immutableSortedSet = ImmutableSortedSet<T>.Empty;
            foreach (T uniqueKey in _keys)
            {
                immutableSortedSet = immutableSortedSet.Add(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                immutableSortedSet = immutableSortedSet.Remove(uniqueKey);
            }
            return immutableSortedSet;
        }

        [Benchmark]
        public ImmutableDictionary<T, T> ImmutableDictionary()
        {
            ImmutableDictionary<T, T> immutableDictionary = ImmutableDictionary<T, T>.Empty;
            foreach (T uniqueKey in _keys)
            {
                immutableDictionary = immutableDictionary.Add(uniqueKey, uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                immutableDictionary = immutableDictionary.Remove(uniqueKey);
            }
            return immutableDictionary;
        }

        [Benchmark]
        public ImmutableSortedDictionary<T, T> ImmutableSortedDictionary()
        {
            ImmutableSortedDictionary<T, T> immutableSortedDictionary = ImmutableSortedDictionary<T, T>.Empty;
            foreach (T uniqueKey in _keys)
            {
                immutableSortedDictionary = immutableSortedDictionary.Add(uniqueKey, uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                immutableSortedDictionary = immutableSortedDictionary.Remove(uniqueKey);
            }
            return immutableSortedDictionary;
        }

        [Benchmark]
        public ImmutableStack<T> ImmutableStack()
        {
            ImmutableStack<T> immutableStack = ImmutableStack<T>.Empty;
            foreach (T uniqueKey in _keys)
            {
                immutableStack = immutableStack.Push(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                immutableStack = immutableStack.Pop();
            }
            return immutableStack;
        }

        [Benchmark]
        public ImmutableQueue<T> ImmutableQueue()
        {
            ImmutableQueue<T> immutableQueue = ImmutableQueue<T>.Empty;
            foreach (T uniqueKey in _keys)
            {
                immutableQueue = immutableQueue.Enqueue(uniqueKey);
            }
            foreach (T uniqueKey in _keys)
            {
                immutableQueue = immutableQueue.Dequeue();
            }
            return immutableQueue;
        }
    }
}