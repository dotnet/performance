# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from abc import ABC, abstractmethod
from dataclasses import dataclass
from enum import Enum
from functools import reduce
from math import isnan
from pathlib import Path
from statistics import mean, median, stdev
from typing import (
    Any,
    Callable,
    cast,
    Dict,
    FrozenSet,
    Iterable,
    Mapping,
    Optional,
    Sequence,
    Tuple,
    TypeVar,
    Union,
)

from result import Err, Ok, Result

from ..commonlib.bench_file import TestResult, TestRunStatus
from ..commonlib.collection_util import count, empty_mapping, is_empty, map_to_mapping
from ..commonlib.document import Cell
from ..commonlib.frozen_dict import FrozenDict
from ..commonlib.option import map_option, non_null
from ..commonlib.result_utils import all_non_err, fn_to_ok, flat_map_ok, map_ok, unwrap
from ..commonlib.score_spec import ScoreElement, ScoreSpec
from ..commonlib.type_utils import check_cast, enum_value, E, T, U, with_slots
from ..commonlib.util import (
    bytes_to_mb,
    float_to_str_smaller,
    get_or_did_you_mean,
    get_percent,
    mb_to_gb,
    msec_to_seconds,
    remove_extension,
    stdev_frac,
)

from .clr import Clr
from .clr_types import (
    AbstractGCPerHeapHistory,
    AbstractGCPerHeapHistoryGenData,
    AbstractGCStats,
    AbstractIProcessIDToProcessName,
    AbstractIThreadIDToProcessID,
    AbstractJoinInfoForProcess,
    AbstractJoinInfoForGC,
    AbstractJoinInfoForHeap,
    AbstractServerGcHistory,
    AbstractTimeSpan,
    AbstractTraceGC,
    AbstractTraceProcess,
    AbstractTraceLoadedDotNetRuntime,
)
from .enums import (
    EMPTY_GC_GLOBAL_MECHANISMS,
    GCGlobalMechanisms,
    gc_heap_compact_reason,
    gc_heap_expand_mechanism,
    gc_reason,
    GCType,
    Gens,
    invert_gc_global_mechanisms,
    MarkRootType,
    StartupFlags,
    union_gc_global_mechanisms,
)


@with_slots
@dataclass(frozen=True)
class ValueAndErrors:
    value: float
    err_neg: float
    err_pos: float

    def to_tuple(self) -> Tuple[float, float, float]:
        return self.value, self.err_neg, self.err_pos


class SpecialSampleKind(Enum):
    mean = 0
    median = 1
    min = 2
    max = 3


SampleKind = Union[int, SpecialSampleKind]

SAMPLE_KIND_DOC = """
When multiple iterations of a test were run, what to statistic as the representative sample.
If an integer, uses the nth iteration.
""".strip()

RUN_METRICS_DOC = """
Metrics applying to entire test run.
See `metrics.md` for a list.
"""
SINGLE_GC_METRICS_DOC = """
Metrics applying to each individual GC.
See `metrics.md` for a list.
"""
SINGLE_HEAP_METRICS_DOC = """
Metrics applying to each individual heap within each individual GC.
See `metrics.md` for a list.
"""


@with_slots
@dataclass(frozen=True)
class MetricValue:
    all_samples: Sequence[float]
    n_samples: float
    # Determined by sample_kind
    sample: float
    stdev: float
    median: float
    min: float
    max: float

    def __post_init__(self) -> None:
        assert self.min in self.all_samples and self.max in self.all_samples

    def sample_and_errors(self) -> ValueAndErrors:
        return ValueAndErrors(self.sample, self.sample - self.min, self.max - self.sample)

    @property
    def stdev_frac(self) -> float:
        return stdev_frac(self.stdev, self.sample)

    def cells_sample_stdev_pct(self) -> Sequence[Cell]:
        return (
            Cell(self.sample),
            Cell(
                float_to_str_smaller(self.stdev_frac * 100),
                color="red" if self.stdev_frac > 0.1 else None,
            ),
        )


# When we fail to get a metric, we use Err.
# This is because we want to display the failure but continue to show other metrics,
# rather than exiting the program immediately with an exception.
Failable = Result[str, T]
# Allowed kinds of metric values. Note all of these can convert to float.
AnyValue = Union[bool, int, float]
FailableValue = Failable[AnyValue]
FailableBool = Failable[bool]
FailableInt = Failable[int]
FailableFloat = Failable[float]
FailableValues = Failable[Sequence[FailableValue]]
FailableMetricValue = Failable[MetricValue]


def _take_sample(values: Sequence[AnyValue], sample_kind: SampleKind) -> AnyValue:
    if isinstance(sample_kind, int):
        return values[sample_kind]
    else:
        fn: Callable[[Sequence[float]], float]
        if sample_kind == SpecialSampleKind.mean:
            fn = mean
        elif sample_kind == SpecialSampleKind.median:
            fn = median
        elif sample_kind == SpecialSampleKind.min:
            fn = min
        elif sample_kind == SpecialSampleKind.max:
            fn = max
        else:
            raise Exception(sample_kind)
        return fn(values)


def metric_value_of(
    values: Sequence[FailableValue], sample_kind: SampleKind
) -> FailableMetricValue:
    assert not is_empty(values)
    return flat_map_ok(
        all_non_err(values),
        lambda vs: Ok(
            MetricValue(
                all_samples=vs,
                n_samples=len(vs),
                sample=_take_sample(vs, sample_kind),
                stdev=0 if len(vs) == 1 else stdev(vs),
                median=median(vs),
                min=min(vs),
                max=max(vs),
            )
        ),
    )


