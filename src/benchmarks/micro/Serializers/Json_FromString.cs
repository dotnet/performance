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
    public class Json_FromString<T>
    {
        private string serialized;

        [GlobalSetup(Target = nameof(Jil_))]
        public void SetupJil() => serialized = Jil.JSON.Serialize<T>(DataGenerator.Generate<T>(), Jil.Options.ISO8601);

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Jil")]
#if NET7_0 // https://github.com/dotnet/runtime/issues/64657
        [OperatingSystemsArchitectureFilter(false, System.Runtime.InteropServices.Architecture.Arm64)]
#endif
        public T Jil_() => Jil.JSON.Deserialize<T>(serialized, Jil.Options.ISO8601);

        [GlobalSetup(Target = nameof(JsonNet_))]
        public void SerializeJsonNet() => serialized = Newtonsoft.Json.JsonConvert.SerializeObject(DataGenerator.Generate<T>());

        [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.ThirdParty)]
        [Benchmark(Description = "JSON.NET")]
        [MemoryRandomization]
        public T JsonNet_() => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serialized);

        [GlobalSetup(Target = nameof(Utf8Json_))]
        public void SerializeUtf8Json_() => serialized = Utf8Json.JsonSerializer.ToJsonString(DataGenerator.Generate<T>());

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Utf8Json")]
        public T Utf8Json_() => Utf8Json.JsonSerializer.Deserialize<T>(serialized);

        [GlobalSetup(Target = nameof(SystemTextJson_Reflection_))]
        public void SetupSystemTextJson_Reflection_() => serialized = System.Text.Json.JsonSerializer.Serialize(DataGenerator.Generate<T>());

        [BenchmarkCategory(Categories.Runtime, Categories.Libraries)]
        [Benchmark(Description = "SystemTextJson_Reflection")]
        [MemoryRandomization]
        public T SystemTextJson_Reflection_() => System.Text.Json.JsonSerializer.Deserialize<T>(serialized);

#if NET6_0_OR_GREATER
        private System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> sourceGenMetadata;

        [GlobalSetup(Target = nameof(SystemTextJson_SourceGen_))]
        public void SetupSystemTextJson_SourceGen_()
        {
            sourceGenMetadata = DataGenerator.GetSystemTextJsonSourceGenMetadata<T>();
            serialized = System.Text.Json.JsonSerializer.Serialize(DataGenerator.Generate<T>(), sourceGenMetadata);
        }

        [BenchmarkCategory(Categories.Runtime, Categories.Libraries)]
        [Benchmark(Description = "SystemTextJson_SourceGen")]
        public T SystemTextJson_SourceGen_() => System.Text.Json.JsonSerializer.Deserialize(serialized, sourceGenMetadata);
#endif
    }
}
