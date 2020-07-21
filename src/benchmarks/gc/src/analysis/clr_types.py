# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from abc import ABC, abstractmethod
from typing import (
    Any,
    Callable,
    Generic,
    Iterable,
    Iterator,
    Optional,
    Mapping,
    Sequence,
    Tuple,
    Type,
    List)

from result import Err, Ok, Result

from ..commonlib.type_utils import E, K, T, U, V

GCType = int  # an enum
GCReason = int  # an enum
GcJoinType = int  # an enum
GcJoinTime = int  # an enum
GCGlobalMechanisms = int  # an enum


Gens = int  # an enum

gc_heap_compact_reason = int  # anenum


class AbstractKeyValuePair(ABC, Generic[K, V]):
    Key: K
    Value: V


class AbstractIReadOnlyDictionary(ABC, Generic[K, V]):
    Count: int
    Keys: Iterable[K]
    Values: Iterable[V]

    def ContainsKey(self, key: K) -> V:
        raise NotImplementedError()

    def __getitem__(self, key: K) -> V:
        raise NotImplementedError()

    def __iter__(self) -> Iterator[AbstractKeyValuePair[K, V]]:
        raise NotImplementedError()


class AbstractList(ABC, Generic[T]):
    Count: int

    def __iter__(self) -> Iterator[T]:
        raise NotImplementedError()

    def __getitem__(self, index: int) -> T:
        raise NotImplementedError()


class AbstractTuple(ABC, Generic[T, U]):
    Item1: T
    Item2: U


class AbstractAction(ABC, Generic[T]):
    pass


class AbstractTextWriter(ABC):
    def Close(self) -> None:
        raise NotImplementedError()


class AbstractSymbolReader(ABC):
    @abstractmethod
    def __init__(self, log: AbstractTextWriter, nt_symbol_path: Optional[str]):
        raise NotImplementedError()


# .net event type, not a real class
class AbstractEvent(ABC, Generic[T]):
    # This is for '+=', there is no '+'
    @abstractmethod
    def __iadd__(self, cb: Callable[[T], None]) -> Any:
        raise NotImplementedError()


class AbstractTraceEvent(ABC):
    # incomplete (very many properties, we only use a few)
    ProcessorNumber: int
    EventName: str
    ProcessID: int
    TimeStampRelativeMSec: float
    ThreadID: int


class AbstractTraceEventSource(ABC):
    def Dispose(self) -> None:
        raise NotImplementedError()

    AllEvents: AbstractEvent[AbstractTraceEvent]


class AbstractTraceEventDispatcher(AbstractTraceEventSource):
    def Dispose(self) -> None:
        raise NotImplementedError()

    def Process(self) -> None:
        raise NotImplementedError()


class AbstractCtfTraceEventSource(AbstractTraceEventDispatcher):
    @abstractmethod
    def __init__(self, file_name: str):
        raise NotImplementedError()


class AbstractETWTraceEventSource(AbstractTraceEventDispatcher):
    @abstractmethod
    def __init__(self, file_name: str):
        raise NotImplementedError()


class AbstractEventPipeEventSource(AbstractTraceEventDispatcher):
    def __init__(self, file_name: str):
        raise NotImplementedError()


class AbstractDateTime(ABC):
    Ticks: float


class AbstractTraceThread(ABC):
    ThreadID: int


# Microsoft.Diagnostics.Tracing.Analysis.TraceProcess
class AbstractTraceProcess(ABC):
    ProcessID: int
    Name: str
    ParentID: int
    CommandLine: str

    StartTime: AbstractDateTime
    EndTime: AbstractDateTime

    # ... many more properties ...

    Threads: Iterable[AbstractTraceThread]


class AbstractGCStats(ABC):
    IsServerGCUsed: int  # -1 unknown, 0 false, 1 true
    SegmentSize: Optional[int]

    @abstractmethod
    def GetGCPauseTimePercentage(self) -> float:
        raise NotImplementedError()

    ProcessDuration: float