# Maps event name to its frequency
EventNames = Mapping[str, int]


@with_slots
@dataclass(frozen=True)
class ProcessInfo:
    event_names: Optional[EventNames]
    # Not necessarily the name of the process,
    # since we may run two tests with the same process name and want to tell them apart
    name: str
    trace_path: Path
    process: AbstractTraceProcess
    mang: AbstractTraceLoadedDotNetRuntime
    all_gcs_including_incomplete: Sequence[AbstractTraceGC]
    gcs: Sequence[AbstractTraceGC]
    stats: AbstractGCStats
    # Note: can't use process.StartTime and EndTime properties
    # as those are just 1/1/0001 12:00:00 AM
    events_time_span: Optional[AbstractTimeSpan]
    per_heap_history_times: Optional[Sequence[float]]

    @property
    def id(self) -> int:
        return self.process.ProcessID

    @property
    def startup_flags(self) -> StartupFlags:
        return StartupFlags(self.mang.StartupFlags)

    @property
    def uses_server_gc(self) -> Optional[bool]:
        i = self.stats.IsServerGCUsed
        if i == -1:
            return None
        elif i == 0:
            return False
        else:
            return True


class MetricType(Enum):
    bool = 0
    float = 1


class Better(Enum):
    """Is it better for a metric to be lower or higher?"""

    less = 0
    greater = 1


class MetricBase(ABC):
    @property
    @abstractmethod
    def name(self) -> str:
        raise NotImplementedError()

    @property
    @abstractmethod
    def type(self) -> MetricType:
        raise NotImplementedError()

    @property
    @abstractmethod
    def is_from_test_status(self) -> bool:
        raise NotImplementedError()

    @property
    @abstractmethod
    def doc(self) -> Optional[str]:
        raise NotImplementedError()

    @property
    @abstractmethod
    def is_aggregate(self) -> bool:
        raise NotImplementedError

    @property
    @abstractmethod
    def do_not_use_scientific_notation(self) -> bool:
        raise NotImplementedError()

    @property
    @abstractmethod
    def better(self) -> Better:
        raise NotImplementedError()


@with_slots
@dataclass(frozen=True)
class NamedMetricBase(MetricBase):
    name: str
    type: MetricType = MetricType.float
    doc: Optional[str] = None
    # True for a metric that aggregates others, e.g. a run metric aggregating single GCs
    is_aggregate: bool = False
    do_not_use_scientific_notation: bool = False
    is_from_test_status: bool = False
    better: Better = Better.less

    def __post_init__(self) -> None:
        # Apparently the base class must have this or it won't be called on subclasses
        pass

    def __eq__(self, other: object) -> bool:
        assert self is other or other is None or self.name != check_cast(self.__class__, other).name
        return self is other

    def __lt__(self, other: object) -> bool:
        return self.name < check_cast(self.__class__, other).name

    def __hash__(self) -> int:
        return hash(self.name)


TMetric = TypeVar("TMetric", bound=MetricBase)
TMetricB = TypeVar("TMetricB", bound=MetricBase)
TNamedMetric = TypeVar("TNamedMetric", bound=NamedMetricBase)


class NamedRunMetric(NamedMetricBase):
    """
    Statistic that applies to a test run as a whole.
    Contrast with SingleGcStat.
    """

    def __post_init__(self) -> None:
        super().__post_init__()
        assert self.name not in NAME_TO_RUN_METRIC, f"Already exists a metric {self.name}"
        NAME_TO_RUN_METRIC[self.name] = self


@with_slots
@dataclass(frozen=True)
class ScoreRunMetric(MetricBase):
    name: str
    spec: ScoreSpec = empty_mapping()  # TODO: shouldn't have a default

    @property
    # @overrides
    def type(self) -> MetricType:
        return MetricType.float

    @property
    # @overrides
    def is_from_test_status(self) -> bool:
        return False

    @property
    # @overrides
    def doc(self) -> Optional[str]:
        return None

    @property
    # @overrides
    def is_aggregate(self) -> bool:
        return True

    @property
    # @overrides
    def do_not_use_scientific_notation(self) -> bool:
        return False

    @property
    # @overrides
    def better(self) -> Better:
        return Better.less


RunMetric = Union[NamedRunMetric, ScoreRunMetric]
# Can't use ScoreRunMetric or ScoreElement as those are frozen. (Must be frozen to be hashable.)
# Tuples are (weight, par) pairs.
SerializedRunMetric = Union[str, Tuple[str, Mapping[str, Tuple[float, Optional[float]]]]]


NAME_TO_RUN_METRIC: Dict[str, NamedRunMetric] = {}


def run_metric_must_exist_for_name(name: str) -> NamedRunMetric:
    return get_or_did_you_mean(NAME_TO_RUN_METRIC, name, "run metric")


def serialize_run_metric(r: RunMetric) -> SerializedRunMetric:
    if isinstance(r, NamedRunMetric):
        return r.name
    else:
        return r.name, {k: (v.weight, v.par) for k, v in r.spec.items()}


def deserialize_run_metric(m: SerializedRunMetric) -> RunMetric:
    if isinstance(m, str):
        return run_metric_must_exist_for_name(m)
    else:
        return ScoreRunMetric(m[0], FrozenDict((k, ScoreElement(*v)) for k, v in m[1].items()))


