// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class DnsTests
    {
        [Benchmark]
        public IPHostEntry GetHostEntry() => Dns.GetHostEntry("34.206.253.53");

        [Benchmark]
        public string GetHostName() => Dns.GetHostName();
    }
}