# TraceManagedProcess.cs
class AbstractGCHeapStats(ABC):
    TotalHeapSize: int
    TotalPromoted: int
    Depth: int
    GenerationSize0: int
    TotalPromotedSize0: int
    GenerationSize1: int
    TotalPromotedSize1: int
    GenerationSize2: int
    TotalPromotedSize2: int
    GenerationSize3: int
    TotalPromotedSize3: int
    FinalizationPromotedSize: int
    FinalizationPromotedCount: int
    PinnedObjectCount: int
    SinkBlockCount: int
    GCHandleCount: int


class AbstractGCGlobalHeapHistory(ABC):
    FinalYoungestDesired: int
    NumHeaps: int
    CondemnedGeneration: int
    Gen0ReductionCount: int
    Reason: GCReason
    GlobalMechanisms: GCGlobalMechanisms
    MemoryPressure: int
    HasMemoryPressure: bool


# From ClrTraceEventParser.cs in PerfView
class AbstractGCPerHeapHistoryGenData(ABC):
    # Size of the generation before the GC, includes fragmentation
    SizeBefore: int
    # Size of the generation after GC.  Includes fragmentation
    SizeAfter: int

    # Size occupied by objects at the beginning of the GC, discounting fragmentation.
    ObjSpaceBefore: int
    # This is the fragmenation at the end of the GC.
    Fragmentation: int

    # Size occupied by objects, discounting fragmentation.
    ObjSizeAfter: int

    # This is the free list space
    # (ie, what's threaded onto the free list) at the beginning of the GC.
    FreeListSpaceBefore: int

    # This is the free obj space
    # (ie, what's free but not threaded onto the free list) at the beginning of the GC.
    FreeObjSpaceBefore: int

    # This is the free list space (ie, what's threaded onto the free list) at the end of the GC.
    FreeListSpaceAfter: int
    # This is the free obj space
    # (ie, what's free but not threaded onto the free list) at the end of the GC.
    FreeObjSpaceAfter: int

    # This is the amount that came into this generation on this GC
    In: int
    # This is the number of bytes survived in this generation.
    Out: int
    # This is the new budget for the generation
    Budget: int

    # This is the survival rate
    SurvRate: int

    PinnedSurv: int

    NonePinnedSurv: int


# See TraceManagedProcess.cs in PerfView.
# Note: this is different from GCPerHeapHistoryTraceData;
# converted from that in `ProcessPerHeapHistory`
class AbstractGCPerHeapHistory(ABC):
    MemoryPressure: int
    HasMemoryPressure: bool
    VersionRecognized: bool
    FreeListAllocated: int
    HasFreeListAllocated: bool
    FreeListRejected: int
    HasFreeListRejected: bool
    CondemnReasons0: int
    CondemnReasons1: int
    HasCondemnReasons1: bool
    Version: int
    GenData: Sequence[AbstractGCPerHeapHistoryGenData]
    ExpandMechanisms: int  # gc_heap_expand_mechanism
    CompactMechanisms: int  # gc_heap_compact_reason


class AbstractGcJoin(ABC):
    # WARN: 'Heap' here comes from the 'ProcessorNumber' of the event, not the 'Heap' of the event!
    Heap: int
    RelativeTimestampMsc: float
    AbsoluteTimestampMsc: float
    Type: GcJoinType
    Time: GcJoinTime
    JoinID: int


# See TraceManagedProcess.cs in PerfView
class AbstractThreadWorkSpan(ABC):
    ThreadId: int
    ProcessId: int
    ProcessName: str
    ProcessorNumber: int
    AbsoluteTimestampMsc: float
    DurationMsc: float
    Priority: int
    WaitReason: int


# See TraceMangedProcess.cs in PerfView
class AbstractGcWorkSpan(AbstractThreadWorkSpan):
    Type: int
    RelativeTimestampMsc: int


class AbstractServerGcHistory(ABC):
    HeapId: int
    ProcessId: int
    GcWorkingThreadId: int
    GcWorkingThreadPriority: int
    SwitchSpans: Sequence[AbstractGcWorkSpan]
    SampleSpans: Sequence[AbstractGcWorkSpan]
    GcJoins: AbstractList[AbstractGcJoin]


