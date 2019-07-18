// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class CreateAddAndClear<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private T[] _keys;

        [GlobalSetup]
        public void Setup() => _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [BenchmarkCategory(Categories.CoreCLR)]
        [Benchmark]
        public T[] Array()
        {
            T[] array = new T[_keys.Length];
            for (int i = 0; i < _keys.Length; i++)
            {
                array[i] = _keys[i];
            }
            System.Array.Clear(array, 0, array.Length);
            return array;
        }

        [BenchmarkCategory(Categories.CoreCLR, Categories.Span)]
        [Benchmark]
        public Span<T> Span()
        {
            Span<T> span = new T[_keys.Length];
            for (int i = 0; i < _keys.Length; i++)
            {
                span[i] = _keys[i];
            }
            span.Clear();
            return span;
        }

        [Benchmark]
        public List<T> List()
        {
            List<T> list = new List<T>();
            foreach (T uniqueKey in _keys)
            {
                list.Add(uniqueKey);
            }
            list.Clear();
            return list;
        }

        [Benchmark]
        public LinkedList<T> LinkedList()
        {
            LinkedList<T> linkedList = new LinkedList<T>(_keys);
            linkedList.Clear();
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
            hashSet.Clear();
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
            dictionary.Clear();
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
            sortedList.Clear();
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
            sortedSet.Clear();
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
            sortedDictionary.Clear();
            return sortedDictionary;
        }

        [Benchmark]
        public ConcurrentDictionary<T, T> ConcurrentDictionary()
        {
            ConcurrentDictionary<T, T> concurrentDictionary = new ConcurrentDictionary<T, T>();
            foreach (T uniqueKey in _keys)
            {
                concurrentDictionary.TryAdd(uniqueKey, uniqueKey);
            }
            concurrentDictionary.Clear();
            return concurrentDictionary;
        }

        [Benchmark]
        public Stack<T> Stack()
        {
            Stack<T> stack = new Stack<T>();
            foreach (T uniqueKey in _keys)
            {
                stack.Push(uniqueKey);
            }
            stack.Clear();
            return stack;
        }

        [Benchmark]
        public ConcurrentStack<T> ConcurrentStack()
        {
            ConcurrentStack<T> stack = new ConcurrentStack<T>();
            foreach (T uniqueKey in _keys)
            {
                stack.Push(uniqueKey);
            }
            stack.Clear();
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
            queue.Clear();
            return queue;
        }

#if !NETFRAMEWORK // API added in .NET Core 2.0
        [Benchmark]
        public ConcurrentQueue<T> ConcurrentQueue()
        {
            ConcurrentQueue<T> concurrentQueue = new ConcurrentQueue<T>();
            foreach (T uniqueKey in _keys)
            {
                concurrentQueue.Enqueue(uniqueKey);
            }
            concurrentQueue.Clear();
            return concurrentQueue;
        }
#endif
    }
}