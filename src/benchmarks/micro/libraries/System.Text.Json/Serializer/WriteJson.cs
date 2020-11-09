// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
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
    [GenericTypeArguments(typeof(int))]
    public class WriteJson<T>
    {
        private T _value;
        private MemoryStream _memoryStream;
        private object _objectWithObjectProperty;

        [GlobalSetup]
        public async Task Setup()
        {
            _value = DataGenerator.Generate<T>();

            _memoryStream = new MemoryStream(capacity: short.MaxValue);
            await JsonSerializer.SerializeAsync(_memoryStream, _value);

            _objectWithObjectProperty = new { Prop = (object)_value };
        }

        [GlobalCleanup]
        public void Cleanup() => _memoryStream.Dispose();

        [Benchmark]
        public string SerializeToString() => JsonSerializer.Serialize(_value);

        [Benchmark]
        public byte[] SerializeToUtf8Bytes() => JsonSerializer.SerializeToUtf8Bytes(_value);

        [Benchmark]
        public async Task SerializeToStream()
        {
            _memoryStream.Position = 0;
            await JsonSerializer.SerializeAsync(_memoryStream, _value);
        }

        [Benchmark]
        public string SerializeObjectProperty() => JsonSerializer.Serialize(_objectWithObjectProperty);
    }
}
