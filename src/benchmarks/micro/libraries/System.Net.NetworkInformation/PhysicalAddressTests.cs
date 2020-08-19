// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.NetworkInformation.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class PhysicalAddressTests
    {
        private readonly PhysicalAddress _short = new PhysicalAddress(new byte[1] { 42 });
        private readonly PhysicalAddress _long = new PhysicalAddress(Enumerable.Range(0, 256).Select(i => (byte)i).ToArray());

        [Benchmark]
        public void PAShort() => _short.ToString();

        [Benchmark]
        public void PALong() => _long.ToString();
    }
}
