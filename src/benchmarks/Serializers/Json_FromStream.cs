using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Serializers
{
    public class Json_FromStream<T>
    {
        private readonly T value;

        private readonly MemoryStream memoryStream;

        private DataContractJsonSerializer dataContractJsonSerializer;
        private Newtonsoft.Json.JsonSerializer newtonSoftJsonSerializer;

        public Json_FromStream()
        {
            value = DataGenerator.Generate<T>();

            // the stream is pre-allocated, we don't want the benchmarks to include stream allocaton cost
            memoryStream = new MemoryStream(capacity: short.MaxValue);

            dataContractJsonSerializer = new DataContractJsonSerializer(typeof(T));
            newtonSoftJsonSerializer = new Newtonsoft.Json.JsonSerializer();
        }

        [IterationSetup(Target = nameof(Jil_))]
        public void SetupJil_()
        {
            memoryStream.Position = 0;

            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, short.MaxValue, leaveOpen: true))
            {
                Jil.JSON.Serialize<T>(value, writer);
                writer.Flush();
            }
        }

        [IterationSetup(Target = nameof(JsonNet_))]
        public void SetupJsonNet_()
        {
            memoryStream.Position = 0;

            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, short.MaxValue, leaveOpen: true))
            {
                newtonSoftJsonSerializer.Serialize(writer, value);
                writer.Flush();
            }
        }

        [IterationSetup(Target = nameof(Utf8Json_))]
        public void SetupUtf8Json_()
        {
            memoryStream.Position = 0;
            Utf8Json.JsonSerializer.Serialize<T>(memoryStream, value);
        }

        [IterationSetup(Target = nameof(DataContractJsonSerializer_))]
        public void SetupDataContractJsonSerializer_()
        {
            memoryStream.Position = 0;
            dataContractJsonSerializer.WriteObject(memoryStream, value);
        }

        [Benchmark(Description = "Jil")]
        public T Jil_()
        {
            memoryStream.Position = 0;

            using (var reader = CreateNonClosingReaderWithDefaultSizes())
                return Jil.JSON.Deserialize<T>(reader);
        }

        [Benchmark(Description = "JSON.NET")]
        public T JsonNet_()
        {
            memoryStream.Position = 0;

            using (var reader = CreateNonClosingReaderWithDefaultSizes())
                return (T)newtonSoftJsonSerializer.Deserialize(reader, typeof(T));
        }

        [Benchmark(Description = "Utf8Json")]
        public T Utf8Json_()
        {
            memoryStream.Position = 0;
            return Utf8Json.JsonSerializer.Deserialize<T>(memoryStream);
        }

        [Benchmark(Description = "DataContractJsonSerializer")]
        public T DataContractJsonSerializer_()
        {
            memoryStream.Position = 0;
            return (T)dataContractJsonSerializer.ReadObject(memoryStream);
        }

        private StreamReader CreateNonClosingReaderWithDefaultSizes()
            => new StreamReader(
                memoryStream, 
                Encoding.UTF8, 
                true, // default is true https://github.com/dotnet/corefx/blob/708e4537d8944199af7d580def0d97a030be98c7/src/Common/src/CoreLib/System/IO/StreamReader.cs#L98
                1024, // default buffer size from CoreFX https://github.com/dotnet/corefx/blob/708e4537d8944199af7d580def0d97a030be98c7/src/Common/src/CoreLib/System/IO/StreamReader.cs#L27 
                leaveOpen: true); // we want to reuse the same string in the benchmarks to make sure that cost of allocating stream is not included in the benchmarks

        [GlobalCleanup]
        public void Cleanup() => memoryStream.Dispose();
    }
}
