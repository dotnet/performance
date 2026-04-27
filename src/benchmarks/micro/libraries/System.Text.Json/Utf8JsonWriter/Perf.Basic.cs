// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Basic
    {
        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private int[] _numberArrayValues;
        private byte[] _extraArrayUtf8;
        private byte[] _firstUtf8;
        private byte[] _lastUtf8;
        private byte[] _ageUtf8;
        private byte[] _phoneNumbersUtf8;
        private byte[] _addressUtf8;
        private byte[] _streetUtf8;
        private byte[] _cityUtf8;
        private byte[] _zipUtf8;

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

            _numberArrayValues = new int[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _numberArrayValues[i] = random.Next(-10000, 10000);
            }

            _extraArrayUtf8 = Encoding.UTF8.GetBytes("ExtraArray");
            _firstUtf8 = Encoding.UTF8.GetBytes("first");
            _lastUtf8 = Encoding.UTF8.GetBytes("last");
            _ageUtf8 = Encoding.UTF8.GetBytes("age");
            _phoneNumbersUtf8 = Encoding.UTF8.GetBytes("phoneNumbers");
            _addressUtf8 = Encoding.UTF8.GetBytes("address");
            _streetUtf8 = Encoding.UTF8.GetBytes("street");
            _cityUtf8 = Encoding.UTF8.GetBytes("city");
            _zipUtf8 = Encoding.UTF8.GetBytes("zip");
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteBasicUtf8()
        {
            _arrayBufferWriter.Clear();

            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {
                json.WriteStartObject();
                json.WriteNumber(_ageUtf8, 42);
                json.WriteString(_firstUtf8, "John");
                json.WriteString(_lastUtf8, "Smith");
                json.WriteStartArray(_phoneNumbersUtf8);
                json.WriteStringValue("425-000-1212");
                json.WriteStringValue("425-000-1213");
                json.WriteEndArray();
                json.WriteStartObject(_addressUtf8);
                json.WriteString(_streetUtf8, "1 Microsoft Way");
                json.WriteString(_cityUtf8, "Redmond");
                json.WriteNumber(_zipUtf8, 98052);
                json.WriteEndObject();

                json.WriteStartArray(_extraArrayUtf8);
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteNumberValue(_numberArrayValues[i]);
                }
                json.WriteEndArray();

                json.WriteEndObject();
                json.Flush();
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteBasicUtf16()
        {
            _arrayBufferWriter.Clear();

            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {
                json.WriteStartObject();
                json.WriteNumber("age", 42);
                json.WriteString("first", "John");
                json.WriteString("last", "Smith");
                json.WriteStartArray("phoneNumbers");
                json.WriteStringValue("425-000-1212");
                json.WriteStringValue("425-000-1213");
                json.WriteEndArray();
                json.WriteStartObject("address");
                json.WriteString("street", "1 Microsoft Way");
                json.WriteString("city", "Redmond");
                json.WriteNumber("zip", 98052);
                json.WriteEndObject();

                json.WriteStartArray("ExtraArray");
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteNumberValue(_numberArrayValues[i]);
                }
                json.WriteEndArray();

                json.WriteEndObject();
                json.Flush();
            }
        }
    }
}
