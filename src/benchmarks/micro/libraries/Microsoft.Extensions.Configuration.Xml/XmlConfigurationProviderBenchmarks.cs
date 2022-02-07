using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration.Xml;
using System.IO;

namespace MicroBenchmarks.libraries.Microsoft.Extensions.Configuration
{
    [BenchmarkCategory(Categories.Libraries)]
    public class XmlConfigurationProviderBenchmarks
    {
        private MemoryStream _simpleXml;
        private MemoryStream _deepXml;
        private MemoryStream _namesXml;
        private MemoryStream _repeatedXml;
        private XmlConfigurationProvider _provider;

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
            _simpleXml = ReadTestFile("simple.xml");
            _deepXml = ReadTestFile("deep.xml");
            _namesXml = ReadTestFile("names.xml");
            _repeatedXml = ReadTestFile("repeated.xml");

            _provider = new XmlConfigurationProvider(new XmlConfigurationSource());
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _simpleXml.Position = 0;
            _deepXml.Position = 0;
            _namesXml.Position = 0;
            _repeatedXml.Position = 0;
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _simpleXml.Dispose();
            _deepXml.Dispose();
            _namesXml.Dispose();
            _repeatedXml.Dispose();
        }

        [Benchmark]
        public void Simple()
        {
            _provider.Load(_simpleXml);
        }

        [Benchmark]
        public void Deep()
        {
            _provider.Load(_deepXml);
        }

        [Benchmark]
        public void Names()
        {
            _provider.Load(_namesXml);
        }

        [Benchmark]
        public void Repeated()
        {
            _provider.Load(_repeatedXml);
        }
    }
}
