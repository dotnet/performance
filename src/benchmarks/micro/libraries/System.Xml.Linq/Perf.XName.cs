// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Xml.Linq
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_XName
    {
        private XName _noNamespace;
        private XName _hasNamespace;

        [Benchmark]
        [MemoryRandomization]
        public XName CreateElement() => XName.Get("node");

        [Benchmark]
        public XName CreateElementWithNamespace() => XName.Get("Root", "http://www.example.test");

        [Benchmark]
        public XName CreateElementWithNamespaceImplicitOperator() => "{Namespace}Root";

        [GlobalSetup(Target = nameof(EmptyNameSpaceToString))]
        public void SetupEmptyNameSpaceToString() => _noNamespace = XName.Get("Root");

        [Benchmark]
        public string EmptyNameSpaceToString() => _noNamespace.ToString();

        [GlobalSetup(Target = nameof(NonEmptyNameSpaceToString))]
        public void SetupNonEmptyNameSpaceToString() => _hasNamespace = XName.Get("{http://www.example.test}Root");

        [Benchmark]
        public string NonEmptyNameSpaceToString() => _hasNamespace.ToString();
    }
}
