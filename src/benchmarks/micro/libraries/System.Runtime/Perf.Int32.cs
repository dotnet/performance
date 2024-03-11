// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Int32
    {
        private char[] _destination = new char[int.MinValue.ToString().Length];
        
        public static IEnumerable<object> Values => new object[]
        {
            int.MinValue,
            4, // single digit
            (int)12345, // same value used by other tests to compare the perf
            int.MaxValue
        };

        public static IEnumerable<object> StringValuesDecimal => Values.Select(value => value.ToString()).ToArray();
        public static IEnumerable<object> StringValuesHex => Values.Select(value => ((int)value).ToString("X")).ToArray();

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public string ToString(int value) => value.ToString();

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        [MemoryRandomization]
        public string ToStringHex(int value) => value.ToString("X");

        [Benchmark]
        [ArgumentsSource(nameof(StringValuesDecimal))]
        [MemoryRandomization]
        public int Parse(string value) => int.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValuesHex))]
        public int ParseHex(string value) => int.Parse(value, NumberStyles.HexNumber);

        [Benchmark]
        [ArgumentsSource(nameof(StringValuesDecimal))]
        [MemoryRandomization]
        public bool TryParse(string value) => int.TryParse(value, out _);

#if !NETFRAMEWORK // API added in .NET Core 2.1
        [Benchmark]
        [ArgumentsSource(nameof(StringValuesDecimal))]
        public int ParseSpan(string value) => int.Parse(value.AsSpan());

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        [MemoryRandomization]
        public bool TryFormat(int value) => value.TryFormat(new Span<char>(_destination), out _);

        [Benchmark]
        [ArgumentsSource(nameof(StringValuesDecimal))]
        [MemoryRandomization]
        public bool TryParseSpan(string value) => int.TryParse(value.AsSpan(), out _);
#endif

#if NET7_0_OR_GREATER
        [Benchmark]
        [Arguments(1, -1)]
        public int CopySign(int value, int sign) => int.CopySign(value, sign);
#endif
    }
}
