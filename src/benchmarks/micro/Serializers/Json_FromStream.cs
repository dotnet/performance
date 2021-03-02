// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.Serializers
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(CollectionsOfPrimitives))]
    public class Json_FromStream<T>
    {
        private T value;
        private MemoryStream memoryStream;
        private DataContractJsonSerializer dataContractJsonSerializer;
        private Newtonsoft.Json.JsonSerializer newtonSoftJsonSerializer;

        [GlobalSetup(Target = nameof(Jil_))]
        public void SetupJil_()
        {
            value = DataGenerator.Generate<T>();

            // the stream is pre-allocated, we don't want the benchmarks to include stream allocaton cost
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;

            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, short.MaxValue, leaveOpen: true))
            {
                Jil.JSON.Serialize<T>(value, writer, Jil.Options.ISO8601);
                writer.Flush();
            }
        }

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Jil")]
        public T Jil_()
        {
            memoryStream.Position = 0;

            using (var reader = CreateNonClosingReaderWithDefaultSizes())
                return Jil.JSON.Deserialize<T>(reader, Jil.Options.ISO8601);
        }

        [GlobalSetup(Target = nameof(JsonNet_))]
        public void SetupJsonNet_()
        {
            value = DataGenerator.Generate<T>();

            // the stream is pre-allocated, we don't want the benchmarks to include stream allocaton cost
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;

            newtonSoftJsonSerializer = new Newtonsoft.Json.JsonSerializer();

            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, short.MaxValue, leaveOpen: true))
            {
                newtonSoftJsonSerializer.Serialize(writer, value);
                writer.Flush();
            }
        }

        [BenchmarkCategory(Categories.Runtime, Categories.Libraries, Categories.ThirdParty)] // JSON.NET is so popular that despite being 3rd Party lib we run the benchmarks for CoreFX and CoreCLR CI
        [Benchmark(Description = "JSON.NET")]
        public T JsonNet_()
        {
            memoryStream.Position = 0;

            using (var reader = CreateNonClosingReaderWithDefaultSizes())
                return (T)newtonSoftJsonSerializer.Deserialize(reader, typeof(T));
        }

        [GlobalSetup(Target = nameof(Utf8Json_))]
        public void SetupUtf8Json_()
        {
            value = DataGenerator.Generate<T>();

            // the stream is pre-allocated, we don't want the benchmarks to include stream allocaton cost
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;
            Utf8Json.JsonSerializer.Serialize<T>(memoryStream, value);
        }

        [BenchmarkCategory(Categories.ThirdParty)]
        [Benchmark(Description = "Utf8Json")]
        public T Utf8Json_()
        {
            memoryStream.Position = 0;
            return Utf8Json.JsonSerializer.Deserialize<T>(memoryStream);
        }

        [GlobalSetup(Target = nameof(DataContractJsonSerializer_))]
        public void SetupDataContractJsonSerializer_()
        {
            value = DataGenerator.Generate<T>();

            // the stream is pre-allocated, we don't want the benchmarks to include stream allocaton cost
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;
            dataContractJsonSerializer = new DataContractJsonSerializer(typeof(T));
            dataContractJsonSerializer.WriteObject(memoryStream, value);
        }

        [BenchmarkCategory(Categories.Runtime, Categories.Libraries)]
        [Benchmark(Description = "DataContractJsonSerializer")]
        public T DataContractJsonSerializer_()
        {
            memoryStream.Position = 0;
            return (T)dataContractJsonSerializer.ReadObject(memoryStream);
        }

        [GlobalCleanup]
        public void Cleanup() => memoryStream.Dispose();

        private StreamReader CreateNonClosingReaderWithDefaultSizes()
            => new StreamReader(
                memoryStream, 
                Encoding.UTF8, 
                true, // default is true https://github.com/dotnet/corefx/blob/708e4537d8944199af7d580def0d97a030be98c7/src/Common/src/CoreLib/System/IO/StreamReader.cs#L98
                1024, // default buffer size from CoreFX https://github.com/dotnet/corefx/blob/708e4537d8944199af7d580def0d97a030be98c7/src/Common/src/CoreLib/System/IO/StreamReader.cs#L27 
                leaveOpen: true); // we want to reuse the same string in the benchmarks to make sure that cost of allocating stream is not included in the benchmarks
    }
}
