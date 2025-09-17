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

        [McpServerTool(Name = "run_gcperfsim_command"), Description("Run gcperfsim Command And Return Output Path.")]
        public string RunGCPerfSimCommand(string configurationPath, bool serverRun=false)
        {
            string[] args = { "gcperfsim", "-c", configurationPath };
            if (serverRun)
            {
                args = args.Append("-s").ToArray();
            }
            _app.Run(args);
            GCPerfSimConfiguration configuration = GCPerfSimConfigurationParser.Parse(configurationPath);
            return configuration.Output!.Path;
        }

        [McpServerTool(Name = "run_gcperfsim_analyze_command"), Description("Run gcperfsim-analyze Command And Return Output Path.")]
        public string RunGCPerfSimAnalyzeCommand(string configurationPath)
        {
            string[] args = { "gcperfsim-analyze", "-c", configurationPath };
            _app.Run(args);
            GCPerfSimConfiguration configuration = GCPerfSimConfigurationParser.Parse(configurationPath);
            return configuration.Output!.Path;
        }

        [McpServerTool(Name = "run_gcperfsim_compare_command"), Description("Run gcperfsim-compare Command.")]
        public void RunGCPerfSimCompareCommand(string baselinePath, string comparandPath, string output)
        {
            string[] args = { "gcperfsim-compare", "-b", baselinePath, "-c", comparandPath, "-o", output };
            _app.Run(args);
        }

        [McpServerTool(Name = "run_gcperfsim_functional_command"), Description("Run gcperfsim-functional Command And Return Output Path.")]
        public string RunGCPerfSimFunctionalCommand(string configurationPath, bool serverRun = false)
        {
            string[] args = { "gcperfsim-functional", "-c", configurationPath };
            if (serverRun)
            {
                args = args.Append("-s").ToArray();
            }
            _app.Run(args);
            GCPerfSimFunctionalConfiguration configuration = GCPerfSimFunctionalConfigurationParser.Parse(configurationPath);
            return configuration.output_path;
        }
    }
}
