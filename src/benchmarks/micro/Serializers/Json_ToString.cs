// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Filters;

namespace MicroBenchmarks.Serializers
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(CollectionsOfPrimitives))]
    [BenchmarkCategory(Categories.NoAOT)]
    [AotFilter("Dynamic code generation is not supported.")]
    public class Json_ToString<T>
    {
        private T value;
#if NET6_0_OR_GREATER
        private System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> sourceGenMetadata;
#endif

        [GlobalSetup]
        public void Setup()
        {
            value = DataGenerator.Generate<T>();
#if NET6_0_OR_GREATER
            sourceGenMetadata = DataGenerator.GetSystemTextJsonSourceGenMetadata<T>();
#endif
        }

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Jil")]
        public string Jil_() => Jil.JSON.Serialize<T>(value, Jil.Options.ISO8601);

        [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.ThirdParty)]
        [Benchmark(Description = "JSON.NET")]
        [MemoryRandomization]
        public string JsonNet_() => Newtonsoft.Json.JsonConvert.SerializeObject(value);

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Utf8Json")]
        public string Utf8Json_() => Utf8Json.JsonSerializer.ToJsonString(value);

        // DataContractJsonSerializer does not provide an API to serialize to string
        // so it's not included here (apples vs apples thing)

        [BenchmarkCategory(Categories.Runtime, Categories.Libraries)]
        [Benchmark(Description = "SystemTextJson_Reflection")]
        // DataContractJsonSerializer does not provide an API to serialize to string
        // so it's not included here (apples vs apples thing)

        [MemoryRandomization]
        public string SystemTextJson_Reflection_() => System.Text.Json.JsonSerializer.Serialize(value);

#if NET6_0_OR_GREATER
        [BenchmarkCategory(Categories.Runtime, Categories.Libraries)]
        [Benchmark(Description = "SystemTextJson_SourceGen")]
        public string SystemTextJson_SourceGen_() => System.Text.Json.JsonSerializer.Serialize(value, sourceGenMetadata);
#endif
    }
}
