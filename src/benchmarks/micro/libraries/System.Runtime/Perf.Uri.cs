// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Uri
    {
        private Uri _uri = new Uri("http://dot.net");

        [Benchmark]
        public Uri Ctor() => new Uri("http://dot.net");

        [Benchmark]
        public string ParseAbsoluteUri() => new Uri("http://127.0.0.1:80").AbsoluteUri;

        [Benchmark]
        public string DnsSafeHost() => new Uri("http://[fe80::3]%1").DnsSafeHost;

        [Benchmark]
        public string GetComponents() => _uri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped);

        [Benchmark]
        public string PathAndQuery() => _uri.PathAndQuery;

        [Benchmark]
        public string Unescape() => Uri.UnescapeDataString("%E4%BD%A0%E5%A5%BD");

        [Benchmark]
        public string BuilderToString()
        {
            var builder = new UriBuilder();
            builder.Scheme = "https";
            builder.Host = "dotnet.microsoft.com";
            builder.Port = 443;
            builder.Path = "/platform/try-dotnet";
            return builder.ToString();
        }
    }
}
