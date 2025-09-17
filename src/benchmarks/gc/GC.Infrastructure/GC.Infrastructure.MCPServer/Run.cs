using GC.Infrastructure.Commands.RunCommand;
using GC.Infrastructure.Core.Configurations;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GC.Infrastructure.MCPServer
{
    [McpServerToolType]
    internal class Run
    {
        private CommandApp _app = new CommandApp();
        public Run()
        {
            _app.Configure(configuration =>
            {
                // Run 
                configuration.AddCommand<RunCommand>("run");
                configuration.AddCommand<CreateSuitesCommand>("createsuites");
                configuration.AddCommand<RunSuiteCommand>("run-suite");
            });
        }

        [McpServerTool(Name = "run_run_command"), Description("Executes an end-to-end test including gcperfsim test, microbenchmarks test and aspnetbenchmarks test based on a configuration file.")]
        public string RunRunCommand(
            [Description("The absolute path to the run configuration file (YAML format) that defines the specific performance test or benchmark to execute, including test parameters, environment settings, and output configuration.")] string configurationPath)
        {
            try
            {
                string[] args = { "run", "-c", configurationPath };
                _app.Run(args);
                InputConfiguration configuration = InputConfigurationParser.Parse(configurationPath);
                return $"End-to-end test execution completed successfully. Results and output data saved to: {configuration.output_path}. Test config is available in {configurationPath}.";
            }
            catch (Exception ex)
            {
                return $"Failed to execute end-to-end test. Error: {ex.Message}. Please verify the configuration file path exists and contains valid test settings.";
            }
        }

        [McpServerTool(Name = "run_createsuites_command"), Description("Creates test suites for end-to-end run based on a configuration template.")]
        public string RunCreateSuitesCommand(
            [Description("The absolute path to the run configuration file (YAML format) that defines the specific performance test or benchmark to execute, including test parameters, environment settings, and output configuration.")] string configurationPath)
        {
            try
            {
                string[] args = { "createsuites", "-c", configurationPath };
                _app.Run(args);
                return $"Test suites based on {configurationPath} created successfully.";
            }
            catch (Exception ex)
            {
                return $"Failed to create test suites. Error: {ex.Message}. Please verify the configuration file path exists and contains valid suite creation settings.";
            }
        }

        [McpServerTool(Name = "run_run-suite_command"), Description("Executes an end-to-end test based on test suites.")]
        public string RunRunSuiteCommand(
            [Description("The absolute path to the directory where end-to-end test suites are generated.")] string suiteBasePath)
        {
            try
            {
                string[] args = { "run-suite", "-p", suiteBasePath };
                _app.Run(args);
                string outputPath = Path.GetDirectoryName(suiteBasePath)!;
                return $"Test suite execution completed successfully. All tests in the suite have been executed and results are organized in: {outputPath}";
            }
            catch (Exception ex)
            {
                return $"Failed to execute test suite. Error: {ex.Message}. Please verify the suite base path exists and contains valid test suite configurations.";
            }
        }
    }
}
