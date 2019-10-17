// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class UnboundedBufferBlockPerfTests : DefaultPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() => new BufferBlock<int>();
    }

    public class BoundedBufferBlockPerfTests : DefaultBoundedPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() => new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 100 });
    }

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


    // public class BufferedActionBlockPerfTests : DefaultTargetPerfTests
    // {
    //     public override ITargetBlock<int> CreateBlock()
    //     {
    //         var buffer = new BufferBlock<int>();
    //         var action = new ActionBlock<int>(i => { });
    //         buffer.LinkTo(action, new DataflowLinkOptions { PropagateCompletion = true });
    //         return DataflowBlock.Encapsulate(action, buffer);
    //     }
    // }

    public class TransformBlockPerfTests : DefaultPropagatorPerfTests
    {
        public override IPropagatorBlock<int, int> CreateBlock() =>
            new TransformBlock<int, int>(i => i);
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
            var block1 = new TransformBlock<int, int>(i => ++i, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });
            var block2 = new TransformBlock<int, int>(i => --i, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });
            block1.LinkTo(block2, new DataflowLinkOptions { PropagateCompletion = true });
            return DataflowBlock.Encapsulate(block1, block2);
        }
    }

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
        public async Task PostMultiReceiveParallel()
        {
            var action1 = new ActionBlock<int>(i => { });
            var action2 = new ActionBlock<int>(i => { });
            block.LinkTo(action1, new DataflowLinkOptions { PropagateCompletion = true });
            block.LinkTo(action2, new DataflowLinkOptions { PropagateCompletion = true });

            for (int i = 0; i < 100_000; i++)
            {
                block.Post(i);
            }
            block.Complete();

            await Task.WhenAll(action1.Completion, action2.Completion);
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendMultiReceiveAsyncParallel()
        {
            var action1 = new ActionBlock<int>(i => { });
            var action2 = new ActionBlock<int>(i => { });
            block.LinkTo(action1, new DataflowLinkOptions { PropagateCompletion = true });
            block.LinkTo(action2, new DataflowLinkOptions { PropagateCompletion = true });
            
            for (int i = 0; i < 100_000; i++)
            {
                await block.SendAsync(i);
            }
            block.Complete();
            
            await Task.WhenAll(action1.Completion, action2.Completion);
        }
    }

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
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class DefaultTargetPerfTests : TargetPerfTests<ITargetBlock<int>> { }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class TargetPerfTests<T> : PerfTests<T> where T : ITargetBlock<int>
    {
        [Benchmark(OperationsPerInvoke = 100_000)]
        public void Post()
        {
            for (int i = 0; i < 100_000; i++)
            {
                block.Post(i);
            }
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendAsync()
        {
            for (int i = 0; i < 100_000; i++)
            {
                await block.SendAsync(i);
            }
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class DefaultBoundedPropagatorPerfTests : BoundedPropagatorPerfTests<IPropagatorBlock<int, int>> { }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class DefaultPropagatorPerfTests : PropagatorPerfTests<IPropagatorBlock<int, int>> { }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class PropagatorPerfTests<T> : BoundedPropagatorPerfTests<T> where T : IPropagatorBlock<int, int>
    {
        [Benchmark(OperationsPerInvoke = 100_000)]
        public void PostReceiveSequential()
        {
            for (int i = 0; i < 100_000; i++)
            {
                block.Post(i);
            }

            for (int i = 0; i < 100_000; i++)
            {
                block.Receive();
            }
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendReceiveAsyncSequential()
        {
            for (int i = 0; i < 100_000; i++)
            {
                await block.SendAsync(i);
            }

            for (int i = 0; i < 100_000; i++)
            {
                await block.ReceiveAsync();
            }
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class BoundedPropagatorPerfTests<T> : PerfTests<T> where T : IPropagatorBlock<int, int>
    {
        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task PostReceiveParallel()
        {
            await Task.WhenAll(Post(), Receive());

            Task Post() => Task.Run(() =>
            {
                for (int i = 0; i < 100_000; i++)
                {
                    while (!block.Post(i)) ;
                }
            });

            Task Receive() => Task.Run(() =>
            {
                for (int i = 0; i < 100_000; i++)
                {
                    block.Receive();
                }
            });
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task SendReceiveAsyncParallel()
        {
            await Task.WhenAll(SendAsync(), ReceiveAsync());

            async Task SendAsync()
            {
                for (int i = 0; i < 100_000; i++)
                {
                    await block.SendAsync(i);
                }
            }

            async Task ReceiveAsync()
            {
                for (int i = 0; i < 100_000; i++)
                {
                    await block.ReceiveAsync();
                }
            }
        }
    }
}
