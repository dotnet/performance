// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace PerfLabTests
{
    [BenchmarkCategory(Categories.Runtime, Categories.Perflab)]
    public class BlockCopyPerf
    {
        private byte[] bytes;

        [Params(10, 100, 1000)]
        public int numElements;

        [GlobalSetup]
        public void Setup() => bytes = new byte[numElements * 2];

        [Benchmark]
        [MemoryRandomization]
        public void CallBlockCopy() => Buffer.BlockCopy(bytes, 0, bytes, numElements, numElements);   
    }
}
