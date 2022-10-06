using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Newtonsoft.Json;
using Perfolizer.Horology;
using Reporting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace BenchmarkDotNet.Extensions
{
    public static class RecommendedConfig
    {
        public static IConfig Create(
            DirectoryInfo artifactsPath,
            ImmutableHashSet<string> mandatoryCategories,
            int? partitionCount = null,
            int? partitionIndex = null,
            List<string> exclusionFilterValue = null,
            List<string> categoryExclusionFilterValue = null,
            Dictionary<string, string> parameterFilterValue = null,
            Job job = null,
            bool getDiffableDisasm = false,
            bool resumeRun = false)
        {
            if (job is null)
            {
                job = Job.Default
                    .WithWarmupCount(1) // 1 warmup is enough for our purpose
                    .WithIterationTime(TimeInterval.FromMilliseconds(250)) // the default is 0.5s per iteration, which is slighlty too much for us
                    .WithMinIterationCount(15)
                    .WithMaxIterationCount(20) // we don't want to run more that 20 iterations
                    .DontEnforcePowerPlan(); // make sure BDN does not try to enforce High Performance power plan on Windows
            }

            if (resumeRun)
            {
                exclusionFilterValue ??= new List<string>();
                exclusionFilterValue.AddRange(GetBenchmarksToResume(artifactsPath));
            }

            var config = ManualConfig.CreateEmpty()
                .WithBuildTimeout(TimeSpan.FromMinutes(15)) // for slow machines
                .AddLogger(ConsoleLogger.Default) // log output to console
                .AddValidator(DefaultConfig.Instance.GetValidators().ToArray()) // copy default validators
                .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray()) // copy default analysers
                .AddExporter(MarkdownExporter.GitHub) // export to GitHub markdown
                .AddColumnProvider(DefaultColumnProviders.Instance) // display default columns (method name, args etc)
                .AddJob(job.AsDefault()) // tell BDN that this are our default settings
                .WithArtifactsPath(artifactsPath.FullName)
                .AddDiagnoser(MemoryDiagnoser.Default) // MemoryDiagnoser is enabled by default
                .AddFilter(new PartitionFilter(partitionCount, partitionIndex))
                .AddFilter(new ExclusionFilter(exclusionFilterValue))
                .AddFilter(new CategoryExclusionFilter(categoryExclusionFilterValue))
                .AddFilter(new ParameterFilter(parameterFilterValue))
                .AddExporter(JsonExporter.Full) // make sure we export to Json
                .AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
                .AddValidator(TooManyTestCasesValidator.FailOnError)
                .AddValidator(new UniqueArgumentsValidator()) // don't allow for duplicated arguments #404
                .AddValidator(new MandatoryCategoryValidator(mandatoryCategories))
                .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(36)); // the default is 20 and trims too aggressively some benchmark results

            if (Reporter.CreateReporter().InLab)
            {
                config = config.AddExporter(new PerfLabExporter());
            }

            if (getDiffableDisasm)
            {
                config = config.AddDiagnoser(CreateDisassembler());
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

        private static IEnumerable<string> GetBenchmarksToResume(DirectoryInfo artifacts)
        {
            if (!artifacts.Exists)
                return new string[0];

            // Get all existing report files, of any export type; order by descending filename length to avoid rename collisions
	    var toRename = artifacts.GetFiles("*-report-*", SearchOption.AllDirectories)
		    .Where(resultFile => !resultFile.FullName.Contains("-resume-report-") || File.Exists(resultFile.FullName.Replace("-resume-report-", "-report-")))
		    .OrderByDescending(resultFile => resultFile.FullName.Length);

	    foreach (var resultFile in toRename)
            {
		// Prepend the report name with -resume, potentially multiple times if multiple reports for the same
		// benchmarks exist, so that they don't collide with one another. But don't unnecessarily prepend
		// -resume multiple times.
                File.Move(resultFile.FullName, resultFile.FullName.Replace("-report-", "-resume-report-"));
	    }

            // From the JSON reports involved in the resume, get the list of benchmarks that were already run in each report	    
            var existingBenchmarks = artifacts.GetFiles("*-resume-report-*.json", SearchOption.AllDirectories)
                .SelectMany(resultFile =>
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject<BdnResult>(File.ReadAllText(resultFile.FullName));
                        var benchmarks = result.Benchmarks.Select(benchmark =>
                        {
                            var nameParts = new[] { benchmark.Namespace, benchmark.Type, benchmark.Method };
                            return string.Join(".", nameParts.Where(part => !string.IsNullOrEmpty(part)));
                        }).Distinct();

                        return benchmarks;
                    }
                    catch (JsonSerializationException)
                    {
                    }

		    // If we could not parse the JSON report data, then we will not try to skip any benchmarks from that report
                    return new string[0];
                });

            if (existingBenchmarks.Any())
            {
                Console.WriteLine($"// Found {existingBenchmarks.Count()} existing result(s) to be skipped:");

                foreach (var benchmark in existingBenchmarks.OrderBy(b => b))
                {
                    Console.WriteLine($"// ***** {benchmark}");
                }
            }

            return existingBenchmarks;
        }

        private class Benchmark
        {
            public string Namespace { get; set; }
            public string Type { get; set; }
            public string Method { get; set; }
        }

        private class BdnResult
        {
            public List<Benchmark> Benchmarks { get; set; }
        }
    }
}
