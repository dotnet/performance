// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;

namespace Microsoft.Extensions.Caching.Memory.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class MemoryCacheTests
    {
        private MemoryCache _memCache;
        private (object key, object value)[] _items;

        [GlobalSetup(Targets = new[] { nameof(GetHit), nameof(TryGetValueHit), nameof(GetMiss), nameof(TryGetValueMiss), nameof(SetOverride), nameof(CreateEntry) })]
        public void SetupBasic()
        {
            _memCache = new MemoryCache(new MemoryCacheOptions());
            for (var i = 0; i < 1024; i++)
            {
                _memCache.Set(i.ToString(), i.ToString());
            }
        }

        [GlobalCleanup(Targets = new[] { nameof(GetHit), nameof(TryGetValueHit), nameof(GetMiss), nameof(TryGetValueMiss), nameof(SetOverride), nameof(CreateEntry) })]
        public void CleanupBasic() => _memCache.Dispose();

        [Benchmark]
        public object GetHit() => _memCache.Get("256");

        [Benchmark]
        public bool TryGetValueHit() => _memCache.TryGetValue("256", out _);

        [Benchmark]
        public object GetMiss() => _memCache.Get("-1");

        [Benchmark]
        public bool TryGetValueMiss() => _memCache.TryGetValue("-1", out _);

        [Benchmark]
        public object SetOverride() => _memCache.Set("512", "512");

        [Benchmark]
        public void CreateEntry() => _memCache.CreateEntry(this).Dispose();

        [GlobalSetup(Targets = new[] { nameof(AddThenRemove_NoExpiration), nameof(AddThenRemove_AbsoluteExpiration), nameof(AddThenRemove_RelativeExpiration), nameof(AddThenRemove_SlidingExpiration), nameof(AddThenRemove_ExpirationTokens) })]
        public void Setup_AddThenRemove()
        {
            _items = ValuesGenerator.ArrayOfUniqueValues<int>(100).Select(x => ((object)x.ToString(), (object)x.ToString())).ToArray();
        }

        [Benchmark]
        public void AddThenRemove_NoExpiration()
        {
            using (MemoryCache cache = new MemoryCache(new MemoryCacheOptions()))
            {
                foreach (var item in _items)
                {
                    using (ICacheEntry entry = cache.CreateEntry(item.key))
                    {
                        entry.Value = item.value;
                    } // entry.Dispose is adding it to the cache
                }

                foreach (var item in _items)
                {
                    cache.Remove(item.key);
                }
            }
        }

        [Benchmark]
        public void AddThenRemove_AbsoluteExpiration()
        {
            DateTimeOffset absolute = DateTimeOffset.UtcNow.AddHours(1);

            using (MemoryCache cache = new MemoryCache(new MemoryCacheOptions()))
            {
                foreach (var item in _items)
                {
                    using (ICacheEntry entry = cache.CreateEntry(item.key))
                    {
                        entry.AbsoluteExpiration = absolute;
                        entry.Value = item.value;
                    }
                }

                foreach (var item in _items)
                {
                    cache.Remove(item.key);
                }
            }
        }

        [Benchmark]
        public void AddThenRemove_RelativeExpiration()
        {
            TimeSpan relative = TimeSpan.FromHours(1);

            using (MemoryCache cache = new MemoryCache(new MemoryCacheOptions()))
            {
                foreach (var item in _items)
                {
                    using (ICacheEntry entry = cache.CreateEntry(item.key))
                    {
                        entry.AbsoluteExpirationRelativeToNow = relative;
                        entry.Value = item.value;
                    }
                }

                foreach (var item in _items)
                {
                    cache.Remove(item.key);
                }
            }
        }

        [Benchmark]
        public void AddThenRemove_SlidingExpiration()
        {
            TimeSpan relative = TimeSpan.FromHours(1);

            using (MemoryCache cache = new MemoryCache(new MemoryCacheOptions()))
            {
                foreach (var item in _items)
                {
                    using (ICacheEntry entry = cache.CreateEntry(item.key))
                    {
                        entry.SlidingExpiration = relative;
                        entry.Value = item.value;
                    }
                }

                foreach (var item in _items)
                {
                    cache.Remove(item.key);
                }
            }
        }

        [Benchmark]
        public void AddThenRemove_ExpirationTokens()
        {
            Token token = new Token();

            using (MemoryCache cache = new MemoryCache(new MemoryCacheOptions()))
            {
                foreach (var item in _items)
                {
                    using (ICacheEntry entry = cache.CreateEntry(item.key))
                    {
                        entry.ExpirationTokens.Add(token);
                        entry.Value = item.value;
                    }
                }

                foreach (var item in _items)
                {
                    cache.Remove(item.key);
                }
            }
        }

        private sealed class Token : IChangeToken, IDisposable
        {
            public bool HasChanged => false;

            public bool ActiveChangeCallbacks => false;

            public void Dispose() { }

            public IDisposable RegisterChangeCallback(Action<object> callback, object state) => this;
        }
    }
}
