// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using System.IO;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Configs;

namespace MicroBenchmarks
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var argsList = new List<string>(args);
            int? partitionCount;
            int? partitionIndex;
            List<string> exclusionFilterValue;
            List<string> categoryExclusionFilterValue;
            bool getDiffableDisasm;

            // Parse and remove any additional parameters that we need that aren't part of BDN
            try
            {
                argsList = CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out partitionCount);
                argsList = CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out partitionIndex);
                argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--exclusion-filter", out exclusionFilterValue);
                argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--category-exclusion-filter", out categoryExclusionFilterValue);
                CommandLineOptions.ParseAndRemoveBooleanParameter(argsList, "--disasm-diff", out getDiffableDisasm);

                CommandLineOptions.ValidatePartitionParameters(partitionCount, partitionIndex);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException: {0}", e.Message);
                return 1;
            }

            // Use RunAsync (not Run) so BDN does not install its single-threaded
            // BenchmarkDotNetSynchronizationContext on the entrypoint thread. The sync
            // entrypoint installs that context before benchmark discovery, which
            // deadlocks any sync-over-async work performed by [ParamsSource]/[ArgumentsSource]
            // callbacks (e.g. SslStreamTests.GetTls13Support).
            var summaries = await BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .RunAsync(argsList.ToArray(),
                    RecommendedConfig.Create(
                        artifactsPath: new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts")), 
                        mandatoryCategories: ImmutableHashSet.Create([Categories.Libraries, Categories.Runtime, Categories.ThirdParty, Categories.Sve]),
                        partitionCount: partitionCount,
                        partitionIndex: partitionIndex,
                        exclusionFilterValue: exclusionFilterValue,
                        categoryExclusionFilterValue: categoryExclusionFilterValue,
                        getDiffableDisasm: getDiffableDisasm)
                    .AddValidator(new NoWasmValidator(Categories.NoWASM)))
                .ConfigureAwait(false);

            return summaries.ToExitCode();
        }
    }
}