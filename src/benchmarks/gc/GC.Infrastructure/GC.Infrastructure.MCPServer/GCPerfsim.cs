using GC.Infrastructure.Commands.GCPerfSim;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.GCPerfSim;
using ModelContextProtocol.Server;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GC.Infrastructure.MCPServer
{
    [McpServerToolType]
    internal class GCPerfsim
    {
        private CommandApp _app = new CommandApp();
        public GCPerfsim()
        {
            _app.Configure(configuration =>
            {
                // GC PerfSim
                configuration.AddCommand<GCPerfSimCommand>("gcperfsim");
                configuration.AddCommand<GCPerfSimAnalyzeCommand>("gcperfsim-analyze");
                configuration.AddCommand<GCPerfSimCompareCommand>("gcperfsim-compare");
                configuration.AddCommand<GCPerfSimFunctionalCommand>("gcperfsim-functional");
            });
        }

        [McpServerTool(Name = "run_gcperfsim_command"), Description("Executes a GC performance simulation using GCPerfSim to measure garbage collection behavior and performance metrics. This tool runs synthetic workloads that stress the garbage collector and generates detailed performance data for analysis.")]
        public string RunGCPerfSimCommand(
            [Description("The absolute path to the GCPerfSim configuration file (YAML format) that defines the simulation parameters, workload characteristics, and output settings.")] string configurationPath, 
            [Description("Whether to run test on server. The test is run on local machine by default.")] bool serverRun = false)
        {
            try
            {
                string[] args = { "gcperfsim", "-c", configurationPath };
                if (serverRun)
                {
                    args = args.Append("-s").ToArray();
                }
                _app.Run(args);
                GCPerfSimConfiguration configuration = GCPerfSimConfigurationParser.Parse(configurationPath);
                return $"GCPerfSim test completed successfully. Results saved to: {configuration.Output!.Path}. Test configuration is available in {configurationPath}";
            }
            catch (Exception ex)
            {
                return $"Failed to run GCPerfSim command. Error: {ex.Message}. Please verify the configuration file path and format.";
            }
        }

        [McpServerTool(Name = "run_gcperfsim_analyze_command"), Description("Analyzes the results from a GCPerfSim test run to generate comprehensive performance reports, metrics, and visualizations. This tool processes raw performance data and produces human-readable analysis of GC behavior.")]
        public string RunGCPerfSimAnalyzeCommand(
            [Description("The absolute path to the GCPerfSim configuration file (YAML format) that defines the simulation parameters, workload characteristics, and output settings.")] string configurationPath)
        {
            try
            {
                string[] args = { "gcperfsim-analyze", "-c", configurationPath };
                _app.Run(args);
                GCPerfSimConfiguration configuration = GCPerfSimConfigurationParser.Parse(configurationPath);
                return $"GCPerfSim analysis completed successfully. Analysis reports generated at: {configuration.Output!.Path}. Test configuration is available in {configurationPath}";
            }
            catch (Exception ex)
            {
                return $"Failed to run GCPerfSim analysis. Error: {ex.Message}. Please ensure the configuration file exists and contains valid test results to analyze.";
            }
        }

        [McpServerTool(Name = "run_gcperfsim_compare_command"), Description("Performs a comparative analysis between two GCPerfSim test results to identify performance differences, regressions, or improvements. This tool generates side-by-side comparisons and highlights significant changes in GC metrics.")]
        public string RunGCPerfSimCompareCommand(
            [Description("The absolute path to the baseline trace file. This represents the reference performance data that the comparison will be made against.")] string baselinePath, 
            [Description("The absolute path to the comparand trace file. This represents the new or modified performance data to compare against the baseline.")] string comparandPath, 
            [Description("The absolute path where the comparison report will be saved. This will contain detailed analysis showing differences between baseline and comparand results.")] string output)
        {
            try
            {
                string[] args = { "gcperfsim-compare", "-b", baselinePath, "-c", comparandPath, "-o", output };
                _app.Run(args);
                return $"GCPerfSim comparison between {baselinePath} and {comparandPath} completed successfully. Comparison report saved to: {output}";
            }
            catch (Exception ex)
            {
                return $"Failed to run GCPerfSim comparison. Error: {ex.Message}. Please verify that both baseline and comparand paths exist and contain valid test results.";
            }
        }

        [McpServerTool(Name = "run_gcperfsim_functional_command"), Description("Executes GCPerfSim functional tests to validate garbage collector correctness and behavior under various scenarios. This tool focuses on testing GC functionality rather than performance.")]
        public string RunGCPerfSimFunctionalCommand(
            [Description("The absolute path to the GCPerfSim functional test configuration file (YAML format) that defines the test scenarios, validation criteria, and output settings for functional testing.")] string configurationPath, 
            [Description("Whether to run test on server. The test is run on local machine by default.")] bool serverRun = false)
        {
            try
            {
                string[] args = { "gcperfsim-functional", "-c", configurationPath };
                if (serverRun)
                {
                    args = args.Append("-s").ToArray();
                }
                _app.Run(args);
                GCPerfSimFunctionalConfiguration configuration = GCPerfSimFunctionalConfigurationParser.Parse(configurationPath);
                return $"GCPerfSim functional tests completed successfully. Test results saved to: {configuration.output_path}. Test configuration is available in {configurationPath}";
            }
            catch (Exception ex)
            {
                return $"Failed to run GCPerfSim functional tests. Error: {ex.Message}. Please verify the functional test configuration file path and format.";
            }
        }
    }
}
