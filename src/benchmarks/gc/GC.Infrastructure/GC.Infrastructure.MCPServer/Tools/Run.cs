using GC.Infrastructure.Commands.RunCommand;
using GC.Infrastructure.Core.Configurations;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace GC.Infrastructure.MCPServer.Tools
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

        [McpServerTool(Name = "run_run_command"), Description("Run run Command.")]
        public string RunRunCommand(string configurationPath)
        {
            string[] args = { "run", "-c", configurationPath };
            _app.Run(args);
            InputConfiguration configuration = InputConfigurationParser.Parse(configurationPath);
            return configuration.output_path;
        }

        [McpServerTool(Name = "run_createsuites_command"), Description("Run createsuites Command.")]
        public void RunCreateSuitesCommand(string configurationPath)
        {
            string[] args = { "createsuites", "-c", configurationPath };
            _app.Run(args);
        }

        [McpServerTool(Name = "run_run-suite_command"), Description("Run run-suite Command.")]
        public string RunRunSuiteCommand(string suiteBasePath)
        {
            string[] args = { "run-suite", "-p", suiteBasePath };
            _app.Run(args);
            string outputPath = Path.GetDirectoryName(suiteBasePath)!;
            return outputPath;
        }
    }
}
