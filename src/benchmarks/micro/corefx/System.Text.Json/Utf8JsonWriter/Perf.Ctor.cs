// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Ctor
    {
        private ArrayBufferWriter<byte> _arrayBufferWriter;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();
        }

        [Benchmark]
        public void Ctor()
        {
            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {

            }
        }
    }
}
