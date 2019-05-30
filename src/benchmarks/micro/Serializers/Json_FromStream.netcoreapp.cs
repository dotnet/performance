// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
