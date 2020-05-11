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

        /// <summary>
        /// Uses default settings of case-sensitive comparison and JSON that matches the Pascal-casing
        /// of the properties on the class.
        /// </summary>
        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T Baseline() => JsonSerializer.Deserialize<T>(_serialized, _optionsBaseline);

        /// <summary>
        /// Properties are missing because the comparison is case-sensitive and the JSON uses camel-casing
        /// which does not match the properties on the class.
        /// </summary>
        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T MissingProperties() => JsonSerializer.Deserialize<T>(_serializedCamelCased, _optionsBaseline);

        /// <summary>
        /// Case-insensitive is enabled and the casing in JSON matches the properties on the class.
        /// </summary>
        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T CaseInsensitiveMatching() => JsonSerializer.Deserialize<T>(_serialized, _optionsCaseInsensitive);

        /// <summary>
        /// Case-insensitive is enabled and the casing in JSON does not match the properties on the class.
        /// </summary>
        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T CaseInsensitiveNotMatching() => JsonSerializer.Deserialize<T>(_serializedCamelCased, _optionsCaseInsensitive);
    }
}
