// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_DateTimes
    {
        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;

        private DateTime[] _dateArrayValues;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();

            _dateArrayValues = new DateTime[DataSize];

            var startDate = new DateTime(2000, 01, 01);

            for (int i = 0; i < DataSize; i++)
            {
                _dateArrayValues[i] = startDate.AddDays(i);
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteDateTimes()
        {
            _arrayBufferWriter.Clear();

            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {
                json.WriteStartArray();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteStringValue(_dateArrayValues[i]);
                }
                json.WriteEndArray();
                json.Flush();
            }
        }
    }
}
