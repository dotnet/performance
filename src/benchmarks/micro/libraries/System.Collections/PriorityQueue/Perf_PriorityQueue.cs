// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private PriorityQueue<int, int> _priorityQueue;

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            _priorities = new int[Size];
            for (int i = 0; i < Size; i++)
            {
                _priorities[i] = random.Next();
            }

            _priorityQueue = new PriorityQueue<int, int>(initialCapacity: Size);
        }

        [Benchmark]
        public void HeapSort()
        {
            var queue = _priorityQueue;
            var priorities = _priorities;

            for (int i = 0; i < Size; i++)
            {
                queue.Enqueue(i, priorities[i]);
            }

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
