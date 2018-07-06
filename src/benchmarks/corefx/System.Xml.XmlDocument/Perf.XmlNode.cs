// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using BenchmarkDotNet.Attributes;

namespace XmlDocumentTests.XmlNodeTests
{
    public class Perf_XmlNode
    {
        private const int innerIterations = 10000;

        private XmlNode _node; 

        [GlobalSetup]
        public void Setup()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<a attr1='test' attr2='test2' />");
            _node = doc.DocumentElement;
        }
        
        [Benchmark]
        public XmlAttributeCollection GetAttributes()
        {
            XmlAttributeCollection attr = default;
            XmlNode node = _node;

            for (int i = 0; i < innerIterations; i++)
            {
                attr = node.Attributes; attr = node.Attributes; attr = node.Attributes;
                attr = node.Attributes; attr = node.Attributes; attr = node.Attributes;
                attr = node.Attributes; attr = node.Attributes; attr = node.Attributes;
            }

            return attr;
        }

        [Benchmark]
        public string GetValue()
        {
            string value = default;
            XmlNode node = _node;

            for (int i = 0; i < innerIterations; i++)
            {
                value = node.Value; value = node.Value; value = node.Value;
                value = node.Value; value = node.Value; value = node.Value;
                value = node.Value; value = node.Value; value = node.Value;
            }

            return value;
        }
    }
}
