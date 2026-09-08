// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using System.IO;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Configs;

namespace MicroBenchmarks
{
    class Program
    {
        // The MonoAotLLVM toolchain cannot build without these. Failing here reports an actionable message
        // through the ArgumentException handler below, instead of passing an empty path into the generated
        // project and failing deep inside MSBuild many minutes later.
        private static string RequireMonoAotLLVMValue(List<string> values, string parameter)
            => values.Count > 0
                ? values[0]
                : throw new ArgumentException($"{parameter} must be specified when running with --runtimes monoaotllvm");

        // The TFM this assembly was compiled for. Environment.Version reports the runtime that actually
        // loaded, which can roll forward to a newer major than --customruntimepack points at, producing an
        // opaque restore/publish failure in the generated project.
        private static string GetTargetFrameworkMoniker()
        {
            if (AppContext.TargetFrameworkName is string frameworkName)
            {
                var parsed = new FrameworkName(frameworkName);
                if (parsed.Identifier == ".NETCoreApp")
                {
                    return $"net{parsed.Version.Major}.{parsed.Version.Minor}";
                }
            }

            return $"net{Environment.Version.Major}.0";
        }

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
                // BDN spells this option "-r, --runtimes" and matches monikers case-insensitively, so accept both forms here.
                string runtimesArg = argsList.Contains("--runtimes") ? "--runtimes" : "-r";
                argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, runtimesArg, out var runtimesValues);
                int monoAotLLVMIndex = runtimesValues.FindIndex(runtime => string.Equals(runtime, "monoaotllvm", StringComparison.OrdinalIgnoreCase));
                if (monoAotLLVMIndex >= 0)
                {
                    runtimesValues.RemoveAt(monoAotLLVMIndex);
                    if (runtimesValues.Count > 0)
                    {
                        argsList.Add(runtimesArg);
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
                    string aotCompilerPath = RequireMonoAotLLVMValue(aotCompilerPathValue, "--aotcompilerpath");
                    argsList.Add("--aotcompilerpath");
                    argsList.AddRange(aotCompilerPathValue);

                    argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--customruntimepack", out var customRuntimePackValue);
                    string customRuntimePack = RequireMonoAotLLVMValue(customRuntimePackValue, "--customruntimepack");
                    argsList.Add("--customruntimepack");
                    argsList.AddRange(customRuntimePackValue);

                    argsList = CommandLineOptions.ParseAndRemoveStringsParameter(argsList, "--aotcompilermode", out var aotCompilerModeValue);
                    monoAotToolchain = new MonoAotLLVMToolChain(new MonoAotLLVMRuntime(Environment.Version), new()
                    {
                        CliPath = cliPathValue.Count > 0 ? new(cliPathValue[0]) : null,
                        PackagesPath = packagesPathValue.Count > 0 ? new(packagesPathValue[0]) : null,
                        TargetFrameworkMoniker = GetTargetFrameworkMoniker(),
                        CustomRuntimePack = customRuntimePack,
                        AotCompilerPath = aotCompilerPath,
                        AotCompilerMode = aotCompilerModeValue.Count > 0 ? Enum.Parse<MonoAotCompilerMode>(aotCompilerModeValue[0], ignoreCase: true) : MonoAotCompilerMode.mini
                    });
                }
                else if (runtimesValues.Count > 0)
                {
                    argsList.Add(runtimesArg);
                    argsList.AddRange(runtimesValues);
                }

                CommandLineOptions.ValidatePartitionParameters(partitionCount, partitionIndex);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("ArgumentException: {0}", e.Message);
                return 1;
            }

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