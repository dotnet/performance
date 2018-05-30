// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BenchmarkDotNet.Attributes;

namespace PerfLabTests
{
    public class BlockCopyPerf
    {
        private byte[] bytes;

        [Params(0, 10, 100, 1000)]
        public int NumElements;

        [GlobalSetup]
        public void Setup(int numElements)
        {
            bytes = new byte[numElements * 2];
            Buffer.BlockCopy(bytes, 0, bytes, numElements, numElements);
        }

        [Benchmark]
        public void CallBlockCopy() => Buffer.BlockCopy(bytes, 0, bytes, NumElements, NumElements);
    }
}
