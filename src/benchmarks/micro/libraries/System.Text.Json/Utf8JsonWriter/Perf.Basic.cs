// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.libraries.Common;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Basic
    {
        private static readonly byte[] ExtraArrayUtf8 = Encoding.UTF8.GetBytes("ExtraArray");
        private static readonly byte[] FirstUtf8 = Encoding.UTF8.GetBytes("first");
        private static readonly byte[] LastUtf8 = Encoding.UTF8.GetBytes("last");
        private static readonly byte[] AgeUtf8 = Encoding.UTF8.GetBytes("age");
        private static readonly byte[] PhoneNumbersUtf8 = Encoding.UTF8.GetBytes("phoneNumbers");
        private static readonly byte[] AddressUtf8 = Encoding.UTF8.GetBytes("address");
        private static readonly byte[] StreetUtf8 = Encoding.UTF8.GetBytes("street");
        private static readonly byte[] CityUtf8 = Encoding.UTF8.GetBytes("city");
        private static readonly byte[] ZipUtf8 = Encoding.UTF8.GetBytes("zip");

        private SimpleArrayBufferWriter<byte> _arrayBufferWriter;
        private SimpleMemoryManagerBufferWriter<byte> _memoryManagerBufferWriter;

        private int[] _numberArrayValues;

        [Params(true, false)]
        public bool WithMemoryManager;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [Params(10, 100_000)]
        public int DataSize;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new SimpleArrayBufferWriter<byte>(128 * 1024 * 1024);
            _memoryManagerBufferWriter = new SimpleMemoryManagerBufferWriter<byte>(128 * 1024 * 1024);

            var random = new Random(42);

            _numberArrayValues = new int[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _numberArrayValues[i] = random.Next(-10000, 10000);
            }
        }

        [Benchmark]
        public void WriteBasicUtf8()
        {
            _arrayBufferWriter.Clear();
            _memoryManagerBufferWriter.Clear();
            var bufferWriter = WithMemoryManager ? (IBufferWriter<byte>)_memoryManagerBufferWriter : _arrayBufferWriter;
            using (var json = new Utf8JsonWriter(bufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {

                json.WriteStartObject();
                json.WriteNumber(AgeUtf8, 42);
                json.WriteString(FirstUtf8, "John");
                json.WriteString(LastUtf8, "Smith");
                json.WriteStartArray(PhoneNumbersUtf8);
                json.WriteStringValue("425-000-1212");
                json.WriteStringValue("425-000-1213");
                json.WriteEndArray();
                json.WriteStartObject(AddressUtf8);
                json.WriteString(StreetUtf8, "1 Microsoft Way");
                json.WriteString(CityUtf8, "Redmond");
                json.WriteNumber(ZipUtf8, 98052);
                json.WriteEndObject();

                json.WriteStartArray(ExtraArrayUtf8);
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
        public void WriteBasicUtf16()
        {
            _arrayBufferWriter.Clear();
            _memoryManagerBufferWriter.Clear();
            var bufferWriter = WithMemoryManager ? (IBufferWriter<byte>)_memoryManagerBufferWriter : _arrayBufferWriter;
            using (var json = new Utf8JsonWriter(bufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
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
