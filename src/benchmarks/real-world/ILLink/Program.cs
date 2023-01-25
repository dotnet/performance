// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace ILLinkBenchmarks;

public class ILLinkBench
{
    public static int Main(string[] args)
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

        return BenchmarkSwitcher
            .FromAssembly(typeof(BasicBenchmark).Assembly)
            .Run(args, RecommendedConfig.Create(
                           artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(BasicBenchmark).Assembly.Location),
                                                                         "BenchmarkDotNet.Artifacts")),
                           mandatoryCategories: ImmutableHashSet.Create("ILLink"),
                           job: job))
            .ToExitCode();
    }
}