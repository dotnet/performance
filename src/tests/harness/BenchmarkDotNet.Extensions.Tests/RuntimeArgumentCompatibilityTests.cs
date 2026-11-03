// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.Wasm;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.R2R;
using Xunit;

namespace Tests
{
    public class RuntimeArgumentCompatibilityTests
    {
        [Theory]
        [InlineData("wasmnet80", "monowasm8.0")]
        [InlineData("wasmnet90", "monowasm9.0")]
        [InlineData("wasmnet10_0", "monowasm10.0")]
        [InlineData("wasmnet11_0", "monowasm11.0")]
        public void HistoricalMonoInterpreterSelectsSameRuntime(string legacy, string expected)
        {
            var args = RuntimeArgumentCompatibility.Normalize(new[] { "--runtimes", legacy });
            var (success, config, _) = ConfigParser.Parse(args.ToArray(), NullLogger.Instance);

            Assert.True(success);
            var job = Assert.Single(config!.GetJobs());
            var toolchain = Assert.IsType<CsProjMonoWasmToolchain>(job.Infrastructure.Toolchain);
            Assert.Equal(Runtime.Parse(expected), toolchain.Runtime);
        }

        [Theory]
        [InlineData("wasmnet80", "corewasm8.0")]
        [InlineData("wasmnet90", "corewasm9.0")]
        [InlineData("wasmnet10_0", "corewasm10.0")]
        [InlineData("wasmnet11_0", "corewasm11.0")]
        [InlineData("corewasm12.0", "corewasm12.0")]
        public void HistoricalCoreClrFlavorSelectsCoreClrToolchain(string input, string expected)
        {
            var args = RuntimeArgumentCompatibility.Normalize(
                new[] { "--wasmRuntimeFlavor", "CoreCLR", "--runtimes", input });
            var (success, config, _) = ConfigParser.Parse(args.ToArray(), NullLogger.Instance);

            Assert.True(success);
            var job = Assert.Single(config!.GetJobs());
            var toolchain = Assert.IsType<CsProjCoreWasmToolchain>(job.Infrastructure.Toolchain);
            Assert.Equal(Runtime.Parse(expected), toolchain.Runtime);
        }

        [Theory]
        [InlineData("wasmnet80", "monowasmaot8.0")]
        [InlineData("wasmnet90", "monowasmaot9.0")]
        [InlineData("wasmnet10_0", "monowasmaot10.0")]
        [InlineData("wasmnet11_0", "monowasmaot11.0")]
        public void HistoricalWasmAotSelectsAotToolchain(string input, string expected)
        {
            var args = RuntimeArgumentCompatibility.Normalize(
                new[] { "--runtimes", input, "--aotcompilermode", "wasm" });
            var (success, config, _) = ConfigParser.Parse(args.ToArray(), NullLogger.Instance);

            Assert.True(success);
            var job = Assert.Single(config!.GetJobs());
            var toolchain = Assert.IsType<CsProjMonoWasmAotToolchain>(job.Infrastructure.Toolchain);
            Assert.Equal(Runtime.Parse(expected), toolchain.Runtime);
        }

        [Fact]
        public void OtherArgumentsAndRuntimeOrderArePreserved()
        {
            var input = new[] { "-r", "net8.0", "wasmnet11_0", "--wasmEngine", "engine",
                "--wasmProcessTimeout", "20", "--msbuild-arguments", "/p:PublishReadyToRun=true" };
            var expected = new[] { "-r", "net8.0", "monowasm11_0", "--wasmEngine", "engine",
                "--wasmProcessTimeout", "20", "--msbuild-arguments", "/p:PublishReadyToRun=true" };

            Assert.Equal(expected, RuntimeArgumentCompatibility.Normalize(input));
            Assert.Equal("wasmnet11_0", input[2]);
        }

        [Theory]
        [InlineData("r2r11_0", 11)]
        [InlineData("r2r12_0", 12)]
        public void R2rRetainsCustomRuntimePackAndCrossgen2(string runtime, int major)
        {
            string compiler = Path.GetDirectoryName(typeof(RuntimeArgumentCompatibilityTests).Assembly.Location)!;
            string pack = Path.GetDirectoryName(compiler)!;
            var args = RuntimeArgumentCompatibility.Normalize(new[] {
                "--runtimes", runtime, "--aotcompilerpath", compiler, "--customruntimepack", pack });
            var (success, config, _) = ConfigParser.Parse(args.ToArray(), NullLogger.Instance);

            Assert.True(success);
            var toolchain = Assert.IsType<CsProjR2RToolchain>(Assert.Single(config!.GetJobs()).Infrastructure.Toolchain);
            Assert.IsType<R2RRuntime>(toolchain.Runtime);
            Assert.Equal(major, toolchain.Runtime.Version!.Major);
            var settings = Assert.IsType<R2RSettings>(Assert.IsAssignableFrom<DotNetCliBuilder>(toolchain.Builder).Settings);
            Assert.Equal(pack, settings.CustomRuntimePack!.FullName);
            Assert.Equal(compiler, settings.Crossgen2Pack!.FullName);
        }

        [Theory]
        [InlineData("--wasmRuntimeFlavor")]
        [InlineData("--wasmRuntimeFlavor CoreCLR")]
        [InlineData("--wasmRuntimeFlavor Invalid --runtimes wasmnet11_0")]
        [InlineData("--wasmRuntimeFlavor Mono --wasmRuntimeFlavor CoreCLR --runtimes wasmnet11_0")]
        [InlineData("--wasmRuntimeFlavor Mono --runtimes corewasm11.0")]
        [InlineData("--wasmRuntimeFlavor CoreCLR --runtimes monowasm11.0")]
        [InlineData("--aotcompilermode wasm")]
        [InlineData("--aotcompilermode wasm --runtimes corewasm11.0")]
        [InlineData("--aotcompilermode invalid --runtimes wasmnet11_0")]
        public void InvalidOrConflictingOptionsFailExplicitly(string commandLine)
        {
            Assert.Throws<ArgumentException>(() => RuntimeArgumentCompatibility.Normalize(commandLine.Split(' ')));
        }

        [Theory]
        [InlineData("--runtimes monoaotllvm")]
        [InlineData("--runtimes monoaotllvm --aotcompilermode llvm")]
        public void NativeMonoLlvmIsNeverReplacedWithLegacyMonoAot(string commandLine)
        {
            var error = Assert.Throws<ArgumentException>(
                () => RuntimeArgumentCompatibility.Normalize(commandLine.Split(' ')));

            Assert.Contains("Mono AOT+LLVM", error.Message);
        }
    }
}
