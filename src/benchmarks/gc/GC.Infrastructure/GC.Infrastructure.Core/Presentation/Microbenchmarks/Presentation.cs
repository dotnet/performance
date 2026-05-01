using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;

namespace GC.Infrastructure.Core.Presentation.Microbenchmarks
{
    public static class Presentation
    {
        public static void Present(MicrobenchmarkConfiguration configuration, List<MicrobenchmarkComparisonResults> comparisonResultsGroupedByName, Dictionary<string, ProcessExecutionDetails> executionDetails)
        {
            foreach (var format in configuration.Output.Formats)
            {
                if (format == "markdown")
                {
                    //Markdown.GenerateTable(configuration, comparisonResultsGroupedByName, executionDetails, Path.Combine(configuration.Output.Path, "Results.md"));
                    continue;
                }

                if (format == "json")
                {
                    Json.Generate(configuration, comparisonResultsGroupedByName, Path.Combine(configuration.Output.Path, "Results.json"));
                    continue;
                }
            }
        }
    }
}
