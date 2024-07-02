using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System.Reflection;
using System.Text;

namespace GC.Infrastructure.Core.Presentation.GCPerfSim
{
    public sealed class ComparisonResult
    {
        public ComparisonResult(ResultItem baseline, ResultItem comparand, string metricName)
        {
            Baseline = baseline;
            Comparand = comparand;

            MetricName = metricName;
            PropertyInfo pInfo = typeof(ResultItem).GetProperty(metricName, BindingFlags.Instance | BindingFlags.Public);

            // Property found on the ResultItem.
            if (pInfo != null)
            {
                BaselineMetric = (double)pInfo.GetValue(baseline);
                ComparandMetric = (double)pInfo.GetValue(comparand);
            }

            // If property isn't found on the ResultItem, look in GCStats.
            // TODO: Add the case where we look into the map.
            else
            {
                pInfo = typeof(GCStats).GetProperty(metricName, BindingFlags.Instance | BindingFlags.Public);
                if (pInfo == null)
                {
                    FieldInfo fieldInfo = typeof(GCStats).GetField(metricName, BindingFlags.Instance | BindingFlags.Public);
                    if (fieldInfo == null)
                    {
                        // Out of luck!
                        BaselineMetric = double.NaN;
                        ComparandMetric = double.NaN;
                    }

                    else
                    {
                        BaselineMetric = (double)fieldInfo.GetValue(baseline);
                        ComparandMetric = (double)fieldInfo.GetValue(comparand);
                    }
                }

                else
                {
                    BaselineMetric = (double)pInfo.GetValue(baseline);
                    ComparandMetric = (double)pInfo.GetValue(comparand);
                }
            }
        }

        public string ToMarkdownString(string runName, string baselineCorerunName, string comparandCorerunName, string configurationName)
        {
            StringBuilder sb = new();
            sb.AppendLine($" | {configurationName}: {runName} - Metric | {baselineCorerunName} | {comparandCorerunName} | Δ%  |  Δ |");
            sb.AppendLine($" | ---- | ------  | ---  |  --- | --- | ");
            sb.AppendLine($" | {MetricName} | {BaselineMetric:N2} | {ComparandMetric:N2} | {PercentageDelta:N2} | {Delta:N2}");
            sb.AppendLine();
            return sb.ToString();
        }

        public string RunName => Baseline.RunName;
        public string MetricName { get; }
        public double BaselineMetric { get; }
        public double ComparandMetric { get; }
        public double Delta => ComparandMetric - BaselineMetric;
        public double PercentageDelta => (Delta / BaselineMetric) * 100;
        public string Key => $"{Baseline.ConfigurationName}_{RunName}";
        public ResultItem Baseline { get; }
        public ResultItem Comparand { get; }
    }

    public sealed class ResultItemComparison
    {
        private readonly ResultItem _baseline;
        private readonly ResultItem _comparand;

        public ResultItemComparison(ResultItem baseline, ResultItem comparand)
        {
            _baseline = baseline;
            _comparand = comparand;
        }

        public ComparisonResult GetComparison(string nameOfMetric)
            => new ComparisonResult(_baseline, _comparand, nameOfMetric);
    }
}
