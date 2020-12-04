// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Buffers;
using System.Text.Json.Tests;

namespace System.Text.Json.Document.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_ParseThenWrite
    {
        public enum TestCaseType
        {
            HelloWorld,
            DeepTree,
            BroadTree,
            LotsOfNumbers,
            LotsOfStrings,
            Json400B,
            Json4KB,
            Json400KB
        }

        private byte[] _dataUtf8;
        private Utf8JsonWriter _writer;

        [ParamsAllValues]
        public TestCaseType TestCase;

        [Params(true, false)]
        public bool IsDataIndented;

        [GlobalSetup]
        public void Setup()
        {
            string jsonString = JsonStrings.ResourceManager.GetString(TestCase.ToString());

            if (!IsDataIndented)
            {
                _dataUtf8 = DocumentHelpers.RemoveFormatting(jsonString);
            }
            else
            {
                _dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            }

            var abw = new ArrayBufferWriter<byte>();
            _writer = new Utf8JsonWriter(abw, new JsonWriterOptions { Indented = IsDataIndented });
        }

        [GlobalCleanup]
        public void CleanUp()
        {
            _writer.Dispose();
        }

        [Benchmark]
        public void ParseThenWrite()
        {
            _writer.Reset();

            using (JsonDocument document = JsonDocument.Parse(_dataUtf8))
            {
                document.WriteTo(_writer);
            }
        }
    }
}
