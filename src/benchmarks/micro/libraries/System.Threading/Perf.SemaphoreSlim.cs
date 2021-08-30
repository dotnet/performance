// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Threading.Tasks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_SemaphoreSlim
    {
        private readonly SemaphoreSlim _sem = new SemaphoreSlim(0, 1);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        [Benchmark]
        public void ReleaseWait()
        {
            _sem.Release();
            _sem.Wait();
        }

        [Benchmark]
        public Task ReleaseWaitAsync()
        {
            _sem.Release();
            return _sem.WaitAsync();
        }

        [Benchmark]
        public Task ReleaseWaitAsync_WithCancellationToken()
        {
            Task t = _sem.WaitAsync(_cts.Token);
            _sem.Release();
            return t;
        }

        [Benchmark]
        public Task ReleaseWaitAsync_WithTimeout()
        {
            Task t = _sem.WaitAsync(TimeSpan.FromDays(1));
            _sem.Release();
            return t;
        }

        [Benchmark]
        public Task ReleaseWaitAsync_WithCancellationTokenAndTimeout()
        {
            Task t = _sem.WaitAsync(TimeSpan.FromDays(1), _cts.Token);
            _sem.Release();
            return t;
        }
    }
}