class SingleGCMetric(NamedMetricBase):
    """Statistic that applies to a single invocation of the GC."""

    def __post_init__(self) -> None:
        super().__post_init__()
        assert self.name not in NAME_TO_SINGLE_GC_METRIC, f"Duplicate SingleGcMetric {self.name}"
        NAME_TO_SINGLE_GC_METRIC[self.name] = self


NAME_TO_SINGLE_GC_METRIC: Dict[str, SingleGCMetric] = {}


def single_gc_metric_must_exist_for_name(name: str) -> SingleGCMetric:
    return get_or_did_you_mean(NAME_TO_SINGLE_GC_METRIC, name, "single-gc metric")


class SingleHeapMetric(NamedMetricBase):
    """Statistic for a single heap from a single GC"""

    def __post_init__(self) -> None:
        super().__post_init__()
        assert (
            self.name not in NAME_TO_SINGLE_HEAP_METRIC
        ), f"Duplicate SingleHeapMetric {self.name}"
        NAME_TO_SINGLE_HEAP_METRIC[self.name] = self


NAME_TO_SINGLE_HEAP_METRIC: Dict[str, SingleHeapMetric] = {}


def single_heap_metric_must_exist_for_name(name: str) -> SingleHeapMetric:
    return get_or_did_you_mean(NAME_TO_SINGLE_HEAP_METRIC, name, "single-heap metric")


# Using sequence to preserve ordering. FrozenSet has non-deterministic ordering.
RunMetrics = Sequence[RunMetric]
SingleGCMetrics = Sequence[SingleGCMetric]
SingleHeapMetrics = Sequence[SingleHeapMetric]


MetricValuesForSingleIteration = Mapping[RunMetric, FailableValue]
# Will be a single CouldNotGetValue if the test as a whole failed or has no GCs.
# Else will have CouldNotGetValue for each individual metric that failed.
MaybeMetricValuesForSingleIteration = Failable[MetricValuesForSingleIteration]
MetricStatisticsFromAllIterations = Mapping[RunMetric, FailableMetricValue]
MaybeMetricStatisticsFromAllIterations = Failable[MetricStatisticsFromAllIterations]


@with_slots
@dataclass(frozen=True)
class ProcessedGenData:
    _g: AbstractGCPerHeapHistoryGenData

    @property
    def surv_rate(self) -> int:
        return self._g.SurvRate

    @property
    def pinned_surv(self) -> int:
        return self._g.PinnedSurv

    @property
    def non_pinned_surv(self) -> int:
        return self._g.NonePinnedSurv

    @property
    def non_free_size_before(self) -> int:
        return self.size_before - self.free_list_space_before - self.free_obj_space_before

    @property
    def non_free_size_after(self) -> int:
        return self.size_after - self.free_list_space_after - self.free_obj_space_after

    @property
    def size_before(self) -> int:
        return self._g.SizeBefore

    @property
    def size_after(self) -> int:
        return self._g.SizeAfter

    @property
    def obj_space_before(self) -> int:
        return self._g.ObjSpaceBefore

    @property
    def fragmentation(self) -> int:
        return self._g.Fragmentation

    @property
    def obj_size_after(self) -> int:
        return self._g.ObjSizeAfter

    @property
    def free_list_space_before(self) -> int:
        return self._g.FreeListSpaceBefore

    @property
    def free_obj_space_before(self) -> int:
        return self._g.FreeObjSpaceBefore

    @property
    def free_list_space_after(self) -> int:
        return self._g.FreeListSpaceAfter

    @property
    def free_obj_space_after(self) -> int:
        return self._g.FreeObjSpaceAfter

    @property
    def in_bytes(self) -> int:
        return self._g.In

    @property
    def in_mb(self) -> float:
        return bytes_to_mb(self.in_bytes)

    @property
    def out_bytes(self) -> int:
        return self._g.Out

    @property
    def out_mb(self) -> float:
        return bytes_to_mb(self.out_bytes)

    @property
    def budget(self) -> int:
        return self._g.Budget


@with_slots
@dataclass(frozen=True)
class ProcessedHeap:
    gc: "ProcessedGC"
    index: int
    per_heap_history: Result[str, AbstractGCPerHeapHistory]
    # Missing for BGCs (don't know why...)
    server_gc_history: Result[str, AbstractServerGcHistory]
    ## Index is a member of MarkRootType enum
    _mark_times: Failable[Sequence[float]]
    _mark_promoted: Failable[Sequence[float]]
    join_info: Result[str, AbstractJoinInfoForHeap]

    @property
    def clr(self) -> Clr:
        return self.gc.clr

    def metric(self, metric: SingleHeapMetric) -> FailableValue:
        from .single_heap_metrics import get_single_heap_stat

        return get_single_heap_stat(self, metric)

    def gen(self, gen: Gens) -> ProcessedGenData:
        return unwrap(self.gen_result(gen))

    def gen_result(self, gen: Gens) -> Result[str, ProcessedGenData]:
        return map_ok(
            self.per_heap_history, lambda phh: ProcessedGenData(phh.GenData[enum_value(gen)])
        )

    @property
    def gens(self) -> Result[str, Sequence[ProcessedGenData]]:
        return map_ok(
            self.per_heap_history,
            lambda phh: [ProcessedGenData(phh.GenData[enum_value(gen)]) for gen in Gens],
        )

    @property
    def FreeListAllocated(self) -> Result[str, int]:
        return map_ok(self.per_heap_history, lambda phh: phh.FreeListAllocated)

    @property
    def FreeListRejected(self) -> Result[str, int]:
        return map_ok(self.per_heap_history, lambda phh: phh.FreeListRejected)

    def mark_time(self, mark_type: MarkRootType) -> FailableFloat:
        return map_ok(self._mark_times, lambda m: m[enum_value(mark_type)])

    def mark_promoted(self, mark_type: MarkRootType) -> FailableFloat:
        return map_ok(self._mark_promoted, lambda m: m[enum_value(mark_type)])

    @property
    def TotalMarkMSec(self) -> FailableFloat:
        return map_ok(self._mark_times, sum)

    @property
    def TotalMarkPromoted(self) -> FailableFloat:
        return map_ok(self._mark_promoted, sum)

    # TODO: better return type
    @property
    def compact_mechanisms(self) -> Result[str, int]:
        return map_ok(self.per_heap_history, lambda phh: phh.CompactMechanisms)

    # TODO: better return type
    @property
    def expand_mechanisms(self) -> Result[str, int]:
        return map_ok(self.per_heap_history, lambda phh: phh.ExpandMechanisms)


