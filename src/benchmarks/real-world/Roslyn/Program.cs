// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;

namespace CompilerBenchmarks
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            await Setup();

            return BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args, RecommendedConfig.Create(
                               artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location),
                                                                             "BenchmarkDotNet.Artifacts")),
                               mandatoryCategories: ImmutableHashSet.Create("Roslyn"),
                               job: Job.Default.WithMaxRelativeError(0.01)))
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