class AbstractBGCAllocWaitInfo(ABC):
    WaitStartRelativeMSec: float
    WaitStopRelativeMSec: float


# From TraceManagedProcess.cs in PerfView
class AbstractMarkInfo(ABC):
    # The sequence index is a MarkRootType (see enums.py)
    MarkTimes: Sequence[float]
    MarkPromoted: Sequence[int]


# Analysis.cs
class AbstractPythonnetUtil:
    @staticmethod
    def TryGetValue(
        d: AbstractIReadOnlyDictionary[int, AbstractMarkInfo], key: int
    ) -> Optional[AbstractMarkInfo]:
        raise NotImplementedError()


class AbstractBGCRevisitInfo(ABC):
    PagesRevisited: int
    ObjectsRevisited: int


class AbstractGCCondemnedReasons(ABC):
    # This records which reasons are used and the value. Since the biggest value
    # we need to record is the generation number a byte is sufficient.
    CondemnedReasonGroups: Sequence[int]


class AbstractFreeListEfficiency(ABC):
    Valid: bool
    # NOTE: this is just FreeListALlocated_Sum
    Allocated: float
    # NOTE: This is just FreeListAllocated + FreeListRejected for each heap
    FreeListConsumed: float


# TODO: fill out all the properties! (from TraceManagedProcess.cs in PerfView)
class AbstractTraceGC(ABC):
    Number: int  # NOTE: Starts counting at 1 (or higher if the trace doesn't start at the first gc)
    Type: GCType
    Reason: GCReason
    # Note: 0, 1, or 2, not 3. LOH is collected along with gen 2.
    Generation: int

    StartRelativeMSec: float
    EndRelativeMSec: float
    DurationMSec: float
    PauseDurationMSec: float
    SuspendDurationMSec: float
    PercentTimeInGC: float
    ProcessCpuMSec: float
    GCCpuMSec: float
    PerHeapMarkTimes: Optional[AbstractIReadOnlyDictionary[int, AbstractMarkInfo]]
    DurationSinceLastRestartMSec: float
    PauseStartRelativeMSec: float
    IsComplete: bool

    # The 2 fields below would only make sense if the type is BackgroundGC.
    BGCCurrentPhase: int  # see BGCPhase in enums.py
    BGCRevisitInfoArr: Sequence[Sequence[AbstractBGCRevisitInfo]]
    BGCFinalPauseMSec: float

    ServerGcHeapHistories: Sequence[AbstractServerGcHistory]

    # Amount of memory allocated since last GC.  Requires GCAllocationTicks enabled.  The
    # data is split into small and large heaps
    # (array of length 2)
    AllocedSinceLastGCBasedOnAllocTickMB: Sequence[float]

    HeapCount: int

    # Calculate the size of all pinned objects
    def GetPinnedObjectSizes(self) -> int:
        raise NotImplementedError()

    # Percentage of the pinned objects created by the user
    def GetPinnedObjectPercentage(self) -> int:
        raise NotImplementedError()

    def GetTotalGCTime(self) -> float:
        raise NotImplementedError()

    HeapSizeAfterMB: float
    PromotedMB: float
    # gen is a Gens (see enums.py)
    def SurvivalPercent(self, gen: int) -> float:
        raise NotImplementedError()

    def GenSizeAfterMB(self, gen: int) -> float:
        raise NotImplementedError()

    def GenFragmentationMB(self, gen: int) -> float:
        raise NotImplementedError()

    def GenFragmentationPercent(self, gen: int) -> float:
        raise NotImplementedError()

    def GenInMB(self, gen: int) -> float:
        raise NotImplementedError()

    def GenOutMB(self, gen: int) -> float:
        raise NotImplementedError()

    def GenPromotedMB(self, gen: int) -> float:
        raise NotImplementedError()

    def GenBudgetMB(self, gen: int) -> float:
        raise NotImplementedError()

    def GenObjSizeAfterMB(self, gen: int) -> float:
        raise NotImplementedError()

    PerHeapCondemnedReasons: Sequence[AbstractGCCondemnedReasons]

    def FindFirstHighestCondemnedHeap(self) -> int:
        raise NotImplementedError()

    def IsLowEphemeral(self) -> bool:
        raise NotImplementedError()

    def IsNotCompacting(self) -> bool:
        raise NotImplementedError()

    # NOTE: this writes to `reasons_info`!
    # DIct key is a CondemnedReasonsGroup -- see enums.py
    def GetCondemnedReasons(self, reasons_info: AbstractIReadOnlyDictionary[int, int]) -> None:
        raise NotImplementedError()

    PerHeapHistories: Sequence[AbstractGCPerHeapHistory]

    TotalPinnedPlugSize: int
    TotalUserPinnedPlugSize: int

    HeapStats: AbstractGCHeapStats
    LOHWaitThreads: AbstractIReadOnlyDictionary[int, AbstractBGCAllocWaitInfo]
    GlobalHeapHistory: Optional[AbstractGCGlobalHeapHistory]

    FreeList: AbstractFreeListEfficiency
    AllocedSinceLastGCMB: float
    # Ratio of heap size before and after
    RatioPeakAfter: float
    AllocRateMBSec: float
    # Peak heap size before GCs (mb)
    HeapSizePeakMB: float

    # Per generation view of user allocated data
    # In MB
    UserAllocated: Sequence[float]

    HeapSizeBeforeMB: float

    GenSizeBeforeMB: Sequence[float]

    PauseTimePercentageSinceLastGC: float


