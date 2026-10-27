// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Filters;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Xunit;

namespace Tests
{
    public class AotFilterTests
    {
        [Theory]
        [InlineData("net12.0", true)]
        [InlineData("nativeaot12.0", false)]
        [InlineData("monoaot", false)]
        [InlineData("monowasmaot11.0", false)]
        [InlineData("corewasm12.0", true)]
        public void ExcludesOnlyAotRuntimes(string runtime, bool expected)
        {
            var config = ManualConfig.CreateEmpty().AddJob(Job.Default.WithRuntime(Runtime.Parse(runtime)));
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(Benchmarks), config);
            var benchmark = Assert.Single(benchmarks.BenchmarksCases);
            var filter = Assert.Single(new AotFilterAttribute("Requires dynamic code").Config.GetFilters());

            Assert.Equal(expected, filter.Predicate(benchmark));
        }

        public class Benchmarks
        {
            [Benchmark]
            public int Value() => 42;
        }
    }
}
