// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
