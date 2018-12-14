// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Net;

namespace System.Net.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_WebUtility
    {
        [Benchmark]
        public string Decode_DecodingRequired() => WebUtility.UrlDecode("abcdefghijklmnopqrstuvwxy%22");

        [Benchmark]
        public string Decode_NoDecodingRequired() => WebUtility.UrlDecode("abcdefghijklmnopqrstuvwxyz");
    }
}
