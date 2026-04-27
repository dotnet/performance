using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Text.Json.Nodes;

namespace System.Text.Json.Node.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Create
    {
        private readonly JsonNode[] _results = new JsonNode[50];

        [Benchmark]
        public Span<JsonNode> Create_JsonBool()
        {
            Span<JsonNode> results = _results.AsSpan();
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = (JsonNode)true;
            }

            return results;
        }

        [Benchmark]
        public Span<JsonNode> Create_JsonNumber()
        {
            Span<JsonNode> results = _results.AsSpan();
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = (JsonNode)42;
            }

            return results;
        }

        [Benchmark]
        public Span<JsonNode> Create_JsonString()
        {
            Span<JsonNode> results = _results.AsSpan();
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = (JsonNode)"some string";
            }

            return results;
        }

        [Benchmark]
        public Span<JsonNode> Create_JsonArray()
        {
            Span<JsonNode> results = _results.AsSpan(0, 20);
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = new JsonArray { null, null, null, null };
            }

            return results;
        }

        [Benchmark]
        public JsonNode Create_JsonObject_Small()
        {
            return new JsonObject
            {
                ["prop0"] = null,
                ["prop1"] = null,
                ["prop2"] = null,
                ["prop3"] = null,
                ["prop4"] = null,
                ["prop5"] = null,
                ["prop6"] = null,
                ["prop7"] = null,
                ["prop8"] = null,
            };
        }

        [Benchmark]
        public JsonNode Create_JsonObject_Large()
        {
            return new JsonObject
            {
                ["prop0"] = null,
                ["prop1"] = null,
                ["prop2"] = null,
                ["prop3"] = null,
                ["prop4"] = null,
                ["prop5"] = null,
                ["prop6"] = null,
                ["prop7"] = null,
                ["prop8"] = null,
                ["prop9"] = null,
                ["prop10"] = null,
                ["prop11"] = null,
                ["prop12"] = null,
                ["prop13"] = null,
                ["prop14"] = null,
                ["prop15"] = null,
                ["prop16"] = null,
                ["prop17"] = null,
                ["prop18"] = null,
                ["prop19"] = null,
            };
        }
    }
}