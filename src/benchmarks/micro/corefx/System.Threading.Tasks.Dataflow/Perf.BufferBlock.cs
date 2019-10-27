// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class UnboundedBufferBlockPerfTests : ReceivablePropagatorPerfTests<BufferBlock<int>, int>
    {
        public override BufferBlock<int> CreateBlock() => new BufferBlock<int>();
    }

    public class BoundedBufferBlockPerfTests : BoundedReceivablePropagatorPerfTests<BufferBlock<int>, int>
    {
        public override BufferBlock<int> CreateBlock() =>
            new BufferBlock<int>(
                new DataflowBlockOptions
                {
                    BoundedCapacity = 100
                });
    }
}