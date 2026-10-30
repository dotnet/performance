// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRun;
using Xunit;

namespace Tests
{
    public class MonoAotLLVMTests
    {
        [Theory]
        [InlineData("--runtimes", "monoaotllvm")]
        [InlineData("-r", "MonoAotLLVM")]
        public void HistoricalArgumentsSelectCustomLlvmAndAssemblyTarget(string runtimeOption, string runtime)
        {
            var args = new List<string> { runtimeOption, runtime, "--aotcompilerpath", "mono-aot-cross",
                "--customruntimepack", "mono-pack", "--aotcompilermode", "llvm",
                "--cli", "dotnet", "--packages", "packages", "--filter", "*Value*" };

            var toolchain = RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v8.0");
            Assert.NotNull(toolchain);
            Assert.Equal(new Version(8, 0), toolchain.Runtime.Version);
            var builder = Assert.IsType<MonoAotLLVMBuilder>(toolchain.Builder);
            var settings = Assert.IsType<MonoAotLLVMSettings>(builder.Settings);
            Assert.Equal("net8.0", settings.TargetFrameworkMoniker);
            Assert.Equal(MonoAotCompilerMode.llvm, settings.AotCompilerMode);
            Assert.Equal(Path.GetFullPath("mono-aot-cross"), settings.AotCompilerPath);
            Assert.Equal(Path.GetFullPath("mono-pack"), settings.CustomRuntimePack);
            Assert.Equal(Path.GetFullPath("dotnet"), settings.CliPath!.FullName);
            Assert.Equal(Path.GetFullPath("packages"), settings.PackagesPath!.FullName);
            Assert.Equal(new[] { "--cli", "dotnet", "--packages", "packages", "--filter", "*Value*" },
                RuntimeArgumentCompatibility.Normalize(args));
        }

        [Theory]
        [InlineData("--runtimes monoaotllvm")]
        [InlineData("--runtimes monoaotllvm --aotcompilerpath compiler")]
        [InlineData("--runtimes monoaotllvm --aotcompilerpath compiler --customruntimepack pack --aotcompilermode wasm")]
        [InlineData("--runtimes monoaotllvm --aotcompilerpath compiler --customruntimepack pack --aotcompilermode 2")]
        [InlineData("--runtimes monoaotllvm net11.0")]
        [InlineData("--runtimes net11.0 monoaotllvm")]
        [InlineData("--runtimes monoaotllvm --runtimes net11.0")]
        [InlineData("--runtimes monoaotllvm monoaotllvm")]
        public void InvalidLlvmRequestsCannotSilentlyReplaceAJob(string commandLine)
        {
            Assert.Throws<ArgumentException>(() => RuntimeArgumentCompatibility.ParseMonoAotLLVM(
                new List<string>(commandLine.Split(' ')), ".NETCoreApp,Version=v11.0"));
        }

