// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tests
{
    /// <summary>
    /// Measures ThreadPool scheduling efficiency by distributing a fixed total
    /// amount of compute (TotalWorkItems × IterationsPerItem FP divisions) across
    /// threads and comparing against a single-threaded baseline.
    ///
    /// Ideal parallel time ≈ Sequential / ProcessorCount.
    /// Efficiency = (Sequential / ProcessorCount) / ActualParallelTime.
    ///
    /// Varying IterationsPerItem reveals the crossover between scheduling-dominated
    /// (small work items → high overhead) and compute-dominated (large items →
    /// near-linear scaling) regimes.
    /// </summary>
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_ThreadPool_Scheduling
    {
        [Params(10_000, 100_000)]
        public int TotalWorkItems { get; set; }

        [Params(100, 1_000, 10_000)]
        public int IterationsPerItem { get; set; }

        /// <summary>
        /// Fixed-cost work kernel using reciprocal FP divisions that nearly cancel
        /// out, keeping the value stable (~123456789.0) without drifting toward 0 or ∞.
        /// Each loop iteration performs 2 dependent divisions, creating a tight
        /// data-dependency chain that the CPU cannot parallelise.
        /// NoInlining prevents cross-method optimisation; returning the value keeps
        /// the computation live.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static double DoWork(int iterations)
        {
            double divd = 123456789.0;
            for (int i = 0; i < iterations; i++)
            {
                divd /= 1.0000001;
                divd /= 0.9999999;
            }
            return divd;
        }

        /// <summary>
        /// Baseline: run all work items sequentially on one thread.
        /// </summary>
        [Benchmark(Baseline = true)]
        public double Sequential()
        {
            double sum = 0;
            for (int i = 0; i < TotalWorkItems; i++)
                sum += DoWork(IterationsPerItem);
            return sum;
        }

        /// <summary>
        /// Queue every work item to the ThreadPool at once, then wait.
        /// Stresses the global queue under a large burst of incoming work.
        /// Uses UnsafeQueueUserWorkItem to avoid ExecutionContext capture overhead.
        /// </summary>
        [Benchmark]
        public void ThreadPool_BatchQueue()
        {
            int remaining = TotalWorkItems;
            using var done = new ManualResetEventSlim();

            for (int i = 0; i < TotalWorkItems; i++)
            {
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    DoWork(IterationsPerItem);
                    if (Interlocked.Decrement(ref remaining) == 0)
                        done.Set();
                }, null);
            }

            done.Wait();
        }

        /// <summary>
        /// Self-scheduling: seed ProcessorCount callbacks, each of which processes
        /// one item then re-queues itself. Keeps queue depth ≈ ProcessorCount,
        /// reducing global-queue contention.
        /// </summary>
        [Benchmark]
        public void ThreadPool_SelfScheduling()
        {
            int remaining = TotalWorkItems;
            using var done = new ManualResetEventSlim();

            WaitCallback callback = null;
            callback = _ =>
            {
                DoWork(IterationsPerItem);
                if (Interlocked.Decrement(ref remaining) <= 0)
                    done.Set();
                else
                    ThreadPool.UnsafeQueueUserWorkItem(callback, null);
            };

            int seedCount = Math.Min(Environment.ProcessorCount, TotalWorkItems);
            for (int i = 0; i < seedCount; i++)
                ThreadPool.UnsafeQueueUserWorkItem(callback, null);

            done.Wait();
        }

        /// <summary>
        /// Parallel.For with the default partitioner. The runtime's work-stealing
        /// scheduler decides how to split and distribute work.
        /// </summary>
        [Benchmark]
        public void Parallel_For()
        {
            Parallel.For(0, TotalWorkItems, _ => DoWork(IterationsPerItem));
        }
    }
}
