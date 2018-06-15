// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace PerfLabTests
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Perflab)]
    public class JITIntrinsics
    {
        public static int InnerIterationCount = 100000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        
        private static int s_i;
        private static string s_s;

        [GlobalSetup(Target = nameof(CompareExchangeIntNoMatch))]
        public void SetupCompareExchangeIntNoMatch() => s_i = 0;

        [Benchmark]
        public void CompareExchangeIntNoMatch()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                Interlocked.CompareExchange(ref s_i, 5, -1);
        }

        [GlobalSetup(Target = nameof(CompareExchangeIntMatch))]
        public void SetupCompareExchangeIntMatch() => s_i = 1;
        
        [Benchmark]
        public void CompareExchangeIntMatch()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                Interlocked.CompareExchange(ref s_i, 5, 1);
        }

        [GlobalSetup(Target = nameof(CompareExchangeObjNoMatch))]
        public void SetupCompareExchangeObjNoMatch() => s_s = "Hello";
        
        [Benchmark]
        public void CompareExchangeObjNoMatch()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                Interlocked.CompareExchange(ref s_s, "World", "What?");
        }

        [GlobalSetup(Target = nameof(CompareExchangeObjMatch))]
        public void SetupCompareExchangeObjMatch() => s_s = "Hello";
        
        [Benchmark]
        public void CompareExchangeObjMatch()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                Interlocked.CompareExchange(ref s_s, "World", "What?");
        }

        [Benchmark]
        public void InterlockedIncrement()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                Interlocked.Increment(ref s_i);
        }

        [Benchmark]
        public void InterlockedDecrement()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                Interlocked.Decrement(ref s_i);
        }
    }
}