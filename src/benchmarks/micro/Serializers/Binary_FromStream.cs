// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Filters;
using MicroBenchmarks.Serializers.Helpers;

namespace MicroBenchmarks.Serializers
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(CollectionsOfPrimitives))]
    [BenchmarkCategory(Categories.NoWASM)]
    [AotFilter("Dynamic code generation is not supported.")]
    public class Binary_FromStream<T>
    {
        private T value;
        private MemoryStream memoryStream;

        [GlobalSetup(Target = nameof(ProtoBuffNet))]
        public void SetupProtoBuffNet()
        {
            value = DataGenerator.Generate<T>();
            if (memoryStream is null) // to ensure it's done only once
            {
                ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(DateTimeOffset), false).SetSurrogate(typeof(DateTimeOffsetSurrogate)); // https://stackoverflow.com/a/7046868
            }
            // the stream is pre-allocated, we don't want the benchmarks to include stream allocaton cost
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;
            ProtoBuf.Serializer.Serialize(memoryStream, value);
        }

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "protobuf-net")]
        public T ProtoBuffNet()
        {
            memoryStream.Position = 0;
            return ProtoBuf.Serializer.Deserialize<T>(memoryStream);
        }

        [GlobalSetup(Target = nameof(MessagePack_))]
        public void SetupMessagePack()
        {
            value = DataGenerator.Generate<T>();
            // the stream is pre-allocated, we don't want the benchmarks to include stream allocation cost
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;
            MessagePack.MessagePackSerializer.Serialize<T>(memoryStream, value);
        }

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "MessagePack")]
        public T MessagePack_()
        {
            memoryStream.Position = 0;
            return MessagePack.MessagePackSerializer.Deserialize<T>(memoryStream);
        }

        [GlobalCleanup]
        public void Cleanup() => memoryStream.Dispose();
    }
}
