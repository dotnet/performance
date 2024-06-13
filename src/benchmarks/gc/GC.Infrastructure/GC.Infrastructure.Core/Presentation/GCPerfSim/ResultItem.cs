using GC.Analysis.API;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace GC.Infrastructure.Core.Presentation.GCPerfSim
{
    public sealed class ResultItem
    {
        public static ResultItem GetNullItem(string runName, string corerun) =>
            new ResultItem(runName, corerun);

        private ResultItem(string runName, string corerun) 
        {
            ConfigurationName                         = corerun;
            RunName                                   = runName;
            PctTimePausedInGC                         = double.NaN;
            FirstToLastGCSeconds                      = double.NaN;
            HeapSizeBeforeMB_Mean                     = double.NaN;
            HeapSizeAfter_Mean                        = double.NaN;
            PauseDurationMSec_95PWhereIsGen0          = double.NaN;
            PauseDurationMSec_95PWhereIsGen1          = double.NaN;
            PauseDurationMSec_95PWhereIsBackground    = double.NaN;
            PauseDurationMSec_MeanWhereIsBackground   = double.NaN;
            PauseDurationMSec_95PWhereIsBlockingGen2  = double.NaN;
            PauseDurationMSec_MeanWhereIsBlockingGen2 = double.NaN;
            CountIsBlockingGen2                       = double.NaN;
            PauseDurationSeconds_SumWhereIsGen1       = double.NaN;
            PauseDurationMSec_MeanWhereIsEphemeral    = double.NaN;
            PromotedMB_MeanWhereIsGen1                = double.NaN;
            CountIsGen1                               = double.NaN;
            CountIsGen0                               = double.NaN;
            HeapCount                                 = double.NaN;
            PauseDurationMSec_Sum                     = double.NaN;
            TotalAllocatedMB                          = double.NaN; 
            TotalNumberGCs                            = double.NaN;
            Speed_MBPerMSec                           = double.NaN;
            ExecutionTimeMSec                         = double.NaN;
        }

        public ResultItem(GCProcessData processData, string runName, string configurationName)
        {
            RunName = runName;
            ConfigurationName = configurationName;
            ExecutionTimeMSec = processData.DurationMSec; 

            PctTimePausedInGC = processData.Stats.GetGCPauseTimePercentage();
            FirstToLastGCSeconds = ( processData.GCs.Last().StartRelativeMSec - processData.GCs.First().StartRelativeMSec ) / 1000;
            HeapSizeAfter_Mean = processData.GCs.EagerAverage(gc => gc.HeapSizeAfterMB);
            HeapSizeBeforeMB_Mean = processData.GCs.EagerAverage(gc => gc.HeapSizeBeforeMB);

            var properties = processData.Stats.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var property in properties) 
            { 
                if (property.PropertyType != typeof(double) || property.PropertyType != typeof(int))
                {
                    continue;
                }

                string propertyName = property.Name;
                double propertyValue = (double)(property.GetValue(processData.Stats) ?? double.NaN);
                StatsData[propertyName] = propertyValue;
            }

            var fields = processData.Stats.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields) 
            { 
                if (field.FieldType != typeof(double) || field.FieldType!= typeof(int))
                {
                    continue;
                }

                string name = field.Name;
                double value = (double)(field.GetValue(processData.Stats) ?? double.NaN);
                StatsData[name] = value;
            }

            // 95P
            PauseDurationMSec_95PWhereIsGen0 = processData.GCs.EagerWhere(gc => gc.Generation == 0).EagerSelect(gc => gc.PauseDurationMSec).Percentile(0.95);
            PauseDurationMSec_95PWhereIsGen1 = processData.GCs.EagerWhere(gc => gc.Generation == 1).EagerSelect(gc => gc.PauseDurationMSec).Percentile(0.95);

            PauseDurationMSec_95PWhereIsBackground = processData.GCs.EagerWhere(gc => gc.Type == GCType.BackgroundGC).EagerSelect(gc => gc.PauseDurationMSec).Percentile(0.95);
            PauseDurationMSec_MeanWhereIsBackground = processData.GCs.EagerWhere(gc => gc.Type == GCType.BackgroundGC).EagerAverage(gc => gc.PauseDurationMSec);

            PauseDurationMSec_95PWhereIsBlockingGen2 = processData.GCs.EagerWhere(gc => gc.Type != GCType.BackgroundGC && gc.Generation == 2).EagerSelect(gc => gc.PauseDurationMSec).Percentile(0.95);
            PauseDurationMSec_MeanWhereIsBlockingGen2 = processData.GCs.EagerWhere(gc => gc.Type != GCType.BackgroundGC && gc.Generation == 2).EagerAverage(gc => gc.PauseDurationMSec);

            CountIsBlockingGen2 = processData.GCs.Count(gc => gc.Generation == 2 && gc.Type != GCType.BackgroundGC);

            HeapCount = processData.Stats.HeapCount;
            TotalNumberGCs = processData.Stats.Count;
            TotalAllocatedMB = processData.Stats.TotalAllocatedMB;

            Speed_MBPerMSec = processData.Stats.TotalPromotedMB / processData.Stats.TotalPauseTimeMSec;

            PauseDurationMSec_MeanWhereIsEphemeral =
                processData.GCs.EagerWhere(gc => gc.Generation == 1 || gc.Generation == 0).EagerAverage(gc => gc.PauseDurationMSec);
            PauseDurationSeconds_SumWhereIsGen1 =
                processData.GCs.EagerWhere(gc => gc.Generation == 1).EagerSum(gc => gc.PauseDurationMSec);
            PauseDurationMSec_Sum = processData.GCs.EagerSum(gc => gc.PauseDurationMSec); 
            CountIsGen1 = processData.GCs.EagerWhere(gc => gc.Generation == 1).Count;
            CountIsGen0 = processData.GCs.EagerWhere(gc => gc.Generation == 0).Count;
        }

        public double PctTimePausedInGC                       { get; }
        public double FirstToLastGCSeconds                    { get; }
        public double HeapSizeBeforeMB_Mean                   { get; }
        public double HeapSizeAfter_Mean                      { get; }
        public double PauseDurationMSec_95PWhereIsGen0        { get; }
        public double PauseDurationMSec_95PWhereIsGen1         { get; }
        public double PauseDurationMSec_95PWhereIsBackground   { get; }
        public double PauseDurationMSec_MeanWhereIsBackground  { get; }
        public double PauseDurationMSec_95PWhereIsBlockingGen2 { get; }
        public double PauseDurationMSec_MeanWhereIsBlockingGen2 { get; }
        public double CountIsBlockingGen2 { get; }
        public double PauseDurationSeconds_SumWhereIsGen1     { get; }
        public double PauseDurationMSec_MeanWhereIsEphemeral  { get; }
        public double PromotedMB_MeanWhereIsGen1              { get; }
        public double CountIsGen1  { get; }
        public double CountIsGen0  { get; }
        public double HeapCount { get; }
        public double PauseDurationMSec_Sum  { get; }
        public double TotalAllocatedMB { get; set; }
        public double TotalNumberGCs { get; }
        public double Speed_MBPerMSec { get; }
        public string RunName { get; }
        public string ConfigurationName { get; }
        public double ExecutionTimeMSec { get; }
        public Dictionary<string, double> StatsData { get; } = new();
    }
}
