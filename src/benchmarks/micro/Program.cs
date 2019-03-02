// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using BenchmarkDotNet.Running;
using System.IO;
using BenchmarkDotNet.Extensions;

namespace MicroBenchmarks
{
    class Program
    {
        static int Main(string[] args)
            => BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args, RecommendedConfig.Create(
                    artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BenchmarkDotNet.Artifacts")), 
                    mandatoryCategories: ImmutableHashSet.Create(Categories.CoreFX, Categories.CoreCLR, Categories.ThirdParty)))
                .ToExitCode();
    }
}