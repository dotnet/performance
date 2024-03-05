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
    public class Perf_UInt32
    {
        private char[] _destination = new char[uint.MaxValue.ToString().Length];

        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();
        
        public static IEnumerable<object> Values => new object[]
        {
            uint.MinValue,
            (uint)12345, // same value used by other tests to compare the perf
            uint.MaxValue,
        };

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        [MemoryRandomization]
        public string ToString(uint value) => value.ToString();

#if !NETFRAMEWORK // API added in .NET Core 2.1
        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        [MemoryRandomization]
        public bool TryFormat(uint value) => value.TryFormat(new Span<char>(_destination), out _);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public uint ParseSpan(string value) => uint.Parse(value.AsSpan());
#endif

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public uint Parse(string value) => uint.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        [MemoryRandomization]
        public bool TryParse(string value) => uint.TryParse(value, out _);

        public static IEnumerable<object> StringHexValues
            => Values.OfType<UInt32>().Select(value => value.ToString("X", CultureInfo.InvariantCulture));

        [Benchmark]
        [ArgumentsSource(nameof(StringHexValues))]
        public bool TryParseHex(string value) => uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }
}
