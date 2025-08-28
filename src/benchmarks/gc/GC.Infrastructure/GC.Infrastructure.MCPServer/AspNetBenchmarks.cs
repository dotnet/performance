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

        [McpServerTool(Name = "run_aspnetbenchmarks_command"), Description("Run aspnetbenchmarks Command.")]
        public string RunAspNetBenchmarksCommand(string configurationPath)
        {
            string[] args = { "aspnetbenchmarks", "-c", configurationPath };
            _app.Run(args);
            ASPNetBenchmarksConfiguration configuration = ASPNetBenchmarksConfigurationParser.Parse(configurationPath);
            return configuration.Output!.Path;
        }

        [McpServerTool(Name = "run_aspnetbenchmarks_analyze_command"), Description("Run aspnetbenchmarks-analyze Command.")]
        public string RunAspNetBenchmarksAnalyzeCommand(string configurationPath)
        {
            string[] args = { "aspnetbenchmarks-analyze", "-c", configurationPath };
            _app.Run(args);
            ASPNetBenchmarksConfiguration configuration = ASPNetBenchmarksConfigurationParser.Parse(configurationPath);
            return configuration.Output!.Path;
        }
    }
}
