// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroBenchmarks;

namespace System.Linq.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.LINQ)]
    public class Perf_NullableSum
    {
        private const int BatchSize = 32_768;
        private readonly Consumer _consumer = new Consumer();
        private int?[] _singleton = null!;
        private IEnumerable<int?> _singletonEnumerable = null!;
        private int?[] _int32Batch = null!;
        private long?[] _int64Batch = null!;

        [GlobalSetup]
        public void Setup()
        {
            _singleton = new int?[] { 42 };
            _singletonEnumerable = _singleton;
            _int32Batch = new int?[BatchSize];
            _int64Batch = new long?[BatchSize];

            var random = new Random(451);
            for (int i = 0; i < BatchSize; i++)
            {
                int value = random.Next(-1024, 1025);
                if (random.Next(2) == 0)
                {
                    _int32Batch[i] = value;
                    _int64Batch[i] = value;
                }
            }
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Int32SingletonArray()
        {
            int? result = _singleton.Sum();
            _consumer.Consume(in result);
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Int32SingletonEnumerable()
        {
            int? result = _singletonEnumerable.Sum();
            _consumer.Consume(in result);
        }

        [Benchmark]
        public void Int32MixedArray()
        {
            int? result = _int32Batch.Sum();
            _consumer.Consume(in result);
        }

        [Benchmark]
        public void Int64MixedArray()
        {
            long? result = _int64Batch.Sum();
            _consumer.Consume(in result);
        }
    }
}
