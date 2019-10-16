using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Xml.Linq
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_XDocument
    {
        private XDocument _doc;

        [Benchmark]
        public XDocument Create() => new XDocument();

        [Benchmark]
        public XDocument Parse()
        {
            return XDocument.Parse("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
        }

        [Benchmark]
        public XDocument CreateWithRootElement()
        {
            return new XDocument(new XElement("Root", "text node two e1; text node three"));
        }

        [GlobalSetup(Target = nameof(GetRootElement))]
        public void SetupGetRootElement()
        {
            _doc = XDocument.Parse("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
        }

        [Benchmark]
        public XElement GetRootElement() => _doc.Root;
    }
}
