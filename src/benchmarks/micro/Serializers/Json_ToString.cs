// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.Serializers
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(CollectionsOfPrimitives))]
    public class Json_ToString<T>
    {
        private readonly T value;

        public Json_ToString() => value = DataGenerator.Generate<T>();

        [GlobalSetup(Target = nameof(Jil_))]
        public void WarmupJil() => Jil_(); // workaround for https://github.com/dotnet/BenchmarkDotNet/issues/837

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Jil")]
        public string Jil_() => Jil.JSON.Serialize<T>(value, Jil.Options.ISO8601);

        [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.ThirdParty)]
        [Benchmark(Description = "JSON.NET")]
        public string JsonNet_() => Newtonsoft.Json.JsonConvert.SerializeObject(value);

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Utf8Json")]
        public string Utf8Json_() => Utf8Json.JsonSerializer.ToJsonString(value);

        // DataContractJsonSerializer does not provide an API to serialize to string
        // so it's not included here (apples vs apples thing)
    }
}
