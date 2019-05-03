// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    public class Json_ToStream<T>
    {
        private readonly T value;

        private readonly MemoryStream memoryStream;
        private readonly StreamWriter streamWriter;

        private DataContractJsonSerializer dataContractJsonSerializer;
        private Newtonsoft.Json.JsonSerializer newtonSoftJsonSerializer;

        public Json_ToStream()
        {
            value = DataGenerator.Generate<T>();

            // the stream is pre-allocated, we don't want the benchmarks to include stream allocaton cost
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);

            dataContractJsonSerializer = new DataContractJsonSerializer(typeof(T));
            newtonSoftJsonSerializer = new Newtonsoft.Json.JsonSerializer();
        }

        [BenchmarkCategory(Categories.ThirdParty, Categories.JsonSerializer)]
        [Benchmark(Description = "Jil")]
        public void Jil_()
        {
            memoryStream.Position = 0;
            Jil.JSON.Serialize<T>(value, streamWriter);
        }

        [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.ThirdParty, Categories.JsonSerializer)]
        [Benchmark(Description = "JSON.NET")]
        public void JsonNet_()
        {
            memoryStream.Position = 0;
            newtonSoftJsonSerializer.Serialize(streamWriter, value);
        }

        [BenchmarkCategory(Categories.ThirdParty, Categories.JsonSerializer)]
        [Benchmark(Description = "Utf8Json")]
        public void Utf8Json_()
        {
            memoryStream.Position = 0;
            Utf8Json.JsonSerializer.Serialize(memoryStream, value);
        }

        [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.JsonSerializer)]
        [Benchmark(Description = "DataContractJsonSerializer")]
        public void DataContractJsonSerializer_()
        {
            memoryStream.Position = 0;
            dataContractJsonSerializer.WriteObject(memoryStream, value);
        }

        [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.JsonSerializer)]
        [Benchmark(Description = "System.Text.Json")]
        public async Task SystemTextJson_()
        {
            memoryStream.Position = 0;
            await JsonSerializer.WriteAsync(value, memoryStream);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            streamWriter.Dispose();
            memoryStream.Dispose();
        }
    }
}
