using BenchmarkDotNet.Attributes;
using System.Threading.Tasks;

namespace MicroBenchmarks.Serializers
{
    public partial class Json_ToStream<T>
    {
        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "SpanJson")]
        public ValueTask SpanJson_()
        {
            memoryStream.Position = 0;
            return SpanJson.JsonSerializer.Generic.Utf8.SerializeAsync(value, memoryStream);
        }
    }
}
