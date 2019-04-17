// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Doubles
    {
        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;

        private double[] _numberArrayValues;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();

            var random = new Random(42);

            _numberArrayValues = new double[DataSize];

            int halfSize = DataSize / 2;

            for (int i = 0; i < halfSize; i++)
            {
                double value = NextDouble(random, double.MinValue / 10, double.MaxValue / 10);
                _numberArrayValues[i] = value;
            }
            for (int i = 0; i < halfSize; i++)
            {
                double value = NextDouble(random, 1_000_000, -1_000_000);
                _numberArrayValues[i + halfSize] = value;
            }
        }

        private static double NextDouble(Random random, double minValue, double maxValue)
        {
            double value = random.NextDouble() * (maxValue - minValue) + minValue;
            return value;
        }

        [Benchmark]
        public void WriteDoubles()
        {
            _arrayBufferWriter.Clear();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(_arrayBufferWriter, state);

            json.WriteStartArray();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteNumberValue(_numberArrayValues[i]);
            }
            json.WriteEndArray();
            json.Flush(isFinalBlock: true);
        }
    }
}
