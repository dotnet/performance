// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    public class Json_ToString<T>
    {
        private readonly T value;

        public Json_ToString() => value = DataGenerator.Generate<T>();

        [BenchmarkCategory(Categories.ThirdParty, Categories.JsonSerializer)]
        [Benchmark(Description = "Jil")]
        public string Jil_() => Jil.JSON.Serialize<T>(value);

        [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.ThirdParty, Categories.JsonSerializer)]
        [Benchmark(Description = "JSON.NET")]
        public string JsonNet_() => Newtonsoft.Json.JsonConvert.SerializeObject(value);

        [BenchmarkCategory(Categories.ThirdParty, Categories.JsonSerializer)]
        [Benchmark(Description = "Utf8Json")]
        public string Utf8Json_() => Utf8Json.JsonSerializer.ToJsonString(value);

        [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.JsonSerializer)]
        [Benchmark(Description = "System.Text.Json")]
        public string SystemTextJson_() => Serialization.JsonSerializer.ToString(value);

        // DataContractJsonSerializer does not provide an API to serialize to string
        // so it's not included here (apples vs apples thing)
    }
}
