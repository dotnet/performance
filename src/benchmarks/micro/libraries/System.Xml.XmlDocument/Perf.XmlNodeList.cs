// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroBenchmarks;

namespace XmlDocumentTests.XmlNodeListTests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_XmlNodeList
    {
        private readonly Consumer _consumer = new Consumer();
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
            Consumer consumer = _consumer;

            foreach (var element in list)
            {
                consumer.Consume(element);
            }
        }
        
        [GlobalSetup(Target = nameof(GetCount))]
        public void SetupGetCount()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<a><sub1/><sub2/></a>");
            _list = doc.DocumentElement.ChildNodes;
        }

        [Benchmark]
        public int GetCount() => _list.Count;
    }
}