class AbstractTraceGarbageCollector(ABC):
    def Stats(self) -> AbstractGCStats:
        raise NotImplementedError()

    GCs: Sequence[AbstractTraceGC]  # Seems to be automatically converted


class AbstractTraceLoadedDotNetRuntime(ABC):
    GC: AbstractTraceGarbageCollector
    StartupFlags: int


class AbstractTraceLoadedDotNetRuntimeExtensions(ABC):
    @staticmethod
    @abstractmethod
    def NeedLoadedDotNetRuntimes(source: AbstractTraceEventDispatcher) -> None:
        raise NotImplementedError()

    # Returns None if this is not a managed process
    @staticmethod
    @abstractmethod
    def LoadedDotNetRuntime(
        process: AbstractTraceProcess,
    ) -> Optional[AbstractTraceLoadedDotNetRuntime]:
        raise NotImplementedError()


# This is Microsoft.Diagnostics.Tracing.Etlx.TraceProcess,
# not Microsoft.Diagnostics.Tracing.Analysis.TraceProcess.
# Seems similar enough to "inherit" from that.
class AbstractEtlxTraceProcess(AbstractTraceProcess, ABC):
    pass


class AbstractTraceProcesses(ABC):
    @abstractmethod
    def __iter__(self) -> Iterator[AbstractTraceProcess]:
        raise NotImplementedError()


class AbstractTraceProcessesExtensions(ABC):
    @staticmethod
    @abstractmethod
    def Processes(source: AbstractTraceEventSource) -> Optional[AbstractTraceProcesses]:
        raise NotImplementedError()


class AbstractGCIssues(ABC):
    # NOTE: last two parameters are out params, so just pass None
    # second return value is ProcessGCParameters
    @staticmethod
    @abstractmethod
    def Analyze(
        process: AbstractTraceProcess, param: None, issues: None
    ) -> Tuple[bool, object, Sequence[object]]:
        raise NotImplementedError()


class AbstractTraceCodeAddress(ABC):
    Address: int
    FullMethodName: str


class AbstractTraceCallStack(ABC):
    CodeAddress: AbstractTraceCodeAddress
    Caller: object  # AbstractTraceCallStack


class AbstractTraceEvents(ABC):
    # This is a generic function whose type argument must be provided
    ByEventType: Mapping[Type[AbstractTraceEvent], Callable[[], Iterable[AbstractTraceEvent]]]
    # Trying to directly iterate will fail


class AbstractTraceModuleFiles(ABC):
    @abstractmethod
    def GetType(self) -> Any:
        raise NotImplementedError()


class AbstractTraceModuleFile(ABC):
    pass


