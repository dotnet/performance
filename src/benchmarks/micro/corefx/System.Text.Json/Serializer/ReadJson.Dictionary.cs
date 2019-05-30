// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Text.Json.Serialization.Tests
{
    public class ReadDictionary
    {
        private const string _jsonString = @"{""Hello"":""World"",""Hello2"":""World2""}";

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public Dictionary<string, string> DeserializeDict()
        {
            return JsonSerializer.Parse<Dictionary<string, string>>(_jsonString);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public IDictionary<string, string> DeserializeIDict()
        {
            return JsonSerializer.Parse<IDictionary<string, string>>(_jsonString);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public IReadOnlyDictionary<string, string> DeserializeIReadOnlyDict()
        {
            return JsonSerializer.Parse<IReadOnlyDictionary<string, string>>(_jsonString);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public ImmutableDictionary<string, string> DeserializeImmutableDict()
        {
            return JsonSerializer.Parse<ImmutableDictionary<string, string>>(_jsonString);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public IImmutableDictionary<string, string> DeserializeIImmutableDict()
        {
            return JsonSerializer.Parse<IImmutableDictionary<string, string>>(_jsonString);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public ImmutableSortedDictionary<string, string> DeserializeImmutableSortedDict()
        {
            return JsonSerializer.Parse<ImmutableSortedDictionary<string, string>>(_jsonString);
        }
    }
}
