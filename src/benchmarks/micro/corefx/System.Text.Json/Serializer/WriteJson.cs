// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using System.IO;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    public class WriteJson<T>
    {
        private T _value;
        private MemoryStream _memoryStream;

        [GlobalSetup]
        public async Task Setup()
        {
            _value = DataGenerator.Generate<T>();

            _memoryStream = new MemoryStream(capacity: short.MaxValue);
            await JsonSerializer.WriteAsync(_value, _memoryStream);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON, Categories.JsonSerializer)]
        [Benchmark]
        public string SerializeToString() => JsonSerializer.ToString(_value);

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON, Categories.JsonSerializer)]
        [Benchmark]
        public byte[] SerializeToUtf8Bytes() => JsonSerializer.ToBytes(_value);

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON, Categories.JsonSerializer)]
        [Benchmark]
        public async Task SerializeToStream()
        {
            _memoryStream.Position = 0;
            await JsonSerializer.WriteAsync(_value, _memoryStream);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _memoryStream.Dispose();
        }
    }
}
