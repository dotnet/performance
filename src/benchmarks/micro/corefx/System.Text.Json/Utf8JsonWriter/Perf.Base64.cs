// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
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
    }
}
