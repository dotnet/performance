// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;

namespace CompilerBenchmarks
{
    /// <summary>
    /// A collection of benchmarks for Roslyn APIs
    /// </summary>
    [BenchmarkCategory("Roslyn")]
    public class RoslynApis
    {
        [Benchmark]
        public ImmutableArray<AnalyzerConfigOptionsResult> BuildAnalyzerConfigs()
        {
            var cmdLineArgs = Helpers.GetReproCommandLineArgs();
            var analyzerConfigs = cmdLineArgs.AnalyzerConfigPaths
               .SelectAsArray(p => AnalyzerConfig.Parse(File.ReadAllText(p), p));
            var set = AnalyzerConfigSet.Create(analyzerConfigs);

            return cmdLineArgs.SourceFiles
                .SelectAsArray(s => set.GetOptionsForSourcePath(s.Path));
        }
    }
}
