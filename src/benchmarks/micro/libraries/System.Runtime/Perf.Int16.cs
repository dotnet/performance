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
    public class Perf_Int16
    {
        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();

        public static IEnumerable<object> Values => new object[]
        {
            short.MinValue,
            0,
            short.MaxValue
        };

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        [MemoryRandomization]
        public short Parse(string value) => short.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        [MemoryRandomization]
        public bool TryParse(string value) => short.TryParse(value, out _);

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        [MemoryRandomization]
        public string ToString(short value) => value.ToString();

#if NET7_0_OR_GREATER
        [Benchmark]
        [Arguments(1, -1)]
        public short CopySign(short value, short sign) => short.CopySign(value, sign);
#endif
    }
}
