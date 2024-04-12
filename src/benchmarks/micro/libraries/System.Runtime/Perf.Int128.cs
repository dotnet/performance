// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Int128
    {
        private char[] _destination = new char[Int128.MinValue.ToString().Length];

        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();

        public static IEnumerable<object> Values => new object[]
        {
            Int128.MinValue,
            (Int128)12345, // same value used by other tests to compare the perf
            Int128.MaxValue,
        };

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        [MemoryRandomization]
        public string ToString(Int128 value) => value.ToString();

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public Int128 Parse(string value) => Int128.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public bool TryParse(string value) => Int128.TryParse(value, out _);

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public bool TryFormat(Int128 value) => value.TryFormat(new Span<char>(_destination), out _);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public Int128 ParseSpan(string value) => Int128.Parse(value.AsSpan());

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public bool TryParseSpan(string value) => Int128.TryParse(value.AsSpan(), out _);

        [Benchmark]
        [Arguments(1, -1)]
        public Int128 CopySign(Int128 value, Int128 sign) => Int128.CopySign(value, sign);
    }
}
