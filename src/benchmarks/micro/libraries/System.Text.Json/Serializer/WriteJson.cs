// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Filters;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using System.Buffers;
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
    [GenericTypeArguments(typeof(DateTimeOffset?))]
    [GenericTypeArguments(typeof(int))]
    [AotFilter("Currently not supported due to missing metadata.")]
    public class WriteJson<T>
    {
        private T _value;
        private MemoryStream _memoryStream;
        private object _objectWithObjectProperty;

        private ArrayBufferWriter _bufferWriter;
        private Utf8JsonWriter _writer;

        [GlobalSetup]
        public async Task Setup()
        {
            _value = DataGenerator.Generate<T>();

            _memoryStream = new MemoryStream(capacity: short.MaxValue);
            await JsonSerializer.SerializeAsync(_memoryStream, _value);

            _objectWithObjectProperty = new { Prop = (object)_value };

            _bufferWriter = new ArrayBufferWriter();
            _writer = new Utf8JsonWriter(_bufferWriter);
        }

        [GlobalCleanup]
        public void Cleanup() => _memoryStream.Dispose();

        [Benchmark]
        public string SerializeToString() => JsonSerializer.Serialize(_value);

        [Benchmark]
        public byte[] SerializeToUtf8Bytes() => JsonSerializer.SerializeToUtf8Bytes(_value);

        [Benchmark]
        public void SerializeToWriter()
        {
            JsonSerializer.Serialize(_writer, _value);
            _bufferWriter.Reset();
            _writer.Reset();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task SerializeToStream()
        {
            _memoryStream.Position = 0;
            await JsonSerializer.SerializeAsync(_memoryStream, _value);
        }

        [Benchmark]
        public string SerializeObjectProperty() => JsonSerializer.Serialize(_objectWithObjectProperty);

        private sealed class ArrayBufferWriter : IBufferWriter<byte>
        {
            private int _offset = 0;
            private byte[] _buffer = new byte[128];

            // Clearing buffers not necessary in this benchmark.
            public void Reset() => _offset = 0;

            public void Advance(int count) => _offset += count;

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                int size = EnsureSize(sizeHint);
                return _buffer.AsMemory(_offset, size);
            }

            public Span<byte> GetSpan(int sizeHint = 0)
            {
                int size = EnsureSize(sizeHint);
                return _buffer.AsSpan(_offset, size);
            }

            private int EnsureSize(int sizeHint)
            {
                int totalSize = _buffer.Length;
                int size = sizeHint == 0 ? 128 : sizeHint;

                if ((uint)(_offset + size) > (uint)totalSize)
                {
                    int newSize = Math.Max(2 * totalSize, _offset + size);
                    Array.Resize(ref _buffer, newSize);
                }

                return size;
            }
        }
    }
}
