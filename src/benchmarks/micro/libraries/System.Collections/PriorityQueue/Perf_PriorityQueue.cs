// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    public class Perf_PriorityQueue
    {
        [Params(10, 100, 1000, 10_000, 100_000)]
        public int Size;

        private int[] _priorities;
        private (int Element, int Priority)[] _elements;
        private PriorityQueue<int, int> _priorityQueue;
        private PriorityQueue<int, int> _populatedPriorityQueue;

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            _priorities = new int[Size];
            for (int i = 0; i < Size; i++)
            {
                _priorities[i] = random.Next();
            }

            _elements = _priorities.Select((i, x) => (i, x)).ToArray();
            _priorityQueue = new PriorityQueue<int, int>(initialCapacity: Size);
            _populatedPriorityQueue = new PriorityQueue<int, int>(_elements);
        }

        [Benchmark]
        public void HeapSort()
        {
            var queue = _priorityQueue;
            queue.EnqueueRange(_elements);

            while (queue.Count > 0)
            {
                queue.Dequeue();
            }
        }

        [Benchmark]
        public void Enumerate()
        {
            foreach (var _ in _populatedPriorityQueue.UnorderedItems)
            {

            }
        }

        [Benchmark]
        public void Dequeue_And_Enqueue()
        {
            // benchmarks dequeue and enqueue operations
            // for heaps of fixed size.

            var queue = _priorityQueue;
            var priorities = _priorities;

            // populate the heap: incorporated in the 
            // benchmark to achieve determinism.
            for (int i = 0; i < Size; i++)
            {
                queue.Enqueue(i, priorities[i]);
            }

            for (int i = 0; i < Size; i++)
            {
                queue.Dequeue();
                queue.Enqueue(i, priorities[i]);
            }

            // Drain the heap
            while (queue.Count > 0)
            {
                queue.Dequeue();
            }
        }

        [Benchmark]
        public void K_Min_Elements()
        {
            const int k = 5;
            var queue = _priorityQueue;
            var priorities = _priorities;

            for (int i = 0; i < k; i++)
            {
                queue.Enqueue(i, _priorities[i]);
            }

            for (int i = k; i < Size; i++)
            {
                queue.EnqueueDequeue(i, _priorities[i]);
            }

            for (int i = 0; i < k; i++)
            {
                queue.Dequeue();
            }
        }
    }
}
