namespace GC.Analysis.API.DynamicEvents
{
    internal static class GCDynamicEvents
    {
        public static DynamicEventSchema SizeAdaptationSampleSchema = new DynamicEventSchema
        {
            DynamicEventName = "SizeAdaptationSample",
            Fields = new List<KeyValuePair<string, Type>>
            {
                KeyValuePair.Create("version", typeof(ushort)),
                KeyValuePair.Create("GCIndex", typeof(ulong)),
                KeyValuePair.Create("ElapsedTimeBetweenGCs", typeof(uint)),
                KeyValuePair.Create("GCPauseTime", typeof(uint)),
                KeyValuePair.Create("SOHMSLWaitTime", typeof(uint)),
                KeyValuePair.Create("UOHMSLWaitTime", typeof(uint)),
                KeyValuePair.Create("TotalSOHStableSize", typeof(ulong)),
                KeyValuePair.Create("Gen0BudgetPerHeap", typeof(uint)),
            }
        };

        public static DynamicEventSchema SizeAdaptationTuningSchema = new DynamicEventSchema
        {
            DynamicEventName = "SizeAdaptationTuning",
            Fields = new List<KeyValuePair<string, Type>>
            {
                KeyValuePair.Create("version", typeof(ushort)),
                KeyValuePair.Create("NewNHeaps", typeof(ushort)),
                KeyValuePair.Create("MaxHeapCountDatas", typeof(ushort)),
                KeyValuePair.Create("MinHeapCountDatas", typeof(ushort)),
                KeyValuePair.Create("CurrentGCIndex", typeof(ulong)),
                KeyValuePair.Create("TotalSOHStableSize", typeof(ulong)),
                KeyValuePair.Create("MedianThroughputCostPercent", typeof(float)),
                KeyValuePair.Create("TcpToConsider", typeof(float)),
                KeyValuePair.Create("CurrentAroundTargetAccumulation", typeof(float)),
                KeyValuePair.Create("RecordedTcpCount", typeof(ushort)),
                KeyValuePair.Create("RecordedTcpSlope", typeof(float)),
                KeyValuePair.Create("NumGcsSinceLastChange", typeof(uint)),
                KeyValuePair.Create("AggFactor", typeof(bool)),
                KeyValuePair.Create("ChangeDecision", typeof(ushort)),
                KeyValuePair.Create("AdjReason", typeof(ushort)),
                KeyValuePair.Create("HcChangeFreqFactor", typeof(ushort)),
                KeyValuePair.Create("HcFreqReason", typeof(ushort)),
                KeyValuePair.Create("AdjMetric", typeof(bool))
            }
        };
    }
}
