﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using BenchmarkDotNet.Extensions;

namespace CompilerBenchmarks
{
    public class Program
    {
        public static async Task Main()
        {
            var config = RecommendedConfig.Create(
                artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BenchmarkDotNet.Artifacts")), 
                mandatoryCategories: ImmutableHashSet.Create<string>("Roslyn"));

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

            _ = BenchmarkRunner.Run<StageBenchmarks>(config);
        }
    }
}
