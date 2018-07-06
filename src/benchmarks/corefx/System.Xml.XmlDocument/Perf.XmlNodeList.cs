// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using BenchmarkDotNet.Attributes;

namespace XmlDocumentTests.XmlNodeListTests
{
    public class Perf_XmlNodeList
    {
        private const int innerIterations = 10000;

        private XmlNodeList _list;

        [GlobalSetup(Target = nameof(Enumerator))]
        public void SetupEnumerator()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<a><sub1/><sub1/><sub2/><sub1/><sub2/><sub1/><sub2/><sub1/><sub2/><sub1/><sub2/><sub2/></a>");
            _list = doc.DocumentElement.ChildNodes;
        }
        
        [Benchmark]
        public void Enumerator()
        {
            XmlNodeList list = _list;

            for (int i = 0; i < innerIterations; i++)
                foreach (var element in list) { }
        }
        
        [GlobalSetup(Target = nameof(GetCount))]
        public void SetupGetCount()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<a><sub1/><sub2/></a>");
            _list = doc.DocumentElement.ChildNodes;
        }

        [Benchmark]
        public int GetCount()
        {
            int count = default;
            XmlNodeList list = _list;

            for (int i = 0; i < innerIterations; i++)
            {
                count = list.Count; count = list.Count; count = list.Count;
                count = list.Count; count = list.Count; count = list.Count;
                count = list.Count; count = list.Count; count = list.Count;
            }

            return count;
        }
    }
}
