// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Writer.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Base64
    {
        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private byte[] _dataWithNoEscaping;
        private byte[] _dataWithEscaping;

        [Params(100, 1000)]
        public int NumberOfBytes { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();

            // Results in a number of A plus padding
            _dataWithNoEscaping = new byte[NumberOfBytes];

            // Results in a lot + and /
            _dataWithEscaping = Enumerable.Range(0, NumberOfBytes)
                .Select(i => i % 2 == 0 ? 0xFB : 0xFF)
                .Select(i => (byte)i)
                .ToArray();
        }

        [Benchmark]
        public void WriteByteArrayAsBase64_NoEscaping() => WriteByteArrayAsBase64Core(_dataWithNoEscaping);

        [Benchmark]
        public void WriteByteArrayAsBase64_HeavyEscaping() => WriteByteArrayAsBase64Core(_dataWithEscaping);

        private void WriteByteArrayAsBase64Core(byte[] data)
        {
            _arrayBufferWriter.Clear();
            using (var json = new Utf8JsonWriter(_arrayBufferWriter))
            {
                json.WriteBase64StringValue(data);
                json.Flush();
            }
        }
    }
}
