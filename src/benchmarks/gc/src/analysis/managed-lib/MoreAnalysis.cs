// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// In order to enable this code, you must be using an in-the-works version of TraceEvent, not available on nuget.

// #define NEW_JOIN_ANALYSIS // Also must do this in Analysis.cs
#if NEW_JOIN_ANALYSIS

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Analysis.GC;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace GCPerf
{
    using GCNumber = UInt32;
    using HeapID = UInt32;
    using Priority = Int32;
    using ThreadID = Int32;

    [Obsolete]
    public readonly struct WorstJoinInstance
    {
        public readonly GCNumber GCNumber;
        public readonly HeapID HeapID;
        public readonly JoinStageInfo Join;

        public WorstJoinInstance(
            GCNumber gcNumber,
            HeapID heapNumber,
            JoinStageInfo join)
        {
            GCNumber = gcNumber;
            HeapID = heapNumber;
            Join = join;
        }

        public double StartTimeMSec =>
            Join.StartMSec;

        public double DurationMSec =>
            Join.DurationMSec;

        public TimeSpan TimeSpan =>
            Join.TimeSpan;
    }

    [Obsolete]
    public readonly struct StolenTimeInstanceWithGcNumber
    {
        public readonly GCNumber GCNumber;
        public readonly StolenTimeInstance Instance;

        public StolenTimeInstanceWithGcNumber(GCNumber gcNumber, StolenTimeInstance instance)
        {
            GCNumber = gcNumber;
            Instance = instance;
        }
    }

    [Obsolete]
    public readonly struct StolenTimeInstance
    {
        public readonly HeapID HeapID;
        public readonly Priority OldPriority;
        // WARN: Don't use Span.ProcessName, that may be empty if we didn't have the process name info yet at the time of the event.
        public readonly GcWorkSpan Span;
        public readonly ServerGCState State;
        public readonly GCJoinStage Stage;

        public StolenTimeInstance(
            uint heapId,
            Priority oldPriority,
            GcWorkSpan span,
            ServerGCState state,
            GCJoinStage stage
        ) {
            HeapID = heapId;
            OldPriority = oldPriority;
            Span = span;
            State = state;
            Stage = stage;
            Util.Assert(OldThreadID != Util.THREAD_ID_IDLE && NewThreadID != Util.THREAD_ID_IDLE, "Can't steal from or be stolen from by Idle!");
        }

        // TID of a GC thread.
        public ThreadID? OldThreadID =>
            // May be null, because a CPU sample doesn't come with OldThreadID
            Span.OldThreadId;
        // TID that is stealing from this GC thread.
        public ThreadID NewThreadID =>
            Span.ThreadId;
        public int NewPriority =>
            Span.Priority;
        public int Processor =>
            Span.ProcessorNumber;

        public TimeSpan TimeSpan => TimeSpan.FromStartLengthMSec(StartTimeMSec, DurationMSec);
        public double DurationMSec => Span.DurationMsc;
        public double StartTimeMSec => Span.AbsoluteTimestampMsc;


        public GCJoinPhase Phase =>
            GcJoinPhaseUtil.GetGcJoinPhaseFromStage(Stage);

        public override string ToString() =>
            $"StolenTimeInstance(HeapId: {HeapID}, Span: {TimeSpan.FromStartLengthMSec(StartTimeMSec, DurationMSec)}, State: {State}, Stage: {Stage})";
    }

    [Obsolete]
    internal readonly struct PendingStolenTimeInstance
    {
        public readonly Priority OldPriority;
        public readonly GcWorkSpan Span;
        public readonly ServerGCState State;
        public PendingStolenTimeInstance(Priority oldPriority, GcWorkSpan span, ServerGCState state)
        {
            OldPriority = oldPriority;
            Span = span;
            State = state;

            Util.Assert(Span.OldThreadId != Util.THREAD_ID_IDLE, "Can't steal from Idle!");
            Util.Assert(Span.ThreadId != Util.THREAD_ID_IDLE, "Time can't be stolen by Idle!");
        }
    }

    [Obsolete]
    public readonly struct JoinInfoForProcess
    {
        public readonly IReadOnlyList<Result<string, JoinInfoForGC>> GCs;
        public readonly IReadOnlyList<StolenTimeInstanceWithGcNumber> WorstStolenTimeInstances;
        public readonly IReadOnlyList<WorstJoinInstance> WorstForegroundJoins;
        public readonly IReadOnlyDictionary<ThreadID, double> ThreadIDToTotalStolenMSec;

        public JoinInfoForProcess(
            IReadOnlyList<Result<string, JoinInfoForGC>> gcs,
            IReadOnlyList<StolenTimeInstanceWithGcNumber> worstStolenTimeInstances,
            IReadOnlyList<WorstJoinInstance> worstForegroundJoins)
        {
            GCs = gcs;
            WorstStolenTimeInstances = worstStolenTimeInstances;
            WorstForegroundJoins = worstForegroundJoins;

            ThreadIDToTotalStolenMSec = Util.SumDictionaries<ThreadID>(
                from gc in gcs
                from dict in gc.IsOK
                    ? Util.Single<IReadOnlyDictionary<ThreadID, double>>(gc.AsOK.ThreadIDToTotalStolenMSec)
                    : Enumerable.Empty<IReadOnlyDictionary<ThreadID, double>>()
                select dict);
        }
    }

    [Obsolete] // allow us to use experimental PerfView features
    public readonly struct JoinInfoForGC
    {
        public readonly TraceGC GC;
        public readonly IReadOnlyList<JoinInfoForHeap> Heaps;
        public readonly IReadOnlyList<StolenTimeInstance> WorstStolenTimeInstances;
        public readonly IReadOnlyList<WorstJoinInstance> WorstForegroundJoins;
        public readonly IReadOnlyDictionary<ThreadID, double> ThreadIDToTotalStolenMSec;
        public readonly Strictness Strict;

        public JoinInfoForGC(
            TraceGC gc,
            IReadOnlyList<JoinInfoForHeap> heaps,
            Strictness strict)
        {
            GC = gc;
            Heaps = heaps;
            Strict = strict;

            IReadOnlyList<GCJoinStage> foregroundStages = heaps[0].ForegroundGCJoinStages;
            IReadOnlyList<GCJoinStage> backgroundStages = heaps[0].BackgroundGCJoinStages;
            IReadOnlyList<GCJoinPhase> foregroundPhases = heaps[0].ForegroundGCJoinPhases;

            // We should ensure they match in CombineHeaps
            if (!Heaps.All(h => h.ForegroundGCJoinStages.SequenceEqual(foregroundStages))
                || !Heaps.All(h => h.BackgroundGCJoinStages.SequenceEqual(backgroundStages)))
            {
                for (int i = 0; i < Heaps.Count; i++)
                {
                    var h = Heaps[i];
                    Console.WriteLine($"h{i}:\n\tforeground: {string.Join(", ", h.ForegroundGCJoinStages)}\n\tbackground: {string.Join(", ", h.BackgroundGCJoinStages)}");
                }
                Util.Assert(false, "Stages don't match");
            }

            Util.Assert(Heaps.All(h => h.ForegroundGCJoinPhases.SequenceEqual(foregroundPhases)), "phases don't match");
            WorstStolenTimeInstances = LeastNUtil.GetLongestStolenTimeInstances(from h in Heaps from instance in h.WorstStolenTimeInstances select instance);
            WorstForegroundJoins = LeastNUtil.GetWorstJoins(from h in Heaps from instance in h.GetWorstForegroundJoins() select instance);
            ThreadIDToTotalStolenMSec = Util.SumDictionaries<ThreadID>(from h in Heaps select h.ThreadIDToTotalStolenMSec);
        }

        public static JoinInfoForGC Empty(TraceGC gc)
        {
            IReadOnlyList<JoinInfoForHeap> heaps = Util.Repeat((uint) gc.HeapCount, JoinInfoForHeap.Empty(gc.StartRelativeMSec));
            return new JoinInfoForGC(gc, heaps, Strictness.strict);
        }

        // Guaranteed to be the same for all heaps.
        public IReadOnlyList<GCJoinStage> ForegroundGCJoinStages =>
            Heaps[0].ForegroundGCJoinStages;
        public IReadOnlyList<GCJoinStage> BackgroundGCJoinStages =>
            Heaps[0].BackgroundGCJoinStages;
        public IReadOnlyList<GCJoinPhase> ForegroundGCJoinPhases =>
            Heaps[0].ForegroundGCJoinPhases;
        public IReadOnlyList<PhaseAndStages> ForegroundStagesByPhase() =>
            Heaps[0].ForegroundStagesByPhase();

        public bool IsEESuspendedForForegroundStage(uint stageIndex)
        {
            bool res = Heaps[0].ForegroundStages[(int)stageIndex].IsEESuspended;
            if (Strict == Strictness.strict)
            {
                foreach (JoinInfoForHeap hp in Heaps)
                {
                    Util.Assert(hp.ForegroundStages[(int)stageIndex].IsEESuspended == res);
                }
            }
            return res;
        }

        public bool IsEESuspendedForForegroundPhase(uint phaseIndex)
        {
            bool res = Heaps[0].ForegroundPhases[(int)phaseIndex].IsEESuspended;
            if (Strict == Strictness.strict)
            {
                foreach (JoinInfoForHeap hp in Heaps)
                {
                    Util.Assert(hp.ForegroundPhases[(int)phaseIndex].IsEESuspended == res);
                }
            }
            return res;
        }

        public bool IsEESuspendedForBackgroundStage(uint stageIndex)
        {
            bool res = Heaps[0].BackgroundStages[(int)stageIndex].IsEESuspended;
            if (Strict == Strictness.strict)
            {
                foreach (JoinInfoForHeap hp in Heaps)
                {
                    Util.Assert(hp.BackgroundStages[(int)stageIndex].IsEESuspended == res);
                }
            }
            return res;
        }

        public TimeSpan TimeSpanForForegroundStage(uint stageIndex) =>
            TimeSpan.Union(from hp in Heaps select hp.ForegroundStages[(int)stageIndex].TimeSpan);

        public TimeSpan TimeSpanForForegroundPhase(uint phaseIndex) =>
            TimeSpan.Union(from hp in Heaps select hp.ForegroundPhases[(int)phaseIndex].TimeSpan);

        public TimeSpan TimeSpanForBackgroundStage(uint stageIndex) =>
            TimeSpan.Union(from hp in Heaps select hp.BackgroundStages[(int)stageIndex].TimeSpan);

        public TimeSpan TimeSpanForAllBackgroundStages()
        {
            var that = this;
            return TimeSpan.Union(from i in Util.Range((uint)BackgroundGCJoinStages.Count) select that.TimeSpanForBackgroundStage(i));
        }
    }

    [Obsolete]
    public struct PhaseAndStages
    {
        public readonly GCJoinPhase Phase;
        public readonly IReadOnlyList<GCJoinStage> Stages;

        public PhaseAndStages(GCJoinPhase phase, IReadOnlyList<GCJoinStage> stages)
        {
            Phase = phase;
            Stages = stages;
        }
    }

    [Obsolete] // allow us to use experimental PerfView features
    internal class TempJoinInfoForHeap
    {
        public readonly GCNumber GCNumber;
        public readonly HeapID HeapID;
        public readonly ThreadID ForegroundThreadId;
        public readonly ThreadID? BackgroundThreadId;
        public int ForegroundNExtraRestarts;
        public readonly bool ForegroundFinalRestartDropped;
        public readonly TimeSpan GcTimeSpan;
        public readonly List<JoinStageInfo> ForegroundStages; // Intentionally mutable so we can FixUpStages
        public readonly List<JoinStageInfo> BackgroundStages;
        public readonly IReadOnlyDictionary<ThreadID, double> ThreadIDToTotalStolenMSec;
        public readonly IReadOnlyList<StolenTimeInstance> WorstStolenTimeInstances;

        public TempJoinInfoForHeap(
            GCNumber gcNumber,
            HeapID heapID,
            ThreadID foregroundThreadId,
            ThreadID? backgroundThreadId,
            int foregroundNExtraRestarts,
            bool foregroundFinalRestartDropped,
            TimeSpan gcTimeSpan,
            List<JoinStageInfo> foregroundStages,
            List<JoinStageInfo> backgroundStages,
            IReadOnlyDictionary<ThreadID, double> threadIDToTotalStolenMSec,
            IReadOnlyList<StolenTimeInstance> worstStolenTimeInstances
        ) {
            GCNumber = gcNumber;
            HeapID = heapID;
            ForegroundThreadId = foregroundThreadId;
            BackgroundThreadId = backgroundThreadId;
            GcTimeSpan = gcTimeSpan;
            ForegroundNExtraRestarts = foregroundNExtraRestarts;
            ForegroundFinalRestartDropped = foregroundFinalRestartDropped;
            ForegroundStages = foregroundStages;
            BackgroundStages = backgroundStages;
            ThreadIDToTotalStolenMSec = threadIDToTotalStolenMSec;
            WorstStolenTimeInstances = worstStolenTimeInstances;
        }

        public JoinInfoForHeap Finish(Strictness strict)
        {
            Util.Assert(
                strict == Strictness.loose || ForegroundNExtraRestarts == 0,
                $"Heap {HeapID} had {ForegroundNExtraRestarts} extra restarts");
            return new JoinInfoForHeap(
                gcNumber: GCNumber,
                heapID: HeapID,
                foregroundThreadId: ForegroundThreadId,
                backgroundThreadId: BackgroundThreadId,
                gcTimeSpan: GcTimeSpan,
                foregroundStages: ForegroundStages,
                backgroundStages: BackgroundStages,
                threadIDToTotalStolenMSec: ThreadIDToTotalStolenMSec,
                worstStolenTimeInstances: WorstStolenTimeInstances,
                strict: strict);
        }
    }

    [Obsolete] // allow us to use experimental PerfView features
    public readonly struct JoinInfoForHeap
    {
        public readonly GCNumber GCNumber;
        public readonly HeapID HeapID;
        public readonly ThreadID ForegroundThreadID;
        public readonly ThreadID? BackgroundThreadID;
        public readonly IReadOnlyList<JoinStageInfo> ForegroundStages;
        public readonly IReadOnlyList<JoinStageInfo> BackgroundStages;
        public readonly IReadOnlyList<JoinPhaseInfo> ForegroundPhases;
        // Overall, includes both foreground and background stages
        public readonly IReadOnlyList<double> TotalMSecPerState;
        public readonly IReadOnlyDictionary<ThreadID, double> ThreadIDToTotalStolenMSec;
        public readonly IReadOnlyList<StolenTimeInstance> WorstStolenTimeInstances;

        public JoinInfoForHeap(
            GCNumber gcNumber,
            HeapID heapID,
            ThreadID foregroundThreadId,
            ThreadID? backgroundThreadId,
            TimeSpan gcTimeSpan,
            IReadOnlyList<JoinStageInfo> foregroundStages,
            IReadOnlyList<JoinStageInfo> backgroundStages,
            IReadOnlyDictionary<ThreadID, double> threadIDToTotalStolenMSec,
            IReadOnlyList<StolenTimeInstance> worstStolenTimeInstances,
            Strictness strict
        ) {
            GCNumber = gcNumber;
            HeapID = heapID;
            ForegroundThreadID = foregroundThreadId;
            BackgroundThreadID = backgroundThreadId;
            Debug.Assert(ForegroundThreadID != BackgroundThreadID);
            ForegroundStages = foregroundStages;
            BackgroundStages = backgroundStages;
            ForegroundPhases = CalculatePhases(foregroundStages, strict);
            ThreadIDToTotalStolenMSec = threadIDToTotalStolenMSec;
            WorstStolenTimeInstances = worstStolenTimeInstances;

            double foregroundStagesSum = ForegroundStages.Sum(s => s.DurationMSec);
            double foregroundPhasesSum = ForegroundPhases.Sum(s => s.DurationMSec);
            double backgroundStagesSum = BackgroundStages.Sum(s => s.DurationMSec);
            double allStagesSum = foregroundStagesSum + backgroundStagesSum;

            if (strict == Strictness.strict)
            {
                // Some events come out slightly before GC/Start or slightly after GC/End, so stages sum may be longer. Add a 20ms tolerance.
                // Also, foreground and background stages may have slight overlap.
                Util.AssertAboutEquals(gcTimeSpan.DurationMSec, allStagesSum, description: "stages", tolerance: 20);
                Util.AssertAboutEquals(foregroundStagesSum, foregroundPhasesSum, description: "phases");
            }

            CheckStages(ForegroundStages, strict);
            CheckStages(BackgroundStages, strict);

            double[] msecPerState = new double[(int)ServerGCState.count];
            foreach (JoinPhaseInfo p in ForegroundPhases)
            {
                Util.AddToArray(msecPerState, p.MSecPerState);
            }
            foreach (JoinStageInfo s in BackgroundStages)
            {
                Util.AddToArray(msecPerState, s.MSecPerState);
            }
            TotalMSecPerState = msecPerState;
            Util.AssertAboutEquals(allStagesSum, TotalMSecPerState.Sum(), description: "states");
        }

        public IReadOnlyList<WorstJoinInstance> GetWorstForegroundJoins()
        {
            JoinInfoForHeap that = this;
            return LeastNUtil.GetWorstJoins(from j in ForegroundStages select new WorstJoinInstance(that.GCNumber, that.HeapID, j));
        }

        private static void CheckStages(IReadOnlyList<JoinStageInfo> stages, Strictness strict)
        {
            for (int i = 1; i < stages.Count; i++)
            {
                JoinStageInfo prev = stages[i - 1];
                JoinStageInfo cur = stages[i];
                // TODO: Is it right that scan_dependent_handles can occur multiple times?
                if (prev.JoinStage != GCJoinStage.scan_dependent_handles)
                {
                    Util.Assert(prev.JoinStage != cur.JoinStage, $"Repeated stage {stages[i].JoinStage}");
                    // TODO: shouldn't this always be the case?
                    Util.Assert(strict == Strictness.loose || prev.TimeSpan.EndMSec == cur.TimeSpan.StartMSec, "Stages must line up");
                }
            }
        }

        public static JoinInfoForHeap Empty(double startMSec) => new JoinInfoForHeap(
            gcNumber: 0,
            heapID: 0,
            foregroundThreadId: 0,
            backgroundThreadId: 1,
            gcTimeSpan: TimeSpan.Empty(startMSec),
            foregroundStages: Util.EmptyReadOnlyList<JoinStageInfo>(),
            backgroundStages: Util.EmptyReadOnlyList<JoinStageInfo>(),
            threadIDToTotalStolenMSec: Util.EmptyReadOnlyDictionary<ThreadID, double>(),
            worstStolenTimeInstances: Util.EmptyReadOnlyList<StolenTimeInstance>(),
            strict: Strictness.strict);

        // Guaranteed to be the same for every heap.
        public IReadOnlyList<GCJoinStage> ForegroundGCJoinStages =>
            (from s in ForegroundStages select s.JoinStage).ToArray();

        public IReadOnlyList<GCJoinStage> BackgroundGCJoinStages =>
            (from s in BackgroundStages select s.JoinStage).ToArray();

        public IReadOnlyList<GCJoinPhase> ForegroundGCJoinPhases =>
            (from p in ForegroundPhases select p.JoinPhase).ToArray();

        public IReadOnlyList<PhaseAndStages> ForegroundStagesByPhase()
        {
            List<PhaseAndStages> res = new List<PhaseAndStages>();
            GCJoinPhase curPhase = GCJoinPhase.init;
            List<GCJoinStage> cur = new List<GCJoinStage>();
            void FinishPhase()
            {
                // Must always have 'init' phase
                if (!Util.IsEmpty(cur) || curPhase == GCJoinPhase.init)
                {
                    res.Add(new PhaseAndStages(curPhase, cur));
                }
            }
            void FinishPhaseAndStartNew(GCJoinPhase newPhase)
            {
                FinishPhase();
                curPhase = newPhase;
                cur = new List<GCJoinStage>();
            }
            foreach (JoinStageInfo stageInfo in ForegroundStages)
            {
                GCJoinStage stage = stageInfo.JoinStage;
                GCJoinPhase phase = GcJoinPhaseUtil.GetGcJoinPhaseFromStage(stage);
                if (phase != curPhase)
                {
                    FinishPhaseAndStartNew(newPhase: phase);
                }
                cur.Add(stage);
            }
            FinishPhase();
            Util.AssertEqualList((from r in res select r.Phase).ToList(), ForegroundGCJoinPhases, (a, b) => a == b);
            return res;
        }

        private static IReadOnlyList<JoinPhaseInfo> CalculatePhases(IReadOnlyList<JoinStageInfo> stages, Strictness strict)
        {
            if (stages.Any())
            {
                List<JoinPhaseInfo> res = new List<JoinPhaseInfo>();
                MutJoinPhaseInfo curPhase = new MutJoinPhaseInfo(GCJoinPhase.init, stages[0].StartMSec, stages[0].IsEESuspended);

                foreach (JoinStageInfo s in stages)
                {
                    double stageStart = s.StartMSec;
                    GCJoinPhase itsPhase = GcJoinPhaseUtil.GetGcJoinPhaseFromStage(s.JoinStage);

                    if (itsPhase == curPhase.Phase)
                    {
                        Util.Assert(s.IsEESuspended == curPhase.IsEESuspended, $"IsEESuspended should match. s.IsEESuspended: {s.IsEESuspended}, curPhase.IsEESuspended: {curPhase.IsEESuspended}");
                    }
                    else
                    {
                        Util.Assert(strict == Strictness.loose || itsPhase > curPhase.Phase, "phases must proceed in order");
                        res.Add(curPhase.Finish(stageStart, strict: strict));
                        curPhase = new MutJoinPhaseInfo(itsPhase, stageStart, s.IsEESuspended);
                    }

                    curPhase.Add(s.MSecPerState);
                }

                res.Add(curPhase.Finish(stages.Last().EndMSec, strict: strict));
                return res;
            }
            else
            {
                return Util.EmptyReadOnlyList<JoinPhaseInfo>();
            }
        }

        public double TotalMSecInState(ServerGCState state)
        {
            Util.Assert(0 <= state && state < ServerGCState.count, "invalid state");
            return TotalMSecPerState[(int)state];
        }

        public double TotalMSecInForegroundPhase(GCJoinPhase phase)
        {
            Util.Assert(0 <= phase && phase < GCJoinPhase.count, "invalid phase");
            foreach (JoinPhaseInfo p in ForegroundPhases)
            {
                if (p.JoinPhase == phase)
                {
                    return p.DurationMSec;
                }
            }
            return 0;
        }

        public double TotalMSecInStage(GCJoinStage stage)
        {
            Util.Assert(0 <= stage && stage < GCJoinStage.count, "invalid stage");
            JoinStageInfo? i = ForegroundStages.Cast<JoinStageInfo?>().FirstOrDefault(i => i?.JoinStage == stage)
                ?? BackgroundStages.Cast<JoinStageInfo?>().FirstOrDefault(i => i?.JoinStage == stage);
            return i != null ? i.Value.DurationMSec : 0;
        }
    }

    public enum ServerGCState
    {
        working,
        singleThreaded,
        // For the last joined thread, this is how long it took between restart start and end.
        // For other threads, this is when restart start is fired and when this join actually
        // ended. This usually should be really short and interference is also important.
        restarting,
        waitingInJoin,
        stolen,
        idleForNoGoodReason, // TODO: Kill?
        count
    }

    public readonly struct JoinStageOrPhaseInfo
    {
        public readonly TimeSpan TimeSpan;
        public readonly bool IsEESuspended;
        // index is a ServerGCState
        public readonly IReadOnlyList<double> MSecPerState;

        public JoinStageOrPhaseInfo(
            TimeSpan timeSpan,
            bool isEESuspended,
            IReadOnlyList<double> mSecPerState,
            Strictness strict
        ) {
            TimeSpan = timeSpan;
            IsEESuspended = isEESuspended;
            MSecPerState = mSecPerState;

            foreach (double d in MSecPerState)
            {
                Util.Assert(d >= 0, "Time can't be negative");
            }

            if (strict == Strictness.strict)
            {
                Util.AssertAboutEquals(MSecPerState.Sum(), TimeSpan.DurationMSec, description: "MSecPerState should add to total time");
            }
        }

        public JoinStageOrPhaseInfo MergeWith(JoinStageOrPhaseInfo other, Strictness strict)
        {
            Util.Assert(IsEESuspended == other.IsEESuspended);
            return new JoinStageOrPhaseInfo(
                timeSpan: TimeSpan.MergeWith(other.TimeSpan),
                isEESuspended: IsEESuspended,
                mSecPerState: MSecPerState.Zip(other.MSecPerState, (x, y) => x + y).ToArray(),
                strict: strict);
        }

        public double StartMSec => TimeSpan.StartMSec;
        public double EndMSec => TimeSpan.EndMSec;
        public double DurationMSec => TimeSpan.DurationMSec;
    }

    [Obsolete]
    public readonly struct JoinStageInfo
    {
        public readonly GCJoinStage JoinStage;
        public readonly JoinStageOrPhaseInfo Info;

        public JoinStageInfo(GCJoinStage joinStage, TimeSpan timeSpan, bool isEESuspended, IReadOnlyList<double> mSecPerState, Strictness strict)
            : this(joinStage, new JoinStageOrPhaseInfo(timeSpan, isEESuspended, mSecPerState, strict: strict)) { }

        public JoinStageInfo(GCJoinStage joinStage, JoinStageOrPhaseInfo info)
        {
            JoinStage = joinStage;
            Info = info;
        }

        public TimeSpan TimeSpan => Info.TimeSpan;
        public bool IsEESuspended => Info.IsEESuspended;
        public double StartMSec => Info.StartMSec;
        public double EndMSec => Info.EndMSec;
        public IReadOnlyList<double> MSecPerState => Info.MSecPerState;
        public double DurationMSec => Info.DurationMSec;

        public JoinStageInfo MergeWith(JoinStageInfo other, Strictness strict)
        {
            Util.Assert(JoinStage == other.JoinStage, $"Must be same stage. Prev: {JoinStage}, next: {other.JoinStage}");
            return new JoinStageInfo(JoinStage, Info.MergeWith(other.Info, strict));
        }

        public static JoinStageInfo Empty(GCJoinStage s, double timeMSec, bool isEESuspended) =>
            new JoinStageInfo(
                joinStage: s,
                timeSpan:  TimeSpan.Empty(timeMSec),
                isEESuspended: isEESuspended,
                mSecPerState: EmptyMSecPerState,
                strict: Strictness.strict);

        private static readonly IReadOnlyList<double> EmptyMSecPerState = new double[(int)ServerGCState.count];
    }

    public readonly struct JoinPhaseInfo
    {
        public readonly GCJoinPhase JoinPhase;
        public readonly JoinStageOrPhaseInfo Info;
        public JoinPhaseInfo(GCJoinPhase phase, TimeSpan timeSpan, bool isEESuspended, IReadOnlyList<double> mSecPerState, Strictness strict)
        {
            JoinPhase = phase;
            Info = new JoinStageOrPhaseInfo(timeSpan, isEESuspended, mSecPerState, strict: strict);
        }

        public TimeSpan TimeSpan => Info.TimeSpan;
        public bool IsEESuspended => Info.IsEESuspended;
        public double StartMSec => Info.StartMSec;
        public double EndMSec => Info.EndMSec;
        public IReadOnlyList<double> MSecPerState => Info.MSecPerState;
        public double DurationMSec => Info.DurationMSec;
    }

    internal class MutJoinPhaseInfo
    {
        public readonly GCJoinPhase Phase;
        public readonly double StartMSec;
        public readonly bool IsEESuspended;
        public readonly double[] MSecPerState;

        public MutJoinPhaseInfo(GCJoinPhase phase, double startMSec, bool isEESuspended)
        {
            Phase = phase;
            StartMSec = startMSec;
            IsEESuspended = isEESuspended;
            MSecPerState = new double[(int)ServerGCState.count];
        }

        public JoinPhaseInfo Finish(double endMSec, Strictness strict) =>
            new JoinPhaseInfo(Phase, TimeSpan.FromStartEndMSec(StartMSec, endMSec), IsEESuspended, MSecPerState, strict);

        public void Add(IReadOnlyList<double> mSecPerState)
        {
            Util.AddToArray(MSecPerState, mSecPerState);
        }
    }

    // These aggregate gc_join_stage
    public enum GCJoinPhase
    {
        init = 0,
        mark = 1,
        plan = 2,
        relocate = 3,
        compact = 4,
        sweep = 5,
        heap_verify = 6,
        post_gc = 7,
        count = 8,
    }

    [Obsolete] // allow us to use experimental PerfView features
    internal static class GCThreadKindUtil
    {
        public static GCThreadKind? Combine(GCThreadKind? a, GCThreadKind? b)
        {
            Util.Assert(!(a != null && b != null && a != b), "Should not conflict");
            return a ?? b;
        }

        public static IReadOnlyList<GCThreadKind> All = new GCThreadKind[] { GCThreadKind.Background, GCThreadKind.Foreground };
    }

    [Obsolete]
    internal static class GcJoinPhaseUtil
    {
        public static GCJoinPhase GetGcJoinPhaseFromStage(GCJoinStage stage)
        {
            try
            {
                return stageToPhase[stage];
            }
            catch (KeyNotFoundException)
            {
                throw new Exception($"We didn't choose a phase for {stage}");
            }
        }

        private static readonly IReadOnlyDictionary<GCJoinPhase, IReadOnlyList<GCJoinStage>> phaseToStage = new Dictionary<GCJoinPhase, IReadOnlyList<GCJoinStage>>
        {
            [GCJoinPhase.init] = new GCJoinStage[]
            {
                GCJoinStage.generation_determined,
            },
            [GCJoinPhase.mark] = new GCJoinStage[]
            {
                GCJoinStage.begin_mark_phase,
                GCJoinStage.scan_sizedref_done,
                GCJoinStage.r_join_update_card_bundle,
                GCJoinStage.scan_dependent_handles,
                GCJoinStage.rescan_dependent_handles,
                GCJoinStage.null_dead_short_weak,
                GCJoinStage.scan_finalization,
                GCJoinStage.null_dead_long_weak,
                GCJoinStage.null_dead_syncblk,
            },
            [GCJoinPhase.plan] = new GCJoinStage[] { GCJoinStage.decide_on_compaction },
            [GCJoinPhase.relocate] = new GCJoinStage[]
            {
                GCJoinStage.begin_relocate_phase,
                GCJoinStage.relocate_phase_done,
            },
            [GCJoinPhase.compact] = new GCJoinStage[]
            {
                GCJoinStage.rearrange_segs_compaction,
                GCJoinStage.adjust_handle_age_compact,
            },
            [GCJoinPhase.sweep] = new GCJoinStage[]
            {
                GCJoinStage.adjust_handle_age_sweep,
            },
            [GCJoinPhase.heap_verify] = new GCJoinStage[]
            {
                GCJoinStage.verify_copy_table,
                GCJoinStage.verify_objects_done,
            },
            [GCJoinPhase.post_gc] = new GCJoinStage[]
            {
                GCJoinStage.bgc_after_ephemeral,
                GCJoinStage.start_bgc,
                GCJoinStage.done,
            },
        };

        private static readonly IReadOnlyDictionary<GCJoinStage, GCJoinPhase> stageToPhase = InvertMultiMapping(phaseToStage);

        private static IReadOnlyDictionary<U, T> InvertMultiMapping<T, U>(IReadOnlyDictionary<T, IReadOnlyList<U>> d)
        {
            var res = new Dictionary<U, T>();
            foreach (KeyValuePair<T, IReadOnlyList<U>> pair in d)
            {
                foreach (U u in pair.Value)
                {
                    res.Add(u, pair.Key);
                }
            }
            return res;
        }
    }

    public readonly struct Either<T, U>
    {
        public readonly bool IsLeft;
        private readonly T left;
        private readonly U right;
        private Either(bool isLeft, T left, U right)
        {
            IsLeft = isLeft;
            this.left = left;
            this.right = right;
        }

        // Disable warning about default(T) and default(U). For some reason this is alternately CS8653 or CS8625.
#pragma warning disable CS8653, CS8625
        public static Either<T, U> Left(T left) => new Either<T, U>(true, left, default(U));
        public static Either<T, U> Right(U right) => new Either<T, U>(false, default(T), right);
#pragma warning restore CS8653, CS8625

        public bool IsRight =>
            !IsLeft;

        public T AsLeft
        {
            get
            {
                Util.Assert(IsLeft, "Invalid AsLeft");
                return left;
            }
        }

        public U AsRight
        {
            get
            {
                Util.Assert(!IsLeft, "Invalid AsRight");
                return right;
            }
        }
    }

    // Pythonnet doesn't handle c# exceptions well, so use Result
    public readonly struct Result<E, T>
    {
        private readonly Either<E, T> either;
        private Result(Either<E, T> either) =>
            this.either = either;
        
        public static Result<E, T> OK(T data) =>
            new Result<E, T>(Either<E, T>.Right(data));
        
        public static Result<E, T> Err(E data) =>
            new Result<E, T>(Either<E, T>.Left(data));
        
        public bool IsOK =>
            either.IsRight;
        
        public E AsErr =>
            either.AsLeft;
        
        public T AsOK =>
            either.AsRight;
    }

    [Obsolete] // allow us to use experimental PerfView features
    public static class JoinAnalysis
    {
        internal static bool DEBUG_PRINT(double timeMSec) =>
            false;

        public static Result<string, JoinInfoForProcess> AnalyzeAllGcs(IReadOnlyList<TraceGC> gcs, Strictness strict)
        {
            Result<string, JoinInfoForGC>[] res = new Result<string, JoinInfoForGC>[gcs.Count];
            for (uint i = 0; i < gcs.Count; i++)
            {
                TraceGC gc = gcs[(int)i];
                if (DEBUG_PRINT(gc.StartRelativeMSec))
                {
                    Console.WriteLine($" === Analyzing gc {gc.Number} ({gc.Type}) ({TimeSpan.FromStartLengthMSec(gc.StartRelativeMSec, gc.DurationMSec)}) (last: {gcs.Last().Number}) ===");
                }

                Result<string, JoinInfoForGC> result = AnalyzeSingleGc(gc, strict);
                if (!result.IsOK && result.AsErr != "No ServerGcHeapHistories")
                    return Result<string, JoinInfoForProcess>.Err(result.AsErr);
                res[i] = result;
            }
            IReadOnlyList<StolenTimeInstanceWithGcNumber> worstStolenTimeInstances = LeastNUtil.GetLongestStolenTimeInstances(
                from gc in res
                from instance in GetStolenTimeInstances(gc)
                select instance);
            IReadOnlyList<WorstJoinInstance> worstForegroundJoins = LeastNUtil.GetWorstJoins(
                from gc in res
                from instance in GetWorstForegroundJoins(gc)
                select instance);
            return Result<string, JoinInfoForProcess>.OK(new JoinInfoForProcess(
                gcs: res,
                worstStolenTimeInstances: worstStolenTimeInstances,
                worstForegroundJoins: worstForegroundJoins));
        }

        private static IEnumerable<StolenTimeInstanceWithGcNumber> GetStolenTimeInstances(Result<string, JoinInfoForGC> gc) =>
            gc.IsOK
                ? from instance in gc.AsOK.WorstStolenTimeInstances select new StolenTimeInstanceWithGcNumber((GCNumber)gc.AsOK.GC.Number, instance)
                : Enumerable.Empty<StolenTimeInstanceWithGcNumber>();

        private static IEnumerable<WorstJoinInstance> GetWorstForegroundJoins(Result<string, JoinInfoForGC> gc) =>
            gc.IsOK
                ? gc.AsOK.WorstForegroundJoins
                : Enumerable.Empty<WorstJoinInstance>();

        public static Result<string, JoinInfoForGC> AnalyzeSingleGc(TraceGC gc, Strictness strict)
        {
            try
            {
                int nHeaps = gc.ServerGcHeapHistories.Count;
                Util.Assert(gc.HeapCount == nHeaps, () =>
                    $"gc.HeapCount is {gc.HeapCount} but ServerGcHeapHistories.Count is {nHeaps}");
                if (nHeaps == 0)
                {
                    return Result<string, JoinInfoForGC>.Err("No ServerGcHeapHistories");
                }
                else
                {
                    TempJoinInfoForHeap[] res = new TempJoinInfoForHeap[nHeaps];
                    TimeSpan gcTimeSpan = TimeSpan.FromStartEndMSec(gc.PauseStartRelativeMSec, gc.PauseEndRelativeMSec);
                    for (int i = 0; i < nHeaps; i++)
                    {
                        // Note: In TraceManagedProcess in PerfView there is a line `AbsoluteTimestampMsc = sample.TimeStampRelativeMSec;`.
                        // The "Absolute" times ofthese events are still relative to the start of the session, and comparable to gc's "relative" times.
                        Result<string, TempJoinInfoForHeap> result = SingleHeapAnalyzer.AnalyzeSingleHeap(
                            (GCNumber) gc.Number,
                            gcTimeSpan,
                            gc.ServerGcHeapHistories[i],
                            strict);
                        if (result.IsOK)
                        {
                            res[i] = result.AsOK;
                        }
                        else
                        {
                            return Result<string, JoinInfoForGC>.Err(result.AsErr);
                        }
                    }
                    return Result<string, JoinInfoForGC>.OK(new JoinInfoForGC(gc, CombineHeaps(res, strict), strict));
                }
            }
            catch (Exception e)
            {
                return Result<string, JoinInfoForGC>.Err($"{e.Message}\n{e.StackTrace}");
            }
        }

        // NOTE: Newer versions of coreclr always fire rjoin events, so this code could be simplified. We shouldn't need to worry about extra restarts.
        private static IReadOnlyList<JoinInfoForHeap> CombineHeaps(IReadOnlyList<TempJoinInfoForHeap> res, Strictness strict)
        {
            // Walk heaps together -- they should have same stages.
            // Sometimes, one heap will have an rjoin and the other doesn't.

            // If not, must insert empty stages.
            // Note: the JoinInfoForHeap constructor asserts all stages match up, so we don't have to check everything in this method. Just handle the rjoins.
            int nHeaps = res.Count;

            bool someFinalRestartDropped = false;
            foreach (TempJoinInfoForHeap hp in res)
            {
                if (hp.ForegroundFinalRestartDropped)
                {
                    Util.Assert(!someFinalRestartDropped, "multiple final restart dropped"); // Shouldn't happen on multiple heaps
                    someFinalRestartDropped = true;
                }
            }
            Debug.Assert(!someFinalRestartDropped);
            /*if (someFinalRestartDropped)
            {
                foreach (TempJoinInfoForHeap hp in res)
                {
                    if (!hp.ForegroundFinalRestartDropped)
                    {
                        hp.ForegroundNExtraRestarts++;
                    }
                }
            }*/

            foreach (GCThreadKind tk in GCThreadKindUtil.All)
            {
                FixUpStages(res, tk, strict);
            }
    
            if (strict == Strictness.strict)
            {
                foreach (TempJoinInfoForHeap hp in res)
                {
                    if (hp.ForegroundNExtraRestarts != 0)
                    {
                        if (hp.ForegroundNExtraRestarts > 0)
                        {
                            throw new Exception($"Heap {hp.HeapID} has {hp.ForegroundNExtraRestarts} unexplained extra restart");
                        }
                        else
                        {
                            throw new Exception($"Heap {hp.HeapID} should have observed {-hp.ForegroundNExtraRestarts} more restarts");
                        }
                    }
                }
            }

            return (from t in res select t.Finish(strict)).ToArray();
        }

        private static List<JoinStageInfo> GetStages(TempJoinInfoForHeap hp, GCThreadKind fgOrBg)
        {
            switch (fgOrBg)
            {
                case GCThreadKind.Foreground:
                    return hp.ForegroundStages;
                case GCThreadKind.Background:
                    return Util.NonNull(hp.BackgroundStages);
                default:
                    throw new Exception();
            }
        }

        private static void FixUpStages(IReadOnlyList<TempJoinInfoForHeap> res, GCThreadKind fgOrBg, Strictness strict)
        {
            // In a loop: Choose a reference heap, add stages to other heaps so they have the reference heap's stages.
            // Keep doing this while it has an effect.
            for (int i = 0; ; i++)
            {
                if (i == 100)
                {
                    throw new Exception("FixUpStages didn't finish after 100 iterations?");
                }

                // Use the heap with the most stages -- other heaps will be considered to be missing stages.
                TempJoinInfoForHeap heapForReference = Util.MaxBy(res, hp => GetStages(hp, fgOrBg).Count);
                List<JoinStageInfo> referenceStages = GetStages(heapForReference, fgOrBg);

                bool someChanged = false;
                // Note: I don't think this fixup step is necessary for the BackgroundStages
                // (The last join isn't an rjoin, so just break at heap 0's last one)
                for (uint stageIndex = 0; stageIndex < referenceStages.Count; stageIndex++)
                {
                    GCJoinStage joinStage = referenceStages[(int) stageIndex].JoinStage;

                    bool allSame = res.All(hp => Util.TryIndex(GetStages(hp, fgOrBg), stageIndex)?.JoinStage == joinStage);
                    // Util.Assert(strict == Strictness.loose || allSame); // TODO: With my always-fire-rjoin fix they should all be the same.
                    if (!allSame)
                    {
                        for (int ii = 0; ii < res.Count; ii++)
                        {
                            GCJoinStage? itsStage = Util.TryIndex(GetStages(res[ii], fgOrBg), stageIndex)?.JoinStage;
                        }
                        someChanged = true;
                        EnsureEveryHeapHasStageAtIndex(stageIndex, joinStage, res, fgOrBg, strict);
                    }
                }

                if (!someChanged)
                {
                    break;
                }
            }
        }

        private static void PrintStages(TempJoinInfoForHeap hp, GCThreadKind fgOrBg)
        {
            Console.WriteLine(string.Join(", ", from stage in GetStages(hp, fgOrBg) select stage.JoinStage.ToString()));
        }

        private static void EnsureEveryHeapHasStageAtIndex(
            uint stageIndex,
            GCJoinStage joinStage,
            IReadOnlyList<TempJoinInfoForHeap> res,
            GCThreadKind fgOrBg,
            Strictness strict)
        {
            switch (strict)
            {
                case Strictness.loose:
                    // Fill in dummy stages.
                    // Arbitrarily assuming heap 0 has the correct stage and all others need to add it.
                    foreach (TempJoinInfoForHeap hp in res)
                    {
                        List<JoinStageInfo> stages = GetStages(hp, fgOrBg);
                        if (stageIndex < stages.Count)
                        {
                            JoinStageInfo fg = stages[(int) stageIndex];
                            if (fg.JoinStage != joinStage)
                            {
                                stages.Insert((int) stageIndex, JoinStageInfo.Empty(joinStage, fg.StartMSec, fg.IsEESuspended));
                            }
                        }
                        else
                        {
                            JoinStageInfo? lastStage = Util.OpLast(hp.ForegroundStages);
                            if (lastStage == null)
                            {
                                JoinStageInfo stageFromAnotherHeap = Util.NonNull(Util.Find(res, hp => Util.TryIndex(GetStages(hp, fgOrBg), stageIndex)));
                                stages.Add(JoinStageInfo.Empty(joinStage, stageFromAnotherHeap.StartMSec, isEESuspended: false));
                            }
                            else
                            { 
                                stages.Add(JoinStageInfo.Empty(joinStage, lastStage.Value.StartMSec, lastStage.Value.IsEESuspended));
                            }
                        }
                    }
                    break;

                case Strictness.strict:
                    // One of them should be an rjoin -- the others may be missing events for it.
                    bool someIsRJoin = res.Any(hp => GCJoinStageUtil.IsRJoinStage(hp.ForegroundStages[(int) stageIndex].JoinStage));
                    if (someIsRJoin)
                    {
                        // Add empty rjoin to the others.
                        foreach (TempJoinInfoForHeap hp in res)
                        {
                            List<JoinStageInfo> stages = hp.ForegroundStages;
                            JoinStageInfo info = stages[(int) stageIndex];
                            //TODO: use the particular rjoin stage if not r_join_update_card_bundle
                            if (info.JoinStage != GCJoinStage.r_join_update_card_bundle)
                            {
                                Util.Assert(fgOrBg == GCThreadKind.Foreground);
                                Util.Assert(hp.ForegroundNExtraRestarts > 0, $"Heap {hp.HeapID} must have an extra restart");
                                // This explains the extra restart
                                hp.ForegroundNExtraRestarts--;
                                stages.Insert((int) stageIndex, JoinStageInfo.Empty(GCJoinStage.r_join_update_card_bundle, info.StartMSec, info.IsEESuspended));
                            }
                        }
                    }
                    else
                    {
                        foreach (TempJoinInfoForHeap hp in res)
                        {
                            IEnumerable<GCJoinStage> stages = (from s in GetStages(hp, fgOrBg) select s.JoinStage).ToArray();
                            Console.WriteLine($"h{hp.HeapID}: {string.Join(", ", stages)}");
                        }
                        throw new Exception("Heaps' stages don't match, and it doesn't seem to be a missing rjoin");
                    }
                    break;

                default:
                    throw new Exception();
            }
        }
    }

    public enum Strictness
    {
        loose,
        strict
    }

    [Obsolete]
    internal class MutJoinStageInfo
    {
        public readonly double StartTime;
        public GCJoinStage? JoinStage = null;
        public bool? IsEESuspended = null;

        public double[] MSecPerState = new double[(int)ServerGCState.count];
        public MutJoinStageInfo(double startTime)
        {
            StartTime = startTime;
        }

        public JoinStageInfo Finish(double endTime, Strictness strict)
        {
            Util.Assert(strict == Strictness.loose || IsEESuspended != null);
            return new JoinStageInfo(
                Util.NonNull(JoinStage),
                TimeSpan.FromStartEndMSec(StartTime, endTime),
                IsEESuspended ?? false,
                MSecPerState,
                strict);
        }

        public override string ToString() =>
            $"MutJoinStageInfo(StartTime: {StartTime}, JoinStage: {Util.NullableToString(JoinStage)}, IsEESuspended: {Util.NullableToString(IsEESuspended)})";
    }

    [Obsolete] // allow us to use experimental PerfView features
    internal static class SingleHeapAnalyzer
    {
        internal static Result<string, TempJoinInfoForHeap> AnalyzeSingleHeap(
            GCNumber gcNumber,
            TimeSpan gcTimeSpan,
            ServerGcHistory heap,
            Strictness strict)
        {
            SanityCheckEventsOrder(heap, gcTimeSpan);

            if (JoinAnalysis.DEBUG_PRINT(gcTimeSpan.StartMSec))
            {
                Console.WriteLine($"\n=== Analyze heap {heap.HeapId} ===\n");
            }

            HeapID heapId = (HeapID)heap.HeapId;
            SingleHeapSingleThreadAnalyzer foregroundAnalyzer = new SingleHeapSingleThreadAnalyzer(
                strict: strict, isBackground: false, heapId: heapId, gcTimeSpan: gcTimeSpan, threadID: Util.NonNull(heap.GcWorkingThreadId));
            SingleHeapSingleThreadAnalyzer backgroundAnalyzer = new SingleHeapSingleThreadAnalyzer(
                strict: strict, isBackground: true, heapId: heapId, gcTimeSpan: gcTimeSpan, threadID: null /*heap.GcBackgroundThreadId*/);

            bool anyEvents = false;
            bool anyBgThreads = false;
            foreach (GCEvent ev in EventsWalker.EachEvent(gcTimeSpan, heap))
            {
                if (JoinAnalysis.DEBUG_PRINT(ev.TimeMSec))
                {
                    PrintEvent(ev);
                }

                anyEvents = true;

                switch (ev.ThreadKind)
                {
                    case GCThreadKind.Foreground:
                        foregroundAnalyzer.OnEvent(ev);
                        break;

                    case GCThreadKind.Background:
                        anyBgThreads = true;
                        backgroundAnalyzer.OnEvent(ev);
                        break;

                    case null:
                        Console.WriteLine("WARNING: Ignoring event, don't know thread kind");
                        // Only GCWorkSpan should be allowed to have missing ThreadKind.
                        Util.Assert(!ev.Kind.IsLeft);
                        // Can't do much with this without knowing the ThreadKind.
                        break;

                    default:
                        throw new Exception();
                }
            }

            if (!anyEvents && strict == Strictness.strict)
            {
                return Result<string, TempJoinInfoForHeap>.Err($"GC {gcNumber} had no join events -- did you remember to set 'collect: verbose' or higher?");
            }

            double gcEndMSec = gcTimeSpan.EndMSec;
            SingleHeapSingleThreadResult fg = foregroundAnalyzer.Finish(gcEndMSec);
            SingleHeapSingleThreadResult bg = anyBgThreads
                ? backgroundAnalyzer.Finish(gcEndMSec)
                : SingleHeapSingleThreadResult.Empty;

            if (strict == Strictness.strict && (bg.NExtraRestarts != 0 || bg.FinalRestartDropped))
            {
                throw new Exception($"bg thread should not have extra restarts or final restart dropped, but got {bg.NExtraRestarts} extra restarts and final restart dropped {bg.FinalRestartDropped}");
            }

            return Result<string, TempJoinInfoForHeap>.OK(
                new TempJoinInfoForHeap(
                    gcNumber: gcNumber,
                    heapID: (uint)heap.HeapId,
                    foregroundThreadId: Util.NonNull(foregroundAnalyzer.ThreadID),
                    backgroundThreadId: backgroundAnalyzer.ThreadID,
                    foregroundNExtraRestarts: fg.NExtraRestarts,
                    foregroundFinalRestartDropped: fg.FinalRestartDropped,
                    gcTimeSpan: gcTimeSpan,
                    foregroundStages: fg.Stages,
                    backgroundStages: bg.Stages,
                    threadIDToTotalStolenMSec: Util.SumDictionaries<ThreadID>(fg.ThreadIDToTotalStolenMSec, bg.ThreadIDToTotalStolenMSec),
                    worstStolenTimeInstances: LeastNUtil.GetLongestStolenTimeInstances(fg.WorstStolenTimeInstances, bg.WorstStolenTimeInstances))); ;
        }

        private static bool IsIdleOrRivalOrLowPriThread(GCEvent ev)
        {
            if (ev.Kind.IsLeft)
            {
                return false;
            }
            else
            {
                WorkSpanType type = ev.Kind.AsRight.Type;
                switch (type)
                {
                    case WorkSpanType.RivalThread:
                    case WorkSpanType.LowPriThread:
                    case WorkSpanType.Idle:
                        return true;
                    case WorkSpanType.GcThread:
                        return false;
                    default:
                        throw new Exception(type.ToString());
                }
            }
        }

        private static void PrintEvent(GCEvent ev)
        {
            Console.WriteLine(ev.Show());
        }

        private static void SanityCheckEventsOrder(ServerGcHistory heap, TimeSpan gcTimeSpan)
        {
            double time = gcTimeSpan.StartMSec;
            foreach (GCEvent ev in EventsWalker.EachEvent(gcTimeSpan, heap))
            {
                Util.Assert(ev.TimeMSec >= time, "Event happens earlier than later event");
                time = ev.TimeMSec;
            }
        }
    }

    [Obsolete]
    internal readonly struct SingleHeapSingleThreadResult
    {
        public readonly int NExtraRestarts;
        public readonly bool FinalRestartDropped;
        public readonly List<JoinStageInfo> Stages;
        public readonly IReadOnlyDictionary<ThreadID, double> ThreadIDToTotalStolenMSec;
        public readonly IReadOnlyList<StolenTimeInstance> WorstStolenTimeInstances;

        public SingleHeapSingleThreadResult(
            int nExtraRestarts,
            bool finalRestartDropped,
            List<JoinStageInfo> stages,
            IReadOnlyDictionary<ThreadID, double> threadIDToTotalStolenMSec,
            IReadOnlyList<StolenTimeInstance> worstStolenTimeInstances)
        {
            NExtraRestarts = nExtraRestarts;
            FinalRestartDropped = finalRestartDropped;
            Stages = stages;
            ThreadIDToTotalStolenMSec = threadIDToTotalStolenMSec;
            WorstStolenTimeInstances = worstStolenTimeInstances;
        }

        public static readonly SingleHeapSingleThreadResult Empty = new SingleHeapSingleThreadResult(
            nExtraRestarts: 0,
            finalRestartDropped: false,
            stages: new List<JoinStageInfo>(),
            threadIDToTotalStolenMSec: Util.EmptyReadOnlyDictionary<ThreadID, double>(),
            worstStolenTimeInstances: Util.EmptyReadOnlyList<StolenTimeInstance>());
    }

    [Obsolete] // allow us to use experimental PerfView features
    internal class SingleHeapSingleThreadAnalyzer
    {
        private readonly Strictness strict;
        private readonly bool isBackground;
        // TODO: should always be non-null
        public ThreadID? ThreadID;
        // We'll change this at every work span.
        private int curPriority;

        private readonly uint heapId;
        // Note -- this is total across all heaps, meaning with 4 heaps this may go up to 4x the gc duration.
        // The threadID here is the *rival* thread that is stealing from us.
        private Dictionary<ThreadID, double> threadIDToTotalStolenMSec = new Dictionary<ThreadID, double>();
        private LeastN<StolenTimeInstance> worstStolenTimeInstances = new LeastN<StolenTimeInstance>(LeastNUtil.CollectingNWorstStolenTimeInstances, LeastNUtil.StolenTimeComparer);
        private List<PendingStolenTimeInstance> pendingStolenTimeInstancesWithoutStage = new List<PendingStolenTimeInstance>();

        private List<JoinStageInfo> result = new List<JoinStageInfo>();
        private MutJoinStageInfo currentJoin;
        private ServerGCState _state = ServerGCState.working;
        private ServerGCState state
        {
            get
            {
                return _state;
            }
            set
            {
                if (StateHasStateBeforeStolen(value) && stateBeforeStolen == null)
                {
                    throw new Exception($"Must set stateBeforeStolen before setting state to {value}");
                }
                if (!StateHasStateBeforeStolen(value) && stateBeforeStolen != null)
                {
                    throw new Exception($"stateBeforeStolen should not be set in state {value}");
                }
                _state = value;
            }
        }

        private GCJoinStage? PrevJoinStage =>
            Util.OpLast(result)?.JoinStage;
        
        // Only set if state == ServerGCState.stolen or idleForNoGoodReason
        private ServerGCState? _stateBeforeStolen = null;
        private ServerGCState? stateBeforeStolen
        {
            get
            {
                return _stateBeforeStolen;
            }
            set
            {
                if (value != null && StateHasStateBeforeStolen(value.Value))
                {
                    throw new Exception($"Can't set stateBeforeStolen to {value.Value}");
                }
                _stateBeforeStolen = value;
            }
        }

        // When we see an event, we'll assume that everything lastTime to this event's time is spent in this event.
        private double curStateStartTime;
        private bool isVeryFirstJoin = true;
        // When we do a join start (not LastJoin or FirstJoin), expect to see a restart from the other thread.
        //
        // This will rarely be 2 --
        // Imagine heaps are A and B.
        // A gets to an rjoin first, and finishes quickly. It then begins a restart.
        // By the time B gets to the rjoin it is already done. B does not wait and continues. B then makes it to the *next* join, which is *not* an rjoin.
        // At this point B will have two pending restarts, one for the rjoin and one for its current join.
        //
        // This may also go negative --
        // This is because the joinstart event is fired just after the join start.
        // It's possible that the thread is switched out at that very moment,
        // and the other thread gets the LastJoin and fires the restart before this thread's JoinStart event fires.
        private int expectingRestartFromOtherThread = 0;

        public SingleHeapSingleThreadAnalyzer(
            Strictness strict,
            bool isBackground,
            uint heapId,
            TimeSpan gcTimeSpan,
            ThreadID? threadID)
        {
            this.strict = strict;
            this.isBackground = isBackground;
            this.ThreadID = threadID;
            this.heapId = heapId;
            currentJoin = new MutJoinStageInfo(gcTimeSpan.StartMSec);
            curStateStartTime = gcTimeSpan.StartMSec;
        }

        public void OnEvent(GCEvent ev)
        {
            if (JoinAnalysis.DEBUG_PRINT(ev.TimeMSec))
            {
                Console.WriteLine($"old state: {state}, currentJoin: {currentJoin}");
            }

            if (ev.Kind.IsLeft)
            {
                HandleJoin(ev.TimeMSec, ev.Kind.AsLeft, strict);
            }
            else
            {
                HandleSpan(ev.TimeMSec, ev.Kind.AsRight);
            }

            if (JoinAnalysis.DEBUG_PRINT(ev.TimeMSec))
            {
                Console.WriteLine($"new state: {state}, currentJoin: {currentJoin}");
            }
        }

        // Returns null for unfinished GC
        public SingleHeapSingleThreadResult Finish(double gcEndMSec)
        {
            if (strict == Strictness.strict && currentJoin.JoinStage != null)
            {
                throw new Exception($"Unfinished stage {currentJoin.JoinStage}");
            }
            else
            {
                if (strict == Strictness.strict)
                {
                    GCJoinStage lastStage = result.Last().JoinStage;
                    if (!GCJoinStageUtil.IsPossibleFinalStage(lastStage))
                    {
                        throw new Exception($"Unexpected final stage {lastStage}");
                    }
                }

                int nExtraRestarts = -expectingRestartFromOtherThread;
                return new SingleHeapSingleThreadResult(
                    nExtraRestarts: nExtraRestarts,
                    // TODO: kill this, we shouldn't be dropping events.
                    // We may still have extra restarts though.
                    finalRestartDropped: false,
                    stages: result,
                    threadIDToTotalStolenMSec: threadIDToTotalStolenMSec,
                    worstStolenTimeInstances: worstStolenTimeInstances.Finish());
            }
        }

        private static bool StateHasStateBeforeStolen(ServerGCState state) =>
            state == ServerGCState.stolen || state == ServerGCState.idleForNoGoodReason;

        private ServerGCState StateOrStateBeforeStolen() =>
            StateHasStateBeforeStolen(state) ? Util.NonNull(stateBeforeStolen) : state;

        private void DoTransitionState(double newStartTime, ServerGCState newState)
        {
            // Console.WriteLine($"do transition state from {state} to {newState}");
            double time = newStartTime - curStateStartTime;
            currentJoin.MSecPerState[(int)state] += time;
            curStateStartTime = newStartTime;
            if (StateHasStateBeforeStolen(newState))
            {
                Util.Assert(stateBeforeStolen != null, "stateBeforeStolen should be set");
            }
            else
            {
                stateBeforeStolen = null;
            }
            state = newState;
        }

        private void TransitionState(double newStartTime, ServerGCState newState)
        {
            if (state != newState)
            {
                DoTransitionState(newStartTime, newState);
            }
        }

        private GCJoinStage? PreviousStage() =>
            result.Any() ? result.Last().JoinStage : (GCJoinStage?) null;

        private void HandlePendingStolenTimeInstances(GCJoinStage joinStage)
        {
            foreach (PendingStolenTimeInstance p in pendingStolenTimeInstancesWithoutStage)
            {
                Util.AddToDictionary<ThreadID>(threadIDToTotalStolenMSec, p.Span.ThreadId, p.Span.DurationMsc);
                worstStolenTimeInstances.Add(new StolenTimeInstance(oldPriority: p.OldPriority, heapId: heapId, span: p.Span, state: p.State, stage: joinStage));
            }
            pendingStolenTimeInstancesWithoutStage.Clear();
        }

        private void ExtendPreviousStage(double timeMsc)
        {
            // For the very first join, we might already be in working state as some events are dropped.
            HandlePendingStolenTimeInstances(Util.NonNull(currentJoin.JoinStage));
            DoTransitionState(timeMsc, ServerGCState.working);
            result[result.Count - 1] = result.Last().MergeWith(currentJoin.Finish(timeMsc, strict), strict);
            currentJoin = new MutJoinStageInfo(timeMsc);
        }

        private static GCJoinStage[] AllowedBeginStages = new GCJoinStage[] {
            GCJoinStage.generation_determined,
            GCJoinStage.begin_mark_phase,
            GCJoinStage.restart_ee
        };

        private void FinishStage(double timeMsc)
        {
            GCJoinStage? joinStage = currentJoin.JoinStage;
            Util.Assert(strict == Strictness.loose || joinStage != null);
            if (joinStage != null)
                HandlePendingStolenTimeInstances(joinStage.Value);

            // For the very first join, we might already be in working state as some events are dropped.
            DoTransitionState(timeMsc, ServerGCState.working);

            if (result.Any())
            {
                // TODO: Is it right that scan_dependent_handles can occur multiple times?
                if (currentJoin.JoinStage != GCJoinStage.scan_dependent_handles)
                {
                    Util.Assert(result.Last().JoinStage != currentJoin.JoinStage, $"Repeated stage {currentJoin.JoinStage}");
                }
            }
            else
            {
               
                // An ephemeral GC following a BGC won't have generation_determined.
                Util.Assert(
                    strict == Strictness.loose ||
                    AllowedBeginStages.Any(stage => stage == currentJoin.JoinStage),
                    $"First stage should be {string.Join(" ", AllowedBeginStages)}, got {currentJoin.JoinStage} (isBackground? {isBackground})");
            }

            // If we don't have the stage, just ignore this (loose mode only)
            if (joinStage != null)
                result.Add(currentJoin.Finish(timeMsc, strict));
            currentJoin = new MutJoinStageInfo(timeMsc);
        }

        private static bool IsWaitingInJoinOrRestarting(ServerGCState state)
        {
            switch (state)
            {
                case ServerGCState.waitingInJoin:
                case ServerGCState.restarting:
                    return true;
                default:
                    return false;
            }
        }
        
        private void HandleJoin(double timeMSec, GcJoin j, Strictness strict)
        {
            Util.Assert(j.ThreadId != 0);

            // Restart events come from another thread
            if (j.Type != GcJoinType.Restart)
            {
                if (ThreadID == null)
                {
                    ThreadID = j.ThreadId;
                }
                else if (ThreadID != j.ThreadId)
                {
                    throw new Exception($"Heap {heapId}: should only be one thread per heap, but got {ThreadID} and {j.ThreadId}");
                }
            }

            switch (j.Time)
            {
                case GcJoinTime.Start:
                    HandleJoinStart(timeMSec, j, strict);
                    break;
                case GcJoinTime.End:
                    HandleJoinEnd(timeMSec, j);
                    break;
                default:
                    throw new Exception(j.ToString());
            }
        }

        private static bool AllowIsEESuspendedToDiffer(GCJoinStage? stage)
        {
            switch (stage)
            {
                case GCJoinStage.start_bgc:
                // after_reset calls disable_preemptive then enable_preemptive
                case GCJoinStage.after_reset:
                case GCJoinStage.restart_ee:
                case GCJoinStage.done:
                    return true;
                default:
                    return false;
            }
        }

        private void HandleJoinStart(double timeMSec, GcJoin j, Strictness strict)
        {
            if (j.Type != GcJoinType.Restart)
            {
                Util.Assert(strict == Strictness.loose || currentJoin.JoinStage == null, "Should not start a join if already in one");
                currentJoin.JoinStage = j.JoinStage;
            }

            Util.Assert(
                strict == Strictness.loose
                    || currentJoin.IsEESuspended == null
                    || currentJoin.IsEESuspended == j.IsEESuspended
                    || AllowIsEESuspendedToDiffer(currentJoin.JoinStage)
                    // Also allow if the prev join could change IsEESuspended, since the GCSuspendEEStart or stop event for may come out delayed
                    || AllowIsEESuspendedToDiffer(PrevJoinStage),
                "IsEESuspended differs between the current join and a join event. This shouldn't change in the middle of a join unless it's start_bgc or after_reset."); ; ;
            currentJoin.IsEESuspended = j.IsEESuspended;

            switch (j.Type)
            {
                case GcJoinType.FirstJoin:
                case GcJoinType.LastJoin:
                    TransitionState(timeMSec, ServerGCState.singleThreaded);
                    // There will be a Restart after either FirstJoin or LastJoin.
                    break;

                case GcJoinType.Join:
                    expectingRestartFromOtherThread++;
                    Util.Assert(strict == Strictness.loose || expectingRestartFromOtherThread <= 2, "Too many expectingRestart for a new join"); // See comment on expectingRestartFromOtherTHread for why this can reach 2
                    TransitionState(timeMSec, ServerGCState.waitingInJoin);
                    break;

                case GcJoinType.Restart:
                    Util.Assert(j.JoinStage == GCJoinStage.restart, "restart should be a restart");
                    // It's possible that some other thread is restarting an rjoin that we haven't entered yet and won't see a join event for.
                    if (GCJoinStageUtil.IsRJoinStage((GCJoinStage) j.JoinID))
                    {
                        Util.Assert((state == ServerGCState.working) == (currentJoin.JoinStage == null), () => $"Should have join stage iff in join. state: {state}, joinStage: {Util.NullableToString(currentJoin.JoinStage)}");
                    }


                    // Note: expectingRestartFromOtherThread updated on the restart end.
                    switch (StateOrStateBeforeStolen())
                    {
                        case ServerGCState.singleThreaded:
                            Util.Assert(
                                strict == Strictness.loose || ThreadID == null || ThreadID == j.ThreadId,
                                $"Thought I was the one restarting, but my thread ID is {ThreadID} and restart thread ID is {j.ThreadId}");
                            TransitionState(timeMSec, ServerGCState.restarting);
                            break;

                        default:
                            Util.Assert(
                                ThreadID != j.ThreadId,
                                $"Thought other thread was restarting, but both have thread ID {ThreadID}");
                            // This is the Restart from another heap, ignore.
                            break;
                    }
                    break;
                default:
                    throw new Exception(j.Type.ToString());
            }
        }

        private void HandleJoinEnd(double timeMSec, GcJoin j)
        {
            GCJoinStage joinStage = (GCJoinStage)j.JoinID;

            switch (j.Type)
            {
                case GcJoinType.Restart:
                    Util.Assert(joinStage == GCJoinStage.restart, "restart should be a restart");

                    ServerGCState state = StateOrStateBeforeStolen();

                    switch (state)
                    {
                        case ServerGCState.working:
                            if (isVeryFirstJoin && currentJoin.JoinStage == null)
                            {
                                // Didn't see a join start yet.
                                //
                                // Remember, the first event of the first join may go missing.
                                // There are two possibilities.
                                // A) This thread was the LastJoin and this is our restart.
                                // B) This thread had a regular JoinStart event that was dropped.
                                // Unfortunately it's hard to tell those apart right now.

                                // In case A), this is the last event we'll see for this join. So must finish it up now.
                                // In case B), we will eventually see a JoinEnd event. Then we can *extend* the join we finish here, and patch up the missing JoinStart.

                                // In case A), we don't want to change 'expectingRestartFromOtherThread' as this is our own restart.
                                // In case B), we also don't want to change it as the JoinStart never happened to increment it.

                                Util.Assert(expectingRestartFromOtherThread == 0);
                                currentJoin.JoinStage = GCJoinStage.generation_determined;
                                EndTheJoin(timeMSec, j);
                            }
                            else
                            {
                                expectingRestartFromOtherThread--;
                            }
                            break;

                        case ServerGCState.waitingInJoin:
                        case ServerGCState.singleThreaded: // A restart from another heap may be delayed an arbitrary amount. We might be in the single-threaded portion of an rjoin while that is happening.
                            expectingRestartFromOtherThread--;
                            break;

                        case ServerGCState.restarting:
                            // We're the one doing the restart, so don't decrement expectingRestartFromOtherThread
                            EndTheJoin(timeMSec, j);
                            break;

                        case ServerGCState.stolen:
                        case ServerGCState.idleForNoGoodReason:
                            Util.Assert(false, "We used StateOrStateBeforeStolen()");
                            break;
                    }
                    break;
                case GcJoinType.Join:
                    EndTheJoin(timeMSec, j);
                    break;

                case GcJoinType.FirstJoin:
                case GcJoinType.LastJoin:
                    Util.Assert(false, "FirstJoin and LastJoin shouldn't have end events?");
                    break;
            }
        }

        private void EndTheJoin(double timeMSec, GcJoin j)
        {
            GCJoinStage joinStage = (GCJoinStage)j.JoinID;
            
            // For GCJoinStage.gc_join_generation_determined, possible we see a restart end, then join start, then join end.
            if (joinStage == GCJoinStage.generation_determined && j.Type == GcJoinType.Join && j.Time == GcJoinTime.End)
            {
                // Because the gc_join_generation_determined join is fired *before*
                // `do_pre_gc` which fires the gcstart etw event,
                // we possibly won't see the start of that join.
                // (And since we don't know whether this was started with Join or LastJoin,
                // we don't know whether there should be a JoinEnd.)
                if (currentJoin.JoinStage == null)
                {
                    // We didn't see the JoinStart event. Compensate for that now.
                    expectingRestartFromOtherThread++; // What the JoinStart would have done
                    currentJoin.JoinStage = GCJoinStage.generation_determined;
                }
                else
                {
                    // We did see a JoinStart.
                    // There is still a need for special handling here because we may have seen the RestartEnd and finished the phase, so we extend instead of finishing a new phase.
                    Util.Assert(currentJoin.JoinStage == GCJoinStage.generation_determined, "Expected gc_join_generation_determined");
                }

                if (result.Any())
                {
                    Util.Assert(result.Count == 1, "can only be 1");
                    // The previous stage that exists now came from a restart-end event.
                    // We now know that was not our own, it was from another thread.
                    expectingRestartFromOtherThread--;
                    ExtendPreviousStage(timeMSec);
                }
                else
                {
                    FinishStage(timeMSec);
                }
            }
            else
            {
                if (strict == Strictness.strict)
                {
                    Util.Assert(currentJoin.JoinStage != null, "ending join without a join stage");
                    Util.Assert(joinStage == GCJoinStage.restart || currentJoin.JoinStage == joinStage, "join start and end should match");
                }
                FinishStage(timeMSec);
            }
            isVeryFirstJoin = false;
        }

        // The span comes from either a cpu sample or cswitch event.
        private void HandleSpan(double startTimeMSec, GcWorkSpan s)
        {
            switch (s.Type)
            {
                case WorkSpanType.GcThread:
                    Util.Assert(ThreadID == null || ThreadID == s.ThreadId, $"ThreadID is {ThreadID} but got a work span with id {s.ThreadId}");
                    HandleGCThreadSpan(startTimeMSec);
                    break;
                case WorkSpanType.RivalThread:
                case WorkSpanType.LowPriThread: // This is like RivalThread except the rival thread has low priority
                    HandleRivalOrLowPriThreadSpan(startTimeMSec, s);
                    break;
                case WorkSpanType.Idle:
                    HandleIdleSpan(startTimeMSec);
                    break;
                default:
                    throw new Exception(s.Type.ToString());
            }

            curPriority = s.Priority;
        }

        private void HandleGCThreadSpan(double startTimeMSec)
        {
            if (state != ServerGCState.waitingInJoin)
            {
                if (StateHasStateBeforeStolen(state))
                {
                    TransitionState(startTimeMSec, Util.NonNull(stateBeforeStolen));
                }
                stateBeforeStolen = null;
            }
        }

        private void HandleRivalOrLowPriThreadSpan(double startTimeMSec, GcWorkSpan s)
        {
            Util.Assert(s.OldThreadId != Util.THREAD_ID_IDLE && s.ThreadId != Util.THREAD_ID_IDLE);
            // Ignore a stolen span while we're waiting anyway
            if (state != ServerGCState.waitingInJoin)
            {
                AddStolenSpan(s);
                // We may get multiple stolen events in a row if multiple processes use the stolen time.
                if (state != ServerGCState.stolen)
                {
                    // If the time was stolen from us, clearly we shouldn't be extending past the next ...
                    // TODO: care about this differently based on whether it is stolen while waiting in join or while working
                    if (stateBeforeStolen == null)
                    {
                        stateBeforeStolen = state;
                    }

                    TransitionState(startTimeMSec, ServerGCState.stolen);
                }
            }
        }
        
        private void HandleIdleSpan(double startTimeMSec)
        {
            ServerGCState before = StateOrStateBeforeStolen();
            if (before == ServerGCState.waitingInJoin)
            {
                // If we were in 'stolen' before this moves us back to waiting.
                TransitionState(startTimeMSec, ServerGCState.waitingInJoin);
            }
            else
            {
                // If we weren't waiting, why are we idle?
                if (stateBeforeStolen == null)
                {
                    stateBeforeStolen = state;
                }
                TransitionState(startTimeMSec, ServerGCState.idleForNoGoodReason);
            }
        }

        private void AddStolenSpan(GcWorkSpan span)
        {
            // Console.WriteLine($"Stolen span {span.OldThreadId} -> {span.ThreadId}, my ThreadID is {ThreadID}");
            // A sample span will have no OldThreadID
            Util.Assert(span.OldThreadId == null || span.OldThreadId == ThreadID);
            pendingStolenTimeInstancesWithoutStage.Add(new PendingStolenTimeInstance(curPriority, span, state));
        }
    }

    [Obsolete] // allow us to use experimental PerfView features
    internal readonly struct GCEvent
    {
        // For a stolen span, this is the *old* thread ID.
        public readonly GCThreadKind? ThreadKind;
        public readonly double TimeMSec;
        public readonly Either<GcJoin, GcWorkSpan> Kind;
        public GCEvent(GCThreadKind? threadKind, double timeMSec, Either<GcJoin, GcWorkSpan> kind)
        {
            ThreadKind = threadKind;
            TimeMSec = timeMSec;
            Kind = kind;
        }

        public static GCEvent OfJoin(GcJoin j) =>
            new GCEvent(j.ThreadKind, j.AbsoluteTimestampMsc, Either<GcJoin, GcWorkSpan>.Left(Util.NonNull(j)));

        public static GCEvent OfSpan(GcWorkSpan s) =>
            new GCEvent(s.ThreadKind, s.AbsoluteTimestampMsc, Either<GcJoin, GcWorkSpan>.Right(s));

        public string Show()
        {
            if (Kind.IsLeft)
            {
                GcJoin j = Kind.AsLeft;
                return $"EVENT: threadKind: {ThreadKind}, isEESuspended: {j.IsEESuspended}, Join: {TimeMSec}, type: {j.Type}, time: {j.Time}, stage: {j.JoinStage}, processor: {j.Heap}";
            }
            else
            {
                GcWorkSpan span = Kind.AsRight;
                // Apparently this happens!
                // Util.Assert(span.DurationMsc >= 0.0, "Must have non-negative span duration");
                string neg = span.DurationMsc < 0 ? " (NEGATIVE LENGTH!)" : "";
                TimeSpan timeSpan = TimeSpan.FromStartLengthMSecAllowNegativeLength(TimeMSec, span.DurationMsc);
                return $"EVENT: threadKind: {ThreadKind}, Span: {timeSpan}{neg}, type: {span.Type}, threadId: {span.ThreadId}";
            }
        }
    }

    [Obsolete] // allow us to use experimental PerfView features
    internal class EventsWalker
    {
        private readonly TimeSpan gcTimeSpan;
        private readonly ServerGcHistory heap;
        private int joinEventIndex = 0;
        private int sampleSpanIndex = 0;
        private int switchSpanIndex = 0;

        private EventsWalker(TimeSpan gcTimeSpan, ServerGcHistory heap)
        {
            this.gcTimeSpan = gcTimeSpan;
            this.heap = heap;
        }

        private GCEvent? NextEvent()
        {
            while (true)
            {
                GcJoin? join = joinEventIndex == heap.GcJoins.Count ? null : heap.GcJoins[joinEventIndex];
                GcWorkSpan? sampleSpan = sampleSpanIndex == heap.SampleSpans.Count ? null : heap.SampleSpans[sampleSpanIndex];
                GcWorkSpan? switchSpan = switchSpanIndex == heap.SwitchSpans.Count ? null : heap.SwitchSpans[switchSpanIndex];

                if (join == null && sampleSpan == null && switchSpan == null)
                {
                    return null;
                }

                double joinTimeMSec = join == null ? double.MaxValue : join.AbsoluteTimestampMsc;
                double sampleTimeMSec = sampleSpan == null ? double.MaxValue : sampleSpan.AbsoluteTimestampMsc;
                double switchTimeMSec = switchSpan == null ? double.MaxValue : switchSpan.AbsoluteTimestampMsc;
                if (joinTimeMSec < sampleTimeMSec && joinTimeMSec < switchTimeMSec)
                {
                    joinEventIndex++;
                    GcJoin j = Util.NonNull(join);
                    if (joinTimeMSec > gcTimeSpan.EndMSec)
                    {
                        GCJoinStage stage = (GCJoinStage) j.JoinID;
                        if (!GCJoinStageUtil.IsPossibleFinalStage(stage) && stage != GCJoinStage.restart)
                        {
                            Console.WriteLine(GCEvent.OfJoin(j));
                            throw new Exception($"GC time span is {gcTimeSpan}, but got a join {stage} at {joinTimeMSec}");
                        }
                    }
                    else
                    {
                        if (!gcTimeSpan.Contains(joinTimeMSec))
                        {
                            Console.WriteLine(gcTimeSpan);
                            Console.WriteLine(joinTimeMSec);
                            throw new Exception("join event not within this gc?");
                        }
                    }
                    return GCEvent.OfJoin(j);
                }
                else if (sampleTimeMSec < switchTimeMSec)
                {
                    sampleSpanIndex++;
                    // PerfView calls `AddServerGcSample` for the current GC regardless of where the sample was; this means we get samples from before the GC even started.
                    if (gcTimeSpan.Contains(sampleTimeMSec))
                    {
                        return GCEvent.OfSpan(Util.NonNull(sampleSpan));
                    }
                }
                else
                {
                    switchSpanIndex++;
                    // Similar to samples
                    if (gcTimeSpan.Contains(switchTimeMSec))
                    {
                        return GCEvent.OfSpan(Util.NonNull(switchSpan));
                    }
                }
            }
        }

        public static IEnumerable<GCEvent> EachEvent(TimeSpan gcTimeSpan, ServerGcHistory heap)
        {
            EventsWalker ew = new EventsWalker(gcTimeSpan: gcTimeSpan, heap: heap);
            while (true)
            {
                GCEvent? ev = ew.NextEvent();
                if (ev == null)
                {
                    break;
                }
                else
                {
                    yield return ev.Value;
                }
            }
        }
    }

    [Obsolete]
    static class LeastNUtil
    {
        public const uint CollectingNWorstStolenTimeInstances = 256;
        public const uint CollectingNWorstJoinInstances = 256;

        private static int CompareDoubles(double a, double b) =>
            a < b ? -1 : a > b ? 1 : 0;

        private static int CombineCompare(int a, int b) =>
            a == 0 ? b : a;

        private static int StolenTimeCompareFunction(StolenTimeInstance a, StolenTimeInstance b) =>
            // Collection is called "LeastN" but we'll keep the N *longest* instances, so comparer is in reverse
            CombineCompare(CompareDoubles(b.DurationMSec, a.DurationMSec), CompareDoubles(a.StartTimeMSec, b.StartTimeMSec));

        private static int WorstJoinCompareFunction(WorstJoinInstance a, WorstJoinInstance b) =>
            CombineCompare(CompareDoubles(b.DurationMSec, a.DurationMSec), CompareDoubles(a.StartTimeMSec, b.StartTimeMSec));

        public static readonly IComparer<StolenTimeInstance> StolenTimeComparer =
            Comparer<StolenTimeInstance>.Create(StolenTimeCompareFunction);

        public static readonly IComparer<WorstJoinInstance> WorstJoinComparer =
            Comparer<WorstJoinInstance>.Create(WorstJoinCompareFunction);

        public static readonly IComparer<StolenTimeInstanceWithGcNumber> ComparerWithIndex =
            Comparer<StolenTimeInstanceWithGcNumber>.Create((a, b) => StolenTimeCompareFunction(a.Instance, b.Instance));
        
        public static IReadOnlyList<StolenTimeInstance> GetLongestStolenTimeInstances(IEnumerable<StolenTimeInstance> values) =>
            GetLeastN<StolenTimeInstance>(values, StolenTimeComparer, CollectingNWorstStolenTimeInstances);

        public static IReadOnlyList<WorstJoinInstance> GetWorstJoins(IEnumerable<WorstJoinInstance> values) =>
            GetLeastN<WorstJoinInstance>(values, WorstJoinComparer, CollectingNWorstJoinInstances);

        public static IReadOnlyList<StolenTimeInstance> GetLongestStolenTimeInstances(IEnumerable<StolenTimeInstance> a, IEnumerable<StolenTimeInstance> b) =>
            GetLongestStolenTimeInstances(a.Concat(b));

        public static IReadOnlyList<StolenTimeInstanceWithGcNumber> GetLongestStolenTimeInstances(IEnumerable<StolenTimeInstanceWithGcNumber> values) =>
            GetLeastN<StolenTimeInstanceWithGcNumber>(values, ComparerWithIndex, CollectingNWorstStolenTimeInstances);

        public static IReadOnlyList<T> GetLeastN<T>(IEnumerable<T> values, IComparer<T> comparer, uint n)
        {
            LeastN<T> ln = new LeastN<T>(n, comparer);
            foreach (T v in values)
            {
                ln.Add(v);
            }
            return ln.Finish();
        }
    }

    // Keeps the last N elements around, as determined by comparer
    class LeastN<T>
    {
        private readonly IComparer<T> comparer;
        private readonly T[] data;
        private uint size;

        public LeastN(uint capacity, IComparer<T> comparer)
        {
            this.comparer = comparer;
            data = new T[capacity];
            size = 0;
        }

        private void AssertSorted()
        {
            for (int i = 1; i < size; i++)
            {
                T prev = data[i - 1];
                T cur = data[i];
                Util.Assert(comparer.Compare(prev, cur) <= 0);
            }
        }

        [Obsolete]
        public void Add(T x)
        {
            AssertSorted();

            if (data.Length == 0) { }
            else if (size == 0)
            {
                data[0] = x;
                size++;
            }
            else
            {
                // "If value is not found and value is less than one or more elements
                // in array, the negative number returned is the bitwise complement of the index
                // of the first element that is larger than value"
                int bitwiseComplementOfIndex = Array.BinarySearch<T>(data, 0, (int)size, x, comparer);
                // index of first element >= value
                int index = bitwiseComplementOfIndex < 0 ? ~bitwiseComplementOfIndex : bitwiseComplementOfIndex;
                Util.Assert(0 <= index && index <= size, "index should be valid or == size");
                if (index == size)
                {
                    // It's the greatest element so far
                    if (size != data.Length)
                    {
                        data[size] = x;
                        size++;
                    }
                    // else do nothing, data is full and this is greater than everything else in data
                }
                else
                {
                    if (size != data.Length)
                    {
                        size++;
                    }
                    // Insert at index
                    for (uint i = size - 1; i > index; i--)
                    {
                        data[i] = data[i - 1];
                    }
                    data[index] = x;
                }
            }

            AssertSorted();
        }

        public IReadOnlyList<T> Finish()
        {
            for (uint i = 0; i < size; i++)
            {
                Util.Assert(data[i] != null);
            }
            return data.Take((int)size).ToArray();
        }
    }
}

#endif
