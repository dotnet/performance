// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

//#pragma warning disable CS1998 // async methods without awaits

namespace System.Threading.Tasks.Dataflow.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Dataflow
    {
        [Benchmark]
        public async Task BufferBlock_Completion()
        {
            var block = new BufferBlock<int>();
            block.Complete();
            await block.Completion;
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public void BufferBlock_Post()
        {
            var block = new BufferBlock<int>();
            for (int i = 0; i < 100_000; i++)
            {
                block.Post(i);
            }
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task BufferBlock_SendAsync()
        {
            var block = new BufferBlock<int>();
            for (int i = 0; i < 100_000; i++)
            {
                await block.SendAsync(i);
            }
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public void BufferBlock_PostReceiveSequential()
        {
            var block = new BufferBlock<int>();
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
        public async Task BufferBlock_SendReceiveAsyncSequential()
        {
            var block = new BufferBlock<int>();
            for (int i = 0; i < 100_000; i++)
            {
                await block.SendAsync(i);
            }

            for (int i = 0; i < 100_000; i++)
            {
                await block.ReceiveAsync();
            }
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task BufferBlock_PostReceiveParallel()
        {
            var block = new BufferBlock<int>();
            await Task.WhenAll(Post(), Receive());
            
            Task Post() => Task.Run(()=>
            {
                for (int i = 0; i < 100_000; i++)
                {
                    block.Post(i);
                }
            });

            Task Receive() => Task.Run(()=>
            {
                for (int i = 0; i < 100_000; i++)
                {
                    block.Receive();
                }
            });
        }

        [Benchmark(OperationsPerInvoke = 100_000)]
        public async Task BufferBlock_SendReceiveAsyncParallel()
        {
            var block = new BufferBlock<int>();
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
