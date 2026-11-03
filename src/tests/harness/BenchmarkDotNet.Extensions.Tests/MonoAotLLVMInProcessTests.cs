// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.ConsoleArguments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using Xunit;

namespace Tests
{
    public class MonoAotLLVMInProcessTests
    {
        public static IEnumerable<object[]> BooleanForms()
        {
            foreach (string option in new[] { "--inProcess", "--INPROCESS", "-i", "-I" })
            {
                yield return new object[] { new[] { option }, true };
                yield return new object[] { new[] { option, "true" }, true };
                yield return new object[] { new[] { option, "false" }, false };
                yield return new object[] { new[] { option + "=true" }, true };
                yield return new object[] { new[] { option + "=false" }, false };
            }
        }

        public static IEnumerable<object[]> ParserForms()
            => BooleanForms().Select(form => new[] { form[0] });

        [Theory]
        [MemberData(nameof(ParserForms))]
        public void ActualParserTreatsNonNullableBooleanAsAPresenceSwitch(string[] arguments)
        {
            var mono = CreateToolchain();
            var original = ManualConfig.CreateEmpty().AddJob(Job.Default.WithToolchain(mono).AsDefault());
            var (success, parsed, options) = ConfigParser.Parse(arguments, NullLogger.Instance, original);

            Assert.True(success, string.Join(" ", arguments));
            Assert.True(options!.RunInProcess);
            var job = Assert.Single(parsed!.GetJobs());
            Assert.NotNull(job.Infrastructure.Toolchain);
            Assert.True(job.Infrastructure.Toolchain.IsInProcess);
            Assert.NotSame(mono, job.Infrastructure.Toolchain);
        }

        [Theory]
        [MemberData(nameof(BooleanForms))]
        public void CompatibilityGuardPreservesTheRequestedFinalRuntime(string[] arguments, bool enabled)
        {
            var args = new List<string> { "--runtimes", "monoaotllvm",
                "--aotcompilerpath", "compiler", "--customruntimepack", "pack" };
            args.AddRange(arguments);
            var original = args.ToArray();
            if (enabled)
            {
                Assert.Throws<ArgumentException>(() =>
                    RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
                Assert.Equal(original, args);
                return;
            }

            var mono = RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0");
            Assert.NotNull(mono);
            Assert.Empty(args);
            var config = ManualConfig.CreateEmpty().AddJob(Job.Default.WithToolchain(mono).AsDefault());
            var (success, parsed, _) = ConfigParser.Parse(
                RuntimeArgumentCompatibility.Normalize(args).ToArray(), NullLogger.Instance, config);
            Assert.True(success);
            var job = Assert.Single(parsed!.GetJobs());
            Assert.NotNull(job.Infrastructure.Toolchain);
            Assert.Same(mono, job.Infrastructure.Toolchain);
            Assert.IsType<MonoAotLLVMRuntime>(job.Infrastructure.Toolchain.Runtime);
            Assert.False(job.Infrastructure.Toolchain.IsInProcess);
        }

        [Theory]
        [MemberData(nameof(ParserForms))]
        public void OrdinaryNonMonoArgumentsAreUnchanged(string[] arguments)
        {
            var args = arguments.ToList();
            Assert.Null(RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(arguments, args);
            var (success, parsed, options) = ConfigParser.Parse(args.ToArray(), NullLogger.Instance);
            Assert.True(success);
            Assert.True(options!.RunInProcess);
            var toolchain = Assert.Single(parsed!.GetJobs()).Infrastructure.Toolchain;
            Assert.NotNull(toolchain);
            Assert.True(toolchain.IsInProcess);
        }

        [Theory]
        [InlineData("--inProcess=invalid")]
        [InlineData("--inProcess=false --inProcess")]
        [InlineData("-i false --INPROCESS true")]
        public void InvalidOrMixedInProcessSelectionsFailBeforeConsumingMonoArguments(string options)
        {
            var args = new List<string> { "--runtimes", "monoaotllvm",
                "--aotcompilerpath", "compiler", "--customruntimepack", "pack" };
            args.AddRange(options.Split(' '));
            var original = args.ToArray();
            Assert.Throws<ArgumentException>(() =>
                RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(original, args);
        }

        [Fact]
        public void DisabledInProcessBeforeRuntimeKeepsUnrelatedOptions()
        {
            var args = new List<string> { "-I", "FALSE", "--runtimes", "monoaotllvm",
                "--aotcompilerpath", "compiler", "--customruntimepack", "pack", "--filter", "*Value*" };
            Assert.NotNull(RuntimeArgumentCompatibility.ParseMonoAotLLVM(args, ".NETCoreApp,Version=v11.0"));
            Assert.Equal(new[] { "--filter", "*Value*" }, args);
        }

        private static MonoAotLLVMToolChain CreateToolchain()
            => new(new MonoAotLLVMRuntime(new Version(11, 0)), new()
            {
                TargetFrameworkMoniker = "net11.0", AotCompilerPath = "compiler",
                CustomRuntimePack = "pack", AotCompilerMode = MonoAotCompilerMode.llvm
            });
    }
}
