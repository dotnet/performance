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
    public class ReadJson<T>
    {
        private string _serialized;
        private byte[] _utf8Serialized;
        private MemoryStream _memoryStream;

        [GlobalSetup]
        public async Task Setup()
        {
            T value = DataGenerator.Generate<T>();

            _serialized = JsonSerializer.Serialize(value);

            _utf8Serialized = Encoding.UTF8.GetBytes(_serialized);

            _memoryStream = new MemoryStream(capacity: short.MaxValue);
            await JsonSerializer.SerializeAsync(_memoryStream, value);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public T DeserializeFromString() => JsonSerializer.Deserialize<T>(_serialized);

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public T DeserializeFromUtf8Bytes() => JsonSerializer.Deserialize<T>(_utf8Serialized);

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public async Task<T> DeserializeFromStream()
        {
            _memoryStream.Position = 0;
            T value = await JsonSerializer.DeserializeAsync<T>(_memoryStream);
            return value;
        }

        [GlobalCleanup]
        public void Cleanup() => _memoryStream.Dispose();
    }
}
