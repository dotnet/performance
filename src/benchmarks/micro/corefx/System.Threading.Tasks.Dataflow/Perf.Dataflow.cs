// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tasks.Dataflow.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class PerfTests<T> where T : IDataflowBlock
    {
        protected T block;

        public abstract T CreateBlock();

        [GlobalSetup]
        public void BlockSetup()
        {
            block = CreateBlock();
        }

        [Benchmark]
        public async Task Completion()
        {
            block.Complete();
            await block.Completion;
        }

        protected static Task Post(ITargetBlock<int> target, bool retry = false) => Task.Run(() =>
        {
            for (int i = 0; i < 100_000; i++)
            {
                while (!target.Post(i) && retry) ;
            }
        });

        protected static Task Receive<U>(ISourceBlock<U> source, int receiveSize = 1) => Task.Run(() =>
        {
            for (int i = 0; i < 100_000 / receiveSize; i++)
            {
                source.Receive();
            }
        });

        protected static async Task SendAsync(ITargetBlock<int> target)
        {
            for (int i = 0; i < 100_000; i++)
            {
                await target.SendAsync(i);
            }
        }

        protected static async Task ReceiveAsync<U>(ISourceBlock<U> source, int receiveSize = 1)
        {
            for (int i = 0; i < 100_000 / receiveSize; i++)
            {
                await source.ReceiveAsync();
            }
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class SourceBlockPerfTests<T, U> : PerfTests<T> where T : ISourceBlock<U>
    {
        protected virtual int ReceiveSize { get; } = 1;
        protected Task Receive() => Receive(block, ReceiveSize);
        protected Task ReceiveAsync() => ReceiveAsync(block, ReceiveSize);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class TargetPerfTests<T> : PerfTests<T> where T : ITargetBlock<int>
    {
        [Benchmark(OperationsPerInvoke = 100_000)]
        public Task Post() => Post(block);

        [Benchmark(OperationsPerInvoke = 100_000)]
        public Task SendAsync() => SendAsync(block);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class PropagatorPerfTests<T, U> : TargetPerfTests<IPropagatorBlock<int, U>> where T : IPropagatorBlock<int, U>
    {
        protected virtual int ReceiveSize { get; } = 1;
        protected Task Receive() => Receive(block, ReceiveSize);
        protected Task ReceiveAsync() => ReceiveAsync(block, ReceiveSize);

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task PostReceiveSequential()
        {
            await Post();
            await Receive();
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendReceiveAsyncSequential()
        {
            await SendAsync();
            await ReceiveAsync();
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task PostReceiveParallel()
        {
            await Task.WhenAll(Post(), Receive());
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendReceiveAsyncParallel()
        {
            await Task.WhenAll(SendAsync(), ReceiveAsync());
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class BoundedPropagatorPerfTests<T, U> : PerfTests<T> where T : IPropagatorBlock<int, U>
    {
        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task PostReceiveParallel()
        {
            await Task.WhenAll(Post(block, retry: true), Receive(block));
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendReceiveAsyncParallel()
        {
            await Task.WhenAll(SendAsync(block), ReceiveAsync(block));
        }
    }
}
