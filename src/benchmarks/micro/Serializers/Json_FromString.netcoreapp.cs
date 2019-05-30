using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.Serializers
{
    public partial class Json_FromString<T>
    {
        [GlobalSetup(Target = nameof(SpanJson_))]
        public void SerializeSpanJson_() => serialized = SpanJson.JsonSerializer.Generic.Utf16.Serialize(value);

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "SpanJson")]
        public T SpanJson_() => SpanJson.JsonSerializer.Generic.Utf16.Deserialize<T>(serialized);
    }
}
