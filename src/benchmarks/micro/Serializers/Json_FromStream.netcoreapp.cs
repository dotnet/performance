using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.Serializers
{
    public partial class Json_FromStream<T>
    {
        [GlobalSetup(Target = nameof(SpanJson_))]
        public ValueTask SetupSpanJson_()
        {
            memoryStream.Position = 0;
            return SpanJson.JsonSerializer.Generic.Utf8.SerializeAsync<T>(value, memoryStream);
        }


        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "SpanJson")]
        public ValueTask<T> SpanJson_()
        {
            memoryStream.Position = 0;
            return SpanJson.JsonSerializer.Generic.Utf8.DeserializeAsync<T>(memoryStream);
        }
    }
}
