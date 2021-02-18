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
    public abstract class Perf_PriorityQueue<TElement, TPriority>
    {
        [Params(10, 100, 1000, 10_000, 100_000)]
        public int Size;

        private (TElement Element, TPriority Priority)[] _items;
        private PriorityQueue<TElement, TPriority> _priorityQueue;
        private PriorityQueue<TElement, TPriority> _prePopulatedPriorityQueue;

        public abstract IEnumerable<(TElement Element, TPriority Priority)> GenerateItems(int count);

        [GlobalSetup]
        public void Setup()
        {
            _items = GenerateItems(Size).ToArray();
            //var random = new Random(42);
            //_elements = new int[Size];
            //for (int i = 0; i < Size; i++)
            //{
            //    _elements[i] = random.Next();
            //}

            //_items = _elements.Select((i, x) => (i, x)).ToArray();
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
    }

    public class Perf_PriorityQueue_String_String : Perf_PriorityQueue<string, string>
    {
        public override IEnumerable<(string Element, string Priority)> GenerateItems(int count)
        {
            var random = new Random(42);
            const int MaxSize = 30;
            byte[] buffer = new byte[MaxSize];

            for (int i = 0; i < count; i++)
            {
                yield return (GenerateString(), GenerateString());
            }

            string GenerateString()
            {
                int size = random.Next(MaxSize);
                Span<byte> slice = buffer.AsSpan().Slice(size);
                random.NextBytes(slice);
                return Convert.ToBase64String(slice);
            }
        }
    }

    public class Perf_PriorityQueue_Int_Int : Perf_PriorityQueue<int, int>
    {
        public override IEnumerable<(int Element, int Priority)> GenerateItems(int count)
        {
            var random = new Random(42);
            for (int i = 0; i < count; i++)
            {
                yield return (random.Next(), random.Next());
            }
        }
    }
}
