// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using CompilerBenchmarks;

string cscSourceDownloadLink = "https://github.com/dotnet/roslyn/releases/download/perf-assets-v2/CodeAnalysisReproWithAnalyzers.zip";
string sourceDownloadDir = Path.Combine(AppContext.BaseDirectory, "roslynSource");
var sourceDir = Path.Combine(sourceDownloadDir, "CodeAnalysisReproWithAnalyzers");
if (!Directory.Exists(sourceDir))
{
    await FileTasks.DownloadAndUnzip(cscSourceDownloadLink, sourceDownloadDir);
}

// Benchmark.NET creates a new process to run the benchmark, so the easiest way
// to communicate information is pass by environment variable
Environment.SetEnvironmentVariable(Helpers.TestProjectEnvVarName, sourceDir);

return BenchmarkSwitcher
    .FromAssembly(typeof(Helpers).Assembly)
    .Run(args, RecommendedConfig.Create(
                   artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Helpers).Assembly.Location),
                                                                 "BenchmarkDotNet.Artifacts")),
                   mandatoryCategories: ImmutableHashSet.Create("Roslyn"),
                   job: Job.Default.WithMaxRelativeError(0.01)))
    .ToExitCode();
