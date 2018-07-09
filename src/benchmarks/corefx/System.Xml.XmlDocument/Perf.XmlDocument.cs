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
        private const int innerIterations = 10000;

        private XmlDocument _doc;
        
        [Benchmark]
        public XmlDocument Create()
        {
            XmlDocument doc = default;
            
            for (int i = 0; i < innerIterations; i++)
            {
                doc = new XmlDocument(); doc =new XmlDocument(); doc = new XmlDocument();
                doc = new XmlDocument(); doc =new XmlDocument(); doc = new XmlDocument();
                doc = new XmlDocument(); doc =new XmlDocument(); doc = new XmlDocument();
            }

            return doc;
        }
        
        [GlobalSetup(Target = nameof(LoadXml))]
        public void SetupLoadXml() => _doc = new XmlDocument();

        [Benchmark]
        public void LoadXml()
        {
            XmlDocument doc = _doc;
            
            for (int i = 0; i < innerIterations; i++)
                doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
        }

        [GlobalSetup(Target = nameof(GetDocumentElement))]
        public void SetupGetDocumentElement()
        {
            _doc = new XmlDocument();
            _doc.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two e1; text node three </elem1>");
        }

        [Benchmark]
        public XmlNode GetDocumentElement()
        {
            XmlNode element = default;
            XmlDocument doc = _doc;

            for (int i = 0; i < innerIterations; i++)
            {
                element = doc.DocumentElement; element = doc.DocumentElement; element = doc.DocumentElement;
                element = doc.DocumentElement; element = doc.DocumentElement; element = doc.DocumentElement;
                element = doc.DocumentElement; element = doc.DocumentElement; element = doc.DocumentElement;
            }

            return element;
        }
    }
}
