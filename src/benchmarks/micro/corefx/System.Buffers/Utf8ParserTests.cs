// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Tests;
using System.Text;

namespace System.Buffers.Text.Tests
{
    public class Utf8ParserTests
    {
        public IEnumerable<object> UInt64Values
            => Perf_UInt64.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(UInt64Values))]
        public bool TryParseUInt64(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out ulong _, out int _);

        public IEnumerable<object> UInt64HexValues
            => Perf_UInt64.StringHexValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(UInt64HexValues))]
        public bool TryParseUInt64Hex(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out ulong _, out int _, 'X');

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

        public IEnumerable<object> Int64Values
            => Perf_Int64.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(Int64Values))]
        public bool TryParseInt64(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out long _, out int _);

        public IEnumerable<object> Int32Values
            => Perf_Int32.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(Int32Values))]
        public bool TryParseInt32(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out int _, out int _);

        public IEnumerable<object> Int16Values
            => Perf_Int16.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(Int16Values))]
        public bool TryParseInt16(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out ushort _, out int _);

        public IEnumerable<object> BooleanValues
            => Perf_Boolean.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(BooleanValues))]
        public bool TryParseBool(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out bool _, out int _);

        public IEnumerable<object> SByteValues
            => Perf_SByte.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(ByteValues))]
        public bool TryParseSByte(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out sbyte _, out int _);

        public IEnumerable<object> ByteValues
            => Perf_Byte.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(ByteValues))]
        public bool TryParseByte(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out byte _, out int _);

        public IEnumerable<object> UInt16Values
            => Perf_UInt16.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(UInt16Values))]
        public bool TryParseUInt16(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out ushort _, out int _);

        public IEnumerable<object> DateTimeOffsetValues
            => Perf_DateTimeOffset.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(DateTimeOffsetValues))]
        public bool TryParseDateTimeOffset(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out DateTimeOffset _, out int _);

        public IEnumerable<object> DecimalValues
            => Perf_Decimal.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(DecimalValues))]
        public bool TryParseDecimal(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out decimal _, out int _);

        public IEnumerable<object> DoubleValues
            => Perf_Double.StringValues.OfType<string>().Select(formatted => new Utf8TestCase(formatted));

        [Benchmark]
        [ArgumentsSource(nameof(DoubleValues))]
        public bool TryParseDouble(Utf8TestCase value) => Utf8Parser.TryParse(value.Utf8Bytes, out double _, out int _);

        // Reenable commented out test cases when https://github.com/xunit/xunit/issues/1822 is fixed.
        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("-Infinity")]           // Negative Infinity
        [InlineData("-3.40282347E+38")]     // Min Negative Normal
        [InlineData("-3.14159274")]         // Negative pi
        [InlineData("-2.71828175")]         // Negative e
        [InlineData("-1")]                  // Negative One
        // [InlineData("-1.17549435E-38")]     // Max Negative Normal
        [InlineData("-1.17549421E-38")]     // Min Negative Subnormal
        [InlineData("-1.401298E-45")]       // Max Negative Subnormal (Negative Epsilon)
        [InlineData("-0.0")]                // Negative Zero
        [InlineData("NaN")]                 // NaN
        [InlineData("0")]                   // Positive Zero
        [InlineData("1.401298E-45")]        // Min Positive Subnormal (Positive Epsilon)
        [InlineData("1.17549421E-38")]      // Max Positive Subnormal
        // [InlineData("1.17549435E-38")]      // Min Positive Normal
        [InlineData("1")]                   // Positive One
        [InlineData("2.71828175")]          // Positive e
        [InlineData("3.14159274")]          // Positive pi
        [InlineData("3.40282347E+38")]      // Max Positive Normal
        [InlineData("Infinity")]            // Positive Infinity
        private static void ByteSpanToSingle(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out float value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

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
