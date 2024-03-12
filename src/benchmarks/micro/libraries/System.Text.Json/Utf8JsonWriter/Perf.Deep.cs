// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Deep
    {
        private static readonly byte[] ExtraArrayUtf8 = Encoding.UTF8.GetBytes("ExtraArray");

        private const int DataSize = 100_000;
        private const int Depth = 500;

        private ArrayBufferWriter<byte> _arrayBufferWriter;

        private string[] _propertyNames;
        private byte[][] _propertyNamesUtf8;
        private int[] _numberArrayValues;
        private string[] _stringArrayValues;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [GlobalSetup]
        public void Setup()
        {
            // the "WriteDeepUtf16" benchmark can report up to x4 more time when MemoryRandmization is enabled
            // this is due to having new _arrayBufferWriter every time and allocating a lot of memory
            // so we don't always allocate a new instance
            _arrayBufferWriter ??= new ArrayBufferWriter<byte>();

            var random = new Random(42);

            _propertyNames = new string[Depth];
            _propertyNamesUtf8 = new byte[Depth][];
            _numberArrayValues = new int[DataSize];
            _stringArrayValues = new string[DataSize];

            for (int i = 0; i < Depth; i++)
            {
                _propertyNames[i] = "abcde" + i.ToString();
                _propertyNamesUtf8[i] = Encoding.UTF8.GetBytes(_propertyNames[i]);
            }

            for (int i = 0; i < DataSize; i++)
            {
                int value = random.Next(-10000, 10000);
                _numberArrayValues[i] = value;
                _stringArrayValues[i] = value.ToString();
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteDeepUtf8()
        {
            _arrayBufferWriter.Clear();

            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {
                json.WriteStartObject();
                for (int i = 0; i < Depth; i++)
                {
                    json.WriteStartObject(_propertyNamesUtf8[i]);
                }

                json.WriteStartArray(ExtraArrayUtf8);
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteStringValue(_stringArrayValues[i]);
                    json.WriteNumberValue(_numberArrayValues[i]);
                }
                json.WriteEndArray();

                for (int i = 0; i < Depth; i++)
                {
                    json.WriteEndObject();
                }

                json.WriteEndObject();
                json.Flush();
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteDeepUtf16()
        {
            _arrayBufferWriter.Clear();

            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {
                json.WriteStartObject();
                for (int i = 0; i < Depth; i++)
                {
                    json.WriteStartObject(_propertyNames[i]);
                }

                json.WriteStartArray("ExtraArray");
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteStringValue(_stringArrayValues[i]);
                    json.WriteNumberValue(_numberArrayValues[i]);
                }
                json.WriteEndArray();

                for (int i = 0; i < Depth; i++)
                {
                    json.WriteEndObject();
                }

                json.WriteEndObject();
                json.Flush();
            }
        }
    }
}
