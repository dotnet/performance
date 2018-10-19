// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Monitor
    {
        object _sync = new object();

        [Benchmark]
        public void EnterExit()
        {
            object sync = _sync;

            Monitor.Enter(sync);
            Monitor.Exit(sync);
        }

        [Benchmark]
        public void TryEnterExit()
        {
            object sync = _sync;

            Monitor.TryEnter(sync, 0);
            Monitor.Exit(sync);
        }
    }
}
