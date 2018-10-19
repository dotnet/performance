// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_EventWaitHandle
    {
        private EventWaitHandle _are;

        [GlobalSetup]
        public void Setup() => _are = new EventWaitHandle(false, EventResetMode.AutoReset);

        [GlobalCleanup]
        public void Dispose() => _are.Dispose();

        [Benchmark]
        public bool Set_Reset()
        {
            EventWaitHandle are = _are;
            
            return are.Set() && are.Reset();
        }
    }
}
