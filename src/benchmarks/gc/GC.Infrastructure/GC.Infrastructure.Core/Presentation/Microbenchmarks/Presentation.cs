using GC.Infrastructure.Core.Analysis;
using GC.Infrastructure.Core.Analysis.Microbenchmarks;
using GC.Infrastructure.Core.Configurations;
using GC.Infrastructure.Core.Configurations.Microbenchmarks;

namespace GC.Infrastructure.Core.Presentation.Microbenchmarks
{
    public static class Presentation
    {
        public static IReadOnlyList<MicrobenchmarkComparisonResults> Present(MicrobenchmarkConfiguration configuration, Dictionary<string, ProcessExecutionDetails> executionDetails)
        {
            IReadOnlyList<MicrobenchmarkComparisonResults> comparisonResults = MicrobenchmarkResultsAnalyzer.GetComparisons(configuration);
            foreach (var format in configuration.Output.Formats)
            {
                if (format == "markdown")
                {
                    Markdown.GenerateTable(configuration, comparisonResults, executionDetails, Path.Combine(configuration.Output.Path, "Results.md"));
                    continue;
                }

                if (format == "json")
                {
                    Json.Json.Generate(configuration, comparisonResults, Path.Combine(configuration.Output.Path, "Results.json"));
                    continue;
                }
            }

            return comparisonResults;
        }
    }
}
