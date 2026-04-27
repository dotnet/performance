// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Guids
    {
        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;

        private Guid[] _guidArrayValues;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();

            _guidArrayValues = new Guid[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _guidArrayValues[i] = new Guid();
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteGuids()
        {
            _arrayBufferWriter.Clear();

            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {
                json.WriteStartArray();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteStringValue(_guidArrayValues[i]);
                }
                json.WriteEndArray();
                json.Flush();
            }
        }
    }
}
