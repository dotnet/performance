// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Filters;

namespace MicroBenchmarks.Serializers
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(CollectionsOfPrimitives))]
    [GenericTypeArguments(typeof(XmlElement))]
    [GenericTypeArguments(typeof(SimpleStructWithProperties))]
    [GenericTypeArguments(typeof(ClassImplementingIXmlSerialiable))]
    [AotFilter("Currently not supported due to missing metadata.")]
    public class Xml_FromStream<T>
    {
        private T value;
        private XmlSerializer xmlSerializer;
        private DataContractSerializer dataContractSerializer;
        private MemoryStream memoryStream;
        private byte[] memoryBytes;
        private XmlDictionaryReader xmlDictionaryReader;

        [GlobalSetup(Target = nameof(XmlSerializer_))]
        public void SetupXmlSerializer()
        {
            value = DataGenerator.Generate<T>();
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;
            xmlSerializer = new XmlSerializer(typeof(T));
            xmlSerializer.Serialize(memoryStream, value);
        }

        [BenchmarkCategory(Categories.Libraries, Categories.Runtime, Categories.NoWasmCoreCLR)]
        [Benchmark(Description = nameof(XmlSerializer))]
        public T XmlSerializer_()
        {
            memoryStream.Position = 0;
            return (T)xmlSerializer.Deserialize(memoryStream);
        }

        [GlobalSetup(Target = nameof(DataContractSerializer_))]
        public void SetupDataContractSerializer()
        {
            value = DataGenerator.Generate<T>();
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;
            dataContractSerializer = new DataContractSerializer(typeof(T));
            dataContractSerializer.WriteObject(memoryStream, value);
        }

        [BenchmarkCategory(Categories.Libraries)]
        [Benchmark(Description = nameof(DataContractSerializer))]
        public T DataContractSerializer_()
        {
            memoryStream.Position = 0;
            return (T)dataContractSerializer.ReadObject(memoryStream);
        }

        [GlobalSetup(Target = nameof(DataContractSerializer_BinaryXml_))]
        public void SetupDataContractSerializer_BinaryXml_()
        {
            value = DataGenerator.Generate<T>();
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;
            dataContractSerializer = new DataContractSerializer(typeof(T));
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(memoryStream, null, null, ownsStream: false))
               dataContractSerializer.WriteObject(writer, value);

            memoryBytes = memoryStream.ToArray();
            xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(memoryBytes, XmlDictionaryReaderQuotas.Max);
        }

        [BenchmarkCategory(Categories.Libraries)]
        [Benchmark(Description = nameof(XmlDictionaryReader))]
        public T DataContractSerializer_BinaryXml_()
        {
            ((IXmlBinaryReaderInitializer)xmlDictionaryReader).SetInput(memoryBytes, 0, memoryBytes.Length, null, XmlDictionaryReaderQuotas.Max, null, null);
            return (T)dataContractSerializer.ReadObject(xmlDictionaryReader);
        }

      // YAXSerializer is not included in the benchmarks because it does not allow to deserialize from stream (only from file and string)

        [GlobalCleanup]
        public void Cleanup()
        {
            xmlDictionaryReader?.Dispose();
            memoryStream?.Dispose();
        }
    }
}
