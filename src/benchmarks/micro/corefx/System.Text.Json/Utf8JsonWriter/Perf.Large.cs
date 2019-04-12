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

    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Newtonsoft_Large
    {
        private const int DataSize = 100_000;

        private MemoryStream _memoryStream;

        private string[] _propertNames;
        private int[] _numberArrayValues;
        private string[] _stringArrayValues;

        private TextWriter _writer;

        [Params(Formatting.Indented, Formatting.None)]
        public Formatting Formatting;

        [GlobalSetup]
        public void Setup()
        {
            _memoryStream = new MemoryStream();
            _writer = new StreamWriter(_memoryStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

            var random = new Random(42);

            _propertNames = new string[DataSize];
            _numberArrayValues = new int[DataSize];
            _stringArrayValues = new string[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _propertNames[i] = "abcde" + i.ToString();
                int value = random.Next(-10000, 10000);
                _numberArrayValues[i] = value;
                _stringArrayValues[i] = value.ToString();
            }
        }

        private static string GetString(int minLength, int maxLength)
        {
            var random = new Random(42);
            int length = random.Next(minLength, maxLength);
            var array = new char[length];

            for (int i = 0; i < length; i++)
            {
                array[i] = (char)random.Next(97, 123);
            }

            if (random.NextDouble() > 0.5)
            {
                array[random.Next(0, length)] = '"';
            }

            return new string(array);
        }

        [Benchmark]
        public void WriteLarge()
        {
            _memoryStream.Seek(0, SeekOrigin.Begin);
            TextWriter output = _writer;
            using (var json = new JsonTextWriter(output))
            {
                json.Formatting = Formatting;

                json.WriteStartObject();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WritePropertyName(_propertNames[i]);
                    json.WriteStartArray();

                    int number = _numberArrayValues[i];

                    if (number > 0)
                    {
                        json.WriteValue(_stringArrayValues[i]);
                    }
                    else
                    {
                        json.WriteValue(number);
                    }

                    json.WriteEndArray();
                }
                json.WriteEndObject();

                json.Flush();
            }
        }
    }
}
