// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using System.Threading.Tasks.Tests;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Benchmarks;

namespace System.Threading.Tasks
{
    [BenchmarkCategory(Categories.CoreFX)]
    [MinWarmupCount(2, forceAutoWarmup: true)] // these benchmarks require more warmups than in our default config
    [MaxWarmupCount(10, forceAutoWarmup: true)]
    public class ValueTaskPerfTest
    {
        [Benchmark]
        public async Task Await_FromResult()
        {
            ValueTask<int> vt = new ValueTask<int>(42);

            for (long i = 0; i < 10_000_000L; i++)
            {
                await vt;
            }
        }

        [Benchmark]
        public async Task Await_FromCompletedTask()
        {
            ValueTask<int> vt = new ValueTask<int>(Task.FromResult(42));

            for (long i = 0; i < 10_000_000L; i++)
            {
                await vt;
            }
        }

        [Benchmark]
        public async Task Await_FromCompletedValueTaskSource()
        {
            ValueTask<int> vt = new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed<int>(42), 0);

            for (long i = 0; i < 10_000_000L; i++)
            {
                await vt;
            }
        }

        [Benchmark]
        public async Task CreateAndAwait_FromResult()
        {
            for (long i = 0; i < 10_000_000L; i++)
            {
                await new ValueTask<int>((int) i);
            }
        }

        [Benchmark]
        public async Task CreateAndAwait_FromResult_ConfigureAwait()
        {
            for (long i = 0; i < 10_000_000L; i++)
            {
                await new ValueTask<int>((int) i).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task CreateAndAwait_FromCompletedTask()
        {
            Task<int> t = Task.FromResult(42);

            for (long i = 0; i < 10_000_000L; i++)
            {
                await new ValueTask<int>(t);
            }
        }

        [Benchmark]
        public async Task CreateAndAwait_FromCompletedTask_ConfigureAwait()
        {
            Task<int> t = Task.FromResult(42);

            for (long i = 0; i < 10_000_000L; i++)
            {
                await new ValueTask<int>(t).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task CreateAndAwait_FromCompletedValueTaskSource()
        {
            IValueTaskSource<int> vts = ManualResetValueTaskSourceFactory.Completed(42);

            for (long i = 0; i < 10_000_000L; i++)
            {
                await new ValueTask<int>(vts, 0);
            }
        }

        [Benchmark]
        public async Task CreateAndAwait_FromCompletedValueTaskSource_ConfigureAwait()
        {
            IValueTaskSource<int> vts = ManualResetValueTaskSourceFactory.Completed(42);

            for (long i = 0; i < 10_000_000L; i++)
            {
                await new ValueTask<int>(vts, 0).ConfigureAwait(false);
            }
        }

        [Benchmark]
        public async Task CreateAndAwait_FromYieldingAsyncMethod()
        {
            for (long i = 0; i < 1_000_000L; i++)
            {
                await new ValueTask<int>(YieldOnce());
            }
        }

        [Benchmark]
        public async Task CreateAndAwait_FromDelayedTCS()
        {
            for (long i = 0; i < 1_000_000L; i++)
            {
                var tcs = new TaskCompletionSource<int>();
                ValueTask<int> vt = AwaitTcsAsValueTask(tcs);
                tcs.SetResult(42);
                await vt;
            }
        }

        [Benchmark]
        public void Copy_PassAsArgumentAndReturn_FromResult()
        {
            ValueTask<int> vt = new ValueTask<int>(42);

            for (long i = 0; i < 10_000_000L; i++)
            {
                vt = ReturnValueTask(vt);
            }
        }

        [Benchmark]
        public void Copy_PassAsArgumentAndReturn_FromTask()
        {
            ValueTask<int> vt = new ValueTask<int>(Task.FromResult(42));

            for (long i = 0; i < 10_000_000L; i++)
            {
                vt = ReturnValueTask(vt);
            }
        }

        [Benchmark]
        public void Copy_PassAsArgumentAndReturn_FromValueTaskSource()
        {
            ValueTask<int> vt = new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(42), 0);

            for (long i = 0; i < 10_000_000L; i++)
            {
                vt = ReturnValueTask(vt);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ValueTask<int> ReturnValueTask(ValueTask<int> vt) => vt;

        private async ValueTask<int> AwaitTcsAsValueTask(TaskCompletionSource<int> tcs) 
            => await new ValueTask<int>(tcs.Task).ConfigureAwait(false);

        private async Task<int> YieldOnce()
        {
            await Task.Yield();
            return 42;
        }
    }
}