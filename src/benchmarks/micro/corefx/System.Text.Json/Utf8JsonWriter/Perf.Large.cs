// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Large
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

        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private PooledBufferWriter<byte> _pooledBufferWriter;

        private string[] _propertNames;
        private byte[][] _propertNamesUtf8;
        private int[] _numberArrayValues;
        private string[] _stringArrayValues;

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

            _propertNames = new string[DataSize];
            _propertNamesUtf8 = new byte[DataSize][];
            _numberArrayValues = new int[DataSize];
            _stringArrayValues = new string[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _propertNames[i] = "abcde" + i.ToString();
                _propertNamesUtf8[i] = Encoding.UTF8.GetBytes(_propertNames[i]);
                int value = random.Next(-10000, 10000);
                _numberArrayValues[i] = value;
                _stringArrayValues[i] = value.ToString();
            }
        }

        [Benchmark]
        public void WriteLargeUtf8()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartObject();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteStartArray(_propertNamesUtf8[i]);

                int number = _numberArrayValues[i];

                if (number > 0)
                {
                    json.WriteStringValue(_stringArrayValues[i]);
                }
                else
                {
                    json.WriteNumberValue(number);
                }

                json.WriteEndArray();
            }
            json.WriteEndObject();
            json.Flush(isFinalBlock: true);
        }

        [Benchmark]
        public void WriteLargeUtf16()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartObject();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteStartArray(_propertNames[i]);

                int number = _numberArrayValues[i];

                if (number > 0)
                {
                    json.WriteStringValue(_stringArrayValues[i]);
                }
                else
                {
                    json.WriteNumberValue(number);
                }

                json.WriteEndArray();
            }
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
}
