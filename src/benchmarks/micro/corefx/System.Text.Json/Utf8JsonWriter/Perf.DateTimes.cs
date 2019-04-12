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
    public class Perf_DateTimes
    {
        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private PooledBufferWriter<byte> _pooledBufferWriter;

        private DateTime[] _dateArrayValues;

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

            _dateArrayValues = new DateTime[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _dateArrayValues[i] = DateTime.Now;
            }
        }

        [Benchmark]
        public void WriteDateTimes()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartArray();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteStringValue(_dateArrayValues[i]);
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
    public class Perf_Newtonsoft_DateTimes
    {
        private MemoryStream _memoryStream;

        private const int DataSize = 100_000;

        private DateTime[] _dateArrayValues;

        private TextWriter _writer;

        [Params(Formatting.Indented, Formatting.None)]
        public Formatting Formatting;

        [GlobalSetup]
        public void Setup()
        {
            _memoryStream = new MemoryStream();
            _writer = new StreamWriter(_memoryStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

            _dateArrayValues = new DateTime[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _dateArrayValues[i] = DateTime.Now;
            }
        }

        [Benchmark]
        public void WriteDateTimes()
        {
            _memoryStream.Seek(0, SeekOrigin.Begin);
            TextWriter output = _writer;
            using (var json = new JsonTextWriter(output))
            {
                json.Formatting = Formatting;

                json.WriteStartArray();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteValue(_dateArrayValues[i]);
                }
                json.WriteEndArray();

                json.Flush();
            }
        }
    }
}
