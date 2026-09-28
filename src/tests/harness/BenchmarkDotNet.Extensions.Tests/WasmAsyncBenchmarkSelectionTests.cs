// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines.Tests;
using System.IO.Tests;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Tests;
using BenchmarkDotNet.Running;
using MicroBenchmarks;
using Xunit;

namespace Tests
{
    public class WasmAsyncBenchmarkSelectionTests
    {
        [Theory]
        [InlineData(typeof(Perf_AsyncMethods), nameof(Perf_AsyncMethods.Yield))]
        [InlineData(typeof(ValueTaskPerfTest), nameof(ValueTaskPerfTest.CreateAndAwait_FromYieldingAsyncMethod))]
        [InlineData(typeof(MemoryStreamTests), nameof(MemoryStreamTests.CopyToAsync))]
        [InlineData(typeof(MemoryStreamChunkedTests), nameof(MemoryStreamChunkedTests.ReadAsyncMemory))]
        [InlineData(typeof(MemoryStreamChunkedTests), nameof(MemoryStreamChunkedTests.WriteAsyncMemory))]
        [InlineData(typeof(MemoryStreamChunkedTests), nameof(MemoryStreamChunkedTests.CopyToAsyncWithBufferSize))]
        public void AsyncBenchmarkIsNotExcludedFromWasm(Type benchmarkType, string methodName)
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(benchmarkType).BenchmarksCases
                .Where(benchmark => benchmark.Descriptor.WorkloadMethod.Name == methodName)
                .ToArray();

            Assert.NotEmpty(benchmarks);
            Assert.All(benchmarks, benchmark => Assert.DoesNotContain(Categories.NoWASM, benchmark.Descriptor.Categories));
        }

        [Fact]
        public void OtherAsyncBenchmarksRemainExcludedFromWasm()
        {
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(Perf_Pipe)).BenchmarksCases;

            Assert.NotEmpty(benchmarks);
            Assert.All(benchmarks, benchmark => Assert.Contains(Categories.NoWASM, benchmark.Descriptor.Categories));
        }
    }
}
