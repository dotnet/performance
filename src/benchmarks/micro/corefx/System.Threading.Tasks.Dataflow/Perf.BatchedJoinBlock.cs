// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tasks.Dataflow.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class BatchedJoin2BlockPerfTests : MultiTargetReceivableSourceBlockPerfTests<BatchedJoinBlock<int, int>, Tuple<IList<int>, IList<int>>>
    {
        protected override int ReceiveSize { get; } = 100;

        public override BatchedJoinBlock<int, int> CreateBlock() => new BatchedJoinBlock<int, int>(ReceiveSize);

        protected override ITargetBlock<int>[] Targets => new[] { block.Target1, block.Target2 };
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public class BatchedJoin3BlockPerfTests : MultiTargetReceivableSourceBlockPerfTests<BatchedJoinBlock<int, int, int>, Tuple<IList<int>, IList<int>, IList<int>>>
    {
        protected override int ReceiveSize { get; } = 100;

        public override BatchedJoinBlock<int, int, int> CreateBlock() => new BatchedJoinBlock<int, int, int>(ReceiveSize);

        protected override ITargetBlock<int>[] Targets => new[] { block.Target1, block.Target2, block.Target3 };
    }
}