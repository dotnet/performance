// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests
{
    public class ReadJsonNonGenericCollections
    {
        private static string _jsonString;

        [Params(2, 50, 100)]
        public int ElementCount;

        [GlobalSetup]
        public void Setup()
        {
            IList _ilist = new List<string>();
            for (int i = 0; i < ElementCount; i++)
            {
                _ilist.Add($"hello{i}");
            }
            _jsonString = JsonSerializer.ToString(_ilist);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public void DeserializeIEnumerable()
        {
            JsonSerializer.Parse<IEnumerable>(_jsonString);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public IList DeserializeIList()
        {
            return JsonSerializer.Parse<IList>(_jsonString);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public ICollection DeserializeICollection()
        {
            return JsonSerializer.Parse<ICollection>(_jsonString);
        }
    }
}
