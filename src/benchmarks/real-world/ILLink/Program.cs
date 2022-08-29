// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using CompilerBenchmarks;
using ILLinkBenchmarks;

namespace ILLinkBenchmarks;

public class ILLinkBench
{
    public static int Main(string[] args)
    {
        // string cscSourceDownloadLink = "https://github.com/dotnet/roslyn/releases/download/perf-assets-v2/CodeAnalysisReproWithAnalyzers.zip";
        // string sourceDownloadDir = Path.Combine(AppContext.BaseDirectory, "roslynSource");
        // var sourceDir = Path.Combine(sourceDownloadDir, "CodeAnalysisReproWithAnalyzers");
        // if (!Directory.Exists(sourceDir))
        // {
        //     await FileTasks.DownloadAndUnzip(cscSourceDownloadLink, sourceDownloadDir);
        // }

        // Benchmark.NET creates a new process to run the benchmark, so the easiest way
        // to communicate information is pass by environment variable
        //Environment.SetEnvironmentVariable(Helpers.TestProjectEnvVarName, sourceDir);

        Console.WriteLine($"DOTNET_HOST_PATH: {Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")}");
        string thisAssembly = Assembly.GetExecutingAssembly().Location;
        string sampleProjectFile = Path.Combine(Path.GetDirectoryName(thisAssembly), "SampleProject", "HelloWorld.csproj");
        Environment.SetEnvironmentVariable("ILLINK_SAMPLE_PROJECT", sampleProjectFile);
        Console.WriteLine($"Sample Project File: {sampleProjectFile}");

        return BenchmarkSwitcher
            .FromAssembly(typeof(BasicBenchark).Assembly)
            .Run(args, RecommendedConfig.Create(
                           artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(BasicBenchark).Assembly.Location),
                                                                         "BenchmarkDotNet.Artifacts")),
                           mandatoryCategories: ImmutableHashSet.Create("ILLink"),
                           job: Job.Default.WithMaxRelativeError(0.01)))
            .ToExitCode();
    }
}