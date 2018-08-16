using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess;
using CommandLine;

namespace Benchmarks
{
    public static class ConsoleArguments
    {
        public static (bool success, ReadOnlyConfig result) Parse(string[] arguments)
        {
            ManualConfig config = default;
            Job jobSettings = default, baseJob = default, diffJob = default;

            using (var parser = CreateParser())
            {
                // the code below might look at least suprising..
                // CommandLine library supports verbs, but it assumes that the user can use single verb, not combine two of them
                // an example: git push is OK but git commit push is NOT OK
                // however for our purpose I need to be able to parse:
                //  1. general settings like filters
                //  2. base job settings
                //  3. diff job settings
                // this is why the following code parses all the 3 things separately ;)
                
                var generalArgs = arguments.TakeWhile(arg => !arg.Equals("base", StringComparison.InvariantCultureIgnoreCase));
                parser
                    .ParseArguments<Options>(generalArgs)
                    .WithParsed<Options>(options =>
                    {
                        config = GetConfig(options);
                        jobSettings = GetJobSettings(options);
                    });

                var baseArgs = arguments
                    .SkipWhile(arg => !arg.Equals("base", StringComparison.InvariantCultureIgnoreCase))
                    .TakeWhile(arg => !arg.Equals("diff", StringComparison.InvariantCultureIgnoreCase));
                parser
                    .ParseArguments<BaseOptions, Workaround>(baseArgs)
                    .WithParsed<BaseOptions>(options => baseJob = GetJob(jobSettings, options).WithId("base").AsBaseline());

                var diffArgs = arguments.SkipWhile(arg => !arg.Equals("diff", StringComparison.InvariantCultureIgnoreCase));
                parser
                    .ParseArguments<DiffOptions, Workaround>(diffArgs)
                    .WithParsed<DiffOptions>(options => diffJob = GetJob(jobSettings, options).WithId("diff"));
            }
            
            if (config != null)
            {
                if (baseJob != null) 
                    config.Add(baseJob);
                if (diffJob != null) 
                    config.Add(diffJob);
                if (baseJob == null && diffJob == null) // user did not provide base or diff settings so we are going to run against default (current) runtime 
                    config.Add(jobSettings);
            }
            
            return (config != null, config?.AsReadOnly());
        }

        private static Parser CreateParser()
            => new Parser(settings =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
                settings.EnableDashDash = true;
                settings.IgnoreUnknownArguments = false;
                settings.HelpWriter = Console.Out;
            });

        private static ManualConfig GetConfig(Options options)
        {
            void AddFilter(ManualConfig c, IFilter f) => c.Add(new UnionFilter(f, new OperatingSystemFilter()));

            var config = ManualConfig.Create(DefaultConfig.Instance);

            if (options.UseMemoryDiagnoser)
                config.Add(MemoryDiagnoser.Default);
            if (options.UseDisassemblyDiagnoser)
                config.Add(DisassemblyDiagnoser.Create(DisassemblyDiagnoserConfig.Asm));

            if (options.DisplayAllStatistics)
                config.Add(StatisticColumn.AllStatistics);

            if (options.AllCategories.Any())
                AddFilter(config, new AllCategoriesFilter(options.AllCategories.ToArray()));
            if (options.AnyCategories.Any())
                AddFilter(config, new AnyCategoriesFilter(options.AnyCategories.ToArray()));
            if (options.Filters.Any())
                AddFilter(config, new GlobFilter(options.Filters.ToArray()));

            config.Add(JsonExporter.Full); // make sure we export to Json (for BenchView integration purpose)

            config.Add(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max);

            config.Add(TooManyTestCasesValidator.FailOnError);

            config.SummaryPerType = !options.Join;

            return config;
        }

        private static Job GetJobSettings(Options options)
        {
            Job baseJob = null;

            switch (options.Job.ToLowerInvariant())
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
                        .WithIterationTime(
                            TimeInterval
                                .FromSeconds(
                                    0.25)) // the default is 0.5s per iteration, which is slighlty too much for us
                        .WithWarmupCount(1) // 1 warmup is enough for our purpose
                        .WithOutlierMode(options.Outliers)
                        .WithMaxIterationCount(20); // we don't want to run more that 20 iterations
                    break;
            }

