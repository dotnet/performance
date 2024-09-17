using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Text.Json.Nodes;

namespace System.Text.Json.Node.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Create
    {
        [Benchmark]
        public JsonNode Create_JsonBool()
        {
            return true;
        }

        [Benchmark]
        public JsonNode Create_JsonNumber()
        {
            return 42;
        }

        [Benchmark]
        public JsonNode Create_JsonString()
        {
            return "Some string";
        }

        [Benchmark]
        public JsonNode Create_JsonArray()
        {
            return new JsonArray { null, null, null, null };
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