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
    public class Perf_Int64
    {
        private char[] _destination = new char[long.MinValue.ToString().Length];

        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();

        public static IEnumerable<object> Values => new object[]
        {
            long.MinValue,
            (long)12345, // same value used by other tests to compare the perf
            long.MaxValue,
        };

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public string ToString(long value) => value.ToString();

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public long Parse(string value) => long.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        [MemoryRandomization]
        public bool TryParse(string value) => long.TryParse(value, out _);

#if !NETFRAMEWORK // API added in .NET Core 2.1
        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public bool TryFormat(long value) => value.TryFormat(new Span<char>(_destination), out _);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public long ParseSpan(string value) => long.Parse(value.AsSpan());

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public bool TryParseSpan(string value) => long.TryParse(value.AsSpan(), out _);
#endif

#if NET7_0_OR_GREATER
        [Benchmark]
        [Arguments(1, -1)]
        public long CopySign(long value, long sign) => long.CopySign(value, sign);
#endif
    }
}
