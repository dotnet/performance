// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int), typeof(int))]
    [GenericTypeArguments(typeof(string), typeof(string))]
    [GenericTypeArguments(typeof(Guid), typeof(Guid))]
    public class Perf_PriorityQueue<TElement, TPriority>
    {
        [Params(10, 100, 1000)]
        public int Size;

        private (TElement Element, TPriority Priority)[] _items;
        private PriorityQueue<TElement, TPriority> _priorityQueue;
        private PriorityQueue<TElement, TPriority> _prePopulatedPriorityQueue;

        [GlobalSetup]
        public void Setup()
        {
            _items = ValuesGenerator.Array<TElement>(Size).Zip(ValuesGenerator.Array<TPriority>(Size)).ToArray();
            _priorityQueue = new PriorityQueue<TElement, TPriority>(initialCapacity: Size);
            _prePopulatedPriorityQueue = new PriorityQueue<TElement, TPriority>(_items);
        }

        [Benchmark]
        public void HeapSort()
        {
            var queue = _priorityQueue;
            queue.EnqueueRange(_items);

            while (queue.Count > 0)
            {
                queue.Dequeue();
            }
        }

        [Benchmark]
        public void Enumerate()
        {
            foreach (var _ in _prePopulatedPriorityQueue.UnorderedItems)
            {

            }
        }

        [Benchmark]
        public void Dequeue_And_Enqueue()
        {
            // benchmarks dequeue and enqueue operations
            // for heaps of fixed size.

            var queue = _priorityQueue;
            var items = _items;

            // populate the heap: incorporated in the 
            // benchmark to achieve determinism.
            foreach ((TElement element, TPriority priority) in items)
            {
                queue.Enqueue(element, priority);
            }

            foreach ((TElement element, TPriority priority) in items)
            {
                queue.Dequeue();
                queue.Enqueue(element, priority);
            }

            // Drain the heap
            while (queue.Count > 0)
            {
                queue.Dequeue();
            }
        }

        [Benchmark]
        public void K_Max_Elements()
        {
            const int k = 5;
            var queue = _priorityQueue;
            var items = _items;

            for (int i = 0; i < k; i++)
            {
                (TElement element, TPriority priority) = items[i];
                queue.Enqueue(element, priority);
            }

            for (int i = k; i < Size; i++)
            {
                (TElement element, TPriority priority) = items[i];
                queue.EnqueueDequeue(element, priority);
            }

            for (int i = 0; i < k; i++)
            {
                queue.Dequeue();
            }
        }

        [Benchmark]
        public void DequeueEnqueue()
        {
            const int k = 5;
            var queue = _priorityQueue;
            var items = _items;

            for (int i = 0; i < k; i++)
            {
                (TElement element, TPriority priority) = items[i];
                queue.Enqueue(element, priority);
            }

            for (int i = k; i < Size; i++)
            {
                (TElement element, TPriority priority) = items[i];
                queue.DequeueEnqueue(element, priority);
            }

            for (int i = 0; i < k; i++)
            {
                queue.Dequeue();
            }
        }
    }
}