class AbstractTraceCodeAddresses(ABC):
    @abstractmethod
    def LookupSymbolsForModule(
        self, reader: AbstractSymbolReader, file: AbstractTraceModuleFile
    ) -> None:
        raise NotImplementedError()


class AbstractTraceLog(ABC):
    @abstractmethod
    def GetCallStackForEvent(self, anEvent: AbstractTraceEvent) -> AbstractTraceCallStack:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def OpenOrConvert(etlOrEtlxFilePath: str) -> object:
        raise NotImplementedError()

    Events: AbstractTraceEvents
    ModuleFiles: AbstractTraceModuleFiles
    CodeAddresses: AbstractTraceCodeAddresses


ServerGcThreadState = int  # an enum


class AbstractStackSourceSample(ABC):
    pass


class AbstractTimeSpan(ABC):
    StartMSec: float
    EndMSec: float
    DurationMSec: float


# StackView class. Currently in Analysis.cs, in the future it will be part
# of TraceEvent.
class AbstractCallTreeNodeBase(ABC):
    Name: str
    InclusiveCount: float
    ExclusiveCount: float
    InclusiveMetricPercent: float
    ExclusiveMetricPercent: float
    FirstTimeRelativeMSec: float
    LastTimeRelativeMSec: float

    @abstractmethod
    def ToString(self) -> str:
        raise NotImplementedError()


class AbstractStackView(ABC):
    @abstractmethod
    def FindNodeByName(self, nodeNamePat: str) -> AbstractCallTreeNodeBase:
        raise NotImplementedError()


# See Analysis.cs


class AbstractHistDict(ABC, Generic[T]):
    def get(self, identifier: int, time_msec: float) -> Optional[T]:
        raise NotImplementedError()


class ThreadIDAndTime(ABC):
    ThreadID: int
    TimeQPC: int


class AbstractIThreadIDToProcessID(ABC):
    def ThreadIDToProcessID(self, threadID: int, timeQPC: int) -> Optional[int]:
        raise NotImplementedError()

    def ProcessIDToThreadIDsAndTimes(self, processID: int) -> Iterable[ThreadIDAndTime]:
        raise NotImplementedError()


class AbstractIProcessIDToProcessName(ABC):
    def ProcessIDToProcessName(self, processID: int, timeQPC: int) -> Optional[str]:
        raise NotImplementedError()


class AbstractTracedProcesses(ABC):
    event_names: Optional[Mapping[str, int]]
    processes: Iterable[AbstractEtlxTraceProcess]  # An IEnumerable, not a sequence.
    thread_id_to_process_id: AbstractIThreadIDToProcessID
    my_thread_id_to_process_id: AbstractIThreadIDToProcessID
    process_id_to_process_name: AbstractIProcessIDToProcessName
    events_time_span: AbstractTimeSpan
    per_heap_history_times: Sequence[float]


class AbstractStackSource(ABC):
    @abstractmethod
    def ForEach(self, callback: AbstractAction[AbstractStackSourceSample]) -> None:
        raise NotImplementedError()


class AbstractTraceEventStackSource(ABC):
    @abstractmethod
    def ForEach(self, action: AbstractAction[AbstractStackSourceSample]) -> None:
        raise NotImplementedError()


class AbstractTimeSpanUtil(ABC):
    @staticmethod
    @abstractmethod
    def FromStartEndMSec(startMSec: float, endMSec: float) -> AbstractTimeSpan:
        raise NotImplementedError()


