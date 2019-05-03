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
    public class Json_FromString<T>
    {
        private readonly T value;
        private string serialized;

        public Json_FromString() => value = DataGenerator.Generate<T>();

        [GlobalSetup(Target = nameof(Jil_))]
        public void SerializeJil() => serialized = Jil.JSON.Serialize<T>(value);

        [GlobalSetup(Target = nameof(JsonNet_))]
        public void SerializeJsonNet() => serialized = Newtonsoft.Json.JsonConvert.SerializeObject(value);

        [GlobalSetup(Target = nameof(Utf8Json_))]
        public void SerializeUtf8Json_() => serialized = Utf8Json.JsonSerializer.ToJsonString(value);

        [BenchmarkCategory(Categories.ThirdParty, Categories.JsonSerializer)]
        [Benchmark(Description = "Jil")]
        public T Jil_() => Jil.JSON.Deserialize<T>(serialized);

        [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.ThirdParty, Categories.JsonSerializer)]
        [Benchmark(Description = "JSON.NET")]
        public T JsonNet_() => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serialized);

        [BenchmarkCategory(Categories.ThirdParty, Categories.JsonSerializer)]
        [Benchmark(Description = "Utf8Json")]
        public T Utf8Json_() => Utf8Json.JsonSerializer.Deserialize<T>(serialized);

        [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.JsonSerializer)]
        [Benchmark(Description = "System.Text.Json")]
        public T SystemTextJson_() => Serialization.JsonSerializer.Parse<T>(serialized);
    }
}
