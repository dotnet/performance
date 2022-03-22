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
    public class Json_ToString<T>
    {
        private T value;

        [GlobalSetup]
        public void Setup() => value = DataGenerator.Generate<T>();

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Jil")]
        [AotFilter("Dynamic code generation is not supported on this platform.")]
        public string Jil_() => Jil.JSON.Serialize<T>(value, Jil.Options.ISO8601);

        [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.ThirdParty)]
        [Benchmark(Description = "JSON.NET")]
        public string JsonNet_() => Newtonsoft.Json.JsonConvert.SerializeObject(value);

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Utf8Json")]
        [AotFilter("Dynamic code generation is not supported on this platform.")]
        public string Utf8Json_() => Utf8Json.JsonSerializer.ToJsonString(value);

        // DataContractJsonSerializer does not provide an API to serialize to string
        // so it's not included here (apples vs apples thing)
    }
}
