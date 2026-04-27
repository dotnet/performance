// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_UInt16
    {
        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();

        public static IEnumerable<object> Values => new object[]
        {
            ushort.MinValue,
            (ushort)12345, // same value used by other tests to compare the perf
            ushort.MaxValue
        };

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public ushort Parse(string value) => ushort.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        [MemoryRandomization]
        public bool TryParse(string value) => ushort.TryParse(value, out _);

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        [MemoryRandomization]
        public string ToString(ushort value) => value.ToString();
    }
}
