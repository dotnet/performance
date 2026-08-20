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
            MonoAotLLVMToolChain monoAotToolchain = null;

            // Parse and remove any additional parameters that we need that aren't part of BDN
            try
            {
                argsList = CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-count", out partitionCount);
                argsList = CommandLineOptions.ParseAndRemoveIntParameter(argsList, "--partition-index", out partitionIndex);
                argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--exclusion-filter", out exclusionFilterValue);
                argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--category-exclusion-filter", out categoryExclusionFilterValue);
                CommandLineOptions.ParseAndRemoveBooleanParameter(argsList, "--disasm-diff", out getDiffableDisasm);

                // Extract monoaotllvm args not recognized by BDN and build the toolchain.
                argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--runtimes", out var runtimesValues);
                if (runtimesValues.Remove("monoaotllvm"))
                {
                    if (runtimesValues.Count > 0)
                    {
                        argsList.Add("--runtimes");
                        argsList.AddRange(runtimesValues);
                    }
                    argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--cli", out var cliPathValue);
                    if (cliPathValue.Count > 0)
                    {
                        argsList.Add("--cli");
                        argsList.AddRange(cliPathValue);
                    }
                    argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--packages", out var packagesPathValue);
                    if (packagesPathValue.Count > 0)
                    {
                        argsList.Add("--packages");
                        argsList.AddRange(packagesPathValue);
                    }
                    argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--aotcompilerpath", out var aotCompilerPathValue);
                    string aotCompilerPath;
                    if (aotCompilerPathValue.Count > 0)
                    {
                        aotCompilerPath = aotCompilerPathValue[0];
                        argsList.Add("--aotcompilerpath");
                        argsList.AddRange(aotCompilerPathValue);
                    }
                    else
                    {
                        aotCompilerPath = "";
                    }
                    argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--customruntimepack", out var customRuntimePackValue);
                    string customRuntimePack;
                    if (customRuntimePackValue.Count > 0)
                    {
                        customRuntimePack = customRuntimePackValue[0];
                        argsList.Add("--customruntimepack");
                        argsList.AddRange(customRuntimePackValue);

                    }
                    else
                    {
                        customRuntimePack = "";
                    }
                    argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--aotcompilermode", out var aotCompilerModeValue);
                    monoAotToolchain = new MonoAotLLVMToolChain(new MonoAotLLVMRuntime(Environment.Version), new()
                    {
                        CliPath = cliPathValue.Count > 0 ? new(cliPathValue[0]) : null,
                        PackagesPath = packagesPathValue.Count > 0 ? new(packagesPathValue[0]) : null,
                        TargetFrameworkMoniker = $"net{Environment.Version.Major}.0",
                        CustomRuntimePack = customRuntimePack,
                        AotCompilerPath = aotCompilerPath,
                        AotCompilerMode = aotCompilerModeValue.Count > 0 ? Enum.Parse<MonoAotCompilerMode>(aotCompilerModeValue[0], ignoreCase: true) : 0
                    });
                }
                else
                {
                    argsList.Add("--runtimes");
                    argsList.AddRange(runtimesValues);
                }

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
                        getDiffableDisasm: getDiffableDisasm,
                        toolchain: monoAotToolchain)
                    .AddValidator(new NoWasmValidator(Categories.NoWASM)))
                .ConfigureAwait(false);

            return summaries.ToExitCode();
        }
    }
}