// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace XmlDocumentTests.XmlNodeTests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_XmlNode
    {
        private XmlNode _node; 

        [GlobalSetup]
        public void Setup()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<a attr1='test' attr2='test2' />");
            _node = doc.DocumentElement;
        }
        
        [Benchmark]
        public XmlAttributeCollection GetAttributes() => _node.Attributes;

        [Benchmark]
        public string GetValue() => _node.Value;
    }
}
