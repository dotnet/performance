﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.CustomCoreClr;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess;
using Benchmarks.Serializers;
using Benchmarks.Toolchains;
using CommandLine;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
            => Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunBenchmarks)
                .WithNotParsed(errors => { }); // ignore the errors, the parser prints nice error message

        private static void RunBenchmarks(Options options)
            => BenchmarkSwitcher
                .FromAssemblyAndTypes(typeof(Program).Assembly, SerializerBenchmarks.GetTypes())
                .Run(config: GetConfig(options));

        private static IConfig GetConfig(Options options)
        {
            var baseJob = Job.ShortRun; // let's use the Short Run for better first user experience ;)
            var jobs = GetJobs(options, baseJob).ToArray();

            var config = DefaultConfig.Instance
                .With(jobs.Any() ? jobs : new[] { baseJob });

            if (options.UseMemoryDiagnoser)
                config = config.With(MemoryDiagnoser.Default);
            if (options.UseDisassemblyDiagnoser)
                config = config.With(DisassemblyDiagnoser.Create(DisassemblyDiagnoserConfig.Asm));
            
            if (options.DisplayAllStatistics)
                config = config.With(StatisticColumn.AllStatistics);

            return config;
        }

        private static IEnumerable<Job> GetJobs(Options options, Job baseJob)
        {
            if (options.RunInProcess)
                yield return baseJob.With(InProcessToolchain.Instance);

            if (options.RunClr)
                yield return baseJob.With(Runtime.Clr);
            if (!string.IsNullOrEmpty(options.ClrVersion))
                yield return baseJob.With(new ClrRuntime(options.ClrVersion));

            if (options.RunMono)
                yield return baseJob.With(Runtime.Mono);
            if (!string.IsNullOrEmpty(options.MonoPath))
                yield return baseJob.With(new MonoRuntime("Mono", options.MonoPath));

            if (options.RunCoreRt)
                yield return baseJob.With(Runtime.CoreRT).With(CoreRtToolchain.LatestMyGetBuild);
            if (!string.IsNullOrEmpty(options.CoreRtVersion))
                yield return baseJob.With(Runtime.CoreRT)
                    .With(CoreRtToolchain.CreateBuilder()
                        .UseCoreRtNuGet(options.CoreRtVersion)
                        .AdditionalNuGetFeed("benchmarkdotnet ci", "https://ci.appveyor.com/nuget/benchmarkdotnet")
                        .ToToolchain());
            if (!string.IsNullOrEmpty(options.CoreRtPath))
                yield return baseJob.With(Runtime.CoreRT)
                    .With(CoreRtToolchain.CreateBuilder()
                        .UseCoreRtLocal(options.CoreRtPath)
                        .AdditionalNuGetFeed("benchmarkdotnet ci", "https://ci.appveyor.com/nuget/benchmarkdotnet")
                        .ToToolchain());

            if (options.RunCore)
                yield return baseJob.With(Runtime.Core).With(CsProjCoreToolchain.Current.Value);
            if (options.RunCore20)
                yield return baseJob.With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp20);
            if (options.RunCore21)
                yield return baseJob.With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp21);

            if (options.CoreRunPath != null && options.CoreRunPath.Exists)
                yield return baseJob.With(Runtime.Core).With(new CoreRunToolchain(options.CoreRunPath, createCopy: true, customDotNetCliPath: options.CliPath));

            if (!string.IsNullOrEmpty(options.CoreFxVersion) || !string.IsNullOrEmpty(options.CoreClrVersion))
            {
                var builder = CustomCoreClrToolchain.CreateBuilder();

                if (!string.IsNullOrEmpty(options.CoreFxVersion) && !string.IsNullOrEmpty(options.CoreFxBinPackagesPath))
                    builder.UseCoreFxLocalBuild(options.CoreFxVersion, options.CoreFxBinPackagesPath);
                else if (!string.IsNullOrEmpty(options.CoreFxVersion))
                    builder.UseCoreFxNuGet(options.CoreFxVersion);
                else
                    builder.UseCoreFxDefault();

                if (!string.IsNullOrEmpty(options.CoreClrVersion) && !string.IsNullOrEmpty(options.CoreClrBinPackagesPath) && !string.IsNullOrEmpty(options.CoreClrPackagesPath))
                    builder.UseCoreClrLocalBuild(options.CoreClrVersion, options.CoreClrBinPackagesPath, options.CoreClrPackagesPath);
                else if (!string.IsNullOrEmpty(options.CoreClrVersion))
                    builder.UseCoreClrNuGet(options.CoreClrVersion);
                else
                    builder.UseCoreClrDefault();

                if (options.CliPath != null && options.CliPath.Exists)
                    builder.DotNetCli(options.CliPath.FullName);

                builder.AdditionalNuGetFeed("benchmarkdotnet ci", "https://ci.appveyor.com/nuget/benchmarkdotnet");

                yield return baseJob.With(Runtime.Core).With(builder.ToToolchain());
            }
        }
    }

    public class Options
    {
        [Option("memory", Required = false, Default = true, HelpText = "Prints memory statistics. Enabled by default")]
        public bool UseMemoryDiagnoser { get; set; }

        [Option("disassm", Required = false, Default = false, HelpText = "Gets diassembly for benchmarked code")]
        public bool UseDisassemblyDiagnoser { get; set; }

        [Option("allStats", Required = false, Default = false, HelpText = "Displays all statistics (min, max & more")]
        public bool DisplayAllStatistics { get; set; }

        [Option("inProcess", Required = false, Default = false, HelpText = "Run benchmarks in Process")]
        public bool RunInProcess { get; set; }

        [Option("clr", Required = false, Default = false, HelpText = "Run benchmarks for Clr")]
        public bool RunClr { get; set; }

        [Option("clrVersion", Required = false, HelpText = "Optional version of private CLR build used as the value of COMPLUS_Version env var.")]
        public string ClrVersion { get; set; }

        [Option("mono", Required = false, Default = false, HelpText = "Run benchmarks for Mono (takes the default from PATH)")]
        public bool RunMono { get; set; }

        [Option("monoPath", Required = false, HelpText = "Optional path to Mono which should be used for running benchmarks.")]
        public string MonoPath { get; set; }

        [Option("coreRt", Required = false, Default = false, HelpText = "Run benchmarks for the latest CoreRT")]
        public bool RunCoreRt { get; set; }

        [Option("coreRtVersion", Required = false, HelpText = "Optional version of Microsoft.DotNet.ILCompiler which should be used to run with CoreRT. Example: \"1.0.0-alpha-26414-01\"")]
        public string CoreRtVersion { get; set; }

        [Option("ilcPath", Required = false, HelpText = "Optional IlcPath which should be used to run with private CoreRT build. Example: \"1.0.0-alpha-26414-01\"")]
        public string CoreRtPath { get; set; }

        [Option("core", Required = false, Default = false, HelpText = "Run benchmarks for .NET Core")]
        public bool RunCore { get; set; }

        [Option("core20", Required = false, Default = false, HelpText = "Run benchmarks for .NET Core 2.0")]
        public bool RunCore20 { get; set; }

        [Option("core21", Required = false, Default = false, HelpText = "Run benchmarks for .NET Core 2.1")]
        public bool RunCore21 { get; set; }

        [Option("cli", Required = false, HelpText = "Optional path to dotnet cli which should be used for running benchmarks.")]
        public FileInfo CliPath { get; set; }

        [Option("coreRun", Required = false, HelpText = "Optional path to CoreRun which should be used for running benchmarks.")]
        public FileInfo CoreRunPath { get; set; }

        [Option("coreClrVersion", Required = false, HelpText = "Optional version of Microsoft.NETCore.Runtime which should be used. Example: \"2.1.0-preview2-26305-0\"")]
        public string CoreClrVersion { get; set; }

        [Option("coreClrBin", Required = false, HelpText = @"Optional path to folder with CoreClr NuGet packages. Example: ""C:\coreclr\bin\Product\Windows_NT.x64.Release\.nuget\pkg""")]
        public string CoreClrBinPackagesPath { get; set; }

        [Option("coreClrPackages", Required = false, HelpText = @"Optional path to folder with NuGet packages restored for CoreClr build. Example: ""C:\Projects\coreclr\packages""")]
        public string CoreClrPackagesPath { get; set; }

        [Option("coreFxVersion", Required = false, HelpText = "Optional version of Microsoft.Private.CoreFx.NETCoreApp which should be used. Example: \"4.5.0-preview2-26307-0\"")]
        public string CoreFxVersion { get; set; }

        [Option("coreFxBin", Required = false, HelpText = @"Optional path to folder with CoreFX NuGet packages, Example: ""C:\Projects\forks\corefx\bin\packages\Release""")]
        public string CoreFxBinPackagesPath { get; set; }
    }

    /// <summary>
    /// this config allows you to run benchmarks for multiple runtimes
    /// </summary>
    public class MultipleRuntimesConfig : ManualConfig
    {
        public MultipleRuntimesConfig()
        {
            Add(Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp20)).AsBaseline().WithId("Core 2.0"));
            Add(Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp21)).WithId("Core 2.1"));

            Add(Job.Default.With(Runtime.Clr).WithId("Clr"));
            Add(Job.Default.With(Runtime.Mono).WithId("Mono")); // you can comment this if you don't have Mono installed
            Add(Job.Default.With(Runtime.CoreRT).WithId("CoreRT"));

            Add(MemoryDiagnoser.Default);

            Add(DefaultConfig.Instance.GetValidators().ToArray());
            Add(DefaultConfig.Instance.GetLoggers().ToArray());
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray());

            Add(new CsvMeasurementsExporter(CsvSeparator.Semicolon));
            //Add(RPlotExporter.Default); // it produces nice plots but requires R to be installed
            Add(MarkdownExporter.GitHub);
            Add(HtmlExporter.Default);
            //Add(StatisticColumn.AllStatistics);

            Set(new BenchmarkDotNet.Reports.SummaryStyle
            {
                PrintUnitsInHeader = true,
                PrintUnitsInContent = false,
                TimeUnit = TimeUnit.Microsecond,
                SizeUnit = SizeUnit.B
            });
        }
    }
}