def _fixup_mb(fake_mb: float) -> float:
    return bytes_to_mb(fake_mb * 1_000_000)


@with_slots
@dataclass(frozen=True)
class GenInfoGetter:
    _gc: "ProcessedGC"
    _gen: Gens

    @property
    def _trace_gc(self) -> AbstractTraceGC:
        return self._gc.trace_gc

    @property
    def _gen_value(self) -> int:
        return enum_value(self._gen)

    @property
    def UserAllocatedMB(self) -> float:
        # Amount is already in MB
        return self._trace_gc.UserAllocated[self._gen_value]

    @property
    def SizeBeforeMB(self) -> float:
        return _fixup_mb(self._trace_gc.GenSizeBeforeMB[self._gen_value])

    @property
    def SizeAfterMB(self) -> float:
        return _fixup_mb(self._trace_gc.GenSizeAfterMB(self._gen_value))

    @property
    def SurvivalPct(self) -> FailableFloat:
        pct = self._trace_gc.SurvivalPercent(self._gen_value)
        if isnan(pct):
            return Err(f"{Gens(self._gen_value).name} not collected?")
        else:
            assert 0 <= pct <= 100
            return Ok(pct)

    @property
    def FragmentationMB(self) -> float:
        return _fixup_mb(self._trace_gc.GenFragmentationMB(self._gen_value))

    @property
    def FragmentationPct(self) -> float:
        return self._trace_gc.GenFragmentationPercent(self._gen_value)

    @property
    def InMB(self) -> float:
        return _fixup_mb(self._trace_gc.GenInMB(self._gen_value))

    @property
    def PromotedMB(self) -> float:
        return _fixup_mb(self._trace_gc.GenPromotedMB(self._gen_value))

    @property
    def BudgetMB(self) -> float:
        return _fixup_mb(self._trace_gc.GenBudgetMB(self._gen_value))

    def ObjSizeAfterMB(self) -> float:
        return _fixup_mb(self._trace_gc.GenObjSizeAfterMB(self._gen_value))

    @property
    def FreeListSpaceBeforeMB(self) -> float:
        return bytes_to_mb(sum(hp.gen(self._gen).free_list_space_before for hp in self._gc.heaps))

    @property
    def FreeListSpaceAfterMB(self) -> float:
        return bytes_to_mb(sum(hp.gen(self._gen).free_list_space_after for hp in self._gc.heaps))

    @property
    def FreeObjSpaceBeforeMB(self) -> float:
        return bytes_to_mb(sum(hp.gen(self._gen).free_obj_space_before for hp in self._gc.heaps))

    @property
    def FreeObjSpaceAfterMB(self) -> float:
        return bytes_to_mb(sum(hp.gen(self._gen).free_obj_space_after for hp in self._gc.heaps))


