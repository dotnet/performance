// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Tests;
using System.Text;
using MicroBenchmarks;

namespace System.Buffers.Text.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Utf8ParserTests
    {
        public IEnumerable<object> Int64Values
            => Perf_Int64.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(Int64Values))]
        [MemoryRandomization]
        public bool TryParseInt64(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out long _, out int _);

        public IEnumerable<object> UInt64Values
            => Perf_UInt64.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(UInt64Values))]
        [MemoryRandomization]
        public bool TryParseUInt64(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out ulong _, out int _);

        public IEnumerable<object> UInt64HexValues
            => Perf_UInt64.StringHexValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(UInt64HexValues))]
        public bool TryParseUInt64Hex(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out ulong _, out int _, 'X');

        public IEnumerable<object> Int32Values
            => Perf_Int32.StringValuesDecimal.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(Int32Values))]
        public bool TryParseInt32(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out int _, out int _);

        public IEnumerable<object> UInt32Values
            => Perf_UInt32.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(UInt32Values))]
        public bool TryParseUInt32(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out uint _, out int _);

        public IEnumerable<object> UInt32HexValues
            => Perf_UInt64.StringHexValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(UInt32HexValues))]
        public bool TryParseUInt32Hex(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out uint _, out int _, 'X');

        public IEnumerable<object> Int16Values
            => Perf_Int16.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(Int16Values))]
        public bool TryParseInt16(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out short _, out int _);

        public IEnumerable<object> UInt16Values
            => Perf_UInt16.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(UInt16Values))]
        public bool TryParseUInt16(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out ushort _, out int _);

        public IEnumerable<object> ByteValues
            => Perf_Byte.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(ByteValues))]
        [MemoryRandomization]
        public bool TryParseByte(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out byte _, out int _);

        public IEnumerable<object> SByteValues
            => Perf_SByte.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(SByteValues))]
        public bool TryParseSByte(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out sbyte _, out int _);

        public IEnumerable<object> BooleanValues
            => Perf_Boolean.ValidStringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(BooleanValues))]
        [MemoryRandomization]
        public bool TryParseBool(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out bool _, out int _);

        public IEnumerable<object> DecimalValues
            => Perf_Decimal.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(DecimalValues))]
        [MemoryRandomization]
        public bool TryParseDecimal(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out decimal _, out int _);

        public IEnumerable<object> DoubleValues
            => Perf_Double.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(DoubleValues))]
        [MemoryRandomization]
        public bool TryParseDouble(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out double _, out int _);

        public IEnumerable<object> SingleValues
            => Perf_Single.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(SingleValues))]
        [MemoryRandomization]
        public bool TryParseSingle(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out float _, out int _);

        public IEnumerable<object> DateTimeOffsetValues
            => Perf_DateTimeOffset.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(DateTimeOffsetValues))]
        [MemoryRandomization]
        public bool TryParseDateTimeOffset(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out DateTimeOffset _, out int _);

        public class Utf8TestCase
        {
            public byte[] Utf8Bytes { get; }
            private string Text { get; }

            public Utf8TestCase(string text)
            {
                Text = text;
                Utf8Bytes = Encoding.UTF8.GetBytes(Text);
            }

            public override string ToString() => Text; // displayed by BDN
        }
    }
}
