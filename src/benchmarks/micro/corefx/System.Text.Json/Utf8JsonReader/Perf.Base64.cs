// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Reader.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Base64
    {
        private byte[] _base64NoEscaping;
        private byte[] _base64HeavyEscaping;

        [Params(100, 1000)]
        public int NumberOfBytes { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Results in a number of A plus padding
            var dataWithNoEscaping = new byte[NumberOfBytes];

            // Results in a lot + and /
            var dataWithEscaping = Enumerable.Range(0, NumberOfBytes)
                .Select(i => i % 2 == 0 ? 0xFB : 0xFF)
                .Select(i => (byte)i)
                .ToArray();

            _base64NoEscaping = Write(dataWithNoEscaping);
            _base64HeavyEscaping = Write(dataWithEscaping);

            byte[] Write(byte[] data)
            {
                var memoryStream = new MemoryStream();
                using (var json = new Utf8JsonWriter(memoryStream))
                {
                    json.WriteBase64StringValue(data);
                    json.Flush();

                    return memoryStream.ToArray();
                }
            }
        }

        [Benchmark]
        public byte[] ReadBase64EncodedByteArray_NoEscaping() => ReadBase64EncodedByteArrayCore(_base64NoEscaping);

        [Benchmark]
        public byte[] ReadBase64EncodedByteArray_HeavyEscaping() => ReadBase64EncodedByteArrayCore(_base64HeavyEscaping);

        private byte[] ReadBase64EncodedByteArrayCore(ReadOnlySpan<byte> base64)
        {
            var json = new Utf8JsonReader(base64, true, default);
            json.Read();
            return json.GetBytesFromBase64();
        }
    }
}
