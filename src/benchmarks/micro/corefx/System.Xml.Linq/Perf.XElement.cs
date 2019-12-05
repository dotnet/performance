// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Xml.Linq
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_XElement
    {
        private XElement _element;

        [Benchmark]
        public XElement CreateElement() => new XElement("Root", "text node");

        [Benchmark]
        public XElement CreateElementWithNamespace() => new XElement("{Namespace}Root", "text node");

        [Benchmark(OperationsPerInvoke = 8)]
        public XElement CreateWithElements()
        {
            XElement doc = new XElement("Root",   // Typical XElement creation scenario with children 
                new XElement("elem1", "some xml element content"),
                new XElement("elem1", "some xml element content"),
                new XElement("elem1", "some xml element content"),
                new XElement("elem1", "some xml element content"),
                new XElement("elem1", "some xml element content"),
                new XElement("elem1", "some xml element content"),
                new XElement("elem1", "some xml element content"),
                new XElement("elem1", "some xml element content")
            );

            return doc;
        }

        [Benchmark(OperationsPerInvoke = 8)]
        public XElement CreateElementsWithNamespace()
        {
            XElement doc = new XElement("Root",
                new XElement("{namespace}elem1", "some xml element content"),
                new XElement("{namespace}elem1", "some xml element content"),
                new XElement("{namespace}elem1", "some xml element content"),
                new XElement("{namespace}elem1", "some xml element content"),
                new XElement("{namespace}elem1", "some xml element content"),
                new XElement("{namespace}elem1", "some xml element content"),
                new XElement("{namespace}elem1", "some xml element content"),
                new XElement("{namespace}elem1", "some xml element content")
            );

            return doc;
        }

        [GlobalSetup]
        public void Setup()
        {
            _element = new XElement("Root",
                new XAttribute("id", 123),
                new XElement("Child1", 1),
                new XElement("{ns}Child2", 2),
                new XElement("Child3", 3),
                new XElement("Child4", 4),
                new XElement("{ns}Child4", 4),
                new XElement("Child5", 5)
            );
        }

        [Benchmark]
        public XElement GetElement() => _element.Element("Child4");

        [Benchmark]
        public XElement GetElementWithNamespace() => _element.Element("{ns}Child4");

        [Benchmark]
        public XAttribute GetAttribute() => _element.Attribute("id");

        [Benchmark]
        public string GetValue() => _element.Value;
    }
}
