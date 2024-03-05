// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Tests;
using MicroBenchmarks;

namespace System.Buffers.Text.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Utf8FormatterTests
    {
        private readonly byte[] _destination = new byte[1000]; // big enough to store anything we want to format

        public IEnumerable<object> Int64Values() => Perf_Int64.Values; // use same values as other formatting tests so we can compare apples to apples

        [Benchmark]
        [ArgumentsSource(nameof(Int64Values))]
        [MemoryRandomization]
        public bool FormatterInt64(long value) => Utf8Formatter.TryFormat(value, _destination, out _);

        public IEnumerable<object> UInt64Values() => Perf_UInt64.Values;

        [Benchmark]
        [ArgumentsSource(nameof(UInt64Values))]
        [MemoryRandomization]
        public bool FormatterUInt64(ulong value) => Utf8Formatter.TryFormat(value, _destination, out _);

        public IEnumerable<object> Int32Values() => Perf_Int32.Values;

        [Benchmark]
        [ArgumentsSource(nameof(Int32Values))]
        [MemoryRandomization]
        public bool FormatterInt32(int value) => Utf8Formatter.TryFormat(value, _destination, out _);

        public IEnumerable<object> UInt32Values() => Perf_UInt32.Values;

        [Benchmark]
        [ArgumentsSource(nameof(UInt32Values))]
        public bool FormatterUInt32(uint value) => Utf8Formatter.TryFormat(value, _destination, out _);

        public IEnumerable<object> DecimalValues() => Perf_Decimal.Values;

        [Benchmark]
        [ArgumentsSource(nameof(DecimalValues))]
        [MemoryRandomization]
        public bool FormatterDecimal(decimal value) => Utf8Formatter.TryFormat(value, _destination, out _);

        public IEnumerable<object> DoubleValues() => Perf_Double.Values;

        [Benchmark]
        [ArgumentsSource(nameof(DoubleValues))]
        public bool FormatterDouble(double value) => Utf8Formatter.TryFormat(value, _destination, out _);

        public IEnumerable<object> DateTimeOffsetValues() => Perf_DateTimeOffset.Values;

        [Benchmark]
        [ArgumentsSource(nameof(DateTimeOffsetValues))]
        [MemoryRandomization]
        public bool FormatterDateTimeOffsetNow(DateTimeOffset value) => Utf8Formatter.TryFormat(value, _destination, out int bytesWritten);
    }
}
