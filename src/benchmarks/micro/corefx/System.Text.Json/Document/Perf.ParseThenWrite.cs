// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.IO;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
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
		private ArrayBufferWriter<byte> _arrayBufferWriter;

        [ParamsAllValues]
        public TestCaseType TestCase;

        [Params(true, false)]
        public bool IsDataCompact;

        [GlobalSetup]
        public void Setup()
        {
            string jsonString = JsonStrings.ResourceManager.GetString(TestCase.ToString());

            if (IsDataCompact)
            {
                // Remove all formatting/indentation
                using (var jsonReader = new JsonTextReader(new StringReader(jsonString)))
                using (var stringWriter = new StringWriter())
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                {
                    JToken obj = JToken.ReadFrom(jsonReader);
                    obj.WriteTo(jsonWriter);
                    jsonString = stringWriter.ToString();
                }
            }

            _dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            _arrayBufferWriter = new ArrayBufferWriter<byte>();
        }

        [Benchmark]
        public void ParseThenWrite()
        {
            using (JsonDocument document = JsonDocument.Parse(_dataUtf8))
            using (Utf8JsonWriter writer = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = !IsDataCompact }))
            {
                document.WriteTo(writer);
            }
        }
    }
}
