using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using Reporting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace BenchmarkDotNet.Extensions
{
    public static class RecommendedConfig
    {
        private static IEnvironment Environment = new EnvironmentProvider();

        public static IConfig Create(
            DirectoryInfo artifactsPath,
            ImmutableHashSet<string> mandatoryCategories,
            PerfLabCommandLineOptions? options = null,
            Job? baseJob = null)
        {
            var config = ManualConfig.CreateEmpty()
                .WithBuildTimeout(TimeSpan.FromMinutes(15)) // for slow machines
                .AddLogger(ConsoleLogger.Default) // log output to console
                .AddValidator(DefaultConfig.Instance.GetValidators().ToArray()) // copy default validators
                .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray()) // copy default analysers
                .AddExporter(MarkdownExporter.GitHub) // export to GitHub markdown
                .AddColumnProvider(DefaultColumnProviders.Instance) // display default columns (method name, args etc)
                .WithArtifactsPath(artifactsPath.FullName)
                .AddDiagnoser(MemoryDiagnoser.Default) // MemoryDiagnoser is enabled by default
                .AddExporter(JsonExporter.Full) // make sure we export to Json
                .AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
                .AddValidator(new MandatoryCategoryValidator(mandatoryCategories))
                .AddValidator(TooManyTestCasesValidator.FailOnError)
                .AddValidator(new UniqueArgumentsValidator()) // don't allow for duplicated arguments #404
                .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(36)); // the default is 20 and trims too aggressively some benchmark results

            if (baseJob is null && options?.Manifest is not null)
            {
                if (options?.Manifest?.BaseJob is not null)
                    baseJob = options.Manifest.BaseJob.ModifyJob(Job.Default);
            }
            else
            {
                baseJob ??= Job.Default
                    .WithWarmupCount(1) // 1 warmup is enough for our purpose
                    .WithIterationTime(TimeInterval.FromMilliseconds(250)) // the default is 0.5s per iteration, which is slightly too much for us
                    .WithMinIterationCount(15)
                    .WithMaxIterationCount(20) // we don't want to run more that 20 iterations
                    .DontEnforcePowerPlan(); // make sure BDN does not try to enforce High Performance power plan on Windows
            }

            if (baseJob is not null)
                config.AddJob(baseJob.AsDefault());

            if (Environment.IsLabEnvironment())
                config.AddExporter(new PerfLabExporter());

            if (options is not null)
            {
                if (options.ExclusionFilters is IEnumerable<string> exclusionFilters)
                    config.AddFilter(new ExclusionFilter([.. exclusionFilters]));

                if (options.CategoryExclusionFilters is IEnumerable<string> categoryExclusionFilters)
                    config.AddFilter(new CategoryExclusionFilter([.. categoryExclusionFilters]));

                if (options.PartitionCount is int partitionCount && options.PartitionIndex is int partitionIndex)
                    config.AddFilter(new PartitionFilter(partitionCount, partitionIndex));

                if (options.GetDiffableDisasm)
                    config.AddDiagnoser(CreateDisassembler());

                if (options.Manifest?.BenchmarkCases is List<string> benchmarkCases)
                    config.AddFilter(new TestNameFilter([.. benchmarkCases]));

                if (options.Manifest?.Jobs is Dictionary<string, BenchmarkManifest.JobSettings> jobs)
                {
                    var jobsToAdd = jobs
                        .Select(job => job.Value.ModifyJob(Job.Default).WithId(job.Key))
                        .ToArray();

                    config.AddJob(jobsToAdd);
                }
            }

            return config;
        }

        private static DisassemblyDiagnoser CreateDisassembler()
            => new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
                maxDepth: 1, // TODO: is depth == 1 enough?
                syntax: DisassemblySyntax.Masm, // TODO: enable diffable format
                printSource: false, // we are not interested in getting C#
                printInstructionAddresses: false, // would make the diffing hard, however could be useful to determine alignment
                exportGithubMarkdown: false,
                exportHtml: false,
                exportCombinedDisassemblyReport: false,
                exportDiff: false));
    }
}
