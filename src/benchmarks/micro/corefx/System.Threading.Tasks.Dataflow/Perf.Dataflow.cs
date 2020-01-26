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
            BlockSetup();
            block.Complete();
            await block.Completion;
        }

        protected static void Post(ITargetBlock<int> target, bool retry = false)
        {
            for (int i = 0; i < MessagesCount; i++)
            {
                while (!target.Post(i) && retry) ;
            }
        }

        protected static void Receive<U>(ISourceBlock<U> source, int receiveSize = 1)
        {
            for (int i = 0; i < MessagesCount / receiveSize; i++)
            {
                source.Receive();
            }
        }

        protected static void TryReceive<U>(IReceivableSourceBlock<U> source, int receiveSize = 1, bool retry = false)
        {
            for (int i = 0; i < MessagesCount / receiveSize; i++)
            {
                while (!source.TryReceive(out _) && retry) ;
            }
        }

        protected static void TryReceiveAll<U>(IReceivableSourceBlock<U> source)
        {
            source.TryReceiveAll(out _);
        }

        protected static async Task SendAsync(ITargetBlock<int> target)
        {
            for (int i = 0; i < MessagesCount; i++)
            {
                await target.SendAsync(i);
            }
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
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class SourceBlockPerfTests<T, U> : PerfTests<T> where T : ISourceBlock<U>
    {
        protected virtual int ReceiveSize { get; } = 1;
        protected virtual void Receive() => Receive(block, ReceiveSize);
        protected virtual Task ReceiveAsync() => ReceiveAsync(block, ReceiveSize);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class ReceivableSourceBlockPerfTests<T, U> : SourceBlockPerfTests<T, U> where T : IReceivableSourceBlock<U>
    {
        protected virtual void TryReceive() => TryReceive(block, ReceiveSize);
        protected virtual void TryReceiveAll() => TryReceiveAll(block);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class TargetPerfTests<T> : PerfTests<T> where T : ITargetBlock<int>
    {
        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public void Post() => Post(block);

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public Task SendAsync() => SendAsync(block);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class PropagatorPerfTests<T, U> : TargetPerfTests<T> where T : IPropagatorBlock<int, U>
    {
        protected virtual int ReceiveSize { get; } = 1;
        protected void Receive() => Receive(block, ReceiveSize);
        protected Task ReceiveAsync() => ReceiveAsync(block, ReceiveSize);

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public void PostReceiveSequential()
        {
            Post();
            Receive();
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

        #region Reactive Extensions
        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public void RxPublishSubscribe()
        {
            var observer = block.AsObserver();
            var observable = block.AsObservable();
            observable.Subscribe(new IgnoreObserver<U>());
            for (int i = 0; i < MessagesCount; i++)
            {
                observer.OnNext(i);
            }
            observer.OnCompleted();
        }

        class IgnoreObserver<V> : IObserver<V>
        {
            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(V value) { }
        }
        #endregion
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class ReceivablePropagatorPerfTests<T, U> : PropagatorPerfTests<T, U> where T : IPropagatorBlock<int, U>, IReceivableSourceBlock<U>
    {
        protected void TryReceive() => TryReceive(block, ReceiveSize);
        protected void TryReceiveAll() => TryReceiveAll(block);

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public void PostTryReceiveSequential()
        {
            Post();
            TryReceive();
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostTryReceiveParallel()
        {
            await Task.WhenAll(
                Task.Run(() => Post()),
                Task.Run(() => TryReceive()));
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public void PostTryReceiveAllSequential()
        {
            Post();
            TryReceiveAll();
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class BoundedReceivablePropagatorPerfTests<T, U> : PerfTests<T> where T : IPropagatorBlock<int, U>, IReceivableSourceBlock<U>
    {
        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostTryReceiveParallel()
        {
            await Task.WhenAll(
                Task.Run(() => Post(block, retry: true)),
                Task.Run(() => TryReceive(block, retry: true)));
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

        protected void Post() => Task.WhenAll(Targets.Select(target => Task.Run(() => Post(target))));
        protected Task SendAsync() => Task.WhenAll(Targets.Select(target => SendAsync(target)));

        protected override void Receive() => MultiParallel(() => Task.Run(() => Receive(block, ReceiveSize))).GetAwaiter().GetResult();
        protected override void TryReceive() => MultiParallel(() => Task.Run(() => TryReceive(block, ReceiveSize))).GetAwaiter().GetResult();
        protected override Task ReceiveAsync() => MultiParallel(() => ReceiveAsync(block, ReceiveSize));

        private async Task MultiParallel(Func<Task> doTask)
        {
            await Task.WhenAll(
                Enumerable.Range(
                    0,
                    ReceiveSize == 1 ? 1 : Targets.Length
                )
                .Select(
                    _ => doTask()
                )
            );
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostMultiReceiveOnceParallel()
        {
            await Task.WhenAll(
                Task.Run(() => Post()),
                Task.Run(() => Receive()));
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostMultiTryReceiveOnceParallel()
        {
            await Task.WhenAll(
                Task.Run(() => Post()),
                Task.Run(() => TryReceive()));
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public void PostMultiTryReceiveAllOnceSequential()
        {
            Post();
            TryReceiveAll();
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task SendAsyncMultiReceiveOnceParallel()
        {
            await Task.WhenAll(SendAsync(), ReceiveAsync());
        }
    }
}
