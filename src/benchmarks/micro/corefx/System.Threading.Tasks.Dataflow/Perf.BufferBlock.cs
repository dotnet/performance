// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class UnboundedBufferBlockPerfTests : PropagatorPerfTests<IPropagatorBlock<int, int>, int>
    {
        public override IPropagatorBlock<int, int> CreateBlock() => new BufferBlock<int>();
    }

    public class BoundedBufferBlockPerfTests : BoundedPropagatorPerfTests<IPropagatorBlock<int, int>, int>
    {
        public override IPropagatorBlock<int, int> CreateBlock() =>
            new BufferBlock<int>(
                new DataflowBlockOptions
                {
                    BoundedCapacity = 100
                });
    }
}