@with_slots
# frozen=False so heaps can be set lazily
@dataclass(frozen=False)
class ProcessedGC:
    proc: "ProcessedTrace"
    index: int
    trace_gc: AbstractTraceGC
    join_info: Result[str, AbstractJoinInfoForGC]
    heaps: Sequence[ProcessedHeap]

    @property
    def clr(self) -> Clr:
        return self.proc.clr

    @property
    def SuspendDurationMSec(self) -> float:
        return self.trace_gc.SuspendDurationMSec

    def metric(self, single_gc_metric: SingleGCMetric) -> FailableValue:
        from .single_gc_metrics import get_single_gc_stat

        return get_single_gc_stat(self.proc, self.proc.gcs, self.index, single_gc_metric)

    def metric_from_name(self, name: str) -> FailableValue:
        from .parse_metrics import parse_single_gc_metric_arg

        return self.metric(parse_single_gc_metric_arg(name))

    def unwrap_metric_from_name(self, name: str) -> AnyValue:
        return unwrap(self.metric_from_name(name))

    @property
    def Number(self) -> int:
        return self.trace_gc.Number

    @property
    def Generation(self) -> Gens:
        return Gens(self.trace_gc.Generation)

    def collects_generation(self, gen: Gens) -> bool:
        return {
            Gens.Gen0: True,
            Gens.Gen1: self.Generation != Gens.Gen0,
            Gens.Gen2: self.Generation == Gens.Gen2,
            Gens.GenLargeObj: self.Generation == Gens.Gen2,
        }[gen]

    @property
    def AllocedSinceLastGCMB(self) -> float:
        return self.trace_gc.AllocedSinceLastGCMB

    @property
    def AllocRateMBSec(self) -> float:
        return self.trace_gc.AllocRateMBSec

    @property
    def BGCFinalPauseMSec(self) -> float:
        return self.trace_gc.BGCFinalPauseMSec

    @property
    def Type(self) -> GCType:
        return GCType(self.trace_gc.Type)

    @property
    def DurationMSec(self) -> float:
        return self.trace_gc.DurationMSec

    @property
    def DurationSeconds(self) -> float:
        return msec_to_seconds(self.DurationMSec)

    @property
    def DurationSinceLastRestartMSec(self) -> float:
        return self.trace_gc.DurationSinceLastRestartMSec

    @property
    def GCCpuMSec(self) -> float:
        return self.trace_gc.GCCpuMSec

    def gen_info(self, gen: Gens) -> GenInfoGetter:
        return GenInfoGetter(self, gen)

    @property
    def gen0(self) -> GenInfoGetter:
        return self.gen_info(Gens.Gen0)

    @property
    def gen1(self) -> GenInfoGetter:
        return self.gen_info(Gens.Gen1)

    @property
    def gen2(self) -> GenInfoGetter:
        return self.gen_info(Gens.Gen2)

    @property
    def loh(self) -> GenInfoGetter:
        return self.gen_info(Gens.GenLargeObj)

    @property
    def Gen0UserAllocatedMB(self) -> float:
        return self.gen0.UserAllocatedMB

    # User can't allocate directly to gen1 or gen2

    @property
    def LOHUserAllocatedMB(self) -> float:
        return self.loh.UserAllocatedMB

    @property
    def Gen0SizeBeforeMB(self) -> float:
        return self.gen0.SizeBeforeMB

    @property
    def Gen1SizeBeforeMB(self) -> float:
        return self.gen1.SizeBeforeMB

    @property
    def Gen2SizeBeforeMB(self) -> float:
        return self.gen2.SizeBeforeMB

    @property
    def LOHSizeBeforeMB(self) -> float:
        return self.loh.SizeBeforeMB

    @property
    def Gen0BudgetMB(self) -> float:
        return self.gen0.BudgetMB

    @property
    def Gen1BudgetMB(self) -> float:
        return self.gen1.BudgetMB

    @property
    def Gen2BudgetMB(self) -> float:
        return self.gen2.BudgetMB

    @property
    def LOHBudgetMB(self) -> float:
        return self.loh.BudgetMB

    @property
    def Gen0SizeAfterMB(self) -> float:
        return self.gen0.SizeAfterMB

    @property
    def Gen1SizeAfterMB(self) -> float:
        return self.gen1.SizeAfterMB

    @property
    def Gen2SizeAfterMB(self) -> float:
        return self.gen2.SizeAfterMB

    @property
    def LOHSizeAfterMB(self) -> float:
        return self.loh.SizeAfterMB

    @property
    def Gen0FreeListSpaceBeforeMB(self) -> float:
        return self.gen0.FreeListSpaceBeforeMB

    @property
    def Gen1FreeListSpaceBeforeMB(self) -> float:
        return self.gen1.FreeListSpaceBeforeMB

    @property
    def Gen2FreeListSpaceBeforeMB(self) -> float:
        return self.gen2.FreeListSpaceBeforeMB

    @property
    def LOHFreeListSpaceBeforeMB(self) -> float:
        return self.loh.FreeListSpaceBeforeMB

    @property
    def Gen0FreeListSpaceAfterMB(self) -> float:
        return self.gen0.FreeListSpaceAfterMB

    @property
    def Gen1FreeListSpaceAfterMB(self) -> float:
        return self.gen1.FreeListSpaceAfterMB

    @property
    def Gen2FreeListSpaceAfterMB(self) -> float:
        return self.gen2.FreeListSpaceAfterMB

    @property
    def LOHFreeListSpaceAfterMB(self) -> float:
        return self.loh.FreeListSpaceAfterMB

    @property
    def Gen0FreeObjSpaceBeforeMB(self) -> float:
        return self.gen0.FreeObjSpaceBeforeMB

    @property
    def Gen1FreeObjSpaceBeforeMB(self) -> float:
        return self.gen1.FreeObjSpaceBeforeMB

    @property
    def Gen2FreeObjSpaceBeforeMB(self) -> float:
        return self.gen2.FreeObjSpaceBeforeMB

    @property
    def LOHFreeObjSpaceBeforeMB(self) -> float:
        return self.loh.FreeObjSpaceBeforeMB

    @property
    def Gen0FreeObjSpaceAfterMB(self) -> float:
        return self.gen0.FreeObjSpaceAfterMB

    @property
    def Gen1FreeObjSpaceAfterMB(self) -> float:
        return self.gen1.FreeObjSpaceAfterMB

    @property
    def Gen2FreeObjSpaceAfterMB(self) -> float:
        return self.gen2.FreeObjSpaceAfterMB

    @property
    def LOHFreeObjSpaceAfterMB(self) -> float:
        return self.loh.FreeObjSpaceAfterMB

    @property
    def HeapSizeBeforeMB(self) -> float:
        return self.trace_gc.HeapSizeBeforeMB

    @property
    def HeapSizeAfterMB(self) -> float:
        return self.trace_gc.HeapSizeAfterMB

    @property
    def HeapSizePeakMB(self) -> float:
        return self.trace_gc.HeapSizePeakMB

    @property
    def PinnedObjectSizes(self) -> int:
        return self.trace_gc.GetPinnedObjectSizes()

    @property
    def PinnedObjectPercentage(self) -> Optional[float]:
        pct = self.trace_gc.GetPinnedObjectPercentage()
        return None if pct == -1 else pct

    @property
    def TotalGCTime(self) -> Optional[float]:
        t = self.trace_gc.GetTotalGCTime()
        return None if t == 0 else t

    @property
    def PromotedMB(self) -> float:
        return self.trace_gc.PromotedMB

    @property
    def RatioPeakAfter(self) -> float:
        return self.trace_gc.RatioPeakAfter

    @property
    def suspend_duration_msec(self) -> float:
        return self.trace_gc.SuspendDurationMSec

    @property
    def PauseStartRelativeMSec(self) -> float:
        return self.trace_gc.PauseStartRelativeMSec

    @property
    def SuspendToGCStartMSec(self) -> float:
        return self.trace_gc.StartRelativeMSec - self.trace_gc.PauseStartRelativeMSec

    @property
    def PauseDurationMSec(self) -> float:
        return self.trace_gc.PauseDurationMSec

    @property
    def PromotedMBPerSec(self) -> float:
        return self.PromotedMB / self.DurationSeconds

    @property
    def PromotedGBPerSec(self) -> float:
        return mb_to_gb(self.PromotedMB) / self.DurationSeconds

    @property
    def PauseTimePercentageSinceLastGC(self) -> float:
        return self.trace_gc.PauseTimePercentageSinceLastGC

    @property
    def ProcessCpuMSec(self) -> float:
        return self.trace_gc.ProcessCpuMSec

    @property
    def StartRelativeMSec(self) -> float:
        return self.trace_gc.StartRelativeMSec

    @property
    def EndRelativeMSec(self) -> float:
        return self.StartRelativeMSec + self.DurationMSec

    @property
    def reason(self) -> gc_reason:
        return gc_reason(self.trace_gc.Reason)

    @property
    def PercentTimeInGC(self) -> float:
        return self.trace_gc.PercentTimeInGC

    @property
    def IsEphemeral(self) -> bool:
        return self.Generation in (Gens.Gen0, Gens.Gen1)

    @property
    def IsGen0(self) -> bool:
        return self.Generation == Gens.Gen0

    @property
    def IsGen1(self) -> bool:
        return self.Generation == Gens.Gen1

    @property
    def IsGen2(self) -> bool:
        return self.Generation == Gens.Gen2

    @property
    def IsBlockingGen2(self) -> bool:
        return self.IsGen2 and self.IsNonConcurrent

    @property
    def PctReductionInHeapSize(self) -> float:
        return get_percent(1.0 - (self.HeapSizeAfterMB / self.HeapSizeBeforeMB))

    @property
    def IsBackground(self) -> bool:
        res = self.Type == GCType.BackgroundGC
        if res:
            assert self.Generation == Gens.Gen2
        return res

    @property
    def IsForeground(self) -> bool:
        return self.Type == GCType.ForegroundGC

    @property
    def IsNonConcurrent(self) -> bool:
        # TODO: is this just not is_concurrent?
        return self.Type == GCType.NonConcurrentGC

    # TODO: is this just is_background?
    @property
    def IsConcurrent(self) -> FailableBool:
        res = self.has_mechanisms(lambda m: m.concurrent)
        if res.is_ok():
            is_c = res.unwrap()
            assert is_c == self.IsBackground
            if is_c:
                # Only gen2 gcs are concurrent
                assert self.Generation == Gens.Gen2
        return res

    def has_mechanisms(self, cb: Callable[[GCGlobalMechanisms], bool]) -> FailableBool:
        if self.trace_gc.GlobalHeapHistory is None:
            return Err("null GlobalHeapHistory")
        else:
            return Ok(cb(GCGlobalMechanisms(self.trace_gc.GlobalHeapHistory.GlobalMechanisms)))

    # WARN: Does *NOT* include LOH compaction!
    @property
    def UsesCompaction(self) -> FailableBool:
        return self.has_mechanisms(lambda m: m.compaction)

    @property
    def UsesPromotion(self) -> FailableBool:
        return self.has_mechanisms(lambda m: m.promotion)

    @property
    def UsesDemotion(self) -> FailableBool:
        return self.has_mechanisms(lambda m: m.demotion)

    @property
    def UsesCardBundles(self) -> FailableBool:
        return self.has_mechanisms(lambda m: m.cardbundles)

    @property
    def UsesElevation(self) -> FailableBool:
        return self.has_mechanisms(lambda m: m.elevation)

    @property
    def UsesLOHCompaction(self) -> FailableBool:
        # Not implemented on the GC side
        return Err("<not implemented>")
        # return has_mechanisms(gc, lambda m: m.loh_compaction)

    @property
    def HeapCount(self) -> int:
        return len(self.heaps)

    def total_bytes_before(self, gen: Gens) -> Result[str, int]:
        return map_ok(
            all_non_err([hp.gen_result(gen) for hp in self.heaps]),
            lambda gen_datas: sum(gd.non_free_size_before for gd in gen_datas),
        )

    def total_bytes_after(self, gen: Gens) -> Result[str, int]:
        return map_ok(
            all_non_err([hp.gen_result(gen) for hp in self.heaps]),
            lambda gen_datas: sum(gd.non_free_size_after for gd in gen_datas),
        )

    # TODO: revisit info for BGCs
    # TODO: condemned reason
    # TODO: suspend_duration_msec
    # TODO: GlobalHeapHistory


