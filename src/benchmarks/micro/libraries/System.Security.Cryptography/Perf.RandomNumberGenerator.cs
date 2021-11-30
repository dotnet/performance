// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Security.Cryptography.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_RandomNumberGenerator
    {
        private RandomNumberGenerator _rng;
        private byte[] _bytes = new byte[8];

        [GlobalSetup]
        public void Setup() => _rng = RandomNumberGenerator.Create();

        [GlobalCleanup]
        public void Cleanup() => _rng.Dispose();

        [Benchmark]
        public void GetBytes() => _rng.GetBytes(_bytes);
    }
}
