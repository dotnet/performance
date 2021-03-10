using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Microsoft.Extensions.Configuration.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroBenchmarks.libraries.Microsoft.Extensions.Configuration
{
    [BenchmarkCategory(Categories.Libraries)]
    public class XmlConfigurationProviderBenchmarks
    {
        private MemoryStream simpleXml;
        private MemoryStream deepXml;
        private MemoryStream namesXml;
        private MemoryStream repeatedXml;
        private XmlConfigurationProvider provider;

        private MemoryStream ReadTestFile(string fileName)
        {
            using var fileStream = File.OpenRead(Path.Combine("./libraries/Microsoft.Extensions.Configuration.Xml/TestFiles", fileName));

            var memoryStream = new MemoryStream();

            fileStream.CopyTo(memoryStream);

            return memoryStream;
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.simpleXml = ReadTestFile("simple.xml");
            this.deepXml = ReadTestFile("simple.xml");
            this.namesXml = ReadTestFile("simple.xml");
            this.repeatedXml = ReadTestFile("simple.xml");

            this.provider = new XmlConfigurationProvider(new XmlConfigurationSource());
        }

        [SetUp]
        public void Setup()
        {
            this.simpleXml.Position = 0;
            this.deepXml.Position = 0;
            this.namesXml.Position = 0;
            this.repeatedXml.Position = 0;
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            this.simpleXml.Dispose();
            this.deepXml.Dispose();
            this.namesXml.Dispose();
            this.repeatedXml.Dispose();
        }

        [Benchmark]
        public void Simple()
        {
            provider.Load(this.simpleXml);
        }

        [Benchmark]
        public void Deep()
        {
            provider.Load(this.deepXml);
        }

        [Benchmark]
        public void Names()
        {
            provider.Load(this.namesXml);
        }

        [Benchmark]
        public void Repeated()
        {
            provider.Load(this.repeatedXml);
        }
    }
}
