namespace GC.Analysis.API.DynamicEvents
{
    internal static class GCDynamicEvents
    {
        public static DynamicEventSchema SizeAdaptationSampleSchema = new DynamicEventSchema
        {
            DynamicEventName = "SizeAdaptationSample",
            Fields = new List<KeyValuePair<string, Type>>
            {
                new KeyValuePair<string, Type>("version", typeof(ushort)),
                new KeyValuePair<string, Type>("GCIndex", typeof(ulong)),
                new KeyValuePair<string, Type>("ElapsedTimeBetweenGCs", typeof(uint)),
                new KeyValuePair<string, Type>("GCPauseTime", typeof(uint)),
                new KeyValuePair<string, Type>("SOHMSLWaitTime", typeof(uint)),
                new KeyValuePair<string, Type>("UOHMSLWaitTime", typeof(uint)),
                new KeyValuePair<string, Type>("TotalSOHStableSize", typeof(ulong)),
                new KeyValuePair<string, Type>("Gen0BudgetPerHeap", typeof(uint)),
            },
            MinOccurrence = 0
        };

        public static DynamicEventSchema SizeAdaptationTuningSchema = new DynamicEventSchema
        {
            DynamicEventName = "SizeAdaptationTuning",
            Fields = new List<KeyValuePair<string, Type>>
            {
                new KeyValuePair<string, Type>("version", typeof(ushort)),
                new KeyValuePair<string, Type>("NewNHeaps", typeof(ushort)),
                new KeyValuePair<string, Type>("MaxHeapCountDatas", typeof(ushort)),
                new KeyValuePair<string, Type>("MinHeapCountDatas", typeof(ushort)),
                new KeyValuePair<string, Type>("CurrentGCIndex", typeof(ulong)),
                new KeyValuePair<string, Type>("TotalSOHStableSize", typeof(ulong)),
                new KeyValuePair<string, Type>("MedianThroughputCostPercent", typeof(float)),
                new KeyValuePair<string, Type>("TcpToConsider", typeof(float)),
                new KeyValuePair<string, Type>("CurrentAroundTargetAccumulation", typeof(float)),
                new KeyValuePair<string, Type>("RecordedTcpCount", typeof(ushort)),
                new KeyValuePair<string, Type>("RecordedTcpSlope", typeof(float)),
                new KeyValuePair<string, Type>("NumGcsSinceLastChange", typeof(uint)),
                new KeyValuePair<string, Type>("AggFactor", typeof(bool)),
                new KeyValuePair<string, Type>("ChangeDecision", typeof(ushort)),
                new KeyValuePair<string, Type>("AdjReason", typeof(ushort)),
                new KeyValuePair<string, Type>("HcChangeFreqFactor", typeof(ushort)),
                new KeyValuePair<string, Type>("HcFreqReason", typeof(ushort)),
                new KeyValuePair<string, Type>("AdjMetric", typeof(bool))
            },
            MinOccurrence = 0
        };
    }
}
