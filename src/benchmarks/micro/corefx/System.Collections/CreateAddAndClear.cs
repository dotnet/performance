// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
        [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
        public ICollection<T> ICollection() => ICollection(new List<T>());

        [MethodImpl(MethodImplOptions.NoInlining)] // we want to prevent from inlining this particular method to make sure that JIT does not find out that ICollection is always List
        private ICollection<T> ICollection(ICollection<T> collection)
        {
            foreach (T uniqueKey in _keys)
            {
                collection.Add(uniqueKey);
            }
            collection.Clear();
            return collection;
        }

        [Benchmark]
        public LinkedList<T> LinkedList()
        {
            LinkedList<T> linkedList = new LinkedList<T>();
            foreach (T uniqueKey in _keys)
            {
                linkedList.AddLast(uniqueKey);
            }
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
        [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
        public IDictionary<T, T> IDictionary() => IDictionary(new Dictionary<T, T>());

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IDictionary<T, T> IDictionary(IDictionary<T, T> dictionary)
        {
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

        [Benchmark]
        public ConcurrentBag<T> ConcurrentBag()
        {
            ConcurrentBag<T> concurrentBag = new ConcurrentBag<T>();
            foreach (T uniqueKey in _keys)
            {
                concurrentBag.Add(uniqueKey);
            }
            concurrentBag.Clear();
            return concurrentBag;
        }
#endif
    }
}