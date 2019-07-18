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
    [GenericTypeArguments(typeof(BinaryData))]
    public class WriteJson<T>
    {
        private T _value;
        private MemoryStream _memoryStream;

        [GlobalSetup]
        public async Task Setup()
        {
            _value = DataGenerator.Generate<T>();

            _memoryStream = new MemoryStream(capacity: short.MaxValue);
            await JsonSerializer.SerializeAsync(_memoryStream, _value);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public string SerializeToString() => JsonSerializer.Serialize(_value);

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeToUtf8Bytes() => JsonSerializer.SerializeToUtf8Bytes(_value);

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public async Task SerializeToStream()
        {
            _memoryStream.Position = 0;
            await JsonSerializer.SerializeAsync(_memoryStream, _value);
        }

        [GlobalCleanup]
        public void Cleanup() => _memoryStream.Dispose();
    }
}
