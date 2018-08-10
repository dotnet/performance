using BenchmarkDotNet.Attributes;

namespace Benchmarks.Serializers
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

        [Benchmark(Description = "Jil")]
        public string Jil_() => Jil.JSON.Serialize<T>(value);

        [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX)]
        [Benchmark(Description = "JSON.NET")]
        public string JsonNet_() => Newtonsoft.Json.JsonConvert.SerializeObject(value);

        [Benchmark(Description = "Utf8Json")]
        public string Utf8Json_() => Utf8Json.JsonSerializer.ToJsonString(value);

        // DataContractJsonSerializer does not provide an API to serialize to string
        // so it's not included here (apples vs apples thing)
    }
}
