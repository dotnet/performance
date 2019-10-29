// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tasks.Dataflow.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class JoinBlock2PerfTests : MultiTargetReceivableSourceBlockPerfTests<JoinBlock<int, int>, Tuple<int, int>>
    {
        public override JoinBlock<int, int> CreateBlock() => new JoinBlock<int, int>();

        protected override ITargetBlock<int>[] Targets => new[] { block.Target1, block.Target2 };
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public class JoinBlock3PerfTests : MultiTargetReceivableSourceBlockPerfTests<JoinBlock<int, int, int>, Tuple<int, int, int>>
    {
        public override JoinBlock<int, int, int> CreateBlock() => new JoinBlock<int, int, int>();

        protected override ITargetBlock<int>[] Targets => new[] { block.Target1, block.Target2, block.Target3 };
    }
}