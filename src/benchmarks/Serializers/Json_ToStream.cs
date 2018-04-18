using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Serializers
{
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

        [Benchmark(Description = "Jil")]
        public void Jil_()
        {
            memoryStream.Position = 0;
            Jil.JSON.Serialize<T>(value, streamWriter);
        }

        [Benchmark(Description = "JSON.NET")]
        public void JsonNet_()
        {
            memoryStream.Position = 0;
            newtonSoftJsonSerializer.Serialize(streamWriter, value);
        }

        [Benchmark(Description = "Utf8Json")]
        public void Utf8Json_()
        {
            memoryStream.Position = 0;
            Utf8Json.JsonSerializer.Serialize(memoryStream, value);
        }

        [Benchmark(Description = "DataContractJsonSerializer")]
        public void DataContractJsonSerializer_()
        {
            memoryStream.Position = 0;
            dataContractJsonSerializer.WriteObject(memoryStream, value);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            streamWriter.Dispose();
            memoryStream.Dispose();
        }
    }
}
