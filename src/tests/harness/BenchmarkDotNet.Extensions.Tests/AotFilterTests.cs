// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Filters;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Xunit;

namespace Tests
{
    public class AotFilterTests
    {
        [Theory]
        [InlineData("net8.0", true)]
        [InlineData("net11.0", true)]
        [InlineData("net12.0", true)]
        [InlineData("nativeaot8.0", false)]
        [InlineData("nativeaot12.0", false)]
        [InlineData("monoaot", false)]
        [InlineData("monowasm11.0", true)]
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

        [Fact]
        public void ExcludesCustomMonoLlvmRuntime()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Default.WithRuntime(new MonoAotLLVMRuntime(new System.Version(11, 0))));
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(Benchmarks), config);
            var benchmark = Assert.Single(benchmarks.BenchmarksCases);
            var filter = Assert.Single(new AotFilterAttribute("Requires dynamic code").Config.GetFilters());

            Assert.False(filter.Predicate(benchmark));
        }

        public class Benchmarks
        {
            [Benchmark]
            public int Value() => 42;
        }
    }
}
