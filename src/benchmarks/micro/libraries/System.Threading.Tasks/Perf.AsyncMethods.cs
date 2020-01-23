// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

#pragma warning disable CS1998 // async methods without awaits

namespace System.Threading.Tasks.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_AsyncMethods
    {
        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task EmptyAsyncMethodInvocation()
        {
            for (int i = 0; i < 100_000; i++)
            {
                await EmptyAsync();
            }

            async Task EmptyAsync() { }
        }

        [Benchmark(OperationsPerInvoke = 1_000)]
        public async Task SingleYieldMethodInvocation()
        {
            for (int i = 0; i < 1_000; i++)
            {
                await Yield();
            }

            async Task Yield() => await Task.Yield();
        }

        [Benchmark(OperationsPerInvoke = 1_000_000)]
        public async Task Yield()
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                await Task.Yield();
            }
        }
    }
}