        [Fact]
        public void NonLlvmRequestIsUnchanged()
        {
            var args = new List<string> { "--runtimes", "wasmnet11_0", "--aotcompilermode", "wasm" };
            var original = args.ToArray();

            Assert.Null(RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(original, args);
        }

        [Theory]
        [InlineData("--corerun", "CoreRun")]
        [InlineData("--corerun=CoreRun", null)]
        [InlineData("--CORERUN", "CoreRun")]
        public void CoreRunCannotReplaceRequestedMonoLlvm(string option, string? value)
        {
            var args = new List<string> { "--runtimes", "monoaotllvm",
                "--aotcompilerpath", "compiler", "--customruntimepack", "pack", option };
            if (value is not null)
            {
                args.Add(value);
            }

            var original = args.ToArray();
            Assert.Throws<ArgumentException>(() =>
                RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(original, args);
        }

        [Fact]
        public void CoreRunWithoutMonoLlvmIsUnchanged()
        {
            var args = new List<string> { "--corerun", "CoreRun" };
            Assert.Null(RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(new[] { "--corerun", "CoreRun" }, args);
        }

        [Fact]
        public void BenchmarkDotNetCoreRunExpansionReplacesTheDefaultToolchain()
        {
            string coreRun = Path.GetTempFileName();
            try
            {
                var settings = new MonoAotLLVMSettings
                {
                    TargetFrameworkMoniker = "net11.0", AotCompilerPath = "compiler",
                    CustomRuntimePack = "pack", AotCompilerMode = MonoAotCompilerMode.llvm
                };
                var mono = new MonoAotLLVMToolChain(new MonoAotLLVMRuntime(new Version(11, 0)), settings);
                var original = ManualConfig.CreateEmpty().AddJob(Job.Default.WithToolchain(mono).AsDefault());
                var (success, parsed, _) = ConfigParser.Parse(new[] { "--corerun", coreRun }, NullLogger.Instance, original);

                Assert.True(success);
                Assert.IsType<CoreRunToolchain>(Assert.Single(parsed!.GetJobs()).Infrastructure.Toolchain);
            }
            finally
            {
                File.Delete(coreRun);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task CompilerAndRuntimePackResolveAgainstInvocationDirectory(bool relativeCompiler, bool relativePack)
        {
            string compiler = Path.Combine("compiler & directory", "mono-aot-cross");
            string pack = "runtime & pack";
            if (!relativeCompiler)
            {
                compiler = Path.GetFullPath(compiler);
            }
            if (!relativePack)
            {
                pack = Path.GetFullPath(pack);
            }
            var args = new List<string> { "--runtimes", "monoaotllvm",
                "--aotcompilerpath", compiler, "--customruntimepack", pack };
            var toolchain = RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0");
            var settings = Assert.IsType<MonoAotLLVMSettings>(Assert.IsType<MonoAotLLVMBuilder>(toolchain!.Builder).Settings);

            Assert.Equal(Path.GetFullPath(compiler), settings.AotCompilerPath);
            Assert.Equal(Path.GetFullPath(pack), settings.CustomRuntimePack);
            Assert.True(Path.IsPathFullyQualified(settings.AotCompilerPath));
            Assert.True(Path.IsPathFullyQualified(settings.CustomRuntimePack));
            var project = await CreateProject(settings);
            Assert.Equal(Path.GetFullPath(compiler), Assert.Single(project.Descendants("MonoAOTCompiler")).Attribute("CompilerBinaryPath")!.Value);
            Assert.Equal(Path.GetFullPath(pack), Assert.Single(project.Descendants("MicrosoftNetCoreAppRuntimePackDir")).Value);
            Assert.Contains("&amp;", project.ToString());
        }

        [Theory]
        [InlineData(MonoAotCompilerMode.mini, "false")]
        [InlineData(MonoAotCompilerMode.llvm, "true")]
        public async Task GeneratedProjectPreservesCustomPackCompilerAndPublishMode(MonoAotCompilerMode mode, string useLlvm)
        {
            var settings = new MonoAotLLVMSettings
            {
                TargetFrameworkMoniker = "net11.0",
                CustomRuntimePack = "custom & pack",
                AotCompilerPath = "compiler & path",
                AotCompilerMode = mode
            };
            var project = await CreateProject(settings);

            Assert.Equal("Microsoft.NET.Sdk", project.Attribute("Sdk")!.Value);
            Assert.Contains(project.Descendants("TargetFrameworks"), value => value.Value == "net11.0");
            Assert.Equal("custom & pack", Assert.Single(project.Descendants("MicrosoftNetCoreAppRuntimePackDir")).Value);
            var selfContained = Assert.Single(project.Descendants("SelfContained"));
            Assert.Equal("true", selfContained.Value);
            Assert.Equal("'$(BenchmarkDotNetGatherReferences)' != 'true'", selfContained.Parent!.Attribute("Condition")!.Value);
            var aot = Assert.Single(project.Descendants("MonoAOTCompiler"));
            Assert.Equal("compiler & path", aot.Attribute("CompilerBinaryPath")!.Value);
            Assert.Equal(useLlvm, aot.Attribute("UseLLVM")!.Value);
            Assert.Equal("mcpu=native", Assert.Single(project.Descendants("AotArguments")).Value);
            Assert.Equal("Publish", aot.Parent!.Attribute("AfterTargets")!.Value);
            Assert.Equal("$(MicrosoftNetCoreAppRuntimePackDir)",
                Assert.Single(project.Descendants("PackageDirectory")).Value);
            Assert.Contains(project.Elements("Import"), import => import.Attribute("Project")?.Value == "$(MonoTargetsPath)");
            Assert.True(new InspectableBuilder(settings).Publishes);
        }

        private static async Task<XElement> CreateProject(MonoAotLLVMSettings settings)
        {
            var toolchain = new MonoAotLLVMToolChain(new MonoAotLLVMRuntime(new Version(11, 0)), settings);
            await using var run = await BenchmarkConverter.TypeToBenchmarksAsync(typeof(Benchmarks),
                ManualConfig.CreateEmpty().AddJob(Job.Default.WithToolchain(toolchain)));
            var partition = new BuildPartition(new[] { new BenchmarkBuildInfo(
                Assert.Single(run.BenchmarksCases), run.Config, 0, run.CompositeInProcessDiagnoser) }, EngineParameters.DefaultResolver);
            return new InspectableBuilder(settings).Project(partition).Root!;
        }

        private sealed class InspectableBuilder(MonoAotLLVMSettings settings) : MonoAotLLVMBuilder(settings)
        {
            public XDocument Project(BuildPartition partition) => CreateProject(partition, ArtifactsPaths.Empty, NullLogger.Instance);
            public bool Publishes => PublishesOutput;
        }

        public class Benchmarks
        {
            [Benchmark]
            public int Value() => 42;
        }
    }
}
