using GC.Infrastructure.Commands.RunCommand;
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

        [McpServerTool(Name = "run_run_command"), Description("Run run Command.")]
        public void RunRunCommand(string configurationPath)
        {
            string[] args = { "run", "-c", configurationPath };
            _app.Run(args);
        }

        [McpServerTool(Name = "run_createsuites_command"), Description("Run createsuites Command.")]
        public void RunCreateSuitesCommand(string configurationPath)
        {
            string[] args = { "createsuites", "-c", configurationPath };
            _app.Run(args);
        }

        [McpServerTool(Name = "run_run-suite_command"), Description("Run run-suite Command.")]
        public void RunRunSuiteCommand(string suiteBasePath)
        {
            string[] args = { "run-suite", "-p", suiteBasePath };
            _app.Run(args);
        }
    }
}
