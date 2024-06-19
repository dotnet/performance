using Microsoft.Diagnostics.Tracing.AutomatedAnalysis;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace GC.Analysis.API
{
    public sealed class ThreadJoinData
    {
        public int ThreadID { get; set; }
        public double JoinEndTimeStamp { get; set; }
        public double WakeupTimeMSec { get; set; }
    }

    public sealed class JoinWakeUpInfo
    {
        public int JoinID { get; set; }
        public double RestartThreadID { get; set; }
        public double RestartStartTime { get; set; }
        public double RestartStopTime { get; set; }
        public double LastJoinTime { get; set; }
        public Dictionary<int, ThreadJoinData> ThreadIDsToWakeupTimes { get; set; } = new();
    }

    public sealed class JoinAnalysis
    {
        private readonly List<JoinWakeUpInfo> _wakeupInfo = new();

        internal sealed class IntermediateJoinData
        {
            public GcJoinType Type { get; set; }
            public GcJoinTime Time { get; set; }
            public double Timestamp { get; set; }
            public int JoinID { get; set; }
            public int ThreadID { get; set; }
        }

        // Steps:
        // 1. For a particular join:
        //    1. Extract the last join event and note the restart start and stop times
        //    2. For each other threads that aren't the last join one: 
        //    1. Compute the wake up time: Each Join End - Last Join Event's Restart start time.
        public JoinAnalysis(GCProcessData gcProcessData)
        {
            TraceLogEventSource eventSource = gcProcessData.Parent.TraceLog.Events.GetSource();
            List<IntermediateJoinData> joinEvents = new();
            eventSource.Clr.GCJoin += (GCJoinTraceData data) =>
            {
                if (data.ProcessID != gcProcessData.ProcessID)
                {
                    return;
                }

                joinEvents.Add(new IntermediateJoinData
                {
                    JoinID = data.GCID,
                    Type = data.JoinType,
                    Time = data.JoinTime,
                    Timestamp = data.TimeStampRelativeMSec,
                    ThreadID = data.ThreadID
                });
            };
            eventSource.Process();

            IEnumerable<IntermediateJoinData> lastJoins = joinEvents.Where(e => e.Type == GcJoinType.LastJoin && e.Time == GcJoinTime.Start);

            foreach (var j in lastJoins)
            {
                // Find the closest restart start and stop events corresponding to this last join for this join id. 
                IntermediateJoinData lastJoin = j;
                IntermediateJoinData firstRestartStart = joinEvents.First(e => e.Timestamp > lastJoin.Timestamp &&
                                                                          e.ThreadID == j.ThreadID &&
                                                                          e.Time == GcJoinTime.Start &&
                                                                          e.Type == GcJoinType.Restart);
                IntermediateJoinData firstRestartEnd = joinEvents.First(e => e.Timestamp > lastJoin.Timestamp &&
                                                                          e.ThreadID == j.ThreadID &&
                                                                          e.Time == GcJoinTime.End &&
                                                                          e.Type == GcJoinType.Restart);

                // Find all the other join ends from other threads for this join id.
                IEnumerable<IntermediateJoinData> otherJoins = joinEvents.Where(j => j.JoinID == lastJoin.JoinID &&
                                                                                j.ThreadID != lastJoin.ThreadID &&
                                                                                j.Time == GcJoinTime.End &&
                                                                                j.Type == GcJoinType.Join &&
                                                                                j.Timestamp > firstRestartStart.Timestamp)
                                                                         .OrderBy(o => o.Timestamp - firstRestartStart.Timestamp);

                JoinWakeUpInfo info = new JoinWakeUpInfo
                {
                    JoinID = lastJoin.JoinID,
                    RestartThreadID = firstRestartStart.ThreadID,
                    RestartStartTime = firstRestartStart.Timestamp,
                    RestartStopTime = firstRestartEnd.Timestamp,
                    LastJoinTime = lastJoin.Timestamp,
                };

                HashSet<int> remainingThreads = new HashSet<int>(gcProcessData.GCThreadIDsToHeapNumbers.Keys);

                // For all the other join ends, compute the wake up times using: (join end time - restart start time).
                foreach (var otherJoin in otherJoins)
                {
                    if (remainingThreads.Count == 0)
                    {
                        break;
                    }

                    if (remainingThreads.Contains(otherJoin.ThreadID))
                    {
                        info.ThreadIDsToWakeupTimes[otherJoin.ThreadID] = new()
                        {
                            ThreadID = otherJoin.ThreadID,
                            JoinEndTimeStamp = otherJoin.Timestamp,
                            WakeupTimeMSec = otherJoin.Timestamp - firstRestartStart.Timestamp
                        };

                        remainingThreads.Remove(otherJoin.ThreadID);
                    }
                }

                _wakeupInfo.Add(info);
            }
        }

        public IReadOnlyList<JoinWakeUpInfo> WakeupInfo => _wakeupInfo;
    }
}
