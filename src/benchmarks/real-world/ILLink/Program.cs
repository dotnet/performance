// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
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
        Environment.SetEnvironmentVariable("ILLINK_SAMPLE_PROJECT", sampleProjectFile);

        return BenchmarkSwitcher
            .FromAssembly(typeof(BasicBenchmark).Assembly)
            .Run(args, RecommendedConfig.Create(
                           artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(BasicBenchmark).Assembly.Location),
                                                                         "BenchmarkDotNet.Artifacts")),
                           mandatoryCategories: ImmutableHashSet.Create("ILLink"),
                           job: Job.Default.WithMaxRelativeError(0.01)))
            .ToExitCode();
    }
}