class AbstractAnalysis(ABC):
    # Result is in milliseconds
    @staticmethod
    @abstractmethod
    def GetLostCpuBreakdownForHeap(
        gc_instance: AbstractTraceGC, heap: AbstractServerGcHistory, markIdleStolen: bool = False
    ) -> AbstractIReadOnlyDictionary[ServerGcThreadState, AbstractTuple[float, float]]:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def GetLostCpuInstancesForHeap(
        gcInstance: AbstractTraceGC,
        heap: AbstractServerGcHistory,
        eventList: AbstractTraceEvents,
        markIdleStolen: bool = False,
    ) -> AbstractIReadOnlyDictionary[ServerGcThreadState, AbstractList[AbstractThreadWorkSpan]]:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def GetGcThreadStateTransitionTimes(
        heap: AbstractServerGcHistory,
    ) -> AbstractList[AbstractTuple[ServerGcThreadState, float]]:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def LoadTraceAndGetStacks(
        traceLog: AbstractTraceLog, symReader: AbstractSymbolReader
    ) -> AbstractTuple[AbstractTraceEventStackSource, int]:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def DoesStackSampleContainFunction(
        stackSource: AbstractTraceEventStackSource,
        sample: AbstractStackSourceSample,
        functionName: str,
    ) -> Optional[int]:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def GetTracedProcesses(
        tracePath: str, collectEventNames: bool, collectPerHeapHistoryTimes: bool
    ) -> AbstractTracedProcesses:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def PrintEvents(
        tracePath: str,
        timeSpan: Optional[AbstractTimeSpan],
        includeRegex: Optional[str],
        excludeRegex: Optional[str],
        threadID: Optional[int],
        maxEvents: Optional[int],
        useTraceLog: bool,
    ) -> None:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def SliceTraceFile(
        inputTracePath: str, outputTracePath: str, timeSpan: AbstractTimeSpan
    ) -> None:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def GetOpenedTraceLog(
        tracePath: str
    ) -> AbstractTraceLog:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def GetSymbolReader(
        logFile: str,
        symPath: str,
    ) -> AbstractSymbolReader:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def GetProcessFullStackSource(
        traceLog: AbstractTraceLog,
        symReader: AbstractSymbolReader,
        processName: str,
    ) -> AbstractStackSource:
        raise NotImplementedError()

    @staticmethod
    @abstractmethod
    def GetFunctionMetricsWithinTimeRange(
        traceLog: AbstractTraceLog,
        symReader: AbstractSymbolReader,
        fullStackSource: AbstractStackSource,
        timeRange: AbstractTimeSpan,
        functionToAnalyze: str,
    ) -> AbstractCallTreeNodeBase:
        raise NotImplementedError()


class AbstractEtlTrace(ABC):
    @abstractmethod
    def __init__(self, name: str, path: str, symbol_path: str) -> None:
        raise NotImplementedError()

    SymPath: str


class AbstractFile(ABC):
    @staticmethod
    @abstractmethod
    def CreateText(x: str) -> AbstractTextWriter:
        raise NotImplementedError()


class AbstractBindingFlags(ABC):
    Instance: int
    NonPublic: int


class AbstractJoinStageOrPhaseInfo(ABC):
    IsEESuspended: bool
    TimeSpan: AbstractTimeSpan
    # Index is a ServerGCState -- see enums.py
    MSecPerState: Sequence[float]
    # Index is a GcJoinPhase -- see enums.py
    MSecPerPhase: Sequence[float]
    DurationMSec: float  # Shorthand for TimeSpan.DurationMSec


class AbstractJoinStageInfo(AbstractJoinStageOrPhaseInfo):
    # A GcJoinStage -- see enums.py
    JoinStage: int


class AbstractJoinPhaseInfo(AbstractJoinStageOrPhaseInfo):
    # A GcJoinPhase -- see enums.py
    JoinPhase: int


class AbstractStolenTimeInstance:
    Span: AbstractGcWorkSpan

    OldThreadID: Optional[int]
    NewThreadID: int
    OldPriority: int
    NewPriority: int
    TimeSpan: AbstractTimeSpan
    DurationMSec: float
    StartTimeMSec: float
    HeapID: int
    State: int  # A ServerGcState
    Stage: int  # GcJoinStage
    Phase: int  # GcJoinPhase
    Processor: int


class AbstractStolenTimeInstanceWithGcNumber:
    GCNumber: int
    Instance: AbstractStolenTimeInstance


# MoreAnalysis.cs
class AbstractPhaseAndStages(ABC):
    Phase: int  # A GcJoinPhase
    Stages: Sequence[int]  # Each is a GcJoinStage


