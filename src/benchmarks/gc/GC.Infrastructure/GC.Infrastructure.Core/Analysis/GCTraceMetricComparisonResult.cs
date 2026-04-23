using API = GC.Analysis.API;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using System.Reflection;

namespace GC.Infrastructure.Core.Analysis
{
    public sealed class GCTraceMetricComparisonResult
    {
        public GCTraceMetricComparisonResult(IEnumerable<GCTraceMetrics> baselines, IEnumerable<GCTraceMetrics> comparands, string metricName)
        {
            RunName = baselines.FirstOrDefault()?.RunName;
            Key = $"{baselines.FirstOrDefault()?.ConfigurationName}_{RunName}";

            MetricName = metricName;
            PropertyInfo pInfo = typeof(GCTraceMetrics).GetProperty(metricName, BindingFlags.Instance | BindingFlags.Public);

            // Property found on the GCTraceMetrics.
            if (pInfo != null)
            {
                OriginalBaselineMetricCollection = baselines.Select(baseline => (double)pInfo.GetValue(baseline));
                OriginalComparandMetricCollection = comparands.Select(comparand => (double)pInfo.GetValue(comparand));
            }

            // If property isn't found on the GCTraceMetrics, look in GCStats.
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
                        OriginalBaselineMetricCollection = Array.Empty<double>();
                        OriginalComparandMetricCollection = Array.Empty<double>();
                        OutliersFreeBaselineMetricCollection = Array.Empty<double>();
                        OutliersFreeComparandMetricCollection = Array.Empty<double>();
                        AveragedBaselineMetric = double.NaN;
                        AveragedComparandMetric = double.NaN;
                        return;
                    }

                    else
                    {
                        OriginalBaselineMetricCollection = baselines.Select(baseline => baseline.StatsData[fieldInfo.Name]);
                        OriginalComparandMetricCollection = comparands.Select(comparand => comparand.StatsData[fieldInfo.Name]);
                    }
                }

                else
                {
                    OriginalBaselineMetricCollection = baselines.Select(baseline => baseline.StatsData[pInfo.Name]);
                    OriginalComparandMetricCollection = comparands.Select(comparand => comparand.StatsData[pInfo.Name]);
                }
            }

            // Filter out outliers using IQR method
            OutliersFreeBaselineMetricCollection = API.Statistics.RemoveOutliers(OriginalBaselineMetricCollection);
            OutliersFreeComparandMetricCollection = API.Statistics.RemoveOutliers(OriginalComparandMetricCollection);

            // Calculate averaged metrics
            AveragedBaselineMetric = API.GoodLinq.Average(OutliersFreeBaselineMetricCollection, r => r);
            AveragedComparandMetric = API.GoodLinq.Average(OutliersFreeComparandMetricCollection, r => r);
        }

        public string RunName { get; }
        public string Key { get; }
        public string MetricName { get; }
        public IEnumerable<double> OriginalBaselineMetricCollection { get; }
        public IEnumerable<double> OriginalComparandMetricCollection { get; }
        public IEnumerable<double> OutliersFreeBaselineMetricCollection { get; }
        public IEnumerable<double> OutliersFreeComparandMetricCollection { get; }

        public double AveragedBaselineMetric { get; }
        public double AveragedComparandMetric { get; }
        public double Delta => AveragedComparandMetric - AveragedBaselineMetric;
        public double PercentageDelta
        {
            get
            {
                if (AveragedBaselineMetric == 0)
                {
                    if (AveragedComparandMetric == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return double.NaN;
                    }
                }
                else
                {
                    return Delta / AveragedBaselineMetric * 100.0;
                }
            }
        }
    }
}
