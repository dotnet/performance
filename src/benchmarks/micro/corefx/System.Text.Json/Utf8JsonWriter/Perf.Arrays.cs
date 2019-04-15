// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Arrays
    {
        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private JsonWriterState _state;

        private string[] _propertyNames;
        private byte[][] _propertyNamesUtf8;
        private int[] _numberArrayValues;
        private string[] _stringArrayValues;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [Params(10, 100_000)]
        public int DataSize;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();

            var random = new Random(42);

            _propertyNames = new string[DataSize];
            _propertyNamesUtf8 = new byte[DataSize][];
            _numberArrayValues = new int[DataSize];
            _stringArrayValues = new string[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _propertyNames[i] = "abcde" + i.ToString();
                _propertyNamesUtf8[i] = Encoding.UTF8.GetBytes(_propertyNames[i]);
                int value = random.Next(-10000, 10000);
                _numberArrayValues[i] = value;
                _stringArrayValues[i] = value.ToString();
            }
        }

        [IterationSetup(Targets = new[] { nameof(WriteArrayValuesUtf8), nameof(WriteArrayValuesUtf16) })]
        public void SetupWriteArrayValues()
        {
            _arrayBufferWriter.Clear();
            _state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
        }

        [Benchmark]
        public void WriteArrayValuesUtf8()
        {
            var json = new Utf8JsonWriter(_arrayBufferWriter, _state);

            json.WriteStartObject();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteStartArray(_propertyNamesUtf8[i]);

                json.WriteStringValue(_stringArrayValues[i]);
                json.WriteNumberValue(_numberArrayValues[i]);

                json.WriteEndArray();
            }
            json.WriteEndObject();
            json.Flush(isFinalBlock: true);
        }

        [Benchmark]
        public void WriteArrayValuesUtf16()
        {
            var json = new Utf8JsonWriter(_arrayBufferWriter, _state);

            json.WriteStartObject();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteStartArray(_propertyNames[i]);

                json.WriteStringValue(_stringArrayValues[i]);
                json.WriteNumberValue(_numberArrayValues[i]);

                json.WriteEndArray();
            }
            json.WriteEndObject();
            json.Flush(isFinalBlock: true);
        }
    }
}
