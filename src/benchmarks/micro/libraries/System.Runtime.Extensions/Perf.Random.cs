// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Random
    {
        Random _random = new Random(123456);
        byte[] _bytes = new byte[1000];
        
        [Benchmark]
        public Random ctor() => new Random();

        [Benchmark]
        public int Next_int() => _random.Next(10000);

        [Benchmark]
        public int Next_int_int() => _random.Next(100, 10000);

        [Benchmark]
        public void NextBytes() => _random.NextBytes(_bytes);

        [Benchmark]
        public double NextDouble() => _random.NextDouble();
    }
}
