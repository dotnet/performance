// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using BenchmarkDotNet.Attributes;

namespace PerfLabTests
{
    public class JITIntrinsics
    {
        private static int s_i;
        private static string s_s;

        [GlobalSetup(Target = nameof(CompareExchangeIntNoMatch))]
        public void SetupCompareExchangeIntNoMatch() => s_i = 0;

        [Benchmark]
        public void CompareExchangeIntNoMatch() => Interlocked.CompareExchange(ref s_i, 5, -1);

        [GlobalSetup(Target = nameof(CompareExchangeIntMatch))]
        public void SetupCompareExchangeIntMatch() => s_i = 1;
        
        [Benchmark]
        public void CompareExchangeIntMatch() => Interlocked.CompareExchange(ref s_i, 5, 1);

        [GlobalSetup(Target = nameof(CompareExchangeObjNoMatch))]
        public void SetupCompareExchangeObjNoMatch() => s_s = "Hello";
        
        [Benchmark]
        public void CompareExchangeObjNoMatch() => Interlocked.CompareExchange(ref s_s, "World", "What?");
        
        [GlobalSetup(Target = nameof(CompareExchangeObjMatch))]
        public void SetupCompareExchangeObjMatch() => s_s = "Hello";
        
        [Benchmark]
        public void CompareExchangeObjMatch() => Interlocked.CompareExchange(ref s_s, "World", "What?");

        [Benchmark]
        public void InterlockedIncrement() => Interlocked.Increment(ref s_i);

        [Benchmark]
        public void InterlockedDecrement() =>  Interlocked.Decrement(ref s_i);
    }
}