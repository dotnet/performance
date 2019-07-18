// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Uri
    {
        [Benchmark]
        public string ParseAbsoluteUri() => new Uri("http://127.0.0.1:80").AbsoluteUri;

        [Benchmark]
        public string DnsSafeHost() => new Uri("http://[fe80::3]%1").DnsSafeHost;
    }
}
