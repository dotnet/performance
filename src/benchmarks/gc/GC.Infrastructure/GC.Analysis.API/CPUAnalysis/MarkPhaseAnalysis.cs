using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using XPlot.Plotly;

namespace GC.Analysis.API
{
    public static class MarkPhaseAnalysis
    {
        public static PlotlyChart[] ChartStatisticsOfMarkPhaseByType(this GCProcessData processData, int generation, MarkRootType type)
        {
            int heapCount = processData.Stats.HeapCount;
            var generationGCs = processData.GCs.Where(gc => gc.Generation == generation);

            var maxPromoted = generationGCs.Select(gc => (gc.Number, gc.PerHeapMarkTimes.Max(gc => gc.Value.MarkPromoted[(long)type])));
            var minPromoted = generationGCs.Select(gc => (gc.Number, gc.PerHeapMarkTimes.Min(gc => gc.Value.MarkPromoted[(long)type])));
            var avgPromoted = generationGCs.Select(gc => (gc.Number, gc.PerHeapMarkTimes.Sum(gc => gc.Value.MarkPromoted[(long)type]) / heapCount));

            var maxTime = generationGCs.Select(gc => (gc.Number, gc.PerHeapMarkTimes.Max(gc => gc.Value.MarkTimes[(long)type])));
            var minTime = generationGCs.Select(gc => (gc.Number, gc.PerHeapMarkTimes.Min(gc => gc.Value.MarkTimes[(long)type])));
            var avgTime = generationGCs.Select(gc => (gc.Number, gc.PerHeapMarkTimes.Sum(gc => gc.Value.MarkTimes[(long)type]) / heapCount));

            var layoutPromoted = new Layout.Layout
            {
                title = $"Mark Promoted Bytes for Gen{generation} - {type.ToString()}",
                xaxis = new Xaxis { title = "GC #" },
                yaxis = new Yaxis { title = "Bytes" },
                showlegend = true,
            };

            var scatterMaxPromoted = new Scatter
            {
                y = maxPromoted.Select(gc => gc.Item2),
                x = maxPromoted.Select(gc => gc.Number),
                mode = "lines+markers",
                name = $"Max Bytes",
                showlegend = true,
            };

            var scatterMinPromoted = new Scatter
            {
                y = minPromoted.Select(gc => gc.Item2),
                x = maxPromoted.Select(gc => gc.Number),
                mode = "lines+markers",
                name = $"Min Bytes",
                showlegend = true,
            };

            var scatterAveragePromoted = new Scatter
            {
                y = avgPromoted.Select(gc => gc.Item2),
                x = avgPromoted.Select(gc => gc.Number),
                mode = "lines+markers",
                name = $"Avg Bytes",
                showlegend = true,
            };

            var layoutTime = new Layout.Layout
            {
                title = $"Mark Times for Gen{generation} - {type}",
                xaxis = new Xaxis { title = "GC #" },
                yaxis = new Yaxis { title = "ms" },
                showlegend = true,
            };

            var scatterMaxTime = new Scatter
            {
                y = maxTime.Select(gc => gc.Item2),
                x = maxTime.Select(gc => gc.Number),
                mode = "lines+markers",
                name = $"Max Time (ms)",
                showlegend = true,
            };

            var scatterMinTime = new Scatter
            {
                y = minTime.Select(gc => gc.Item2),
                x = minTime.Select(gc => gc.Number),
                mode = "lines+markers",
                name = $"Min Time (ms)",
                showlegend = true,
            };

            var scatterAverageTime = new Scatter
            {
                y = avgTime.Select(gc => gc.Item2),
                x = avgTime.Select(gc => gc.Number),
                mode = "lines+markers",
                name = $"Avg Time (ms)",
                showlegend = true,
            };

            PlotlyChart timeChart = Chart.Plot(new[] { scatterMinTime, scatterAverageTime, scatterMaxTime }, layoutTime);
            PlotlyChart promotedChart = Chart.Plot(new[] { scatterMinPromoted, scatterAveragePromoted, scatterMaxPromoted }, layoutPromoted);

            return new[] { timeChart, promotedChart };
        }


        public static PlotlyChart ChartAverageMarkPhaseTimeByMarkType(this GCProcessData processData, int generation, IEnumerable<MarkRootType> types)
        {
            int heapCount = processData.Stats.HeapCount;
            var generationGCs = processData.GCs.Where(gc => gc.Generation == generation);

            var layout = new Layout.Layout
            {
                title = $"Average Mark Promoted Bytes for Gen{generation}",
                xaxis = new Xaxis { title = "GC #" },
                yaxis = new Yaxis { title = "Bytes" },
                showlegend = true,
            };

            List<Scatter> scatters = new();

            foreach (var type in types)
            {
                var avgPromoted = generationGCs.Select(gc => (gc.Number, gc.PerHeapMarkTimes.Sum(gc => gc.Value.MarkPromoted[(long)type]) / heapCount));
                var scatterAverageTime = new Scatter
                {
                    y = avgPromoted.Select(gc => gc.Item2),
                    x = avgPromoted.Select(gc => gc.Number),
                    mode = "lines+markers",
                    name = $"Avg Time (ms) - {type}",
                    showlegend = true,
                };

                scatters.Add(scatterAverageTime);
            }

            return Chart.Plot(scatters, layout);
        }
    }
}
