using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroBenchmarks;
using System.Collections.Generic;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Linq
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_XElementList
    {
        private readonly Consumer _consumer = new Consumer();
        private IEnumerable<XElement> _list;

        [GlobalSetup(Target = nameof(Enumerator))]
        public void SetupEnumerator()
        {
            XElement doc = XElement.Parse("<a><sub1/><sub1/><sub2/><sub1/><sub2/><sub1/><sub2/><sub1/><sub2/><sub1/><sub2/><sub2/></a>");
            _list = doc.DescendantsAndSelf();
        }

        [Benchmark]
        public void Enumerator()
        {
            IEnumerable<XElement> list = _list;
            Consumer consumer = _consumer;

            foreach (var element in list)
            {
                consumer.Consume(element);
            }
        }
    }
}
