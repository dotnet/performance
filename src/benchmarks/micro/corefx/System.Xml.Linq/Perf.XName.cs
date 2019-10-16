using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Xml.Linq
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_XName
    {
        private XName _noNamespace;
        private XName _hasNamespace;

        [GlobalSetup]
        public void Setup()
        {
            _noNamespace = XName.Get("Root");
            _hasNamespace = XName.Get("{http://www.example.test}Root");
        }

        [Benchmark]
        public XName GetLocalName() => _noNamespace.LocalName;

        [Benchmark]
        public string GetEmptyNameSpaceName() => _noNamespace.NamespaceName;

        [Benchmark]
        public XName GetLocalNameFromExpandedName() => _hasNamespace.LocalName;

        [Benchmark]
        public string GetNonEmptyNameSpaceName() => _hasNamespace.NamespaceName;
    }
}
