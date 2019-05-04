// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;

namespace System.Text.Json.Tests
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    public class Json_ToStream<T>
    {
        private readonly T _value;
        private readonly MemoryStream _memoryStream;

        public Json_ToStream()
        {
            _value = DataGenerator.Generate<T>();

            // the stream is pre-allocated, we don't want the benchmarks to include stream allocaton cost
            _memoryStream = new MemoryStream(capacity: short.MaxValue);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON, Categories.JsonSerializer)]
        [Benchmark]
        public async Task SerializeJsonToStream()
        {
            _memoryStream.Position = 0;
            await Serialization.JsonSerializer.WriteAsync(_value, _memoryStream);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _memoryStream.Dispose();
        }
    }
}
