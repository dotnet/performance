// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.Json.Tests;

namespace System.Text.Json.Document.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_EnumerateObject
    {
        public enum TestCaseType
        {
            ObjectProperties,
            StringProperties,
            NumericProperties
        }

        private byte[] _dataUtf8;

        private const int IterationCount = 1000;

        [ParamsAllValues]
        public TestCaseType TestCase;

        [GlobalSetup]
        public void Setup()
        {
            string jsonString = JsonStrings.ResourceManager.GetString(TestCase.ToString());

            // Remove all formatting/indentation
            using (var jsonReader = new JsonTextReader(new StringReader(jsonString)))
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                JToken obj = JToken.ReadFrom(jsonReader);
                obj.WriteTo(jsonWriter);
                jsonString = stringWriter.ToString();
            }

            _dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
        }

        [Benchmark]
        public void ParseAndEnumerateObject()
        {
            JsonDocument obj = JsonDocument.Parse(_dataUtf8);
            JsonElement elem = obj.RootElement;

            for (int j = 0; j < IterationCount; j++)
            {
                foreach (JsonProperty withinArray in elem.EnumerateObject())
                {
                }
            }

            obj.Dispose();
        }

        [Benchmark]
        public void ParseAndGetFirst()
        {
            JsonDocument obj = JsonDocument.Parse(_dataUtf8);
            JsonElement elem = obj.RootElement;

            for (int j = 0; j < IterationCount; j++)
            {
                foreach(JsonProperty prop in elem.EnumerateObject())
                {                    
                    JsonElement first = elem.GetProperty("first_property");
                }
            }

            obj.Dispose();
        }

        [Benchmark]
        public void ParseAndGetMiddle()
        {
            JsonDocument obj = JsonDocument.Parse(_dataUtf8);
            JsonElement elem = obj.RootElement;

            for (int j = 0; j < IterationCount; j++)
            {
                foreach (JsonProperty prop in elem.EnumerateObject())
                {
                    JsonElement middle = elem.GetProperty("middle_property");
                }
            }

            obj.Dispose();
        }
    }
}