PerHeapGetter = Callable[[ProcessedHeap], FailableValue]


@with_slots
@dataclass(frozen=True)
class ThreadToProcessToName:
    # Maps process ID to name
    thread_id_to_process_id: AbstractIThreadIDToProcessID
    process_id_to_name: AbstractIProcessIDToProcessName

    def get_process_id_for_thread_id(self, thread_id: int, time_msec: float) -> Optional[int]:
        pid = self.thread_id_to_process_id.ThreadIDToProcessID(thread_id, _msec_to_qpc(time_msec))
        return None if pid == -1 else pid

    def get_process_name_for_process_id(self, process_id: int, time_msec: float) -> Optional[str]:
        # TODO: Use a HistDict, then use time_msec
        res = self.process_id_to_name.ProcessIDToProcessName(process_id, _msec_to_qpc(time_msec))
        assert res != ""
        if res is None:
            # TODO: only on windows
            return {0: "Idle", 4: "System"}.get(process_id)
        else:
            return res


def _msec_to_qpc(time_msec: float) -> int:
    return int(time_msec * 10000)


# Used to analyze the *kinds* of gcs we see. Don't care how much.
@with_slots
@dataclass(frozen=False)  # Must be unfrozen to serialize
class MechanismsAndReasons:
    types: FrozenSet[GCType]
    mechanisms: GCGlobalMechanisms
    reasons: FrozenSet[gc_reason]
    heap_expand: FrozenSet[gc_heap_expand_mechanism]
    heap_compact: FrozenSet[gc_heap_compact_reason]

    def is_empty(self) -> bool:
        return (
            is_empty(self.types)
            and self.mechanisms == EMPTY_GC_GLOBAL_MECHANISMS
            and is_empty(self.reasons)
            and is_empty(self.heap_expand)
            and is_empty(self.heap_compact)
        )

    def to_strs(self) -> Sequence[str]:
        return (
            *(str(t) for t in sorted(self.types)),
            *self.mechanisms.names(),
            *(str(r) for r in sorted(self.reasons)),
            *(str(e) for e in sorted(self.heap_expand)),
            *(str(c) for c in sorted(self.heap_compact)),
        )


