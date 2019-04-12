// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Newtonsoft.Json;

namespace System.Text.Json
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Doubles
    {
        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private PooledBufferWriter<byte> _pooledBufferWriter;

        private double[] _numberArrayValues;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [Params(true, false)]
        public bool Pool;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();
            _pooledBufferWriter = new PooledBufferWriter<byte>();

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
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartArray();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteNumberValue(_numberArrayValues[i]);
            }
            json.WriteEndArray();
            json.Flush(isFinalBlock: true);
        }

        private IBufferWriter<byte> GetOutput()
        {
            if (Pool)
            {
                _pooledBufferWriter.Clear();
                return _pooledBufferWriter;
            }
            else
            {
                _arrayBufferWriter.Clear();
                return _arrayBufferWriter;
            }
        }
    }

    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Newtonsoft_Doubles
    {
        private MemoryStream _memoryStream;

        private const int DataSize = 100_000;

        private double[] _numberArrayValues;

        private TextWriter _writer;

        [Params(Formatting.Indented, Formatting.None)]
        public Formatting Formatting;

        [GlobalSetup]
        public void Setup()
        {
            _memoryStream = new MemoryStream();
            _writer = new StreamWriter(_memoryStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

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
            _memoryStream.Seek(0, SeekOrigin.Begin);
            TextWriter output = _writer;
            using (var json = new JsonTextWriter(output))
            {
                json.Formatting = Formatting;

                json.WriteStartArray();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteValue(_numberArrayValues[i]);
                }
                json.WriteEndArray();

                json.Flush();
            }
        }
    }
}
