// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.Threading.Tests
{
    public class Perf_EventWaitHandle
    {
        private const int IterationCount = 100_000;

        private EventWaitHandle _are;

        [GlobalSetup]
        public void Setup() => _are = new EventWaitHandle(false, EventResetMode.AutoReset);

        [GlobalCleanup]
        public void Dispose() => _are.Dispose();

        [Benchmark]
        public void Set_Reset()
        {
            EventWaitHandle are = _are;

            for (int i = 0; i < IterationCount; i++)
            {
                are.Set();
                are.Reset();
            }
        }
    }
}
