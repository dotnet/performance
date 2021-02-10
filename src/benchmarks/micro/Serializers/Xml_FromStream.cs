// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using BenchmarkDotNet.Attributes;

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
    public class Xml_FromStream<T>
    {
        private T value;
        private XmlSerializer xmlSerializer;
        private DataContractSerializer dataContractSerializer;
        private MemoryStream memoryStream;

        [GlobalSetup(Target = nameof(XmlSerializer_))]
        public void SetupXmlSerializer()
        {
            value = DataGenerator.Generate<T>();
            memoryStream = new MemoryStream(capacity: short.MaxValue);
            memoryStream.Position = 0;
            xmlSerializer = new XmlSerializer(typeof(T));
            xmlSerializer.Serialize(memoryStream, value);
        }

        [BenchmarkCategory(Categories.Libraries, Categories.Runtime)]
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

        // YAXSerializer is not included in the benchmarks because it does not allow to deserialize from stream (only from file and string)

        [GlobalCleanup]
        public void Cleanup() => memoryStream.Dispose();
    }
}
