// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Text.Json.Tests;

namespace System.Text.Json.Document.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
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
        private JsonDocument _document;
        private JsonElement _element;

        [ParamsAllValues]
        public TestCaseType TestCase;

        [GlobalSetup]
        public void Setup()
        {
            string jsonString = JsonStrings.ResourceManager.GetString(TestCase.ToString());
            _dataUtf8 = DocumentHelpers.RemoveFormatting(jsonString);

            _document = JsonDocument.Parse(_dataUtf8);
            _element = _document.RootElement;
        }

        [GlobalCleanup]
        public void CleanUp()
        {
            _document.Dispose();
        }

        [Benchmark]
        public void Parse()
        {
            using (JsonDocument obj = JsonDocument.Parse(_dataUtf8))
            {
                JsonElement dummy = obj.RootElement;
            }
        }

        [Benchmark]
        public void EnumerateArray()
        {
            foreach (JsonElement withinArray in _element.EnumerateArray()) { }
        }

        [Benchmark]
        public void EnumerateUsingIndexer()
        {
            int arrayLength = _element.GetArrayLength();
            for (int j = 0; j < arrayLength; j++)
            {
                _ = _element[j];
            }
        }
    }
}
