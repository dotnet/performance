using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace Benchmarks.Serializers
{
    //[Config(typeof(SgenConfig))] // currently blocked https://github.com/dotnet/corefx/issues/27281#issuecomment-367533728
    public class Xml_ToStream<T>
    {
        private readonly T value;
        private readonly XmlSerializer xmlSerializer;
        private readonly DataContractSerializer dataContractSerializer;
        private readonly MemoryStream memoryStream;

        public Xml_ToStream()
        {
            value = DataGenerator.Generate<T>();
            xmlSerializer = new XmlSerializer(typeof(T));
            dataContractSerializer = new DataContractSerializer(typeof(T));
            memoryStream = new MemoryStream(capacity: short.MaxValue);

#if SGEN
            // we need to give some hints to the SGEN tool ;)
            if (typeof(T) == typeof(LoginViewModel))
                xmlSerializer = new XmlSerializer(typeof(LoginViewModel));
            if (typeof(T) == typeof(Location))
                xmlSerializer = new XmlSerializer(typeof(Location));
            if (typeof(T) == typeof(IndexViewModel))
                xmlSerializer = new XmlSerializer(typeof(IndexViewModel));
            if (typeof(T) == typeof(MyEventsListerViewModel))
                xmlSerializer = new XmlSerializer(typeof(MyEventsListerViewModel));
#endif
        }

        [Benchmark(Description = nameof(XmlSerializer))]
        public void XmlSerializer_()
        {
            memoryStream.Position = 0;
            xmlSerializer.Serialize(memoryStream, value);
        }

        [Benchmark(Description = nameof(DataContractSerializer))]
        public void DataContractSerializer_()
        {
            memoryStream.Position = 0;
            dataContractSerializer.WriteObject(memoryStream, value);
        }

        // YAXSerializer is not included in the benchmarks because it does not allow to serialize to stream (only to file and string)

        [GlobalCleanup]
        public void Dispose() => memoryStream.Dispose();
    }

    public class SgenConfig : ManualConfig
    {
        public SgenConfig()
        {
            Add(Job.Dry.With(RunStrategy.ColdStart).WithLaunchCount(10) // Dry job is 1 execution without pre-Jitting
                .WithCustomBuildConfiguration("SGEN") // this is going to use Microsoft.XmlSerializer.Generator
                .With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp21)).WithId("SGEN"));

            Add(Job.Dry.With(RunStrategy.ColdStart).WithLaunchCount(10) // Dry job is 1 execution without pre-Jitting
                .With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp21)).WithId("NO_SGEN"));

            // to make sure that Benchmarks.XmlSerializers.dll file exists (https://github.com/dotnet/core/blob/master/samples/xmlserializergenerator-instructions.md)
            // you can uncomment the line below
            KeepBenchmarkFiles = true;
        }
    }
}
