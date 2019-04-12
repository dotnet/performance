// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_HelloWorld
    {
        private static readonly byte[] Message = Encoding.UTF8.GetBytes("message");
        private static readonly byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello, World!");

        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private PooledBufferWriter<byte> _pooledBufferWriter;

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
        }

        [Benchmark]
        public void WriteHelloWorld_Utf8()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartObject();
            json.WriteString(Message, HelloWorld);
            json.WriteEndObject();
            json.Flush(isFinalBlock: true);
        }

        [Benchmark]
        public void WriteHelloWorld_Utf16()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartObject();
            json.WriteString("message", "Hello, World!");
            json.WriteEndObject();
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
