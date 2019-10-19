// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class ActionBlockPerfTests : TargetPerfTests<ITargetBlock<int>>
    {
        public override ITargetBlock<int> CreateBlock() => new ActionBlock<int>(i => { });
    }

    public class ParallelActionBlockPerfTests : TargetPerfTests<ITargetBlock<int>>
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

    public class UnorderedParallelActionBlockPerfTests : TargetPerfTests<ITargetBlock<int>>
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

    public class SingleProducerConstrainedActionBlockPerfTests : TargetPerfTests<ITargetBlock<int>>
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
}