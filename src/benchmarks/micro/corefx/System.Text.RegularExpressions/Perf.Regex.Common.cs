// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.RegularExpressions.Tests
{
    /// <summary>
    /// Performance tests for Regular Expressions
    /// </summary>
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Regex_Common
    {
        private const string EmailPattern = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,12}|[0-9]{1,3})(\]?)$";
        private const string DatePattern = @"\b(?<month>\d{1,2})/(?<day>\d{1,2})/(?<year>\d{2,4})\b";
        private const string IPPattern = @"(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9])\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9])";
        private const string UriPattern = @"[\w]+://[^/\s?#]+[^\s?#]+(?:\?[^\s#]*)?(?:#[^\s]*)?";

        private Regex _email, _date, _ip, _uri;

        [Params(RegexOptions.None, RegexOptions.Compiled)]
        public RegexOptions Options { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _email = new Regex(EmailPattern, Options);
            _date = new Regex(DatePattern, Options);
            _ip = new Regex(IPPattern, Options);
            _uri = new Regex(UriPattern, Options);
        }

        [Benchmark] public void Email() => _email.IsMatch("yay.performance@dot.net");
        [Benchmark] public void Date() => _date.IsMatch("Today is 11/18/2019");
        [Benchmark] public void IP() => _ip.IsMatch("012.345.678.910");
        [Benchmark] public void Uri() => _uri.IsMatch("http://example.org");

        [Benchmark] public void EmailStatic() => Regex.IsMatch("yay.performance@dot.net", EmailPattern, Options);
        [Benchmark] public void DateStatic() => Regex.IsMatch("Today is 11/18/2019", DatePattern, Options);
        [Benchmark] public void IPStatic() => Regex.IsMatch("012.345.678.910", IPPattern, Options);
        [Benchmark] public void UriStatic() => Regex.IsMatch("http://example.org", UriPattern, Options);

        [Benchmark] public void Ctor() => new Regex(@"(^(.*)(\(([0-9]+),([0-9]+)\)): )(error|warning) ([A-Z]+[0-9]+) ?: (.*)", Options);
    }
}
