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
        public static int InnerIterationCount = 1000000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        
        private byte[] bytes;

        [Params(0, 10, 100, 1000)]
        public int numElements; // the field must be called numElements (starts with lowercase) to keep old benchmark id in BenchView, do NOT change it

        [GlobalSetup]
        public void Setup()
        {
            bytes = new byte[numElements * 2];
            Buffer.BlockCopy(bytes, 0, bytes, numElements, numElements);
        }

        [Benchmark]
        public void CallBlockCopy()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                Buffer.BlockCopy(bytes, 0, bytes, numElements, numElements);   
        }
    }
}
