using GC.Infrastructure.Commands.Microbenchmark;
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

        [McpServerTool(Name = "run_microbenchmarks_command"), Description("Run microbenchmarks Command.")]
        public void RunMicrobenchmarksCommand(string configurationPath)
        {
            string[] args = { "microbenchmarks", "-c", configurationPath };
            _app.Run(args);
        }

        [McpServerTool(Name = "run_microbenchmarks_analyze_command"), Description("Run microbenchmarks-analyze Command.")]
        public void RunMicrobenchmarksAnalyzeCommand(string configurationPath)
        {
            string[] args = { "microbenchmarks-analyze", "-c", configurationPath };
            _app.Run(args);
        }
    }
}
