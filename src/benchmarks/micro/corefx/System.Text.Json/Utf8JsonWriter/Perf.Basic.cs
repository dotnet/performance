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
    public class Perf_Basic
    {
        private static readonly byte[] ExtraArray = Encoding.UTF8.GetBytes("ExtraArray");
        private static readonly byte[] First = Encoding.UTF8.GetBytes("first");
        private static readonly byte[] Last = Encoding.UTF8.GetBytes("last");
        private static readonly byte[] Age = Encoding.UTF8.GetBytes("age");
        private static readonly byte[] PhoneNumbers = Encoding.UTF8.GetBytes("phoneNumbers");
        private static readonly byte[] Address = Encoding.UTF8.GetBytes("address");
        private static readonly byte[] Street = Encoding.UTF8.GetBytes("street");
        private static readonly byte[] City = Encoding.UTF8.GetBytes("city");
        private static readonly byte[] Zip = Encoding.UTF8.GetBytes("zip");

        private const int DataSize = 10;

        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private PooledBufferWriter<byte> _pooledBufferWriter;

        private int[] _numberArrayValues;

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

            _numberArrayValues = new int[DataSize];

            var random = new Random(42);
            for (int i = 0; i < DataSize; i++)
            {
                _numberArrayValues[i] = random.Next(-10000, 10000);
            }
        }

        [Benchmark]
        public void WriteBasicUtf8()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartObject();
            json.WriteNumber(Age, 42);
            json.WriteString(First, "John");
            json.WriteString(Last, "Smith");
            json.WriteStartArray(PhoneNumbers);
            json.WriteStringValue("425-000-1212");
            json.WriteStringValue("425-000-1213");
            json.WriteEndArray();
            json.WriteStartObject(Address);
            json.WriteString(Street, "1 Microsoft Way");
            json.WriteString(City, "Redmond");
            json.WriteNumber(Zip, 98052);
            json.WriteEndObject();

            json.WriteStartArray(ExtraArray);
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteNumberValue(_numberArrayValues[i]);
            }
            json.WriteEndArray();

            json.WriteEndObject();
            json.Flush(isFinalBlock: true);
        }

        [Benchmark]
        public void WriteBasicUtf16()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

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
    public class Perf_Newtonsoft_Basic
    {
        private MemoryStream _memoryStream;

        private TextWriter _writer;

        [Params(Formatting.Indented, Formatting.None)]
        public Formatting Formatting;

        private const int DataSize = 10;

        private int[] _numberArrayValues;

        [GlobalSetup]
        public void Setup()
        {
            _memoryStream = new MemoryStream();
            _writer = new StreamWriter(_memoryStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

            _numberArrayValues = new int[DataSize];

            var random = new Random(42);
            for (int i = 0; i < DataSize; i++)
            {
                _numberArrayValues[i] = random.Next(-10000, 10000);
            }
        }

        [Benchmark]
        public void WriteBasic()
        {
            _memoryStream.Seek(0, SeekOrigin.Begin);
            TextWriter output = _writer;
            using (var json = new JsonTextWriter(output))
            {
                json.Formatting = Formatting;

                json.WriteStartObject();
                json.WritePropertyName("age");
                json.WriteValue(42);
                json.WritePropertyName("first");
                json.WriteValue("John");
                json.WritePropertyName("last");
                json.WriteValue("Smith");
                json.WritePropertyName("phoneNumbers");
                json.WriteStartArray();
                json.WriteValue("425-000-1212");
                json.WriteValue("425-000-1213");
                json.WriteEndArray();
                json.WritePropertyName("address");
                json.WriteStartObject();
                json.WritePropertyName("street");
                json.WriteValue("1 Microsoft Way");
                json.WritePropertyName("city");
                json.WriteValue("Redmond");
                json.WritePropertyName("zip");
                json.WriteValue(98052);
                json.WriteEndObject();

                json.WritePropertyName("ExtraArray");
                json.WriteStartArray();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteValue(_numberArrayValues[i]);
                }
                json.WriteEndArray();

                json.WriteEndObject();

                json.Flush();
            }
        }
    }
}
