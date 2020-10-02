﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks.Serializers.Helpers;

namespace MicroBenchmarks.Serializers
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(CollectionsOfPrimitives))]
    [BenchmarkCategory(Categories.NoWASM)]
    public class Binary_FromStream<T>
    {
        private readonly T value;
        private readonly MemoryStream memoryStream;
        private readonly BinaryFormatter binaryFormatter;

        public Binary_FromStream()
        {
            value = DataGenerator.Generate<T>();

            // the stream is pre-allocated, we don't want the benchmarks to include stream allocaton cost
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            binaryFormatter = new BinaryFormatter();

            ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(DateTimeOffset), false).SetSurrogate(typeof(DateTimeOffsetSurrogate)); // https://stackoverflow.com/a/7046868
        }

        [GlobalSetup(Target = nameof(BinaryFormatter_))]
        public void SetupBinaryFormatter()
        {
            memoryStream.Position = 0;
            binaryFormatter.Serialize(memoryStream, value);
        }

        [GlobalSetup(Target = nameof(ProtoBuffNet))]
        public void SetupProtoBuffNet()
        {
            memoryStream.Position = 0;
            ProtoBuf.Serializer.Serialize(memoryStream, value);
        }

        [GlobalSetup(Target = nameof(MessagePack_))]
        public void SetupMessagePack()
        {
            memoryStream.Position = 0;
            MessagePack.MessagePackSerializer.Serialize<T>(memoryStream, value);
        }

        [BenchmarkCategory(Categories.Libraries)]
        [Benchmark(Description = nameof(BinaryFormatter))]
        public T BinaryFormatter_()
        {
            memoryStream.Position = 0;
            return (T)binaryFormatter.Deserialize(memoryStream);
        }

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "protobuf-net")]
        public T ProtoBuffNet()
        {
            memoryStream.Position = 0;
            return ProtoBuf.Serializer.Deserialize<T>(memoryStream);
        }

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "MessagePack")]
        public T MessagePack_()
        {
            memoryStream.Position = 0;
            return MessagePack.MessagePackSerializer.Deserialize<T>(memoryStream);
        }
    }
}
