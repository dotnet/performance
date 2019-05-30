// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
