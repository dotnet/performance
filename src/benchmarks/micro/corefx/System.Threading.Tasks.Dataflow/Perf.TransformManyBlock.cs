// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class TransformManyBlockPerfTests : PropagatorPerfTests<IPropagatorBlock<int, int>, int>
    {
        public override IPropagatorBlock<int, int> CreateBlock() =>
            new TransformManyBlock<int, int>(
                i => new int[] { i },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });
    }
}