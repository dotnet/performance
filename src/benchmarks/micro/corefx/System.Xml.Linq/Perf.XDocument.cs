// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Xml.Linq
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_XDocument
    {
        private XDocument _doc;
        private XElement _root;

        [Benchmark]
        public XDocument Create() => new XDocument();

        [Benchmark]
        public XDocument Parse()
        {
            return XDocument.Parse("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> some xml element content </elem1>");
        }

        [GlobalSetup(Target = nameof(CreateWithRootlEement))]
        public void SetupRootElement()
        {
            _root = new XElement("Root", "some xml element content");
        }

        [Benchmark]
        public XDocument CreateWithRootlEement()
        {
            return new XDocument(_root);
        }

        [GlobalSetup(Target = nameof(GetRootElement))]
        public void SetupGetRootElement()
        {
            _doc = XDocument.Parse("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> some xml element content <elem2>Hello</elem2></elem1>");
        }

        [Benchmark]
        public XElement GetRootElement() => _doc.Root;

        [Benchmark]
        public XElement Getlement() => _doc.Element("elem2");
    }
}
