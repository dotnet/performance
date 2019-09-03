// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_EnumerateArray
    {
        public enum TestCaseType
        {
            // Test for complex JSON types.
            Json400KB, 
            // Test for primitive JSON types.
            ArrayOfNumbers, ArrayOfStrings
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
        public void ParseAndEnumerateArray()
        {
            JsonDocument obj = JsonDocument.Parse(_dataUtf8);
            JsonElement elem = obj.RootElement;

            for (int j = 0; j < IterationCount; j++)
            {
                foreach (JsonElement withinArray in elem.EnumerateArray())
                {
                }
            }

            obj.Dispose();
        }

        [Benchmark]
        public void ParseAndIterateUsingIndex()
        {
            JsonDocument obj = JsonDocument.Parse(_dataUtf8);
            JsonElement elem = obj.RootElement;
            int arrayLength = elem.GetArrayLength();

            for (int j = 0; j < IterationCount; j++)
            {
                for (int i = 0; i < arrayLength; i++)
                {
                    JsonElement withinArray = elem[i];
                }
            }

            obj.Dispose();
        }

        [Benchmark]
        public void ParseAndIterateUsingIndexReverse()
        {
            JsonDocument obj = JsonDocument.Parse(_dataUtf8);
            JsonElement elem = obj.RootElement;
            int arrayLength = elem.GetArrayLength();

            for (int j = 0; j < IterationCount; j++)
            {
                for (int i = arrayLength - 1; i >= 0; i--)
                {
                    JsonElement withinArray = obj.RootElement[i];
                }
            }

            obj.Dispose();
        }

        [Benchmark]
        public void ParseAndGetFirst()
        {
            JsonDocument obj = JsonDocument.Parse(_dataUtf8);
            JsonElement elem = obj.RootElement;
            int arrayLength = elem.GetArrayLength();

            for (int j = 0; j < IterationCount; j++)
            {
                for (int i = 0; i < arrayLength; i++)
                {
                    JsonElement withinArray = obj.RootElement[0];
                }
            }

            obj.Dispose();
        }

        [Benchmark]
        public void ParseAndGetMiddle()
        {
            JsonDocument obj = JsonDocument.Parse(_dataUtf8);
            JsonElement elem = obj.RootElement;
            int arrayLength = elem.GetArrayLength();

            for (int j = 0; j < IterationCount; j++)
            {
                for (int i = 0; i < arrayLength; i++)
                {
                    JsonElement withinArray = elem[arrayLength / 2];
                }
            }

            obj.Dispose();
        }
    }
}
