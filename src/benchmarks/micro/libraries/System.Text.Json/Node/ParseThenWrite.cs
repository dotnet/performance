using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json.Document.Tests;
using System.Text.Json.Nodes;
using System.Text.Json.Tests;

namespace System.Text.Json.Node.Tests
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
        [MemoryRandomization]
        public void ParseThenWrite()
        {
            _writer.Reset();

            JsonNode jsonNode = JsonNode.Parse(_dataUtf8);
            WalkNode(jsonNode);
            jsonNode.WriteTo(_writer);


            static void WalkNode(JsonNode node)
            {
                // Forces conversion of lazy JsonElement representation of document into
                // a materialized JsonNode tree so that we measure writing performance
                // of the latter representation.

                switch (node)
                {
                    case JsonObject obj:
                        foreach (KeyValuePair<string, JsonNode> kvp in obj)
                            WalkNode(kvp.Value);
                        break;
                    case JsonArray arr:
                        foreach (JsonNode elem in arr)
                            WalkNode(elem);
                        break;
                }
            }
        }
    }
}
