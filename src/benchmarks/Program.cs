using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.CustomCoreClr;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess;
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
                .FromAssembly(typeof(Program).Assembly)
                .Run(GetArgs(options), GetConfig(options));

        private static string[] GetArgs(Options options)
            => options.Join ? new[] {"--join"} : Array.Empty<string>(); 

        private static IConfig GetConfig(Options options)
        {
            IConfig AddFilter(IConfig c, IFilter f) => c.With(new UnionFilter(f, new OperatingSystemFilter()));

            var baseJob = GetBaseJob(options);

            var baseJobPermutations = GetBaseJobPermutations(baseJob, options).ToArray(); 
            var jobs = baseJobPermutations.SelectMany(job => GetJobs(options, job)).ToArray();

            var config = DefaultConfig.Instance.With(jobs.Any() ? jobs : baseJobPermutations);
            
            if (options.UseMemoryDiagnoser)
                config = config.With(MemoryDiagnoser.Default);
            if (options.UseDisassemblyDiagnoser)
                config = config.With(DisassemblyDiagnoser.Create(DisassemblyDiagnoserConfig.Asm));
            
            if (options.DisplayAllStatistics)
                config = config.With(StatisticColumn.AllStatistics);

            if (options.AllCategories.Any())
                config = AddFilter(config, new AllCategoriesFilter(options.AllCategories.ToArray()));
            if (options.AnyCategories.Any())
                config = AddFilter(config, new AnyCategoriesFilter(options.AnyCategories.ToArray()));
            if (options.Filters.Any())
                config = AddFilter(config, new GlobFilter(options.Filters.ToArray()));

            config = config.With(JsonExporter.Full); // make sure we export to Json (for BenchView integration purpose)

            config = config.With(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max);

            config = config.With(TooManyTestCasesValidator.FailOnError);
            
            return config;
        }

        private static Job GetBaseJob(Options options)
        {
            Job baseJob = null;
            
            switch (options.BaseJob.ToLowerInvariant())
            {
                case "dry":
                    baseJob = Job.Dry;
                    break;
                case "short":
                    baseJob = Job.ShortRun;
                    break;
                case "medium":
                    baseJob = Job.MediumRun.WithOutlierMode(options.Outliers);
                    break;
                case "long":
                    baseJob = Job.LongRun;
                    break;
                default: // the recommended settings
                    baseJob = Job.Default
                        .WithIterationTime(TimeInterval.FromMilliseconds(options.IterationTimeInMiliseconds))
                        .WithWarmupCount(options.WarmupIterationCount)
                        .WithOutlierMode(options.Outliers)
                        .WithMinIterationCount(options.MinIterationCount)
                        .WithMaxIterationCount(options.MaxIterationCount);
                    break;
            }

            baseJob = baseJob.WithOutlierMode(options.Outliers);
            
            if (options.Affinity.HasValue && !options.TestAffinity)
                baseJob = baseJob.WithAffinity((IntPtr) options.Affinity.Value);

            return baseJob;
        }

        private static IEnumerable<Job> GetBaseJobPermutations(Job baseJob, Options options)
        {
            IEnumerable<Job> CreateLoopAligmentPermutations(Job job)
            {
                yield return job.With(new[] {new EnvironmentVariable("COMPlus_JitAlignLoops", "1")});
                yield return job.With(new[] {new EnvironmentVariable("COMPlus_JitAlignLoops", "0")});
            }
            
            IEnumerable<Job> CreateAffinityPermutations(Job job)
            {
                yield return job;
                yield return job.WithAffinity((IntPtr)options.Affinity.Value);
            }

            Job[] jobs = { baseJob };
            if (options.TestAlignLoops)
                jobs = jobs.SelectMany(CreateLoopAligmentPermutations).ToArray();
            if (options.TestAffinity && options.Affinity.HasValue)
                jobs = jobs.SelectMany(CreateAffinityPermutations).ToArray();

            return jobs;
        }

        private static IEnumerable<Job> GetJobs(Options options, Job baseJob)
        {
            if (options.RunInProcess)
                yield return baseJob.With(InProcessToolchain.Instance);

            if (options.RunClr)
                yield return baseJob.With(Runtime.Clr);
            if (options.RunLegacyJitX64)
                yield return baseJob.With(Runtime.Clr).With(Jit.LegacyJit).With(Platform.X64);
            if (options.RunLegacyJitX86)
                yield return baseJob.With(Runtime.Clr).With(Jit.LegacyJit).With(Platform.X86);
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
            
            if (options.TargetFrameworkMonikers.Any())
            {
                foreach (var targetFrameworkMoniker in options.TargetFrameworkMonikers)
                {
                    var job = baseJob.With(Runtime.Core)
                        .With(CsProjCoreToolchain.From(
                            new NetCoreAppSettings(
                                targetFrameworkMoniker: targetFrameworkMoniker,
                                runtimeFrameworkVersion: null,
                                name: targetFrameworkMoniker,
                                customDotNetCliPath: options.CliPath?.FullName)));

                    if (options.TargetFrameworkMonikers.Count() > 1 && options.TargetFrameworkMonikers.First() == targetFrameworkMoniker)
                        yield return job.AsBaseline(); // the first TFM is the baseline
                    else
                        yield return job;
                }
            }

            if (options.CoreRunPath != null && options.CoreRunPath.Exists)
            {
                yield return baseJob.With(Runtime.Core)
                    .With(new CoreRunToolchain(
                        options.CoreRunPath,
                        targetFrameworkMoniker: NetCoreAppSettings.Current.Value.TargetFrameworkMoniker, 
                        createCopy: true, 
                        customDotNetCliPath: options.CliPath));
            }

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
        
        [Option("legacyJitx64", Required = false, Default = false, HelpText = "Run benchmarks for Legacy JIT x64")]
        public bool RunLegacyJitX64 { get; set; }
        
        [Option("legacyJitx86", Required = false, Default = false, HelpText = "Run benchmarks for Legacy JIT x86")]
        public bool RunLegacyJitX86 { get; set; }

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
        
        [Option("tfms", Required = false, HelpText = "Optional target framework monikers to compare. Provide the baseline as first.")]
        public IEnumerable<string> TargetFrameworkMonikers { get; set; }

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
        
        [Option("categories", Required = false, HelpText = "Categories to run. If few are provided, only the benchmarks which belong to all of them are going to be executed")]
        public IEnumerable<string> AllCategories { get; set; }
        
        [Option("anyCategories", Required = false, HelpText = "Any Categories to run")]
        public IEnumerable<string> AnyCategories { get; set; }
        
        [Option('f', "filters", Required = false, HelpText = "Filter(s) to apply, globs that operate on namespace.typename.methodname")]
        public IEnumerable<string> Filters { get; set; }
        
        [Option("join", Required = false, Default = false, HelpText = "Prints single table with results for all benchmarks")]
        public bool Join { get; set; }
        
        [Option("baseJob", Required = false, Default = "Default", HelpText = "Dry/Short/Medium/Long or Default")]
        public string BaseJob { get; set; }
        
        [Option("outliers", Required = false, Default = OutlierMode.OnlyUpper, HelpText = "None/OnlyUpper/OnlyLower/All")]
        public OutlierMode Outliers { get; set; }
        
        [Option("testAlignment", Required = false, Default = false, HelpText = "Test COMPlus_JitAlignLoop 0 vs 1")]
        public bool TestAlignLoops { get; set; }
        
        [Option("testAffinity", Required = false, Default = false, HelpText = "Test affinity set vs no affinity")]
        public bool TestAffinity { get; set; }
        
        [Option("affinity", Required = false, HelpText = "Affinity mask to set for the benchmark process")]
        public int? Affinity { get; set; }
        
        [Option("iterationTime", Required = false, Default = 250, HelpText = "How long should a single iteration take, in miliseconds")] // the default is 0.5s per iteration, which is slighlty too much for us
        public int IterationTimeInMiliseconds { get; set; }
        
        [Option("warmupCount", Required = false, Default = 1, HelpText = "Number of Warmup iterations")]  // 1 warmup is enough for our purpose
        public int WarmupIterationCount { get; set; }
        
        [Option("minIterationCount", Required = false, Default = 15, HelpText = "Minimum number of iterations to run")]
        public int MinIterationCount { get; set; }
        
        [Option("maxIterationCount", Required = false, Default = 20, HelpText = "Maximum number of iterations to run")]   // we don't want to run more that 20 iterations
        public int MaxIterationCount { get; set; }
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