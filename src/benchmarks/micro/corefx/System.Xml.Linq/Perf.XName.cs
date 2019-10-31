// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Xml.Linq
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_XName
    {
        private XName _noNamespace;
        private XName _hasNamespace;

        [GlobalSetup]
        public void Setup()
        {
            _noNamespace = XName.Get("Root");
            _hasNamespace = XName.Get("{http://www.example.test}Root");
        }

        [Benchmark]
        public XName GetLocalName() => _noNamespace.LocalName;

        [Benchmark]
        public string EmptyNameSpaceToString() => _noNamespace.ToString();

        [Benchmark]
        public XName GetLocalNameFromExpandedName() => _hasNamespace.LocalName;

        [Benchmark]
        public string NonEmptyNameSpaceToString() => _hasNamespace.ToString();
    }
}
