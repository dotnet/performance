using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration.Xml;
using System.IO;

namespace MicroBenchmarks.libraries.Microsoft.Extensions.Configuration
{
    [BenchmarkCategory(Categories.Libraries)]
    public class XmlConfigurationProviderBenchmarks
    {
        private MemoryStream _memoryStream;
        private XmlConfigurationProvider _provider;

        [Params("simple.xml", "deep.xml", "names.xml", "repeated.xml")]
        public string FileName { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _provider = new XmlConfigurationProvider(new XmlConfigurationSource());

            using (FileStream fileStream = File.OpenRead(Path.Combine("./libraries/Microsoft.Extensions.Configuration.Xml/TestFiles", FileName)))
            {
                _memoryStream = new MemoryStream();

                fileStream.CopyTo(_memoryStream);
            }
        }

        [Benchmark]
        public void Load()
        {
            _memoryStream.Position = 0;

            _provider.Load(_memoryStream);
        }
    }
}
