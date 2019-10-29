// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tasks.Dataflow.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class PerfTests<T> where T : IDataflowBlock
    {
        protected const int MessagesCount = 100_000;
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
            for (int i = 0; i < MessagesCount; i++)
            {
                while (!target.Post(i) && retry) ;
            }
        });

        protected static Task Receive<U>(ISourceBlock<U> source, int receiveSize = 1) => Task.Run(() =>
        {
            for (int i = 0; i < MessagesCount / receiveSize; i++)
            {
                source.Receive();
            }
        });

        protected static Task TryReceive<U>(IReceivableSourceBlock<U> source, int receiveSize = 1, bool retry = false) => Task.Run(() =>
        {
            for (int i = 0; i < MessagesCount / receiveSize; i++)
            {
                while (!source.TryReceive(out _) && retry) ;
            }
        });

        protected static Task TryReceiveAll<U>(IReceivableSourceBlock<U> source) => Task.Run(() =>
        {
            source.TryReceiveAll(out _);
        });

        protected static async Task SendAsync(ITargetBlock<int> target)
        {
            for (int i = 0; i < MessagesCount; i++)
            {
                await target.SendAsync(i);
            }
            target.Complete();
        }

        protected static async Task ReceiveAsync<U>(ISourceBlock<U> source, int receiveSize = 1)
        {
            for (int i = 0; i < MessagesCount / receiveSize; i++)
            {
                if (await source.OutputAvailableAsync())
                {
                    await source.ReceiveAsync();
                }
            }
            await source.Completion;
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class SourceBlockPerfTests<T, U> : PerfTests<T> where T : ISourceBlock<U>
    {
        protected virtual int ReceiveSize { get; } = 1;
        protected virtual Task Receive() => Receive(block, ReceiveSize);
        protected virtual Task ReceiveAsync() => ReceiveAsync(block, ReceiveSize);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class ReceivableSourceBlockPerfTests<T, U> : SourceBlockPerfTests<T, U> where T : IReceivableSourceBlock<U>
    {
        protected virtual Task TryReceive() => TryReceive(block, ReceiveSize);
        protected virtual Task TryReceiveAll() => TryReceiveAll(block);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class TargetPerfTests<T> : PerfTests<T> where T : ITargetBlock<int>
    {
        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public Task Post() => Post(block);

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public Task SendAsync() => SendAsync(block);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class PropagatorPerfTests<T, U> : TargetPerfTests<T> where T : IPropagatorBlock<int, U>
    {
        protected virtual int ReceiveSize { get; } = 1;
        protected Task Receive() => Receive(block, ReceiveSize);
        protected Task ReceiveAsync() => ReceiveAsync(block, ReceiveSize);

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostReceiveSequential()
        {
            await Post();
            await Receive();
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task SendReceiveAsyncSequential()
        {
            await SendAsync();
            await ReceiveAsync();
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task SendReceiveAsyncParallel()
        {
            await Task.WhenAll(SendAsync(), ReceiveAsync());
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class ReceivablePropagatorPerfTests<T, U> : PropagatorPerfTests<T, U> where T : IPropagatorBlock<int, U>, IReceivableSourceBlock<U>
    {
        protected Task TryReceive() => TryReceive(block, ReceiveSize);
        protected Task TryReceiveAll() => TryReceiveAll(block);

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostTryReceiveSequential()
        {
            await Post();
            await TryReceive();
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostTryReceiveParallel()
        {
            await Task.WhenAll(Post(), TryReceive());
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostTryReceiveAllSequential()
        {
            await Post();
            await TryReceiveAll();
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class BoundedReceivablePropagatorPerfTests<T, U> : PerfTests<T> where T : IPropagatorBlock<int, U>, IReceivableSourceBlock<U>
    {
        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostTryReceiveParallel()
        {
            await Task.WhenAll(Post(block, retry: true), TryReceive(block, retry: true));
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task SendReceiveAsyncParallel()
        {
            await Task.WhenAll(SendAsync(block), ReceiveAsync(block));
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class MultiTargetReceivableSourceBlockPerfTests<T, U> : ReceivableSourceBlockPerfTests<T, U> where T : IReceivableSourceBlock<U>
    {
        protected abstract ITargetBlock<int>[] Targets { get; }

        protected Task Post() => Task.WhenAll(Targets.Select(target => Post(target)));
        protected Task SendAsync() => Task.WhenAll(Targets.Select(target => SendAsync(target)));

        protected override Task Receive() => MultiParallel(() => Receive(block, ReceiveSize));
        protected override Task TryReceive() => MultiParallel(() => TryReceive(block, ReceiveSize));
        protected override Task ReceiveAsync() => MultiParallel(() => ReceiveAsync(block, ReceiveSize));

        private Task MultiParallel(Func<Task> doTask) =>
            Task.WhenAll(
                Enumerable.Range(
                    0,
                    ReceiveSize == 1 ? 1 : Targets.Length
                )
                .Select(
                    _ => doTask()
                )
            );

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public Task PostMultiReceiveOnceParallel() => Task.WhenAll(Post(), Receive());

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public Task PostMultiTryReceiveOnceParallel() => Task.WhenAll(Post(), TryReceive());

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostMultiTryReceiveAllOnceParallel()
        {
            await Post();
            await TryReceiveAll();
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public Task SendAsyncMultiReceiveOnceParallel() => Task.WhenAll(SendAsync(), ReceiveAsync());

    }
}