EMPTY_MECHANISMS_AND_REASONS = MechanismsAndReasons(
    types=frozenset(),
    mechanisms=EMPTY_GC_GLOBAL_MECHANISMS,
    reasons=frozenset(),
    heap_expand=frozenset(),
    heap_compact=frozenset(),
)


def invert_mechanisms(m: MechanismsAndReasons) -> MechanismsAndReasons:
    return MechanismsAndReasons(
        types=frozenset(GCType) - m.types,
        mechanisms=invert_gc_global_mechanisms(m.mechanisms),
        reasons=frozenset(gc_reason) - m.reasons,
        heap_expand=frozenset(gc_heap_expand_mechanism) - m.heap_expand,
        heap_compact=frozenset(gc_heap_compact_reason) - m.heap_compact,
    )


def union_all_mechanisms(i: Iterable[MechanismsAndReasons]) -> MechanismsAndReasons:
    return reduce(union_mechanisms, i, EMPTY_MECHANISMS_AND_REASONS)


def union_mechanisms(a: MechanismsAndReasons, b: MechanismsAndReasons) -> MechanismsAndReasons:
    return MechanismsAndReasons(
        types=a.types | b.types,
        mechanisms=union_gc_global_mechanisms(a.mechanisms, b.mechanisms),
        reasons=a.reasons | b.reasons,
        heap_expand=a.heap_expand | b.heap_expand,
        heap_compact=a.heap_compact | b.heap_compact,
    )


ProcessQuery = Optional[Sequence[str]]


