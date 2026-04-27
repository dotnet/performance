// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Thread
    {
        [Benchmark]
        public Thread CurrentThread() => Thread.CurrentThread;

#if !NETFRAMEWORK
        [Benchmark]
        [MemoryRandomization]
        public int GetCurrentProcessorId() => Thread.GetCurrentProcessorId();
#endif
    }
}
