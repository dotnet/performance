// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using System.Threading.Tasks.Tests;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tasks
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    [MinWarmupCount(2, forceAutoWarmup: true)] // these benchmarks require more warmups than in our default config
    [MaxWarmupCount(10, forceAutoWarmup: true)]
    public class ValueTaskPerfTest
    {
        private Task<int> _completedTask = Task.FromResult(42);
        private IValueTaskSource<int> _completedValueTaskSource = ManualResetValueTaskSourceFactory.Completed(42);
        private ValueTask<int> _valueTask;
        
        [Benchmark]
        public async Task Await_FromResult()
        {
            ValueTask<int> vt = new ValueTask<int>(42);

            await vt;
        }

        [Benchmark]
        public async Task Await_FromCompletedTask()
        {
            ValueTask<int> vt = new ValueTask<int>(Task.FromResult(42));

            await vt;
        }

        [Benchmark]
        public async Task Await_FromCompletedValueTaskSource()
        {
            ValueTask<int> vt = new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed<int>(42), 0);

            await vt;
        }

        [Benchmark]
        public async Task CreateAndAwait_FromResult()
        {
            await new ValueTask<int>((int) 0);
        }

        [Benchmark]
        public async Task CreateAndAwait_FromResult_ConfigureAwait()
        {
            await new ValueTask<int>((int) 0).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task CreateAndAwait_FromCompletedTask()
        {
            await new ValueTask<int>(_completedTask);
        }

        [Benchmark]
        public async Task CreateAndAwait_FromCompletedTask_ConfigureAwait()
        {
            await new ValueTask<int>(_completedTask).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task CreateAndAwait_FromCompletedValueTaskSource()
        {
            await new ValueTask<int>(_completedValueTaskSource, 0);
        }

        [Benchmark]
        [MemoryRandomization]
        public async Task CreateAndAwait_FromCompletedValueTaskSource_ConfigureAwait()
        {
            await new ValueTask<int>(_completedValueTaskSource, 0).ConfigureAwait(false);
        }

        [Benchmark]
        public async Task CreateAndAwait_FromYieldingAsyncMethod()
        {
            await new ValueTask<int>(YieldOnce());
        }

        [Benchmark]
        public async Task CreateAndAwait_FromDelayedTCS()
        {
            var tcs = new TaskCompletionSource<int>();
            ValueTask<int> vt = AwaitTcsAsValueTask(tcs);
            tcs.SetResult(42);
            await vt;
        }

        [GlobalSetup(Target = nameof(Copy_PassAsArgumentAndReturn_FromResult))]
        public void Setup_Copy_PassAsArgumentAndReturn_FromResult() => _valueTask = new ValueTask<int>(42);

        [Benchmark]
        public ValueTask<int> Copy_PassAsArgumentAndReturn_FromResult() => ReturnValueTask(_valueTask);

        [GlobalSetup(Target = nameof(Copy_PassAsArgumentAndReturn_FromTask))]
        public void Setup_Copy_PassAsArgumentAndReturn_FromTask() => _valueTask = new ValueTask<int>(Task.FromResult(42));

        [Benchmark]
        public ValueTask<int> Copy_PassAsArgumentAndReturn_FromTask() => ReturnValueTask(_valueTask);

        [GlobalSetup(Target = nameof(Copy_PassAsArgumentAndReturn_FromValueTaskSource))]
        public void Setup_Copy_PassAsArgumentAndReturn_FromValueTaskSource()
            => _valueTask = new ValueTask<int>(ManualResetValueTaskSourceFactory.Completed(42), 0);

        [Benchmark]
        public ValueTask<int> Copy_PassAsArgumentAndReturn_FromValueTaskSource() => ReturnValueTask(_valueTask);

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