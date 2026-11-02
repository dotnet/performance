// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Wasm;
using Xunit;

namespace Tests
{
    public class RuntimeArgumentParsingTests
    {
        [Theory]
        [InlineData("-R net8.0")]
        [InlineData("--runtimes=net8.0")]
        [InlineData("--RUNTIMES=net8.0")]
        public void NativeSelectorsReplaceTheDefaultMonoJobAndMustBeRejectedWhenMixed(string selector)
        {
            var mono = CreateMono();
            var (success, parsed, _) = ConfigParser.Parse(selector.Split(' '), NullLogger.Instance, Config(mono));
            Assert.True(success);
            var native = Assert.Single(parsed!.GetJobs()).Infrastructure.Toolchain;
            Assert.NotNull(native);
            Assert.IsNotType<MonoAotLLVMToolChain>(native);
            Assert.Equal(Runtime.Parse("net8.0"), native.Runtime);

            var args = MonoArguments();
            args.AddRange(selector.Split(' '));
            var original = args.ToArray();
            Assert.Throws<ArgumentException>(() =>
                RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(original, args);
        }

        [Theory]
        [InlineData("-R", false)]
        [InlineData("--RUNTIMES", false)]
        [InlineData("--runtimes", true)]
        public void MonoSelectorFormsRetainTheFinalCompilerAndPack(string option, bool attached)
        {
            var args = new List<string> { attached ? option + "=monoaotllvm" : option };
            if (!attached)
                args.Add("monoaotllvm");
            args.AddRange(new[] { "--aotcompilerpath", "compiler", "--customruntimepack", "pack" });
            var mono = RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0");
            Assert.NotNull(mono);
            var (success, parsed, _) = ConfigParser.Parse(args.ToArray(), NullLogger.Instance, Config(mono));
            Assert.True(success);
            Assert.Same(mono, Assert.Single(parsed!.GetJobs()).Infrastructure.Toolchain);
            var settings = Assert.IsType<MonoAotLLVMSettings>(Assert.IsType<MonoAotLLVMBuilder>(mono.Builder).Settings);
            Assert.Equal(Path.GetFullPath("compiler"), settings.AotCompilerPath);
            Assert.Equal(Path.GetFullPath("pack"), settings.CustomRuntimePack);
        }

        [Theory]
        [InlineData("-R", false)]
        [InlineData("--runtimes", true)]
        public void LegacyWasmSelectorFormsChooseTheRequestedCoreClrToolchain(string option, bool attached)
        {
            var args = new List<string> { "--wasmRuntimeFlavor", "CoreCLR",
                attached ? option + "=wasmnet11_0" : option };
            if (!attached)
                args.Add("wasmnet11_0");
            var normalized = RuntimeArgumentCompatibility.Normalize(args);
            var (success, parsed, _) = ConfigParser.Parse(normalized.ToArray(), NullLogger.Instance);
            Assert.True(success);
            var toolchain = Assert.IsType<CsProjCoreWasmToolchain>(Assert.Single(parsed!.GetJobs()).Infrastructure.Toolchain);
            Assert.Equal(CoreWasmRuntime.From(new Version(11, 0)), toolchain.Runtime);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AttachedSdkAndCacheValuesReachTheFinalMonoJob(bool uppercase)
        {
            using var files = new ArgumentFiles();
            string cli = uppercase ? "--CLI=" : "--cli=";
            string packages = uppercase ? "--PACKAGES=" : "--packages=";
            var native = new[] { "--runtimes=net8.0", cli + files.Cli, packages + files.Packages };
            var (nativeSuccess, nativeConfig, nativeOptions) = ConfigParser.Parse(native, NullLogger.Instance);
            Assert.True(nativeSuccess);
            Assert.Equal(files.Cli, nativeOptions!.CliPath!.FullName);
            Assert.Equal(files.Packages, nativeOptions.RestorePath!.FullName);
            var nativeSettings = Assert.IsAssignableFrom<DotNetCliBuilder>(
                Assert.Single(nativeConfig!.GetJobs()).Infrastructure.Toolchain!.Builder).Settings;
            Assert.Equal(files.Cli, nativeSettings.CliPath!.FullName);

            var args = MonoArguments();
            args.AddRange(new[] { cli + files.Cli, packages + files.Packages });
            var mono = RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0");
            Assert.NotNull(mono);
            var (success, parsed, _) = ConfigParser.Parse(args.ToArray(), NullLogger.Instance, Config(mono));
            Assert.True(success);
            var final = Assert.IsType<MonoAotLLVMToolChain>(Assert.Single(parsed!.GetJobs()).Infrastructure.Toolchain);
            var settings = Assert.IsType<MonoAotLLVMSettings>(Assert.IsType<MonoAotLLVMBuilder>(final.Builder).Settings);
            Assert.Equal(files.Cli, settings.CliPath?.FullName);
            Assert.Equal(files.Packages, settings.PackagesPath?.FullName);
        }

        [Theory]
        [InlineData("--cli=first --CLI second")]
        [InlineData("--packages first --PACKAGES=second")]
        [InlineData("--cli=first --cli=second")]
        [InlineData("--packages=first --packages=second")]
        [InlineData("--aotcompilerpath=second")]
        [InlineData("--customruntimepack=second")]
        public void MixedOptionFormsCannotHideDuplicates(string options)
        {
            var args = MonoArguments();
            args.AddRange(options.Split(' '));
            var original = args.ToArray();
            Assert.Throws<ArgumentException>(() =>
                RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(original, args);
        }

        [Theory]
        [InlineData("--cli=")]
        [InlineData("--packages=")]
        public void EmptyAttachedValuesFailBeforeConsumingMonoArguments(string option)
        {
            var args = MonoArguments();
            args.Add(option);
            var original = args.ToArray();
            Assert.Throws<ArgumentException>(() =>
                RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(original, args);
        }

        [Theory]
        [InlineData("-R net8.0")]
        [InlineData("--runtimes=net8.0")]
        public void OrdinaryNativeSelectorsAndSettingsAreUnchanged(string selector)
        {
            using var files = new ArgumentFiles();
            var args = new List<string>(selector.Split(' '));
            args.AddRange(new[] { "--cli=" + files.Cli, "--packages=" + files.Packages });
            var original = args.ToArray();
            Assert.Null(RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            var normalized = RuntimeArgumentCompatibility.Normalize(args);
            Assert.Equal(original, args);
            Assert.Equal(original, normalized);
            var (success, parsed, _) = ConfigParser.Parse(normalized.ToArray(), NullLogger.Instance);
            Assert.True(success);
            var settings = Assert.IsAssignableFrom<DotNetCliBuilder>(
                Assert.Single(parsed!.GetJobs()).Infrastructure.Toolchain!.Builder).Settings;
            Assert.Equal(files.Cli, settings.CliPath!.FullName);
            Assert.Equal(files.Packages, settings.PackagesPath!.FullName);
        }

        [Theory]
        [InlineData("--inProcess")]
        [InlineData("--corerun")]
        public void ResponseFilesCannotReplaceInlineMonoWithAnotherToolchain(string option)
        {
            using var files = new ArgumentFiles();
            File.WriteAllText(files.Response, option);
            var responseArgs = new List<string> { "@" + files.Response };
            if (option == "--corerun")
                responseArgs.Add(files.Cli);
            var (nativeSuccess, nativeConfig, _) = ConfigParser.Parse(
                responseArgs.ToArray(), NullLogger.Instance, Config(CreateMono()));
            Assert.True(nativeSuccess);
            var native = Assert.Single(nativeConfig!.GetJobs()).Infrastructure.Toolchain;
            Assert.NotNull(native);
            if (option == "--inProcess")
                Assert.True(native.IsInProcess);
            else
                Assert.IsType<CoreRunToolchain>(native);

            var args = MonoArguments();
            args.AddRange(responseArgs);
            var original = args.ToArray();
            var error = Assert.Throws<ArgumentException>(() =>
                RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Contains("response", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(original, args);
        }

        [Theory]
        [InlineData("--runtimes monoaotllvm --aotcompilerpath compiler --customruntimepack pack")]
        [InlineData("--runtimes=monoaotllvm --aotcompilerpath compiler --customruntimepack pack")]
        [InlineData("--runtimes wasmnet11_0 --wasmRuntimeFlavor CoreCLR")]
        public void CompatibilityOptionsInsideResponseFilesFailExplicitly(string contents)
        {
            using var files = new ArgumentFiles();
            File.WriteAllText(files.Response, contents);
            var args = new List<string> { "@" + files.Response };
            Assert.Throws<ArgumentException>(() =>
                RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Throws<ArgumentException>(() => RuntimeArgumentCompatibility.Normalize(args));
            Assert.Equal(new[] { "@" + files.Response }, args);
        }

        [Fact]
        public void NativeResponseFilesRetainBdnQuotingAndFinalSettings()
        {
            using var files = new ArgumentFiles();
            File.WriteAllText(files.Response, "--runtimes=net8.0\n--filter \"*name with spaces*\"");
            var args = new List<string> { "@" + files.Response, "--cli=" + files.Cli, "--packages=" + files.Packages };
            var original = args.ToArray();
            var logger = new AccumulationLogger();
            var (expectedSuccess, expectedConfig, expectedOptions) = ConfigParser.Parse(args.ToArray(), logger);
            Assert.True(expectedSuccess, logger.GetLog());
            Assert.Null(RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(original, args);
            var normalized = RuntimeArgumentCompatibility.Normalize(args);
            Assert.Equal(args, normalized);
            var (success, config, options) = ConfigParser.Parse(normalized.ToArray(), NullLogger.Instance);
            Assert.True(success);
            Assert.Equal(expectedOptions!.CliPath!.FullName, options!.CliPath!.FullName);
            Assert.Equal(expectedOptions.RestorePath!.FullName, options.RestorePath!.FullName);
            Assert.Equal(expectedOptions.Filters, options.Filters);
            Assert.Equal(Assert.Single(expectedConfig!.GetJobs()).Infrastructure.Toolchain!.Runtime,
                Assert.Single(config!.GetJobs()).Infrastructure.Toolchain!.Runtime);
        }

        [Fact]
        public void InvalidNativeResponseFileDoesNotFallBackToTheHost()
        {
            using var files = new ArgumentFiles();
            File.WriteAllText(files.Response, "--runtimes=net8.0\n--cli \"" + files.Cli + "\"");
            var args = new List<string> { "@" + files.Response };
            // BDN662 preserves a leading space for this quoted path and rejects it; do not silently repair it.
            Assert.False(ConfigParser.Parse(args.ToArray(), NullLogger.Instance).isSuccess);
            Assert.Throws<ArgumentException>(() =>
                RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(new[] { "@" + files.Response }, args);
        }

        private static List<string> MonoArguments()
            => new() { "--runtimes", "monoaotllvm", "--aotcompilerpath", "compiler", "--customruntimepack", "pack" };

        private static MonoAotLLVMToolChain CreateMono()
            => new(new MonoAotLLVMRuntime(new Version(11, 0)), new()
            {
                TargetFrameworkMoniker = "net11.0", AotCompilerPath = "compiler", CustomRuntimePack = "pack",
                AotCompilerMode = MonoAotCompilerMode.llvm
            });

        private static ManualConfig Config(MonoAotLLVMToolChain mono)
            => ManualConfig.CreateEmpty().AddJob(Job.Default.WithToolchain(mono).AsDefault());

        private sealed class ArgumentFiles : IDisposable
        {
            private readonly string _directory = Path.Combine(Path.GetTempPath(), "bdn-arguments-" + Guid.NewGuid().ToString("N"));
            public string Cli => Path.Combine(_directory, "SDK with spaces", "dotnet.exe");
            public string Packages => Path.Combine(_directory, "cache with spaces");
            public string Response => Path.Combine(_directory, "arguments with spaces.rsp");

            public ArgumentFiles()
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Cli)!);
                Directory.CreateDirectory(Packages);
                File.WriteAllText(Cli, "");
            }

            public void Dispose() => Directory.Delete(_directory, recursive: true);
        }
    }
}
