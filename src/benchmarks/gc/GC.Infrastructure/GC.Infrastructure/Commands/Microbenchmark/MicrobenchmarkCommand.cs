using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.CommandBuilders;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Presentation.Microbenchmarks;
using GC.Infrastructure.Core.TraceCollection;
using Newtonsoft.Json;
using Spectre.Console.Cli;
using Spectre.Console;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Text;
using System.Configuration;

namespace GC.Infrastructure.Commands.Microbenchmark
{
    public sealed class MicrobenchmarkOutputResults
    {
        public MicrobenchmarkOutputResults(Dictionary<string, ProcessExecutionDetails> processExecutionDetails, IReadOnlyList<MicrobenchmarkComparisonResults> analysisResults)
        {
            ProcessExecutionDetails = processExecutionDetails;
            AnalysisResults = analysisResults;
        }

        public IReadOnlyDictionary<string, ProcessExecutionDetails> ProcessExecutionDetails { get; }
        public IReadOnlyList<MicrobenchmarkComparisonResults> AnalysisResults { get; }
    }

    internal sealed class MicrobenchmarkCommand : Command<MicrobenchmarkCommand.MicrobenchmarkSettings>
    {
        public static string ReplaceInvalidChars(string filename)
        {
            filename = filename.Replace(" ", "").Replace("(", "_").Replace(")", "_").Replace("\"", "");
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars())).Replace(" ", "").Replace("(", "_").Replace(")", "_");
        }

        public sealed class MicrobenchmarkSettings : CommandSettings
        {
            [Description("Path to Configuration.")]
            [CommandOption("-c|--configuration")]
            public string? ConfigurationPath { get; init; }
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] MicrobenchmarkSettings settings)
        {
            AnsiConsole.Write(new Rule("Microbenchmark Orchestrator"));
            AnsiConsole.WriteLine();

            ConfigurationChecker.VerifyFile(settings.ConfigurationPath, nameof(MicrobenchmarkCommand));
            MicrobenchmarkConfiguration configuration = MicrobenchmarkConfigurationParser.Parse(settings.ConfigurationPath);

            RunMicrobenchmarks(configuration);
            return 0;
        }

        public static MicrobenchmarkOutputResults RunMicrobenchmarks(MicrobenchmarkConfiguration configuration)
        {
            Core.Utilities.TryCreateDirectory(configuration.Output.Path);
            string currentDirectory = Directory.GetCurrentDirectory();

            // Extract the invocation counts.
            Dictionary<string, long> invocationCountCache = new();
            if (!string.IsNullOrEmpty(configuration.MicrobenchmarkConfigurations.InvocationCountPath))
            {
                string[] lines = File.ReadAllLines(configuration.MicrobenchmarkConfigurations.InvocationCountPath);
                for (int lineCount = 1; lineCount < lines.Length; lineCount++)
                {
                    string[] split = lines[lineCount].Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    invocationCountCache[split[0]] = long.Parse(split[1]);
                }
            }

            Dictionary<string, ProcessExecutionDetails> executionDetails = new();

            // Extract the benchmarks to run from the filter.
            string filter = configuration.MicrobenchmarkConfigurations.Filter ?? File.ReadAllText(configuration.MicrobenchmarkConfigurations.FilterPath);
            IEnumerable<string> benchmarks = filter.Split("|", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            Directory.SetCurrentDirectory(configuration.microbenchmarks_path);
            string collectType = configuration.TraceConfigurations?.Type ?? "none";

            HashSet<string> alreadyRunBenchmarks = new();
            KeyValuePair<string, Run> baselineKVP = configuration.Runs.FirstOrDefault(r => r.Value.is_baseline);
            Run baseline = baselineKVP.Value;
            if (baseline == null)
            {
                baselineKVP = configuration.Runs.First();
            }

            foreach (var b in benchmarks)
            {
                string benchmark = b.Replace("\"", "");
                string benchmarkCleanedName = ReplaceInvalidChars(benchmark);

                long? invocationCountFromBaseline = null;

                // Get the invocation count if cached, else compute it.
                if (!invocationCountCache.TryGetValue(benchmark, out var invocationCount))
                {
                    string baselineRunPath = Path.Combine(configuration.Output.Path, $"{baselineKVP.Key}_{benchmarkCleanedName}_InvocationCountRun").Replace("<", "").Replace(">", "");

                    (string, string) baselineFileNameAndCommand = MicrobenchmarkCommandBuilder.Build(configuration, baselineKVP, benchmark, null, baselineRunPath);

                    // Create the run path directory.
                    Core.Utilities.TryCreateDirectory(baselineRunPath);

                    using (Process bdnProcess = new())
                    {
                        bdnProcess.StartInfo.FileName = baselineFileNameAndCommand.Item1;
                        bdnProcess.StartInfo.Arguments = baselineFileNameAndCommand.Item2;
                        bdnProcess.StartInfo.UseShellExecute = false;
                        bdnProcess.StartInfo.CreateNoWindow = true;
                        bdnProcess.Start();
                        bdnProcess.WaitForExit((int)configuration.Environment.default_max_seconds * 1000);
                    }

                    string[] jsonFiles = Directory.GetFiles(baselineRunPath, "*full.json", SearchOption.AllDirectories);

                    // Should only be one if it's a fresh run.
                    string jsonFile = jsonFiles.First();

                    MicrobenchmarkResults output = JsonConvert.DeserializeObject<MicrobenchmarkResults>(File.ReadAllText(jsonFile));

                    // Assumption: A particular run, regardless of the parameters, will run ~the same vals.
                    IEnumerable<long> operationsPerNanos = output.Benchmarks.First().Measurements.Where(m => m.IterationMode == "Workload" && m.IterationStage == "Actual")
                                                                                                .Select(m => m.Operations);
                    // For now take the max but we will possibly be sacrificing duration for precision.
                    invocationCountFromBaseline = operationsPerNanos.Max();
                }

                else
                {
                    invocationCountFromBaseline = invocationCount;
                }

                foreach (var run in configuration.Runs)
                {
                    AnsiConsole.Markup($"[bold green] ({DateTime.Now}) Running Microbechmarks: {configuration.Name} - {run.Key} {benchmark} [/]\n");
                    string runPath = Path.Combine(configuration.Output.Path, run.Key);

                    // Create the run path directory.
                    if (!Directory.Exists(runPath))
                    {
                        Directory.CreateDirectory(runPath);
                    }

                    // Build the command.
                    (string, string) fileNameAndCommand = MicrobenchmarkCommandBuilder.Build(configuration, run, benchmark, invocationCountFromBaseline);
                    run.Value.Name = run.Key;

                    // Run The BDN process with the trace collector.
                    using (Process bdnProcess = new())
                    {
                        bdnProcess.StartInfo.FileName = fileNameAndCommand.Item1;
                        bdnProcess.StartInfo.Arguments = fileNameAndCommand.Item2;
                        bdnProcess.StartInfo.UseShellExecute = false;
                        bdnProcess.StartInfo.RedirectStandardError = true;
                        bdnProcess.StartInfo.RedirectStandardOutput = true;
                        bdnProcess.StartInfo.CreateNoWindow = true;

                        StringBuilder consoleOutput = new();
                        StringBuilder consoleError = new();

                        bdnProcess.OutputDataReceived += (s, e) =>
                        {
                            consoleOutput.AppendLine(e.Data);

                        };

                        bdnProcess.ErrorDataReceived += (s, e) =>
                        {
                            consoleError.AppendLine(e.Data);
                        };

                        using (TraceCollector traceCollector = new TraceCollector(benchmarkCleanedName, collectType, runPath))
                        {
                            bdnProcess.Start();
                            bdnProcess.BeginOutputReadLine();
                            bdnProcess.BeginErrorReadLine();
                            bdnProcess.WaitForExit((int)configuration.Environment.default_max_seconds * 1000);
                        }

                        ProcessExecutionDetails details = new(key: $"{run.Key}_{benchmark}",
                                                              commandlineArgs: $"{fileNameAndCommand.Item1} {fileNameAndCommand.Item2}",
                                                              environmentVariables: run.Value.environment_variables,
                                                              standardError: consoleError.ToString(),
                                                              standardOut: consoleOutput.ToString(),
                                                              exitCode: bdnProcess.ExitCode);
                        executionDetails[$"{run.Key}_{benchmark}"] = details;
                    }
                }
            }

            IReadOnlyList<MicrobenchmarkComparisonResults> results = Presentation.Present(configuration, executionDetails);
            Directory.SetCurrentDirectory(currentDirectory);
            AnsiConsole.Markup($"[bold green] ({DateTime.Now}) Wrote Microbechmark Results to: {Markup.Escape(Path.Combine(configuration.Output.Path, "Results.md"))} [/]");
            return new MicrobenchmarkOutputResults(executionDetails, results);
        }
    }
}
