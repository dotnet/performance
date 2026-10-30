// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using Xunit;

namespace Tests
{
    public class BenchmarkIdentityTests
    {
        [Theory]
        [InlineData(nameof(System.Tests.Perf_Enum.InterpolateIntoSpan_NonFlags), 4)]
        [InlineData(nameof(System.Tests.Perf_Enum.InterpolateIntoStringBuilder_NonFlags), 10)]
        public async Task TypedEnumArgumentsKeepHistoricalExportAndPartitionKeys(string methodName, int partition)
        {
            var run = BenchmarkConverter.TypeToBenchmarks(typeof(System.Tests.Perf_Enum),
                ManualConfig.CreateEmpty().AddJob(Job.Default));
            var cases = run.BenchmarksCases.Where(c => c.Descriptor.WorkloadMethod.Name == methodName).ToArray();
            var six = Assert.Single(cases, c => Convert.ToInt32(c.Parameters.GetArgument("value").Value) == 6);
            var argument = six.Parameters.GetArgument("value").Value;
            Assert.IsType<System.Tests.Perf_Enum.Colors>(argument);
            Assert.Equal(6, Convert.ToInt32(argument));
            Assert.Equal(typeof(System.Tests.Perf_Enum.Colors),
                Assert.Single(six.Descriptor.WorkloadMethod.GetParameters()).ParameterType);
            Assert.Contains("Orange, Yellow", FullNameProvider.GetBenchmarkName(six));

            var names = await ExportNames(cases);
            Assert.Contains($"System.Tests.Perf_Enum.{methodName}(value: Saturday)", names);
            Assert.Contains($"System.Tests.Perf_Enum.{methodName}(value: 42)", names);
            Assert.Equal(2, names.Length);
            Assert.Equal(partition, Assert.Single(Enumerable.Range(0, 15),
                index => new PartitionFilter(15, index).Predicate(six)));
            Assert.IsType<System.Tests.Perf_Enum.Colors>(six.Parameters.GetArgument("value").Value);
        }

        [Fact]
        public async Task OtherEnumCasesKeepTheirExistingExportAndPartitionKeys()
        {
            var run = BenchmarkConverter.TypeToBenchmarks(typeof(System.Tests.Perf_Enum),
                ManualConfig.CreateEmpty().AddJob(Job.Default));
            var cases = run.BenchmarksCases.Where(c =>
                c.Descriptor.WorkloadMethod.Name is not (
                    nameof(System.Tests.Perf_Enum.InterpolateIntoSpan_NonFlags) or
                    nameof(System.Tests.Perf_Enum.InterpolateIntoStringBuilder_NonFlags))
                || Convert.ToInt32(c.Parameters.GetArgument("value").Value) != 6).ToArray();
            var expected = cases.Select(FullNameProvider.GetBenchmarkName).OrderBy(name => name).ToArray();
            Assert.Equal(expected, (await ExportNames(cases)).OrderBy(name => name).ToArray());
            foreach (var benchmark in cases)
            {
                var name = FullNameProvider.GetBenchmarkName(benchmark);
                var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name));
                var partition = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(hash.AsSpan(0, 4)) % 15;
                Assert.Equal((int)partition, Assert.Single(Enumerable.Range(0, 15),
                    index => new PartitionFilter(15, index).Predicate(benchmark)));
            }
        }

        [Fact]
        public async Task SameMethodAndArgumentOnAnotherTypeAreNotRemapped()
        {
            var run = BenchmarkConverter.TypeToBenchmarks(typeof(UnrelatedEnumBenchmarks),
                ManualConfig.CreateEmpty().AddJob(Job.Default));
            var benchmark = Assert.Single(run.BenchmarksCases);
            Assert.Equal(FullNameProvider.GetBenchmarkName(benchmark),
                Assert.Single(await ExportNames(new[] { benchmark })));
        }

        public class UnrelatedEnumBenchmarks
        {
            [Benchmark]
            [Arguments((System.Tests.Perf_Enum.Colors)6)]
            public System.Tests.Perf_Enum.Colors InterpolateIntoSpan_NonFlags(System.Tests.Perf_Enum.Colors value) => value;
        }

        private static async Task<string[]> ExportNames(BenchmarkCase[] cases)
        {
            string directory = Path.Combine(Path.GetTempPath(), "performance-export-identity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var reports = cases.Select(benchmark => new BenchmarkReport(
                    true, benchmark, BuildResult.Success(ArtifactsPaths.Empty),
                    new[] { new ExecuteResult(true, 0, 1,
                        new[] { "WorkloadResult   1: 1 op, 100.00 ns, 100.0000 ns/op" },
                        Array.Empty<string>(), Array.Empty<string>(), 0) },
                    null)).ToImmutableArray();
                var summary = new Summary("identity", reports, HostEnvironmentInfo.GetCurrent(),
                    directory, Path.Combine(directory, "log.txt"), TimeSpan.Zero,
                    CultureInfo.InvariantCulture, [], []);
                await new PerfLabExporter(new ExportEnvironment()).ExportAsync(summary, NullLogger.Instance, CancellationToken.None);
                using var json = JsonDocument.Parse(await File.ReadAllTextAsync(
                    Assert.Single(Directory.GetFiles(directory, "*-perf-lab-report.json"))));
                return json.RootElement.GetProperty("tests").EnumerateArray()
                    .Select(test => test.GetProperty("name").GetString()!).ToArray();
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private sealed class ExportEnvironment : Reporting.IEnvironment
        {
            private readonly Dictionary<string, string> _values = new()
            {
                ["PERFLAB_INLAB"] = "1",
                ["PERFLAB_BUILDTIMESTAMP"] = "1970-01-01T00:00:00Z",
            };

            public string GetEnvironmentVariable(string variable) => _values.TryGetValue(variable, out var value) ? value : "";
            public IDictionary GetEnvironmentVariables() => _values;
        }
    }
}
