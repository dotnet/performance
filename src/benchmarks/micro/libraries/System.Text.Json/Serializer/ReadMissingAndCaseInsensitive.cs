// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using Newtonsoft.Json.Serialization;

namespace System.Text.Json.Serialization.Tests
{
    [GenericTypeArguments(typeof(Location))]
    public class ReadMissingAndCaseInsensitive<T>
    {
        private string _serialized;
        private string _serializedCamelCased;
        private JsonSerializerOptions _optionsBaseline;
        private JsonSerializerOptions _optionsCaseInsensitive;

        [GlobalSetup]
        public void Setup()
        {
            T value = DataGenerator.Generate<T>();
            _serialized = JsonSerializer.Serialize(value);

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            _serializedCamelCased = JsonSerializer.Serialize(value, options);

            _optionsBaseline = new JsonSerializerOptions();

            _optionsCaseInsensitive = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T DeserializeBaseline() => JsonSerializer.Deserialize<T>(_serialized, _optionsBaseline);

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T DeserializeMissing() => JsonSerializer.Deserialize<T>(_serializedCamelCased, _optionsBaseline);

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T DeserializeCaseMatching() => JsonSerializer.Deserialize<T>(_serialized, _optionsCaseInsensitive);

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T DeserializeCaseNotMatching() => JsonSerializer.Deserialize<T>(_serializedCamelCased, _optionsCaseInsensitive);
    }
}
