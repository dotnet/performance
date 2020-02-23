// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.libraries.Common;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Base64
    {
        private SimpleArrayBufferWriter<byte> _arrayBufferWriter;
        private SimpleMemoryManagerBufferWriter<byte> _memoryManagerBufferWriter;
        private byte[] _dataWithNoEscaping;
        private byte[] _dataWithEscaping;

        [Params(true, false)]
        public bool WithMemoryManager;

        [Params(100, 1000)]
        public int NumberOfBytes { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new SimpleArrayBufferWriter<byte>(5000);
            _memoryManagerBufferWriter = new SimpleMemoryManagerBufferWriter<byte>(5000);

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
            _memoryManagerBufferWriter.Clear();
            var bufferWriter = WithMemoryManager ? (IBufferWriter<byte>)_memoryManagerBufferWriter : _arrayBufferWriter;
            using (var json = new Utf8JsonWriter(bufferWriter))
            {
                json.WriteBase64StringValue(data);
                json.Flush();
            }
        }
    }
}
