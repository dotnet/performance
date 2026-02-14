// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Memory.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class MemoryCacheSizeLimitContentionTests
    {
        private int _itemsPerThread;

        [Params(1, 4, 8, 16)]
        public int ThreadCount { get; set; }

        [GlobalSetup(Targets = new[] { nameof(ConcurrentSet_WithSizeLimit), nameof(ConcurrentSet_WithoutSizeLimit) })]
        public void Setup()
        {
            _itemsPerThread = 1000 / ThreadCount;
        }

        [GlobalSetup(Targets = new[] { nameof(ConcurrentSetAndGet_WithSizeLimit) })]
        public void SetupMixed()
        {
            _itemsPerThread = 500 / ThreadCount;
        }

        [Benchmark]
        public void ConcurrentSet_WithSizeLimit()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100_000 });
            var tasks = new Task[ThreadCount];

            for (int t = 0; t < ThreadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    int start = threadId * _itemsPerThread;
                    int end = start + _itemsPerThread;
                    for (int i = start; i < end; i++)
                    {
                        cache.Set(i, i, new MemoryCacheEntryOptions { Size = 1 });
                    }
                });
            }

            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void ConcurrentSet_WithoutSizeLimit()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions());
            var tasks = new Task[ThreadCount];

            for (int t = 0; t < ThreadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    int start = threadId * _itemsPerThread;
                    int end = start + _itemsPerThread;
                    for (int i = start; i < end; i++)
                    {
                        cache.Set(i, i);
                    }
                });
            }

            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void ConcurrentSetAndGet_WithSizeLimit()
        {
            using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100_000 });

            // Pre-populate half the items for reads
            for (int i = 0; i < 250; i++)
            {
                cache.Set(i, i, new MemoryCacheEntryOptions { Size = 1 });
            }

            var tasks = new Task[ThreadCount];
            for (int t = 0; t < ThreadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    int start = threadId * _itemsPerThread;
                    int end = start + _itemsPerThread;
                    for (int i = start; i < end; i++)
                    {
                        // Write a new entry
                        cache.Set(500 + i, i, new MemoryCacheEntryOptions { Size = 1 });
                        // Read an existing entry
                        cache.TryGetValue(i % 250, out _);
                    }
                });
            }

            Task.WaitAll(tasks);
        }

        [Benchmark]
        public void ConcurrentSet_HighContention()
        {
            // Small size limit forces frequent capacity checks and rejections
            using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 200 });
            var tasks = new Task[ThreadCount];

            for (int t = 0; t < ThreadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    int start = threadId * _itemsPerThread;
                    int end = start + _itemsPerThread;
                    for (int i = start; i < end; i++)
                    {
                        cache.Set(i, i, new MemoryCacheEntryOptions { Size = 10 });
                    }
                });
            }

            Task.WaitAll(tasks);
        }
    }
}
