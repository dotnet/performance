// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Uri
    {
        public static IEnumerable<object[]> Ctor_MemberData()
        {
            yield return new object[] { "http://dot.net" };
            yield return new object[] { "https://contoso.com" };
            yield return new object[] { "https://CONTOSO.com" };
            yield return new object[] { "https://a.much.longer.domain.name" };
            yield return new object[] { "http://h\u00F6st.with.\u00FCnicode" };
            yield return new object[] { "http://xn--hst-sna.with.xn--nicode-2ya" };
        }

        public static IEnumerable<object[]> CtorIdnHostPathAndQuery_MemberData()
        {
            foreach (object[] schemeAndAuthority in Ctor_MemberData())
            {
                yield return new object[] { $"{schemeAndAuthority[0]}/path/with?key=value#fragment" };
            }

            string[] paths = new[]
            {
                "/",
                "/path?key1=value1&key2=value2&key3=value3&key4=value4",
                "/path with escapable values?key=va lue",
                "/path%20with%20escapable%20values?key=va%20lue",
                "/path with escapable values?key=\u00FCnicode",
                "/path%20with%20escapable%20values?key=%C3%BCnicode",
            };

            foreach (string path in paths)
            {
                yield return new object[] { $"http://host{path}" };
            }
        }

        public static IEnumerable<object[]> EscapeDataString_MemberData()
        {
            yield return new object[] { new string('a', 1000) }; // Nothing to escape
            yield return new object[] { new string('{', 1000) }; // ASCII that needs escaping
            yield return new object[] { new string('\u00FC', 1000) }; // Unicode
            yield return new object[] { string.Concat(Enumerable.Repeat("a{\u00FC", 333)) };
        }

        private static readonly Uri _uri = new Uri("http://contoso.com/path/with?key=value#fragment");

        [Benchmark]
        [ArgumentsSource(nameof(Ctor_MemberData))]
        public Uri Ctor(string input) => new Uri(input);

        [Benchmark]
        [Arguments("/new/path")]
        public Uri CombineAbsoluteRelative(string input) => new Uri(_uri, input);

        [Benchmark]
        public string ParseAbsoluteUri() => new Uri("http://127.0.0.1:80").AbsoluteUri;

        [Benchmark]
        public string DnsSafeHost() => new Uri("http://[fe80::3]%1").DnsSafeHost;

        [Benchmark]
        [MemoryRandomization]
        public string GetComponents() => _uri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped);

        [Benchmark]
        [MemoryRandomization]
        public string PathAndQuery() => _uri.PathAndQuery;

        [Benchmark]
        [ArgumentsSource(nameof(CtorIdnHostPathAndQuery_MemberData))]
        public (string, string) CtorIdnHostPathAndQuery(string input)
        {
            // Representative of the most common usage with HttpClient
            var uri = new Uri(input, UriKind.Absolute);
            return (uri.IdnHost, uri.PathAndQuery);
        }

        [Benchmark]
        [Arguments("abc%20def%20ghi%20")]
        [Arguments("%E4%BD%A0%E5%A5%BD")]
        public string UnescapeDataString(string input) => Uri.UnescapeDataString(input);

        [Benchmark]
        [ArgumentsSource(nameof(EscapeDataString_MemberData))]
        public string EscapeDataString(string input) => Uri.EscapeDataString(input);

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

        [Benchmark]
        public string UriBuilderReplacePort() => new UriBuilder(_uri) { Port = 8080 }.ToString();
    }
}
