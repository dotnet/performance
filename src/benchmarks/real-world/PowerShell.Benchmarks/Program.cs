// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Extensions;

namespace MicroBenchmarks
{
    public sealed class Program
    {
        // Use RunAsync (not Run) so BDN does not install its single-threaded
        // BenchmarkDotNetSynchronizationContext on the entrypoint thread.
        public static async Task<int> Main(string[] args)
        {
            var argsList = new List<string>(args);
            int? partitionCount;
            int? partitionIndex;
            List<string> exclusionFilterValue;
            List<string> categoryExclusionFilterValue;
            bool getDiffableDisasm;

            // Parse and remove any additional parameters that we need that aren't part of BDN (BenchmarkDotnet)
            try
            {
                CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out partitionCount);
                CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out partitionIndex);
                CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--exclusion-filter", out exclusionFilterValue);
                CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--category-exclusion-filter", out categoryExclusionFilterValue);
                CommandLineOptions.ParseAndRemoveBooleanParameter(argsList, "--disasm-diff", out getDiffableDisasm);

                CommandLineOptions.ValidatePartitionParameters(partitionCount, partitionIndex);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException: {0}", e.Message);
                return 1;
            }

            var summaries = await BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .RunAsync(
                    argsList.ToArray(),
                    RecommendedConfig.Create(
                        artifactsPath: new DirectoryInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "BenchmarkDotNet.Artifacts")),
                        mandatoryCategories: ImmutableHashSet.Create(Categories.Components, Categories.Engine),
                        partitionCount: partitionCount,
                        partitionIndex: partitionIndex,
                        exclusionFilterValue: exclusionFilterValue,
                        categoryExclusionFilterValue: categoryExclusionFilterValue,
                        getDiffableDisasm: getDiffableDisasm))
                .ConfigureAwait(false);
            return summaries.ToExitCode();
        }
    }
}
