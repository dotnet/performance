using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Serializers
{
    public class Xml_FromStream<T>
    {
        private readonly T value;
        private readonly XmlSerializer xmlSerializer;
        private readonly DataContractSerializer dataContractSerializer;
        private readonly MemoryStream memoryStream;

        public Xml_FromStream()
        {
            value = DataGenerator.Generate<T>();
            xmlSerializer = new XmlSerializer(typeof(T));
            dataContractSerializer = new DataContractSerializer(typeof(T));
            memoryStream = new MemoryStream(capacity: short.MaxValue);
        }

        [GlobalSetup(Target = nameof(XmlSerializer_))]
        public void SetupXmlSerializer()
        {
            memoryStream.Position = 0;
            xmlSerializer.Serialize(memoryStream, value);
        }

        [GlobalSetup(Target = nameof(DataContractSerializer_))]
        public void SetupDataContractSerializer()
        {
            memoryStream.Position = 0;
            dataContractSerializer.WriteObject(memoryStream, value);
        }

        [Benchmark(Description = nameof(XmlSerializer))]
        public T XmlSerializer_()
        {
            memoryStream.Position = 0;
            return (T)xmlSerializer.Deserialize(memoryStream);
        }

        [Benchmark(Description = nameof(DataContractSerializer))]
        public T DataContractSerializer_()
        {
            memoryStream.Position = 0;
            return (T)dataContractSerializer.ReadObject(memoryStream);
        }

        // YAXSerializer is not included in the benchmarks because it does not allow to deserialize from stream (only from file and string)

        [GlobalCleanup]
        public void Cleanup() => memoryStream.Dispose();
    }
}
