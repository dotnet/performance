// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Base64
    {
        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private byte[] _data;
        private byte[] _dataWithNoEscaping;
        private byte[] _dataWithEscaping;

        [Params(10, 100, 1000)]
        public int NumberOfBytes { get; set; }

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();
            _data = ValuesGenerator.Array<byte>(NumberOfBytes);

            // Results in a number of A plus padding
            _dataWithNoEscaping = new byte[NumberOfBytes];

            // Results in a lot + and /
            _dataWithEscaping = Enumerable.Repeat(0, NumberOfBytes)
                .Select(i => i % 2 == 0 ? 0xFB : 0xFF)
                .Select(i => (byte)i)
                .ToArray();
        }

        [Benchmark]
        public void WriteByteArrayAsBase64()
        {
            _arrayBufferWriter.Clear();
            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {
                json.WriteBase64StringValue(_data);
                json.Flush();
            }
        }

        [Benchmark]
        public void WriteByteArrayAsBase64_NoEscaping()
        {
            _arrayBufferWriter.Clear();
            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {
                json.WriteBase64StringValue(_dataWithNoEscaping);
                json.Flush();
            }
        }

        [Benchmark]
        public void WriteByteArrayAsBase64_HeavyEscaping()
        {
            _arrayBufferWriter.Clear();
            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {
                json.WriteBase64StringValue(_dataWithEscaping);
                json.Flush();
            }
        }
    }
}
