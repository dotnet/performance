using Microsoft.Diagnostics.Tracing.Analysis.GC;
using XPlot.Plotly;

namespace GC.Analysis.API
{
    public static class GCCharting
    {
        public static PlotlyChart ChartGCData(string title, AxisInfo axisInfo, ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = new Layout.Layout
            {
                title = title,
            };

            var scatter = new Scatter
            {
                x = axisInfo.XAxis,
                y = axisInfo.YAxis,
                name = axisInfo.Name,
                mode = "lines+markers",
                showlegend = true,
            };

            return Chart.Plot(scatter, layout);
        }

        public static PlotlyChart ChartGCData(string title, IEnumerable<AxisInfo> axisInfo, ChartInfo? chartInfo = null)
        {
            List<Scatter> scatters = new();
            Layout.Layout layout = new Layout.Layout
            {
                title = title,
            };

            foreach (var axis in axisInfo)
            {
                var scatter = new Scatter
                {
                    x = axis.XAxis,
                    y = axis.YAxis,
                    name = axis.Name,
                    mode = "lines+markers",
                    showlegend = true,
                };

                scatters.Add(scatter);
            }

            return Chart.Plot(scatters, layout);
        }

        public static PlotlyChart ChartGCData(string title, MultiAxisInfo axisInfo, ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title,
                                                                   fieldName: "Count",
                                                                   xAxis: "Count",
                                                                   chartInfo: chartInfo);
            layout.yaxis2 = new Yaxis { title = axisInfo.Name, side = "right", overlaying = "y" };
            List<Scatter> scatters = new();

            var scatter = new Scatter
            {
                x = axisInfo.XAxis,
                y = axisInfo.YAxis1,
                yaxis = "y2",
                mode = "lines+markers",
                name = axisInfo.Name,
                showlegend = true,
            };

            var other = new Scatter
            {
                x = axisInfo.XAxis,
                y = axisInfo.YAxis2,
                name = axisInfo.Name,
                mode = "lines+markers",
                showlegend = true,
            };

            scatters.Add(scatter);
            scatters.Add(other);

            return Chart.Plot(scatters, layout);
        }

        public static PlotlyChart ChartGCData(IEnumerable<TraceGC> gcs,
                                              string title,
                                              string fieldName,
                                              string xAxis = nameof(TraceGC.Number),
                                              ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title,
                                                                   fieldName: fieldName,
                                                                   xAxis: xAxis,
                                                                   chartInfo: chartInfo);

            IEnumerable<double> y = ReflectionHelpers.GetDoubleValueForGCField(gcs, fieldName);
            IEnumerable<double> x = ReflectionHelpers.GetDoubleValueForGCField(gcs, xAxis);

            var scatter = new Scatter
            {
                x = x,
                y = y,
                name = fieldName,
                mode = "lines+markers",
                showlegend = true,
            };

            return Chart.Plot(scatter, layout);
        }

        public static PlotlyChart ChartGCData(IEnumerable<TraceGC> gcs,
                                              string title,
                                              IEnumerable<(string scatterName, string fieldName)> fields,
                                              string xAxis = nameof(TraceGC.Number),
                                              ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title,
                                                                   fieldNames: fields.Select(f => f.fieldName),
                                                                   xAxis: xAxis,
                                                                   chartInfo: chartInfo);

            List<Scatter> scatters = new();
            foreach (var fieldName in fields)
            {
                IEnumerable<double> y = ReflectionHelpers.GetDoubleValueForGCField(gcs, fieldName.fieldName);
                IEnumerable<double> x = ReflectionHelpers.GetDoubleValueForGCField(gcs, xAxis);

                var scatter = new Scatter
                {
                    x = x,
                    y = y,
                    showlegend = true,
                    mode = "lines+markers",
                    name = fieldName.scatterName,
                };

                scatters.Add(scatter);
            }

            return Chart.Plot(scatters, layout);
        }

        public static PlotlyChart ChartGCData(object gcData, // Really an enumerable
                                              string title,
                                              IEnumerable<(string scatterName, string fieldName)> fields,
                                              string xAxis,
                                              ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title, fieldNames: fields.Select(f => f.fieldName), xAxis: xAxis, chartInfo: chartInfo);
            List<Scatter> scatters = new List<Scatter>();

            var customData = gcData as object[];
            if (customData == null)
            {
                throw new ArgumentException($"The input {nameof(gcData)} should be an IEnumerable.");
            }

            foreach (var field in fields)
            {
                IEnumerable<double> y = ReflectionHelpers.GetDoubleValueFromFieldForCustomObjects(customData, field.fieldName);
                IEnumerable<double> x = ReflectionHelpers.GetDoubleValueFromFieldForCustomObjects(customData, xAxis);

                var scatter = new Scatter
                {
                    x = x,
                    y = y,
                    showlegend = true,
                    mode = "lines+markers",
                    name = field.scatterName,
                };

                scatters.Add(scatter);
            }

            return Chart.Plot(scatters, layout);
        }

        public static PlotlyChart ChartGCData(List<(string scatterName, List<TraceGC> gcs)> gcData,
                                              string title,
                                              string fieldName,
                                              bool isXAxisRelative,
                                              string xAxis = nameof(TraceGC.Number),
                                              ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title, fieldName: fieldName, xAxis, chartInfo: chartInfo);
            List<Scatter> scatters = new List<Scatter>();

            IEnumerable<double> relativeGCIndex = Enumerable.Empty<double>();
            if (isXAxisRelative)
            {
                if (xAxis == nameof(TraceGC.Number))
                {
                    int maxNumberOfGCs = gcData.Max(gc => gc.gcs.Count);
                    relativeGCIndex = Enumerable.Range(0, maxNumberOfGCs).Select(r => (double)r);
                }
            }

            foreach (var gcs in gcData)
            {
                IEnumerable<double> y = ReflectionHelpers.GetDoubleValueForGCField(gcs.gcs, fieldName);

                IEnumerable<double> x = Enumerable.Empty<double>();
                if (isXAxisRelative)
                {
                    x = relativeGCIndex;
                }

                else
                {
                    x = ReflectionHelpers.GetDoubleValueForGCField(gcs.gcs, xAxis);
                }

                var scatter = new Scatter
                {
                    x = x,
                    y = y,
                    name = gcs.scatterName,
                    mode = "lines+markers",
                    showlegend = true
                };

                scatters.Add(scatter);
            }

            return Chart.Plot(scatters, layout);
        }

        public static PlotlyChart ChartGCData(IEnumerable<TraceGC> gcs,
                                              string title,
                                              string fieldName,
                                              IEnumerable<(string scatterName, Func<TraceGC, bool> filter)> filters,
                                              string xAxis = nameof(TraceGC.Number),
                                              ChartInfo? chartInfo = null)
        {
            Layout.Layout layout = ChartingHelpers.ConstructLayout(title: title, fieldName: fieldName, xAxis: xAxis, chartInfo: chartInfo);

            List<Scatter> scatters = new();

            foreach (var filter in filters)
            {
                var filtered = gcs.Where(gc => filter.filter(gc));
                IEnumerable<double> y = ReflectionHelpers.GetDoubleValueForGCField(filtered, fieldName);
                IEnumerable<double> x = ReflectionHelpers.GetDoubleValueForGCField(filtered, xAxis);

                var scatter = new Scatter
                {
                    x = x,
                    y = y,
                    showlegend = true,
                    mode = "lines+markers",
                    name = filter.scatterName,
                };

                scatters.Add(scatter);
            }

            return Chart.Plot(scatters, layout);
        }
    }
}