@with_slots
# frozen=False so we can set GCs lazily
@dataclass(frozen=False)
class ProcessedTrace:
    clr: Clr
    test_result: TestResult
    test_status: Optional[TestRunStatus]
    process_info: Optional[ProcessInfo]
    process_names: ThreadToProcessToName
    # '--process' that was used to get this
    process_query: ProcessQuery
    gcs_result: Result[str, Sequence[ProcessedGC]]
    mechanisms_and_reasons: Optional[MechanismsAndReasons]
    join_info: Result[str, AbstractJoinInfoForProcess]

    @property
    def gcs(self) -> Sequence[ProcessedGC]:
        return unwrap(self.gcs_result)

    def metric(self, run_metric: RunMetric) -> FailableValue:
        from .run_metrics import stat_for_proc

        return stat_for_proc(self, run_metric)

    def metric_from_name(self, name: str) -> FailableValue:
        from .parse_metrics import parse_run_metric_arg

        return self.metric(parse_run_metric_arg(name))

    def unwrap_metric_from_name(self, name: str) -> AnyValue:
        return unwrap(self.metric_from_name(name))

    @property
    def name(self) -> str:
        if self.test_result.test_status_path is not None:
            return self.test_result.test_status_path.name
        else:
            return remove_extension(non_null(self.test_result.trace_path)).name

    @property
    def process_id(self) -> Optional[int]:
        return map_option(self.process_info, lambda p: p.id)

    @property
    def UsesServerGC(self) -> Optional[bool]:
        """None if this is unknown"""
        return map_option(self.process_info, lambda p: p.uses_server_gc)

    @property
    def event_names(self) -> Optional[Mapping[str, int]]:
        return map_option(self.process_info, lambda p: p.event_names)

    @property
    def has_mechanisms_and_reasons(self) -> bool:
        return self.mechanisms_and_reasons is not None

    @property
    def has_join_info(self) -> bool:
        return self.join_info is not None

    @property
    def FirstToLastGCSeconds(self) -> FailableFloat:
        if self.process_info is None:
            return Err("Need a trace")
        gcs = self.process_info.all_gcs_including_incomplete
        if len(gcs) < 2:
            return Err("Need at least 2 gcs")
        else:
            return Ok(msec_to_seconds(gcs[-1].StartRelativeMSec - gcs[0].StartRelativeMSec))

    @property
    def FirstEventToFirstGCSeconds(self) -> FailableFloat:
        if self.process_info is None:
            return Err("Need a trace")
        ts = self.process_info.events_time_span
        if ts is None:
            return Err("Did not specify to collect events")
        else:
            return Ok(
                msec_to_seconds(
                    self.process_info.all_gcs_including_incomplete[0].StartRelativeMSec
                    - ts.StartMSec
                )
            )

    @property
    def TotalNonGCSeconds(self) -> FailableFloat:
        return map_ok(
            self.FirstToLastEventSeconds,
            lambda t: t - msec_to_seconds(sum(gc.PauseDurationMSec for gc in self.gcs)),
        )

    @property
    def FirstToLastEventSeconds(self) -> FailableFloat:
        ts = map_option(self.process_info, lambda p: p.events_time_span)
        if ts is None:
            return Err("Did not specify to collect events")
        else:
            return Ok(msec_to_seconds(ts.DurationMSec))

    @property
    def TotalAllocatedMB(self) -> FailableFloat:
        if self.process_info is None:
            return Err("Need a trace")
        else:
            return Ok(sum(gc.AllocedSinceLastGCMB for gc in self.gcs))

    @property
    def HeapCount(self) -> int:
        n = self.gcs[0].HeapCount
        for gc in self.gcs:
            assert gc.HeapCount == n
        return n

    @property
    def NumberGCs(self) -> int:
        return len(self.gcs)

    def number_gcs_per_generation(self, gen: Gens) -> int:
        return count(() for gc in self.gcs if gc.Generation == gen)

    @property
    def number_gcs_in_each_generation(self) -> Mapping[Gens, int]:
        return map_to_mapping(Gens, self.number_gcs_per_generation)


# WARN: `prop` type is deliberately *not* the correct type --
# mypy checks a property access on a class `C.x` as being of the property's callable type
# when it is actually a `property` instacnce. See https://github.com/python/mypy/issues/6192
def fn_of_property(prop: Callable[[T], U]) -> Callable[[T], U]:
    res = check_cast(property, prop).__get__
    assert callable(res)
    return res


def ok_of_property(prop: Callable[[T], U]) -> Callable[[T], Result[E, U]]:
    return fn_to_ok(fn_of_property(prop))


class RegressionKind(Enum):
    # Note: the order of these determines the order we'll print them in.
    LARGE_REGRESSION = 1
    LARGE_IMPROVEMENT = 2
    REGRESSION = 3
    IMPROVEMENT = 4
    STALE = 5

    def __lt__(self, other: Any) -> bool:  # other: RegressionKind
        sv: int = self.value
        ov: int = other.value
        return sv < ov

    def title(self) -> str:
        return {
            RegressionKind.LARGE_REGRESSION: "Large Regressions (Regression of >20%)",
            RegressionKind.REGRESSION: "Regressions (Regression of 5% - 20%)",
            RegressionKind.LARGE_IMPROVEMENT: "Large Improvements (Improvement of >20%)",
            RegressionKind.IMPROVEMENT: "Improvements (Improvement of 5-20%)",
            RegressionKind.STALE: "Stale (Same, or percent difference within 5% margin)",
        }[self]

    def text_color(self) -> Optional[str]:
        return {
            RegressionKind.LARGE_REGRESSION: "red",
            RegressionKind.REGRESSION: "red",
            RegressionKind.LARGE_IMPROVEMENT: "green",
            RegressionKind.IMPROVEMENT: "green",
            RegressionKind.STALE: None,
        }[self]


def get_regression_kind(factor_diff: float, better: Better) -> RegressionKind:
    if better == Better.greater:
        factor_diff *= -1

    if factor_diff > 0.20:
        return RegressionKind.LARGE_REGRESSION
    elif factor_diff > 0.05:
        return RegressionKind.REGRESSION
    elif factor_diff < -0.20:
        return RegressionKind.LARGE_IMPROVEMENT
    elif factor_diff < -0.05:
        return RegressionKind.IMPROVEMENT
    else:
        return RegressionKind.STALE


@with_slots
@dataclass(frozen=True)
class FloatsOrStrs:
    is_floats: bool
    _values: Union[Sequence[float], Sequence[str]]

    @property
    def as_floats(self) -> Sequence[float]:
        assert self.is_floats
        return cast(Sequence[float], self._values)

    @property
    def as_strs(self) -> Sequence[str]:
        assert not self.is_floats
        return cast(Sequence[str], self._values)

    def __len__(self) -> int:
        return len(self._values)


def floats(s: Sequence[float]) -> FloatsOrStrs:
    return FloatsOrStrs(True, s)


def strs(s: Sequence[str]) -> FloatsOrStrs:
    return FloatsOrStrs(False, s)
