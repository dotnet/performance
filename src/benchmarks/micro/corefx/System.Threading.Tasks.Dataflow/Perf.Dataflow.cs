// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tasks.Dataflow.Tests
{
    #region BufferBlock
    public class UnboundedBufferBlockPerfTests : DefaultPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() => new BufferBlock<int>();
    }

    public class BoundedBufferBlockPerfTests : DefaultBoundedPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() =>
            new BufferBlock<int>(
                new DataflowBlockOptions
                {
                    BoundedCapacity = 100
                });
    }
    #endregion

    #region ActionBlock
    public class ActionBlockPerfTests : DefaultTargetPerfTests
    {
        public override ITargetBlock<int> CreateBlock() => new ActionBlock<int>(i => { });
    }

    public class ParallelActionBlockPerfTests : DefaultTargetPerfTests
    {
        public override ITargetBlock<int> CreateBlock() =>
            new ActionBlock<int>(
                i => { },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                }
            );
    }

    public class UnorderedParallelActionBlockPerfTests : DefaultTargetPerfTests
    {
        public override ITargetBlock<int> CreateBlock() =>
            new ActionBlock<int>(
                i => { },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    EnsureOrdered = false
                }
            );
    }

    public class SPCActionBlockPerfTests : DefaultTargetPerfTests
    {
        public override ITargetBlock<int> CreateBlock() =>
            new ActionBlock<int>(
                i => { },
                new ExecutionDataflowBlockOptions
                {
                    SingleProducerConstrained = true
                }
            );
    }
    #endregion

    #region TransformBlock
    [BenchmarkCategory(Categories.CoreFX)]
    public class TransformBlockPerfTests : DefaultPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() =>
            new TransformBlock<int, int>(i => i);

        #region Reactive Extensions
        [Benchmark(OperationsPerInvoke = 100_000)]
        public void RxPublishSubscribe()
        {
            var observer = block.AsObserver();
            var observable = block.AsObservable();
            observable.Subscribe(new IgnoreObserver<int>());
            for (int i = 0; i < 100_000; i++)
            {
                observer.OnNext(i);
            }
            observer.OnCompleted();
        }

        class IgnoreObserver<T> : IObserver<T>
        {
            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(T value) { }
        }
        #endregion
    }

    public class ParallelTransformBlockPerfTests : DefaultPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() =>
            new TransformBlock<int, int>(
                i => i,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                }
            );
    }

    public class UnorderedParallelTransformBlockPerfTests : DefaultPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() =>
            new TransformBlock<int, int>(
                i => i,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    EnsureOrdered = false
                }
            );
    }

    public class EncapsulateBlockPerfTests : DefaultPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock()
        {
            var options = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
            var block1 = new TransformBlock<int, int>(i => ++i, options);
            var block2 = new TransformBlock<int, int>(i => --i, options);
            block1.LinkTo(block2, new DataflowLinkOptions { PropagateCompletion = true });
            return DataflowBlock.Encapsulate(block1, block2);
        }
    }
    #endregion

    #region TransformManyBlock
    public class TransformManyBlockPerfTests : DefaultPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() =>
            new TransformManyBlock<int, int>(
                i => new int[] { i },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });
    }
    #endregion

    #region BroadcastBlock
    [BenchmarkCategory(Categories.CoreFX)]
    public class BroadcastBlockPerfTests : DefaultPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() =>
            new BroadcastBlock<int>(
                i => i,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });

        [Benchmark(OperationsPerInvoke = 100_000)]
        public Task PostMultiReceiveParallel() => MultiParallel(Post());

        [Benchmark(OperationsPerInvoke = 100_000)]
        public Task SendMultiReceiveAsyncParallel() => MultiParallel(SendAsync());

        private async Task MultiParallel(Task task)
        {
            var options = new DataflowLinkOptions { PropagateCompletion = true };
            var action1 = new ActionBlock<int>(i => { });
            var action2 = new ActionBlock<int>(i => { });
            block.LinkTo(action1, options);
            block.LinkTo(action2, options);

            await task;
            block.Complete();

            await Task.WhenAll(action1.Completion, action2.Completion);
        }
    }
    #endregion

    #region BatchBlock
    public class BatchBlockPerfTests : PropagatorPerfTests<IPropagatorBlock<int, int[]>, int[]>
    {
        protected override int ReceiveSize { get; } = 100;
        public override IPropagatorBlock<int, int[]> CreateBlock() => new BatchBlock<int>(ReceiveSize);
    }
    #endregion

    #region JoinBlock
    [BenchmarkCategory(Categories.CoreFX)]
    public class JoinBlockPerfTests : SourceBlockPerfTests<JoinBlock<int, int>, Tuple<int, int>>
    {
        public override JoinBlock<int, int> CreateBlock() => new JoinBlock<int, int>();

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task PostTwiceReceiveOnceParallel()
        {
            await Task.WhenAll(
                Post(block.Target1),
                Post(block.Target2),
                Receive()
            );
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendAsyncTwiceReceiveAsyncOnceParallel()
        {
            await Task.WhenAll(
                SendAsync(block.Target1),
                SendAsync(block.Target2),
                ReceiveAsync()
            );
        }
    }
    #endregion

    #region BatchedJoinBlock
    [BenchmarkCategory(Categories.CoreFX)]
    public class BatchedJoinBlockPerfTests : SourceBlockPerfTests<BatchedJoinBlock<int, int>, Tuple<IList<int>, IList<int>>>
    {
        protected override int ReceiveSize { get; } = 100;

        public override BatchedJoinBlock<int, int> CreateBlock() => new BatchedJoinBlock<int, int>(ReceiveSize);

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task PostTwiceReceiveOnceParallel()
        {
            await Task.WhenAll(
                Post(block.Target1),
                Post(block.Target2),
                Receive()
            );
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendAsyncTwiceReceiveAsyncOnceParallel()
        {
            await Task.WhenAll(
                SendAsync(block.Target1),
                SendAsync(block.Target2),
                ReceiveAsync()
            );
        }
    }
    #endregion

    #region  Abstract classes
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

        protected static Task Post(ITargetBlock<int> target) => Task.Run(() =>
        {
            for (int i = 0; i < 100_000; i++)
            {
                while (!target.Post(i)) ;
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
    public abstract class DefaultTargetPerfTests : TargetPerfTests<ITargetBlock<int>> { }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class TargetPerfTests<T> : PerfTests<T> where T : ITargetBlock<int>
    {
        [Benchmark(OperationsPerInvoke = 100_000)]
        public Task Post() => Post(block);

        [Benchmark(OperationsPerInvoke = 100_000)]
        public Task SendAsync() => SendAsync(block);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class DefaultBoundedPropagatorPerfTests : BoundedPropagatorPerfTests<IPropagatorBlock<int, int>, int> { }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class DefaultPropagatorPerfTests : PropagatorPerfTests<IPropagatorBlock<int, int>, int> { }

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
            await Task.WhenAll(Post(block), Receive(block));
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendReceiveAsyncParallel()
        {
            await Task.WhenAll(SendAsync(block), ReceiveAsync(block));
        }
    }
    #endregion
}
