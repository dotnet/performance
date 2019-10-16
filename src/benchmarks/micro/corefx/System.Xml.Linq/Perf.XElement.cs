using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Xml.Linq
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_XElement
    {
        private XElement _element;

        [Benchmark]
        public XElement CreateElement() => new XElement("Root", "text node");

        [Benchmark(OperationsPerInvoke = 8)]
        public XElement CreateWithElements()
        {
            XElement doc = new XElement("Root",
                new XElement("elem1", "text node two e1; text node three"),
                new XElement("elem1", "text node two e1; text node three"),
                new XElement("elem1", "text node two e1; text node three"),
                new XElement("elem1", "text node two e1; text node three"),
                new XElement("elem1", "text node two e1; text node three"),
                new XElement("elem1", "text node two e1; text node three"),
                new XElement("elem1", "text node two e1; text node three"),
                new XElement("elem1", "text node two e1; text node three")
                );

            return doc;
        }

        [GlobalSetup]
        public void Setup()
        {
            _element = new XElement("Root",
                new XAttribute("id", 123),
                new XElement("Child1", 1),
                new XElement("Child2", 2),
                new XElement("Child3", 3),
                new XElement("Child4", 4),
                new XElement("Child5", 5)
            );
        }

        [Benchmark]
        public XElement GetElement() => _element.Element("Child4");
        
        [Benchmark]
        public XAttribute GetAttribute() => _element.Attribute("id");

        [Benchmark]
        public string GetValue() => _element.Value;

        [Benchmark]
        public XName GetXName() => _element.Name;
    }
}
