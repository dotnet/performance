// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Lazy
    {
        private Lazy<int> _lazy;

        [GlobalSetup]
        public void InitializeLazy()
        {
            _lazy = new Lazy<int>(() => 42);
            int ignored = _lazy.Value;
        }

        [Benchmark]
        public int ValueFromAlreadyInitialized() => _lazy.Value;
    }
}
