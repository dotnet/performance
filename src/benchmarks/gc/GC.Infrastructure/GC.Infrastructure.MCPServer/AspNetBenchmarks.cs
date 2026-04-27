using GC.Infrastructure.Commands.ASPNetBenchmarks;
using GC.Infrastructure.Core.Configurations.ASPNetBenchmarks;
using ModelContextProtocol.Server;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GC.Infrastructure.MCPServer
{
    [McpServerToolType]
    internal class AspNetBenchmarks
    {
        private CommandApp _app = new CommandApp();
        public AspNetBenchmarks()
        {
            _app.Configure(configuration =>
            {
                // ASP.NET Benchmarks
                configuration.AddCommand<AspNetBenchmarksCommand>("aspnetbenchmarks");
                configuration.AddCommand<AspNetBenchmarksAnalyzeCommand>("aspnetbenchmarks-analyze");
            });
        }

        [McpServerTool(Name = "run_aspnetbenchmarks_command"), Description("Executes ASP.NET Core performance benchmarks to measure web application performance characteristics including throughput, latency, memory usage, and scalability under various load conditions. This tool simulates real-world web traffic patterns and provides comprehensive performance metrics for ASP.NET Core applications.")]
        public string RunAspNetBenchmarksCommand(
            [Description("The absolute path to the ASP.NET benchmarks configuration file (YAML format) that defines the web application scenarios to test, load patterns, performance metrics to collect, and output settings for the benchmark execution.")] string configurationPath)
        {
            try
            {
                string[] args = { "aspnetbenchmarks", "-c", configurationPath };
                _app.Run(args);
                ASPNetBenchmarksConfiguration configuration = ASPNetBenchmarksConfigurationParser.Parse(configurationPath);
                return $"ASP.NET benchmarks execution completed successfully. Web application performance metrics, throughput data, and latency analysis saved to: {configuration.Output!.Path}. Test config is available in {configurationPath}.";
            }
            catch (Exception ex)
            {
                return $"Failed to run ASP.NET benchmarks. Error: {ex.Message}. Please verify the configuration file path exists and contains valid ASP.NET benchmark settings.";
            }
        }

        [McpServerTool(Name = "run_aspnetbenchmarks_analyze_command"), Description("Analyzes ASP.NET Core benchmark results to generate comprehensive web performance reports, including throughput analysis, latency percentiles, memory usage patterns, and scalability insights. This tool processes benchmark data to create detailed performance assessments suitable for web application optimization and capacity planning.")]
        public string RunAspNetBenchmarksAnalyzeCommand(
            [Description("The absolute path to the ASP.NET benchmarks configuration file (YAML format) that was used to generate the benchmark results. This configuration contains the output path where detailed analysis reports and performance insights will be saved.")] string configurationPath)
        {
            try
            {
                string[] args = { "aspnetbenchmarks-analyze", "-c", configurationPath };
                _app.Run(args);
                ASPNetBenchmarksConfiguration configuration = ASPNetBenchmarksConfigurationParser.Parse(configurationPath);
                return $"ASP.NET benchmark analysis completed successfully. Web performance reports, throughput analysis, latency percentiles, and scalability insights generated at: {configuration.Output!.Path}. Test config is available in {configurationPath}.";
            }
            catch (Exception ex)
            {
                return $"Failed to analyze ASP.NET benchmark results. Error: {ex.Message}. Please ensure the configuration file exists and contains valid ASP.NET benchmark results to analyze.";
            }
        }
    }
}
