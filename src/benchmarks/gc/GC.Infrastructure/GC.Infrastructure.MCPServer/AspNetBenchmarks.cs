using GC.Infrastructure.Commands.ASPNetBenchmarks;
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

        [McpServerTool(Name = "run_aspnetbenchmarks_command"), Description("Run aspnetbenchmarks Command.")]
        public void RunAspNetBenchmarksCommand(string configurationPath)
        {
            string[] args = { "aspnetbenchmarks", "-c", configurationPath };
            _app.Run(args);
        }

        [McpServerTool(Name = "run_aspnetbenchmarks_analyze_command"), Description("Run aspnetbenchmarks-analyze Command.")]
        public void RunAspNetBenchmarksAnalyzeCommand(string configurationPath)
        {
            string[] args = { "aspnetbenchmarks-analyze", "-c", configurationPath };
            _app.Run(args);
        }
    }
}
