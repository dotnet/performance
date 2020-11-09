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

        [GlobalSetup(Target = nameof(ConcurrentBag))]
        public void SetupConcurrentBag() => _concurrentBag = new ConcurrentBag<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Count));

        [Benchmark]
        public void ConcurrentBag()
        {
            _concurrentBag.TryTake(out T item);
            _concurrentBag.Add(item);
        }

        [GlobalSetup(Target = nameof(ConcurrentQueue))]
        public void SetupConcurrentQueue() => _concurrentQueue = new ConcurrentQueue<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Count));

        [Benchmark]
        public void ConcurrentQueue()
        {
            _concurrentQueue.TryDequeue(out T item);
            _concurrentQueue.Enqueue(item);
        }

        [GlobalSetup(Target = nameof(ConcurrentStack))]
        public void SetupConcurrentStack() => _concurrentStack = new ConcurrentStack<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Count));

        [Benchmark]
        public void ConcurrentStack()
        {
            _concurrentStack.TryPop(out T item);
            _concurrentStack.Push(item);
        }

        [GlobalSetup(Target = nameof(Queue))]
        public void SetupQueue() => _queue = new Queue<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Count));

        [Benchmark]
        public void Queue()
        {
            T item = _queue.Dequeue();
            _queue.Enqueue(item);
        }

        [GlobalSetup(Target = nameof(Stack))]
        public void SetupStack() => _stack = new Stack<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Count));

        [Benchmark]
        public void Stack()
        {
            T item = _stack.Pop();
            _stack.Push(item);
        }
    }
}
