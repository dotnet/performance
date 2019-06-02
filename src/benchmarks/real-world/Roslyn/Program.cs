// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Columns;

namespace CompilerBenchmarks
{
    public class Program
    {
        private static IConfig CustomConfig(DirectoryInfo artifactsPath, ImmutableHashSet<string> mandatoryCategories, int? partitionCount = null, int? partitionIndex = null)
            => DefaultConfig.Instance
                .With(Job.Default) // tell BDN that this are our default settings
                .WithArtifactsPath(artifactsPath.FullName)
                .With(MemoryDiagnoser.Default) // MemoryDiagnoser is enabled by default
                .With(new OperatingSystemFilter())
                .With(new PartitionFilter(partitionCount, partitionIndex))
                .With(JsonExporter.Full) // make sure we export to Json (for BenchView integration purpose)
                .With(new PerfLabExporter())
                .With(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
                .With(TooManyTestCasesValidator.FailOnError)
                .With(new UniqueArgumentsValidator()) // don't allow for duplicated arguments #404
                .With(new MandatoryCategoryValidator(mandatoryCategories));

        public static async Task<int> Main(string[] args)
        {
            await Setup();

            return BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args, CustomConfig(
                    artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BenchmarkDotNet.Artifacts")),
                    mandatoryCategories: ImmutableHashSet.Create("Roslyn")))
                .ToExitCode();
        }

        private static async Task Setup()
        {
            string cscSourceDownloadLink = "https://roslyninfra.blob.core.windows.net/perf-artifacts/CodeAnalysisRepro.zip";
            string sourceDownloadDir = Path.Combine(AppContext.BaseDirectory, "roslynSource");
            var sourceDir = Path.Combine(sourceDownloadDir, "CodeAnalysisRepro");
            if (!Directory.Exists(sourceDir))
            {
                await FileTasks.DownloadAndUnzip(cscSourceDownloadLink, sourceDownloadDir);
            }

            // Benchmark.NET creates a new process to run the benchmark, so the easiest way
            // to communicate information is pass by environment variable
            Environment.SetEnvironmentVariable(Helpers.TestProjectEnvVarName, sourceDir);
        }
    }
}
