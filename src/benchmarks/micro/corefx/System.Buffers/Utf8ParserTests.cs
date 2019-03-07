// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using System.Globalization;
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

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("107")] // standard parse
        [InlineData("127")] // max value
        [InlineData("-128")] // min value
        [InlineData("-21abcdefghijklmnop")]
        [InlineData("21abcdefghijklmnop")]
        [InlineData("00000000000000000000123")]
        private static void StringToSByte_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        sbyte.TryParse(text, out sbyte value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void StringToSByte_VariableLength_Baseline()
        {
            int textLength = s_SByteTextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_SByteTextArray[i]);
            }
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        sbyte.TryParse(s_SByteTextArray[i % textLength], out sbyte value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void ByteSpanToInt16_VariableLength()
        {
            int textLength = s_Int16TextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_Int16TextArray[i]);
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray[i % textLength];
                        Utf8Parser.TryParse(utf8ByteSpan, out short value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("10737")] // standard parse
        [InlineData("32767")] // max value
        [InlineData("-32768")] // min value
        [InlineData("000000000000000000001235abcdfg")]
        private static void StringToInt16_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out short value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        private static void StringToInt16_VariableLength_Baseline()
        {
            int textLength = s_Int16TextArray.Length;
            byte[][] utf8ByteArray = (byte[][])Array.CreateInstance(typeof(byte[]), textLength);
            for (var i = 0; i < textLength; i++)
            {
                utf8ByteArray[i] = Encoding.UTF8.GetBytes(s_Int16TextArray[i]);
            }

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        short.TryParse(s_Int16TextArray[i % textLength], out short value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("True")]
        [InlineData("False")]
        private static void StringToBool_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        bool.TryParse(text, out bool value);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("True")]
        [InlineData("False")]
        private static void BytesSpanToBool(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out bool value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("107")] // standard parse
        [InlineData("127")] // max value
        [InlineData("-128")] // min value
        [InlineData("-21abcdefghijklmnop")]
        [InlineData("21abcdefghijklmnop")]
        [InlineData("00000000000000000000123")]
        private static void ByteSpanToSByte(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out sbyte value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("42")] // standard parse
        [InlineData("0")] // min value
        [InlineData("255")] // max value
        private static void ByteSpanToByte(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out byte value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }


        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("42")] // standard parse
        [InlineData("0")] // min value
        [InlineData("255")] // max value
        private static void StringToByte_Baseline(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        byte.TryParse(text, out byte value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("0")]
        [InlineData("4212")] // standard parse
        [InlineData("-32768")] // min value
        [InlineData("32767")] // max value
        [InlineData("000000000000000000001235abcdfg")]
        private static void ByteSpanToInt16(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out short value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("4212")] // standard parse
        [InlineData("0")] // min value
        [InlineData("65535")] // max value
        private static void ByteSpanToUInt16(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out ushort value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("Fri, 30 Jun 2000 03:15:45 GMT")] // standard parse
        private static void ByteSpanToTimeOffsetR(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out DateTimeOffset value, out int bytesConsumed, 'R');
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("Fri, 30 Jun 2000 03:15:45 GMT")] // standard parse
        private static void StringToTimeOffsetR_Baseline(string text)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        DateTimeOffset.TryParseExact(text, "r", null, DateTimeStyles.None, out DateTimeOffset value);
                        TestHelpers.DoNotIgnore(value, 0);
                    }
                }
            }
        }

        [Benchmark]
        [ArgumentsSource()]
        public bool TryParseDecimal()
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes("1.23456789E+5");
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out decimal value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

        // Reenable commented out test cases when https://github.com/xunit/xunit/issues/1822 is fixed.
        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData("-Infinity")]                   // Negative Infinity
        [InlineData("-1.7976931348623157E+308")]    // Min Negative Normal
        [InlineData("-3.1415926535897931")]         // Negative pi
        [InlineData("-2.7182818284590451")]         // Negative e
        [InlineData("-1")]                          // Negative One
        // [InlineData("-2.2250738585072014E-308")]    // Max Negative Normal
        [InlineData("-2.2250738585072009E-308")]   // Min Negative Subnormal
        [InlineData("-4.94065645841247E-324")]     // Max Negative Subnormal (Negative Epsilon)
        [InlineData("-0.0")]                       // Negative Zero
        [InlineData("NaN")]                        // NaN
        [InlineData("0")]                          // Positive Zero
        [InlineData("4.94065645841247E-324")]      // Min Positive Subnormal (Positive Epsilon)
        [InlineData("2.2250738585072009E-308")]    // Max Positive Subnormal
        // [InlineData("2.2250738585072014E-308")]     // Min Positive Normal
        [InlineData("1")]                          // Positive One
        [InlineData("2.7182818284590451")]         // Positive e
        [InlineData("3.1415926535897931")]         // Positive pi
        [InlineData("1.7976931348623157E+308")]    // Max Positive Normal
        [InlineData("Infinity")]                   // Positive Infinity
        private static void ByteSpanToDouble(string text)
        {
            byte[] utf8ByteArray = Encoding.UTF8.GetBytes(text);
            ReadOnlySpan<byte> utf8ByteSpan = utf8ByteArray;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Utf8Parser.TryParse(utf8ByteSpan, out double value, out int bytesConsumed);
                        TestHelpers.DoNotIgnore(value, bytesConsumed);
                    }
                }
            }
        }

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
