// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.Collections.Generic;

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

#if NET9_0_OR_GREATER
        [Benchmark]
        public OrderedDictionary<T, T> OrderedDictionary()
        {
            OrderedDictionary<T, T> orderedDictionary = new OrderedDictionary<T, T>();
            foreach (T key in _keys)
            {
                orderedDictionary.Add(key, key);
            }
            foreach (T key in _keys)
            {
                orderedDictionary.Remove(key);
            }
            return orderedDictionary;
        }
#endif
    }
}