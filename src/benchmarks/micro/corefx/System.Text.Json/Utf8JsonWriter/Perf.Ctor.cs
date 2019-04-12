// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Newtonsoft.Json;

namespace System.Text.Json
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Ctor
    {
        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private PooledBufferWriter<byte> _pooledBufferWriter;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [Params(true, false)]
        public bool NewOutput;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();
            _pooledBufferWriter = new PooledBufferWriter<byte>();
        }

        [Benchmark]
        public void Ctor_Array()
        {
            IBufferWriter<byte> output = NewOutput ? new ArrayBufferWriter<byte>() : _arrayBufferWriter;
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);
        }

        [Benchmark]
        public void Ctor_Pool()
        {
            IBufferWriter<byte> output = NewOutput ? new PooledBufferWriter<byte>() : _pooledBufferWriter;
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);
        }
    }

    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Newtonsoft_Ctor
    {
        private TextWriter _writer;

        [Params(Formatting.Indented, Formatting.None)]
        public Formatting Formatting;

        [Params(true, false)]
        public bool NewOutput;

        [GlobalSetup]
        public void Setup()
        {
            _writer = new StreamWriter(new MemoryStream());
        }

        [Benchmark]
        public void Ctor()
        {
            TextWriter output = NewOutput ? new StreamWriter(new MemoryStream()) : _writer;
            using (var json = new JsonTextWriter(output))
            {
                json.Formatting = Formatting;
            }
        }
    }
}
