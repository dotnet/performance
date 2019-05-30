using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.Serializers
{
    public partial class Json_ToString<T>
    {
        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "SpanJson")]
        public string SpanJson_() => SpanJson.JsonSerializer.Generic.Utf16.Serialize(value);
    }
}
