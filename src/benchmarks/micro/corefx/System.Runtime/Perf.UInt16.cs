// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_UInt16
    {
        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();

        public static IEnumerable<object> Values => new object[]
        {
            ushort.MinValue,
            0,
            ushort.MaxValue
        };

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public ushort Parse(string value) => ushort.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public bool TryParse(string value) => ushort.TryParse(value, out _);

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public string ToString(ushort value) => value.ToString();
    }
}
