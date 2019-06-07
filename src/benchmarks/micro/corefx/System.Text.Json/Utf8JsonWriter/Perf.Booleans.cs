// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Booleans
    {
        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;

        private bool[] _boolArrayValues;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();

            var random = new Random(42);

            _boolArrayValues = new bool[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _boolArrayValues[i] = random.NextDouble() > 0.5;
            }
        }

        [Benchmark]
        public void WriteBooleans()
        {
            _arrayBufferWriter.Clear();
            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {

                json.WriteStartArray();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteBooleanValue(_boolArrayValues[i]);
                }
                json.WriteEndArray();
                json.Flush();
            }
        }
    }
}
