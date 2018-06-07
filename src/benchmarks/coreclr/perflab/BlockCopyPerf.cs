// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace PerfLabTests
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Perflab)]
    public class BlockCopyPerf
    {
        private byte[] bytes;

        [Params(0, 10, 100, 1000)]
        public int NumElements;

        [GlobalSetup]
        public void Setup()
        {
            bytes = new byte[NumElements * 2];
            Buffer.BlockCopy(bytes, 0, bytes, NumElements, NumElements);
        }

        [Benchmark]
        public void CallBlockCopy() => Buffer.BlockCopy(bytes, 0, bytes, NumElements, NumElements);
    }
}
