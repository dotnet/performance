namespace GC.Infrastructure.Core.Analysis
{
    public sealed class MetricResult
    {
        public MetricResult(string key,
                            string metricName,
                            string baselineName,
                            string comparandName,
                            double baselineValue,
                            double comparandValue)
        {
            Key = key;
            MetricName = metricName;
            BaselineName = baselineName;
            ComparandName = comparandName;
            BaselineValue = baselineValue;
            ComparandValue = comparandValue;
            Delta = Math.Round(ComparandValue - BaselineValue, 4);
        }

        public string Key { get; }
        public string MetricName { get; }
        public string BaselineName { get; }
        public string ComparandName { get; }
        public double BaselineValue { get; }
        public double ComparandValue { get; }
        public double Delta { get; }
        public double DeltaPercent => BaselineValue != 0 ? Math.Round(Delta / BaselineValue, 2) * 100.0
                                                         : double.NaN;
        public override string ToString()
            => $"{Key} | {MetricName} | {BaselineValue} | {ComparandValue} | {DeltaPercent}";
    }
}
