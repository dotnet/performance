// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Guids
    {
        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private PooledBufferWriter<byte> _pooledBufferWriter;

        private Guid[] _guidArrayValues;

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

            _guidArrayValues = new Guid[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _guidArrayValues[i] = new Guid();
            }
        }

        [Benchmark]
        public void WriteGuids()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartArray();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteStringValue(_guidArrayValues[i]);
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
}
