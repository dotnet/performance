// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class Add_Remove_SteadyState<T> // serialized producer/consumer throughput when the collection has reached a steady-state size
    {
        private ConcurrentBag<T> _concurrentBag;
        private ConcurrentQueue<T> _concurrentQueue;
        private ConcurrentStack<T> _concurrentStack;
        private Queue<T> _queue;
        private Stack<T> _stack;

        [Params(Utils.DefaultCollectionSize)]
        public int Count;

        [GlobalSetup]
        public void Setup()
        {
            T[] uniqueValues = ValuesGenerator.ArrayOfUniqueValues<T>(Count);
            _concurrentBag = new ConcurrentBag<T>(uniqueValues);
            _concurrentQueue = new ConcurrentQueue<T>(uniqueValues);
            _concurrentStack = new ConcurrentStack<T>(uniqueValues);
            _queue = new Queue<T>(uniqueValues);
            _stack = new Stack<T>(uniqueValues);
        }

        [Benchmark]
        public void ConcurrentBag()
        {
            _concurrentBag.TryTake(out T item);
            _concurrentBag.Add(item);
        }

        [Benchmark]
        public void ConcurrentQueue()
        {
            _concurrentQueue.TryDequeue(out T item);
            _concurrentQueue.Enqueue(item);
        }

        [Benchmark]
        public void ConcurrentStack()
        {
            _concurrentStack.TryPop(out T item);
            _concurrentStack.Push(item);
        }

        [Benchmark]
        public void Queue()
        {
            T item = _queue.Dequeue();
            _queue.Enqueue(item);
        }

        [Benchmark]
        public void Stack()
        {
            T item = _stack.Pop();
            _stack.Push(item);
        }
    }
}