            baseJob = baseJob.WithOutlierMode(options.Outliers);

            if (options.Affinity.HasValue)
                baseJob = baseJob.WithAffinity((IntPtr) options.Affinity.Value);

            if (options.RunInProcess)
                baseJob = baseJob.With(InProcessToolchain.Instance);

            return baseJob;
        }

        private static Job GetJob(Job job, RuntimeOptions options)
        {
            var result = job
                .With(GetRuntime(options.Framework))
                .With(options.Jit)
                .With(options.Platform);

            if (options.MonoPath != null && options.MonoPath.Exists)
                return job.With(new MonoRuntime("Mono", options.MonoPath.FullName));
            if (!string.IsNullOrEmpty(options.ClrVersion))
                return job.With(new ClrRuntime(options.ClrVersion ));
            if (options.Framework == Framework.Clr && !string.IsNullOrEmpty(options.TargetFrameworkMoniker))
                return result.With(CsProjClassicNetToolchain.From(options.TargetFrameworkMoniker));
            if (options.CoreRunPath != null && options.CoreRunPath.Exists)
            {
                return job.With(new CoreRunToolchain(
                    coreRun: options.CoreRunPath,
                    createCopy: true,
                    targetFrameworkMoniker: options.TargetFrameworkMoniker ?? NetCoreAppSettings.Current.Value.TargetFrameworkMoniker,
                    customDotNetCliPath: options.CliPath));
            }
            if (options.CliPath != null && options.CliPath.Exists)
            {
                return job.With(CsProjCoreToolchain.From(new NetCoreAppSettings(
                    targetFrameworkMoniker: options.TargetFrameworkMoniker ?? NetCoreAppSettings.Current.Value.TargetFrameworkMoniker,
                    runtimeFrameworkVersion: null,
                    name: "Core",
                    customDotNetCliPath: options.CliPath?.FullName)));
            }
            if (options.Framework == Framework.CoreRT)
            {
                if (!string.IsNullOrEmpty(options.CoreRtVersion))
                    return result.With(
                        CoreRtToolchain.CreateBuilder()
                            .UseCoreRtNuGet(options.CoreRtVersion)
                            .AdditionalNuGetFeed("benchmarkdotnet ci", "https://ci.appveyor.com/nuget/benchmarkdotnet")
                            .ToToolchain());
                if (options.CoreRtPath != null && options.CoreRtPath.Exists)
                    return result.With(
                        CoreRtToolchain.CreateBuilder()
                            .UseCoreRtLocal(options.CoreRtPath.FullName)
                            .AdditionalNuGetFeed("benchmarkdotnet ci", "https://ci.appveyor.com/nuget/benchmarkdotnet")
                            .ToToolchain());
                
                return result.With(CoreRtToolchain.LatestMyGetBuild);
            }
            if (options.Framework == Framework.Core && !string.IsNullOrEmpty(options.TargetFrameworkMoniker))
            {
                return job.With(CsProjCoreToolchain.From(new NetCoreAppSettings(
                    targetFrameworkMoniker: options.TargetFrameworkMoniker,
                    runtimeFrameworkVersion: null,
                    name: options.TargetFrameworkMoniker,
                    customDotNetCliPath: options.CliPath?.FullName)));
            }

            return result;
        }

        private static Runtime GetRuntime(Framework framework)
        {
            switch (framework)
            {
                case Framework.Clr:
                    return Runtime.Clr;
                case Framework.Core:
                    return Runtime.Core;
                case Framework.Mono:
                    return Runtime.Mono;
                case Framework.CoreRT:
                    return Runtime.CoreRT;
                case Framework.Native:
                    throw new NotImplementedException("Not implemented (yet)");
                default:
                    throw new ArgumentOutOfRangeException(nameof(framework), framework, null);
            }
        }
    }

    public class Options
    {
        [Option('f', "filters", Required = false, HelpText = "Filter(s) to apply, globs that operate on namespace.typename.methodname")]
        public IEnumerable<string> Filters { get; set; }

        [Option("join", Required = false, Default = false, HelpText = "Prints single table with results for all benchmarks")]
        public bool Join { get; set; }

        [Option('m', "memory", Required = false, Default = true, HelpText = "Prints memory statistics. Enabled by default")]
        public bool UseMemoryDiagnoser { get; set; }

        [Option('d', "disassm", Required = false, Default = false, HelpText = "Gets diassembly for benchmarked code")]
        public bool UseDisassemblyDiagnoser { get; set; }

        [Option('a', "allStats", Required = false, Default = false, HelpText = "Displays all statistics (min, max & more")]
        public bool DisplayAllStatistics { get; set; }

        [Option('i', "inProcess", Required = false, Default = false, HelpText = "Run benchmarks in Process")]
        public bool RunInProcess { get; set; }

        [Option('j', "job", Required = false, Default = "Default", HelpText = "Dry/Short/Medium/Long or Default")]
        public string Job { get; set; }

        [Option("categories", Required = false, HelpText = "Categories to run. If few are provided, only the benchmarks which belong to all of them are going to be executed")]
        public IEnumerable<string> AllCategories { get; set; }

        [Option("anyCategories", Required = false, HelpText = "Any Categories to run")]
        public IEnumerable<string> AnyCategories { get; set; }

        [Option("outliers", Required = false, Default = OutlierMode.OnlyUpper, HelpText = "None/OnlyUpper/OnlyLower/All")]
        public OutlierMode Outliers { get; set; }

        [Option("affinity", Required = false, HelpText = "Affinity mask to set for the benchmark process")]
        public int? Affinity { get; set; }
    }

    public enum Framework : byte
    {
        Clr,
        Core,
        Mono,
        CoreRT,
        Native
    };

    public class RuntimeOptions
    {
        [Value(0, Required = true)]
        public Framework Framework { get; set; }

        [Option("moniker", Required = false, HelpText = "Target Framework Moniker (optional).")]
        public string TargetFrameworkMoniker { get; set; }

        [Option("cli", Required = false, HelpText = "Path to dotnet cli (optional).")]
        public FileInfo CliPath { get; set; }

        [Option("coreRun", Required = false, HelpText = "Path to CoreRun (optional).")]
        public FileInfo CoreRunPath { get; set; }

        [Option("clrVersion", Required = false, HelpText = "Version of private CLR build used as the value of COMPLUS_Version env var (optional).")]
        public string ClrVersion { get; set; }

        [Option("jit", Required = false, Default = Jit.Default, HelpText = "Jit (optional).")]
        public Jit Jit { get; set; }

        [Option("platform", Required = false, Default = Platform.AnyCpu, HelpText = "Platform (optional).")]
        public Platform Platform { get; set; }

        [Option("monoPath", Required = false, HelpText = "Optional path to Mono which should be used for running benchmarks.")]
        public FileInfo MonoPath { get; set; }

        [Option("coreRtVersion", Required = false, HelpText = "Version of Microsoft.DotNet.ILCompiler which should be used to run with CoreRT. Example: \"1.0.0-alpha-26414-01\" (optional).")]
        public string CoreRtVersion { get; set; }

        [Option("ilcPath", Required = false, HelpText = "IlcPath which should be used to run with private CoreRT build. Example: \"1.0.0-alpha-26414-01\" (optional).")]
        public FileInfo CoreRtPath { get; set; }
    }

    [Verb("base", HelpText = "The settings for base run")]
    public class BaseOptions : RuntimeOptions { }

    [Verb("diff", HelpText = "The settings for diff run")]
    public class DiffOptions : RuntimeOptions { }
    
    // this class exist only becasue to parse the Verb you need to provide more than 1 generic type argument in CommandLine library...
    [Verb("ignore")]
    public class Workaround { }
}