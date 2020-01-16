// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public partial class Perf_ThreadPool
    {
        [Params(20_000_000)]
        public int WorkItemsPerCore;

        [Benchmark]
        public void QueueUserWorkItem_WaitCallback_Throughput()
        {
            int remaining = WorkItemsPerCore;
            var mres = new ManualResetEventSlim();

            WaitCallback wc = null;
            wc = delegate
            {
                if (Interlocked.Decrement(ref remaining) <= 0)
                {
                    mres.Set();
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(wc);
                }
            };

            for (int i = 0; i < Environment.ProcessorCount; i++)
                ThreadPool.QueueUserWorkItem(wc);
            mres.Wait();
        }
    }
}
