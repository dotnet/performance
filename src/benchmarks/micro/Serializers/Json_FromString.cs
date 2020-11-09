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
    public class Json_FromString<T>
    {
        private string serialized;

        [GlobalSetup(Target = nameof(Jil_))]
        public void SetupJil() => serialized = Jil.JSON.Serialize<T>(DataGenerator.Generate<T>(), Jil.Options.ISO8601);

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Jil")]
        public T Jil_() => Jil.JSON.Deserialize<T>(serialized, Jil.Options.ISO8601);

        [GlobalSetup(Target = nameof(JsonNet_))]
        public void SerializeJsonNet() => serialized = Newtonsoft.Json.JsonConvert.SerializeObject(DataGenerator.Generate<T>());

        [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.ThirdParty)]
        [Benchmark(Description = "JSON.NET")]
        public T JsonNet_() => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serialized);

        [GlobalSetup(Target = nameof(Utf8Json_))]
        public void SerializeUtf8Json_() => serialized = Utf8Json.JsonSerializer.ToJsonString(DataGenerator.Generate<T>());

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Utf8Json")]
        public T Utf8Json_() => Utf8Json.JsonSerializer.Deserialize<T>(serialized);
    }
}
