// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Filters;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(BinaryData))]
    [GenericTypeArguments(typeof(Dictionary<string, string>))]
    [GenericTypeArguments(typeof(ImmutableDictionary<string, string>))]
    [GenericTypeArguments(typeof(ImmutableSortedDictionary<string, string>))]
    [GenericTypeArguments(typeof(HashSet<string>))]
    [GenericTypeArguments(typeof(ArrayList))]
    [GenericTypeArguments(typeof(Hashtable))]
    [GenericTypeArguments(typeof(SimpleStructWithProperties))]
    [GenericTypeArguments(typeof(LargeStructWithProperties))]
    [GenericTypeArguments(typeof(DateTimeOffset?))]
    [GenericTypeArguments(typeof(int))]
    [AotFilter("Currently not supported due to missing metadata.")]
    public class ReadJson<T>
    {
#if NET6_0_OR_GREATER
        [Params(SystemTextJsonSerializationMode.Reflection, SystemTextJsonSerializationMode.SourceGen)]
#else
        [Params(SystemTextJsonSerializationMode.Reflection)]
#endif
        public SystemTextJsonSerializationMode Mode;

        private JsonSerializerOptions _options;
        private string _serialized;
        private byte[] _utf8Serialized;
        private MemoryStream _memoryStream;

        [GlobalSetup]
        public async Task Setup()
        {
            T value = DataGenerator.Generate<T>();
            _options = DataGenerator.GetJsonSerializerOptions(Mode);

            _serialized = JsonSerializer.Serialize(value, _options);

            _utf8Serialized = Encoding.UTF8.GetBytes(_serialized);

            _memoryStream = new MemoryStream(capacity: short.MaxValue);
            await JsonSerializer.SerializeAsync(_memoryStream, value, _options);
        }

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T DeserializeFromString() => JsonSerializer.Deserialize<T>(_serialized, _options);

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T DeserializeFromUtf8Bytes() => JsonSerializer.Deserialize<T>(_utf8Serialized, _options);

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T DeserializeFromReader()
        {
            Utf8JsonReader reader = new Utf8JsonReader(_utf8Serialized);
            return JsonSerializer.Deserialize<T>(ref reader, _options);
        }

        [BenchmarkCategory(Categories.Libraries, Categories.JSON, Categories.NoWASM)]
        [Benchmark]
        public async Task<T> DeserializeFromStream()
        {
            _memoryStream.Position = 0;
            T value = await JsonSerializer.DeserializeAsync<T>(_memoryStream, _options);
            return value;
        }

        [GlobalCleanup]
        public void Cleanup() => _memoryStream.Dispose();
    }
}
