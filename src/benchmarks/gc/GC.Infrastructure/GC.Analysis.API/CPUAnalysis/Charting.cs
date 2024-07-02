using XPlot.Plotly;

namespace GC.Analysis.API
{
    public static class CPUCharting
    {
        private const string xAxisAsGCNumber = "GC #";

        public static PlotlyChart ChartCountForGCMethod((string name, List<CPUInfo> gcsToCost) data,
                                                        string title,
                                                        ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title, fieldName: data.name, xAxis: xAxisAsGCNumber, chartInfo: chartInfo);
            List<float> cost = data.gcsToCost.Select(gc => gc.Count);
            List<int> gcNumber = data.gcsToCost.Select(gc => gc.GC.Number);

            Scatter scatter = new Scatter
            {
                x = gcNumber,
                y = cost,
                showlegend = true,
                name = data.name,
            };

            return Chart.Plot(scatter, layout);
        }

        public static PlotlyChart ChartCountForGCMethodWithGCData((string name, List<CPUInfo> gcsToCost) data,
                                                                  (string name, List<double> gcData) other,
                                                                   string title,
                                                                   ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title, fieldName: "Inclusive Count", xAxis: xAxisAsGCNumber, chartInfo: chartInfo);
            List<float> cost = data.gcsToCost.Select(gc => gc.Count);
            List<int> gcNumber = data.gcsToCost.Select(gc => gc.GC.Number);

            layout.yaxis2 = new Yaxis { title = other.name, side = "right", overlaying = "y" };

            Scatter cpuScatter = new Scatter
            {
                x = gcNumber,
                y = cost,
                showlegend = true,
                name = data.name,
            };

            Scatter gcScatter = new Scatter
            {
                x = gcNumber,
                y = other.gcData,
                yaxis = "y2",
                showlegend = true,
                name = other.name,
            };

            return Chart.Plot(new[] { cpuScatter, gcScatter }, layout);
        }

        public static PlotlyChart ChartCountForGCMethods(IEnumerable<(string name, List<CPUInfo> gcsToCost)> data,
                                                         string title,
                                                         ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title, fieldName: "Inclusive Cost", xAxis: xAxisAsGCNumber, chartInfo: chartInfo);
            List<Scatter> scatters = new();

            foreach (var d in data)
            {
                List<float> cost = d.gcsToCost.Select(gc => gc.Count);
                List<int> gcNumber = d.gcsToCost.Select(gc => gc.GC.Number);

                Scatter scatter = new Scatter
                {
                    x = gcNumber,
                    y = cost,
                    showlegend = true,
                    mode = "lines+markers",
                    name = d.name,
                };

                scatters.Add(scatter);
            }

            return Chart.Plot(scatters, layout);
        }

        public static PlotlyChart ChartCountForGCMethod(this CPUProcessData cpuProcessData,
                                                        string methodName,
                                                        string title,
                                                        string caller,
                                                        bool isInclusiveCount = true,
                                                        ChartInfo? chartInfo = null)
        {
            string countType = isInclusiveCount ? "Inclusive Count" : "Exclusive Count";
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title, fieldName: countType, xAxis: xAxisAsGCNumber, chartInfo: chartInfo);
            var data = cpuProcessData.GetPerGCMethodCost(methodName: methodName, caller: caller, isInclusiveCount: isInclusiveCount);
            List<float> cost = data.Select(gc => gc.Count);
            List<int> gcNumber = data.Select(gc => gc.GC.Number);

            Scatter scatter = new Scatter
            {
                x = gcNumber,
                y = cost,
                showlegend = true,
                mode = "lines+markers",
                name = methodName,
            };

            return Chart.Plot(scatter, layout);
        }

        public static PlotlyChart ChartCountForGCMethod(this CPUProcessData cpuProcessData,
                                                        string methodName,
                                                        string title,
                                                        bool isInclusiveCount = true,
                                                        ChartInfo? chartInfo = null)
        {
            string countType = isInclusiveCount ? "Inclusive Count" : "Exclusive Count";
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title, fieldName: countType, xAxis: xAxisAsGCNumber, chartInfo: chartInfo);
            var data = cpuProcessData.GetPerGCMethodCost(methodName: methodName, isInclusiveCount: isInclusiveCount);
            List<float> cost = data.Select(gc => gc.Count);
            List<int> gcNumber = data.Select(gc => gc.GC.Number);

            Scatter scatter = new Scatter
            {
                x = gcNumber,
                y = cost,
                showlegend = true,
                mode = "lines+markers",
                name = methodName,
            };

            return Chart.Plot(scatter, layout);
        }

        public static PlotlyChart ChartCountForGCMethods(this CPUProcessData cpuProcessData,
                                                         IEnumerable<string> methodNames,
                                                         string title,
                                                         bool isInclusiveCount = true,
                                                         ChartInfo? chartInfo = null)
        {
            string countType = isInclusiveCount ? "Inclusive Count" : "Exclusive Count";
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title, fieldName: countType, xAxis: xAxisAsGCNumber, chartInfo: chartInfo);

            List<Scatter> scatters = new();

            foreach (var methodName in methodNames)
            {
                var data = cpuProcessData.GetPerGCMethodCost(methodName: methodName, isInclusiveCount: isInclusiveCount);
                List<float> cost = data.Select(gc => gc.Count);
                List<int> gcNumber = data.Select(gc => gc.GC.Number);

                Scatter scatter = new Scatter
                {
                    x = gcNumber,
                    y = cost,
                    showlegend = true,
                    mode = "lines+markers",
                    name = methodName,
                };

                scatters.Add(scatter);
            }

            return Chart.Plot(scatters, layout);
        }
    }
}
