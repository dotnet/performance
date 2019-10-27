// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tasks.Dataflow.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class JoinBlockPerfTests : ReceivableSourceBlockPerfTests<JoinBlock<int, int>, Tuple<int, int>>
    {
        public override JoinBlock<int, int> CreateBlock() => new JoinBlock<int, int>();

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostTwiceReceiveOnceParallel()
        {
            await Task.WhenAll(
                Post(block.Target1),
                Post(block.Target2),
                Receive()
            );
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostTwiceTryReceiveOnceParallel()
        {
            await Task.WhenAll(
                Post(block.Target1),
                Post(block.Target2),
                TryReceive()
            );
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task PostTwiceTryReceiveAllOnce()
        {
            await Task.WhenAll(
                Post(block.Target1),
                Post(block.Target2)
            );
            await TryReceiveAll();
        }

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public async Task SendAsyncTwiceReceiveAsyncOnceParallel()
        {
            await Task.WhenAll(
                SendAsync(block.Target1),
                SendAsync(block.Target2),
                ReceiveAsync()
            );
        }
    }
}