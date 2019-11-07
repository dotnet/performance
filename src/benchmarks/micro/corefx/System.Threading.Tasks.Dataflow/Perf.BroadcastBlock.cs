// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tasks.Dataflow.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class BroadcastBlockPerfTests : ReceivablePropagatorPerfTests<BroadcastBlock<int>, int>
    {
        public override BroadcastBlock<int> CreateBlock() => new BroadcastBlock<int>(i => i);

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public Task PostMultiReceiveParallel() => MultiParallel(() => Post());

        [Benchmark(OperationsPerInvoke = MessagesCount)]
        public Task SendMultiReceiveAsyncParallel() => MultiParallel(() => SendAsync());

        private async Task MultiParallel(Func<Task> doTask)
        {
            BlockSetup();
            var options = new DataflowLinkOptions { PropagateCompletion = true };
            var action1 = new ActionBlock<int>(i => { });
            var action2 = new ActionBlock<int>(i => { });
            block.LinkTo(action1, options);
            block.LinkTo(action2, options);

            await doTask();
            block.Complete();

            await Task.WhenAll(action1.Completion, action2.Completion);
        }
    }
}