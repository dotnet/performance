// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Buffers;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Depth
    {
        private byte[] _dataUtf8;

        [Params(1, 64, 65, 512)]
        public int Depth;

        [GlobalSetup]
        public void Setup()
        {
            var output = new ArrayBufferWriter<byte>(1024);
            var jsonUtf8 = new Utf8JsonWriter(output);

            WriteDepth(jsonUtf8, Depth - 1);

            _dataUtf8 = output.WrittenSpan.ToArray();
        }

        [Benchmark]
        [MemoryRandomization]
        public void ReadSpanEmptyLoop()
        {
            var json = new Utf8JsonReader(_dataUtf8, 
                new JsonReaderOptions { 
                    MaxDepth = Depth
                });
            while (json.Read()) ;
        }

        private static void WriteDepth(Utf8JsonWriter jsonUtf8, int depth)
        {
            jsonUtf8.WriteStartObject();
            for (int i = 0; i < depth; i++)
            {
                jsonUtf8.WriteStartObject("message" + i);
            }
            jsonUtf8.WriteString("message" + depth, "Hello, World!");
            for (int i = 0; i < depth; i++)
            {
                jsonUtf8.WriteEndObject();
            }
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();
        }
    }
}
