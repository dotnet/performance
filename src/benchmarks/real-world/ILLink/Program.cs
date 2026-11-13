// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace ILLinkBenchmarks;

public class ILLinkBench
{
    // Use RunAsync (not Run) so BDN does not install its single-threaded
    // BenchmarkDotNetSynchronizationContext on the entrypoint thread.
    public static async Task<int> Main(string[] args)
    {
        string thisAssembly = Assembly.GetExecutingAssembly().Location;
        string sampleProjectFile = Path.Combine(Path.GetDirectoryName(thisAssembly), "SampleProject", "HelloWorld.csproj");

        // Benchmark.NET creates a new process to run the benchmark, so the easiest way
        // to communicate information is pass by environment variable
        Environment.SetEnvironmentVariable("ILLINK_SAMPLE_PROJECT", sampleProjectFile);

        Job job = Job.Default
            .WithLaunchCount(5)
            .WithWarmupCount(3)
            .WithIterationCount(20)
            .WithStrategy(RunStrategy.Monitoring)
            .WithMaxRelativeError(0.01);

        var summaries = await BenchmarkSwitcher
            .FromAssembly(typeof(BasicBenchmark).Assembly)
            .RunAsync(args, RecommendedConfig.Create(
                           artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(BasicBenchmark).Assembly.Location),
                                                                         "BenchmarkDotNet.Artifacts")),
                           mandatoryCategories: ImmutableHashSet.Create("ILLink"),
                           job: job))
            .ConfigureAwait(false);
        return summaries.ToExitCode();
    }
}