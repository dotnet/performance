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
    public class Perf_Guids
    {
        private const int DataSize = 100_000;

        private SimpleArrayBufferWriter<byte> _arrayBufferWriter;
        private SimpleMemoryManagerBufferWriter<byte> _memoryManagerBufferWriter;

        private Guid[] _guidArrayValues;

        [Params(true, false)]
        public bool WithMemoryManager;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new SimpleArrayBufferWriter<byte>(8 * 1024 * 1024);
            _memoryManagerBufferWriter = new SimpleMemoryManagerBufferWriter<byte>(8 * 1024 * 1024);

            _guidArrayValues = new Guid[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _guidArrayValues[i] = new Guid();
            }
        }

        [Benchmark]
        public void WriteGuids()
        {
            _arrayBufferWriter.Clear();
            _memoryManagerBufferWriter.Clear();
            var bufferWriter = WithMemoryManager ? (IBufferWriter<byte>)_memoryManagerBufferWriter : _arrayBufferWriter;
            using (var json = new Utf8JsonWriter(bufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
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
