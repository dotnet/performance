// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace XmlDocumentTests.XmlDocumentTests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_XmlDocument
    {
        private XmlDocument _doc;
        
        [Benchmark]
        public XmlDocument Create() => new XmlDocument();
        
        [Benchmark(OperationsPerInvoke = 8)]
        public XmlDocument LoadXml()
        {
            XmlDocument doc =  new XmlDocument();

            doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
            doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
            doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
            doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
            
            doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
            doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
            doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
            doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");

            return doc;
        }

        [GlobalSetup(Target = nameof(GetDocumentElement))]
        public void SetupGetDocumentElement()
        {
            _doc = new XmlDocument();
            _doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
        }

        [Benchmark]
        public XmlNode GetDocumentElement() => _doc.DocumentElement;
    }
}
