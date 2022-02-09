// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;

namespace System.Text.Json.Serialization.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    [GenericTypeArguments(typeof(SimpleStructWithProperties))]
    public class ColdStartSerialization<T>
    {
        // Measures cold start performance for the serializer
        // using a fresh JsonSerializerOptions instance per run.

        private T _value;
        private readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions();
        private readonly JsonStringEnumConverter _converter = new JsonStringEnumConverter();

        [GlobalSetup]
        public void Setup()
        {
            _value = DataGenerator.Generate<T>();
            RoundtripSerialization(_defaultOptions);
        }

        private T RoundtripSerialization(JsonSerializerOptions options)
        {
            string json = JsonSerializer.Serialize(_value, options);
            return JsonSerializer.Deserialize<T>(json, options);
        }

        [Benchmark]
        public T CachedDefaultOptions() => RoundtripSerialization(_defaultOptions);

        [Benchmark]
        public T NewDefaultOptions() => RoundtripSerialization(new JsonSerializerOptions());

        [Benchmark]
        public T NewCustomizedOptions() => 
            RoundtripSerialization(
                new JsonSerializerOptions {
                    AllowTrailingCommas = true,
                    WriteIndented = true,
                    MaxDepth = 1000,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

        [Benchmark]
        public T NewCustomConverter() =>
            RoundtripSerialization(
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                });

        [Benchmark]
        public T NewCachedCustomConverter() =>
            RoundtripSerialization(
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { _converter }
                });
    }
}
