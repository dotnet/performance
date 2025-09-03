// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace MicroBenchmarks
{
    class Program
    {
        static int Main(string[] args)
        {
            if (!PerfLabCommandLineOptions.TryParse(args, out var options, out var bdnOnlyArgs))
                return 1;

            var config = 
                RecommendedConfig.Create(
                    artifactsPath: new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts")),
                    mandatoryCategories: ImmutableHashSet.Create(Categories.Libraries, Categories.Runtime, Categories.ThirdParty),
                    options: options)
                .AddValidator(new NoWasmValidator(Categories.NoWASM));

            if (options.Manifest is BenchmarkManifest manifest)
                return RunWithManifest(bdnOnlyArgs, manifest, config);

            return BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(bdnOnlyArgs, config)
                .ToExitCode();
        }

        private static int RunWithManifest(string[] args, BenchmarkManifest manifest, IConfig config)
        {
            var logger = config.GetLoggers().First();
            var (isParsingSuccess, parsedConfig, options) = ConfigParser.Parse(args, logger, config);
            if (!isParsingSuccess)
                return 1; // ConfigParser.Parse will print the error message

            var effectiveConfig = ManualConfig.Union(config, parsedConfig);

            var (allTypesValid, allAvailableTypesWithRunnableBenchmarks) = TypeFilter.GetTypesWithRunnableBenchmarks(
                types: Enumerable.Empty<Type>(),
                assemblies: new [] { typeof(Program).Assembly }, 
                logger);

            if (!allTypesValid)
                return 1; // TypeFilter.GetTypesWithRunnableBenchmarks will print the error message

            if (allAvailableTypesWithRunnableBenchmarks.Count == 0)
            {
                logger.WriteLineError("No runnable benchmarks found before applying filters.");
                return 1;
            }

            var filteredBenchmarks = TypeFilter.Filter(effectiveConfig, allAvailableTypesWithRunnableBenchmarks);
            if (filteredBenchmarks.Length == 0)
            {
                logger.WriteLineError("No runnable benchmarks found after applying filters.");
                return 1;
            }

            if (manifest.BenchmarkCaseRunOverrides is not null)
            {
                var overriddenBenchmarks = new List<BenchmarkRunInfo>();
                foreach (var benchmarkRunInfo in filteredBenchmarks)
                {
                    var updatedCases = new List<BenchmarkCase>(benchmarkRunInfo.BenchmarksCases.Length);
                    foreach (var benchmarkCase in benchmarkRunInfo.BenchmarksCases)
                    {
                        var benchmarkName = FullNameProvider.GetBenchmarkName(benchmarkCase);
                        if (manifest.BenchmarkCaseRunOverrides.TryGetValue(benchmarkName, out var overrideRunInfo))
                        {
                            var updatedJob = overrideRunInfo.ModifyJob(benchmarkCase.Job, benchmarkCase);
                            var newBenchmarkCase = BenchmarkCase.Create(benchmarkCase.Descriptor, updatedJob, benchmarkCase.Parameters, benchmarkCase.Config);
                            updatedCases.Add(newBenchmarkCase);
                        }
                        else
                        {
                            updatedCases.Add(benchmarkCase);
                        }
                    }

                    overriddenBenchmarks.Add(new BenchmarkRunInfo(updatedCases.ToArray(), benchmarkRunInfo.Type, benchmarkRunInfo.Config));
                }

                filteredBenchmarks = overriddenBenchmarks.ToArray();
            }

            return BenchmarkRunner.Run(filteredBenchmarks).ToExitCode();
        }
    }
}