class AbstractJoinInfoForHeap(ABC):
    GCNumber: int
    HeapID: int
    ForegroundThreadID: int
    BackgroundThreadID: int
    ForegroundStages: Sequence[AbstractJoinStageInfo]
    BackgroundStages: Sequence[AbstractJoinStageInfo]
    ForegroundPhases: Sequence[AbstractJoinPhaseInfo]
    WorstStolenTimeInstances: Sequence[AbstractStolenTimeInstance]
    ThreadIDToTotalStolenMSec: AbstractIReadOnlyDictionary[int, float]

    def StagesByPhase(self) -> Sequence[AbstractPhaseAndStages]:
        raise NotImplementedError()

    # state is a ServerGCState
    def TotalMSecInState(self, state: int) -> float:
        raise NotImplementedError()

    # stage is a GcJoinStage
    def TotalMSecInStage(self, stage: int) -> float:
        raise NotImplementedError()

    # phase is a GcJoinPhase
    def TotalMSecInPhase(self, phase: int) -> float:
        raise NotImplementedError()


class AbstractJoinInfoForGC(ABC):
    # TODO: This should not hold on to the GC as that keeps a lot of memory live
    GC: AbstractTraceGC
    Heaps: Sequence[AbstractJoinInfoForHeap]
    # Each is a GcJoinStage -- see enums.py
    ForegroundGCJoinStages: Sequence[int]
    BackgroundGCJoinStages: Sequence[int]
    # Each is a GcJoinPhase -- see enums.py
    ForegroundGCJoinPhases: Sequence[int]
    WorstStolenTimeInstances: Sequence[AbstractStolenTimeInstance]
    ThreadIDToTotalStolenMSec: AbstractIReadOnlyDictionary[int, float]

    def IsEESuspendedForForegroundStage(self, stage_index: int) -> bool:
        raise NotImplementedError()

    def IsEESuspendedForForegroundPhase(self, phase_index: int) -> bool:
        raise NotImplementedError()

    def IsEESuspendedForBackgroundStage(self, stage_index: int) -> bool:
        raise NotImplementedError()

    def ForegroundStagesByPhase(self) -> Sequence[AbstractPhaseAndStages]:
        raise NotImplementedError()

    def TimeSpanForForegroundStage(self, stage_index: int) -> AbstractTimeSpan:
        raise NotImplementedError()

    def TimeSpanForForegroundPhase(self, phase_index: int) -> AbstractTimeSpan:
        raise NotImplementedError()

    def TimeSpanForBackgroundStage(self, stage_index: int) -> AbstractTimeSpan:
        raise NotImplementedError()

    def TimeSpanForAllBackgroundStages(self) -> AbstractTimeSpan:
        raise NotImplementedError()


class AbstractResult(ABC, Generic[E, T]):
    IsOK: bool
    AsOK: T
    AsErr: E


def cs_result_to_result(r: AbstractResult[E, T]) -> Result[E, T]:
    if r.IsOK:
        return Ok(r.AsOK)
    else:
        return Err(r.AsErr)


class AbstractWorstJoinInstance(ABC):
    GCNumber: int
    HeapID: int
    Join: AbstractJoinStageInfo

    TimeSpan: AbstractTimeSpan


class AbstractJoinInfoForProcess(ABC):
    GCs: Sequence[AbstractResult[str, AbstractJoinInfoForGC]]
    WorstStolenTimeInstances: Sequence[AbstractStolenTimeInstanceWithGcNumber]
    WorstForegroundJoins: Sequence[AbstractWorstJoinInstance]
    ThreadIDToTotalStolenMSec: AbstractIReadOnlyDictionary[int, float]


class AbstractJoinAnalysis(ABC):
    @staticmethod
    def AnalyzeAllGcs(
        gcs: Sequence[AbstractTraceGC], strict: int
    ) -> AbstractResult[str, AbstractJoinInfoForProcess]:
        raise NotImplementedError()

    @staticmethod
    def AnalyzeSingleGc(gc: AbstractTraceGC) -> AbstractResult[str, AbstractJoinInfoForGC]:
        raise NotImplementedError()
