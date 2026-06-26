using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Configuration.Xml
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class XmlConfigurationProviderBenchmarks
    {
        private MemoryStream _memoryStream;
        private XmlConfigurationProvider _provider;

        public IEnumerable<string> GetFileNames()
        {
            yield return "simple.xml";
            yield return "deep.xml";
            yield return "names.xml";
#if NET6_0_OR_GREATER // support added in .NET 6 (it throws on older TFMs)
            yield return "repeated.xml";
#endif
        }

        [ParamsSource(nameof(GetFileNames))]
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

        [GlobalCleanup]
        public void Cleanup() => _memoryStream.Dispose();

        [Benchmark]
        public void Load()
        {
            _memoryStream.Position = 0;

            _provider.Load(_memoryStream);
        }
    }
}
