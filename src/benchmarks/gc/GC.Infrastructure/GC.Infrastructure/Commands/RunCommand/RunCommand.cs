using GC.Infrastructure.Commands.ASPNetBenchmarks;
using GC.Infrastructure.Commands.GCPerfSim;
using GC.Infrastructure.Commands.Microbenchmark;
using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using GC.Infrastructure.Core.Presentation;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GC.Infrastructure.Commands.RunCommand
{
    public sealed class RunCommand : Command<RunCommand.RunCommandSettings>
    {
        public sealed class RunCommandSettings : CommandSettings
        {
            [Description("Configuration")]
            [CommandOption("-c|--configuration")]
            public string ConfigurationPath { get; set; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] RunCommandSettings settings)
        {
            ConfigurationChecker.VerifyFile(settings.ConfigurationPath, nameof(RunCommand));
            InputConfiguration configuration = InputConfigurationParser.Parse(settings.ConfigurationPath);

            AnsiConsole.Write(new Rule("Creating Suites"));
            AnsiConsole.WriteLine();

            // I. Create suites.
            // ================
            // 1. GCPerfSim.
            // 2. Microbenchmark.
            // 3. ASPNet.
            // TODO: Add the output to this call to figure out where to look for the yaml files for each type.
            Dictionary<string, string> configurationMap = CreateSuitesCommand.CreateSuites(configuration);
            var configurationRoot = new Tree("[underline] Suite Created: [/]");
            foreach (var c in configurationMap)
            {
                configurationRoot.AddNode($"[blue] {c.Key} at {c.Value} [/]");
            }
            AnsiConsole.Write(configurationRoot);
            AnsiConsole.WriteLine();

            // I.a. Misc Setup Tasks
            // =====================
            // 1. Before runs, make copies of sources, symbols and coreruns.

            // II. Start Runs and Complete Analysis.
            // ================
            // 1. GCPerfSim
            // 2. Microbenchmark.
            // 3. ASPNet.

            Stopwatch sw = new();
            sw.Start();

            int gcperfsimTestCount = 0;
            string gcperfsimBase = configurationMap["GCPerfSim"];
            string[] gcperfsimConfigurations = Directory.GetFiles(gcperfsimBase, "*.yaml");

            Dictionary<string, GCPerfSimResults> allComparisonResults = new();
            Dictionary<string, MicrobenchmarkComparisonResults> allMicrobenchmarkResults = new();

            // Run all GCPerfSim Scenarios.
            AnsiConsole.Write(new Rule("Running GCPerfSim"));
            AnsiConsole.WriteLine();

            Dictionary<string, Dictionary<string, double>> configurationToTopLevelMetrics_GCPerfSim = new();

            HashSet<string> gcTopLevelResults = new HashSet<string>(new List<string>
            {
                "ExecutionTimeMSec",
                "PctTimePausedInGC",
                "HeapSizeBeforeMB_Mean",
                "PauseDurationMSec_MeanWhereIsEphemeral",
                "PauseDurationMSec_MeanWhereIsBackground",
                "PauseDurationMSec_MeanWhereIsBlockingGen2",
            });
            Dictionary<string, string> gcLevelResultsMap = new()
            {
                { "ExecutionTimeMSec", "Execution Time (MSec)" },
                { "PctTimePausedInGC", "% GC Pause Time" },
                { "HeapSizeBeforeMB_Mean", "Mean Heap Size Before (MB)" },
                { "PauseDurationMSec_MeanWhereIsEphemeral", "Mean Ephemeral Pause (MSec)" },
                { "PauseDurationMSec_MeanWhereIsBackground", "Mean BGC Pause (MSec)" },
                { "PauseDurationMSec_MeanWhereIsBlockingGen2", "Mean Full Blocking GC Pause (MSec)" }
            };

            HashSet<string> uniqueGCPerfSimTests = new();

            foreach (var c in gcperfsimConfigurations)
            {
                try
                {
                    GCPerfSimConfiguration config = GCPerfSimConfigurationParser.Parse(c);
                    gcperfsimTestCount += config.Runs.Select(r => r.Key).Distinct().Count();
                    GCPerfSimResults comparisonResult = GCPerfSimCommand.RunGCPerfSim(config, null);

                    foreach (var ar in comparisonResult.AnalysisResults)
                    {
                        uniqueGCPerfSimTests.Add(ar.Key);
                    }

                    string path = Path.GetFileNameWithoutExtension(c);

                    allComparisonResults[path] = comparisonResult;

                    foreach (var metric in comparisonResult.AnalysisResults)
                    {
                        string metricKey = path + "_" + metric.RunName;
                        if (gcTopLevelResults.Contains(metric.MetricName))
                        {
                            if (!configurationToTopLevelMetrics_GCPerfSim.TryGetValue(metricKey, out var val))
                            {
                                val = configurationToTopLevelMetrics_GCPerfSim[metricKey] = new Dictionary<string, double>();
                            }

                            val[metric.MetricName] = metric.PercentageDelta;
                        }
                    }
                }

                catch (Exception e)
                {
                    AnsiConsole.Markup($"[red bold] GCPerfSim Configuration: {c} failed with {e.Message} \n {Markup.Escape(e.StackTrace)} [/]");
                }
            }

            // Run all Microbenchmarks. 
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("Running Microbenchmarks"));
            AnsiConsole.WriteLine();
            string microbenchmarkBase = configurationMap["Microbenchmark"];
            string[] microbenchmarkConfigurations = Directory.GetFiles(microbenchmarkBase, "*.yaml");
            HashSet<string> uniqueMicrobenchmarks = new();

            foreach (var c in microbenchmarkConfigurations)
            {
                try
                {
                    MicrobenchmarkConfiguration config = MicrobenchmarkConfigurationParser.Parse(c);
                    MicrobenchmarkOutputResults microbenchmarkResults = MicrobenchmarkCommand.RunMicrobenchmarks(config);
                    foreach (var r in microbenchmarkResults.ProcessExecutionDetails.Select(p => p.Key.Split("_")[1]))
                    {
                        uniqueMicrobenchmarks.Add(r);
                    }

                    MicrobenchmarkComparisonResults comparisonResults = microbenchmarkResults.AnalysisResults.First();
                    allMicrobenchmarkResults[config.Name] = comparisonResults;
                }

                catch (Exception e)
                {
                    AnsiConsole.Markup($"[red] Microbenchmark Configuration: {c} failed with {e.Message} \n {Markup.Escape(e.StackTrace)} [/]");
                }
            }

            // Run all ASPNet Benchmarks.
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("Running ASPNet Benchmarks"));
            AnsiConsole.WriteLine();
            string aspnetBenchmarks = configurationMap["ASPNetBenchmarks"];
            string[] aspnetConfigurations = Directory.GetFiles(aspnetBenchmarks, "*.yaml");
            Dictionary<string, AspNetBenchmarkResults> aspnetResults = new();
            Dictionary<string, List<MetricResult>> aspNetMetricResults = new();

            foreach (var c in aspnetConfigurations)
            {
                try
                {
                    ASPNetBenchmarksConfiguration config = ASPNetBenchmarksConfigurationParser.Parse(c);
                    AspNetBenchmarkResults results = AspNetBenchmarksCommand.RunASPNetBenchmarks(config);
                    foreach (var kvp in results.ExecutionDetails)
                    {
                        aspnetResults[kvp.Key] = results;
                    }

                    aspNetMetricResults = results.Results;
                }

                catch (Exception e)
                {
                    AnsiConsole.Markup($"[red] ASPNet Configuration: {c} failed with {e.Message} \n {Markup.Escape(e.StackTrace)} [/]");
                }
            }

            // III. Summary Report Generation 
            // ==============================
            // 1. Generate overall report.
            AnsiConsole.Write(new Rule("Generating Report"));
            AnsiConsole.WriteLine();

            string overallReportPath = Path.Combine(configuration.output_path, "Results.md");
            using (StreamWriter swReport = new(overallReportPath))
            {
                StringBuilder sb = new();

                // Add the GC PerfSim Results.
                sb.AppendLine("# GCPerfSim");

                string gcPerfsimBasePath = Path.Combine(configuration.output_path, "GCPerfSim");
                string[] gcPerfsimDirectories = Directory.GetDirectories(gcPerfsimBasePath);

                foreach (var d in gcPerfsimDirectories)
                {
                    string resultPath = Path.Combine(d, "Results.md");
                    string results = File.ReadAllText(resultPath);

                    sb.AppendLine($"\n## Results for: {d}\n");
                    sb.Append(results);
                }

                // Add the Microbenchmark Results.
                sb.AppendLine("# Microbenchmarks");
                string microbenchmarkBasePath = Path.Combine(configuration.output_path, "Microbenchmarks");
                string[] microbenchmarkDirectories = Directory.GetDirectories(microbenchmarkBasePath);
                foreach (var d in microbenchmarkDirectories)
                {
                    string resultPath = Path.Combine(d, "Results.md");
                    string results = File.ReadAllText(resultPath);

                    sb.AppendLine($"\n## Results for: {d}\n");
                    sb.Append(results);
                }

                // Add the ASPNet Benchmarks Results.
                sb.AppendLine("# ASPNet Benchmarks");
                string aspnetBenchmarkBasePath = Path.Combine(configuration.output_path, "ASPNetBenchmarks");
                string aspnetResultPath = Path.Combine(aspnetBenchmarkBasePath, "Results.md");
                string aspnetResultsText = "";
                aspnetResultsText = File.ReadAllText(aspnetResultPath);
                sb.Append(aspnetResultsText);

                swReport.WriteLine($"# Results Comparing {string.Join(" and ", configuration.coreruns.Select(c => $"```{c.Key}```"))}");

                swReport.WriteLine("# Contents");
                swReport.WriteLine("- [Checklist](#checklist)");
                swReport.WriteLine("- [Incomplete Tests](#incomplete-tests)");
                swReport.WriteLine("- [Top Performance Results](#top-performance-results)");
                swReport.WriteLine("- [GC PerfSim](#gcperfsim)");
                swReport.WriteLine("- [Microbenchmarks](#microbenchmarks)");
                swReport.WriteLine("- [ASPNet Benchmarks](#aspnet-benchmarks)\n");

                swReport.GenerateChecklist(gcperfsimConfigurations, microbenchmarkConfigurations, new HashSet<string>(aspNetMetricResults.Keys));

                swReport.WriteLine("# Incomplete Tests");

                swReport.WriteLine($"## GC PerfSim");
                foreach (var d in gcPerfsimDirectories)
                {
                    string gcperfSimResultPath = Path.Combine(d, "Results.md");
                    swReport.Write(MarkdownReportBuilder.CopySectionFromMarkDownPath(gcperfSimResultPath, "Incomplete Tests"));
                }

                swReport.WriteLine("## Microbenchmarks");
                foreach (var d in microbenchmarkDirectories)
                {
                    string microbenchmarkResultPath = Path.Combine(d, "Results.md");
                    swReport.Write(MarkdownReportBuilder.CopySectionFromMarkDownPath(microbenchmarkResultPath, "Incomplete Tests"));
                }

                swReport.WriteLine("## ASPNet Benchmarks");
                swReport.Write(MarkdownReportBuilder.CopySectionFromMarkDownPath(aspnetResultPath, "Incomplete Tests"));

                // Top Level Metrics
                swReport.WriteLine("# Top Performance Results");

                // GC PerfSim Top Level Results.
                swReport.WriteLine($"## GC PerfSim ({uniqueGCPerfSimTests.Count})\n");

                swReport.WriteLine($"|  | {string.Join("|", gcTopLevelResults.Select(r => gcLevelResultsMap[r]))}");
                swReport.WriteLine($"|--- | {string.Join("", Enumerable.Repeat("---|", gcTopLevelResults.Count))}");

                foreach (var r in configurationToTopLevelMetrics_GCPerfSim)
                {
                    string restOfMetrics = "";
                    foreach (var l in gcTopLevelResults)
                    {
                        restOfMetrics += $" {Math.Round(r.Value[l], 2)}% |";
                    }

                    swReport.WriteLine($"| {r.Key} | {restOfMetrics}");
                }
                swReport.WriteLine();

                swReport.WriteLine($"## Microbenchmarks ({uniqueMicrobenchmarks.Count})\n");
                swReport.WriteLine($"|  |  Mean Execution Time (MSec) | {string.Join("|", gcTopLevelResults.Select(r => gcLevelResultsMap[r]))}");
                swReport.WriteLine($"|--- |  ---- | {string.Join("", Enumerable.Repeat("---|", gcTopLevelResults.Count))}");

                foreach (var m in allMicrobenchmarkResults)
                {
                    foreach (var r in m.Value.Ordered)
                    {
                        string restOfMetrics = "";
                        foreach (var l in gcTopLevelResults)
                        {
                            double metric = r.ComparisonResults.FirstOrDefault(f => f.MetricName == l)?.PercentageDelta ?? double.NaN;
                            restOfMetrics += $" {Math.Round(metric, 2)}% |";
                        }

                        swReport.WriteLine($"| {m.Key} {r.MicrobenchmarkName} | {Math.Round(r.MeanDiffPerc, 2)}% | {restOfMetrics}");
                    }
                }

                // ASPNet Top Level Results.
                swReport.WriteLine($"## ASP.NET Benchmarks ({aspnetResults.Keys.Count})\n");
                string aspNetSummary = MarkdownReportBuilder.CopySectionFromMarkDownPath(aspnetResultPath, "Summary");
                swReport.Write(aspNetSummary);
                swReport.WriteLine();
                swReport.Write(sb.ToString());
            }

            sw.Stop();

            AnsiConsole.MarkupLine($"[bold green] Report generated at: {overallReportPath} [/]");
            AnsiConsole.WriteLine($"Took: {sw.ElapsedMilliseconds / 1000} seconds.");

            return 0;
        }
    }
}
