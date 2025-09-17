using GC.Infrastructure.Commands.Microbenchmark;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;
using ModelContextProtocol.Server;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GC.Infrastructure.MCPServer
{
    [McpServerToolType]
    internal class Microbenchmarks
    {
        private CommandApp _app = new CommandApp();
        public Microbenchmarks()
        {
            _app.Configure(configuration =>
            {
                // Microbenchmarks
                configuration.AddCommand<MicrobenchmarkCommand>("microbenchmarks");
                configuration.AddCommand<MicrobenchmarkAnalyzeCommand>("microbenchmarks-analyze");
            });
        }

        [McpServerTool(Name = "run_microbenchmarks_command"), Description("Executes .NET microbenchmarks using BenchmarkDotNet to measure fine-grained performance characteristics of specific code paths, methods, or algorithms. This tool provides highly accurate performance measurements with statistical analysis and is ideal for detecting performance regressions or improvements at the micro level.")]
        public string RunMicrobenchmarksCommand(
            [Description("The absolute path to the microbenchmark configuration file (YAML format) that defines which benchmarks to run, test parameters, runtime configurations, and output settings for the benchmark execution.")] string configurationPath)
        {
            try
            {
                string[] args = { "microbenchmarks", "-c", configurationPath };
                _app.Run(args);
                MicrobenchmarkConfiguration configuration = MicrobenchmarkConfigurationParser.Parse(configurationPath);
                return $"Microbenchmarks execution completed successfully. Benchmark results and statistical analysis saved to: {configuration.Output!.Path}. Test config is available in {configurationPath}.";
            }
            catch (Exception ex)
            {
                return $"Failed to run microbenchmarks. Error: {ex.Message}. Please verify the configuration file path exists and contains valid benchmark settings.";
            }
        }

        [McpServerTool(Name = "run_microbenchmarks_analyze_command"), Description("Analyzes microbenchmark results to generate comprehensive performance reports, statistical summaries, and trend analysis. This tool processes BenchmarkDotNet output data to create detailed performance insights, including performance comparisons, statistical significance tests, and formatted reports suitable for performance reviews.")]
        public string RunMicrobenchmarksAnalyzeCommand(
            [Description("The absolute path to the microbenchmark configuration file (YAML format) that was used to generate the benchmark results. This configuration contains the output path where analysis results and reports will be saved.")] string configurationPath)
        {
            try
            {
                string[] args = { "microbenchmarks-analyze", "-c", configurationPath };
                _app.Run(args);
                MicrobenchmarkConfiguration configuration = MicrobenchmarkConfigurationParser.Parse(configurationPath);
                return $"Microbenchmark analysis completed successfully. Performance reports, statistical summaries, and trend analysis generated at: {configuration.Output!.Path}. Test config is available in {configurationPath}.";
            }
            catch (Exception ex)
            {
                return $"Failed to analyze microbenchmark results. Error: {ex.Message}. Please ensure the configuration file exists and contains valid benchmark results to analyze.";
            }
        }
    }
}
