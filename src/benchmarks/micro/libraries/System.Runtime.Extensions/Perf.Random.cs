// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Random
    {
        private Random _randomUnseeded = new Random();
        private Random _random = new Random(123456);
        private byte[] _bytes = new byte[1000];

        [Benchmark]
        public Random ctor_seeded() => new Random(123456);

        // Retaining historical name instead of naming 'ctor' and 'ctor_unseeded'
        [Benchmark]
        public Random ctor() => new Random();

        [Benchmark]
        public int Next() => _random.Next();

        [Benchmark]
        [MemoryRandomization]
        public int Next_unseeded() => _randomUnseeded.Next();

        [Benchmark]
        public int Next_int() => _random.Next(10000);

        [Benchmark]
        public int Next_int_unseeded() => _randomUnseeded.Next(10000);

        [Benchmark]
        public int Next_int_int() => _random.Next(100, 10000);

        [Benchmark]
        public int Next_int_int_unseeded() => _randomUnseeded.Next(100, 10000);

        [Benchmark]
        public void NextBytes() => _random.NextBytes(_bytes);

        [Benchmark]
        public void NextBytes_unseeded() => _randomUnseeded.NextBytes(_bytes);

        [Benchmark]
        public double NextDouble() => _random.NextDouble();

        [Benchmark]
        public double NextDouble_unseeded() => _randomUnseeded.NextDouble();

#if !NETFRAMEWORK // New API in .NET Core 2.1
        [Benchmark]
        public void NextBytes_span() => _random.NextBytes(_bytes.AsSpan());

        [Benchmark]
        public void NextBytes_span_unseeded() => _randomUnseeded.NextBytes(_bytes.AsSpan());
#endif

#if NET6_0_OR_GREATER // New API in .NET 6.0
        [Benchmark]
        public long Next_long() => _random.NextInt64(2^20);

        [Benchmark]
        public long Next_long_unseeded() => _randomUnseeded.NextInt64(2^48);

        [Benchmark]
        public long Next_long_long() => _random.NextInt64(100, 10000);

        [Benchmark]
        public long Next_long_long_unseeded() => _randomUnseeded.NextInt64(100, 10000);

        [Benchmark]
        public float NextSingle() => _random.NextSingle();

        [Benchmark]
        public float NextSingle_unseeded() => _randomUnseeded.NextSingle();
#endif
    }
}
