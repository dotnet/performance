# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from enum import Enum
from typing import Callable, Iterable, Mapping, Sequence

from result import Err, Ok, Result

from ..commonlib.collection_util import combine_mappings
from ..commonlib.result_utils import flat_map_ok, map_ok
from ..commonlib.type_utils import enum_value
from ..commonlib.util import opt_max, opt_median

from .clr import Clr
from .clr_types import AbstractJoinInfoForHeap, AbstractJoinInfoForProcess, cs_result_to_result
from .enums import GcJoinPhase, GcJoinStage, ServerGCState
from .types import FailableFloat, PerHeapGetter, ProcessedHeap, ProcessInfo, SingleHeapMetric


# From MoreAnalysis.cs
class Strictness(Enum):
    loose = 0
    strict = 1


def get_join_info_for_all_gcs(
    clr: Clr, proc: ProcessInfo
) -> Result[str, AbstractJoinInfoForProcess]:
    if proc.uses_server_gc:
        return cs_result_to_result(
            clr.JoinAnalysis.AnalyzeAllGcs(
                clr.to_array(clr.TraceGC, proc.gcs), enum_value(Strictness.strict)
            )
        )
    else:
        return Err("Can't get join info for non-server GC")


def _get_all_state_durations_for_heap(
    hp: ProcessedHeap, state: ServerGCState
) -> Result[str, Iterable[float]]:
    return map_ok(
        hp.join_info,
        lambda h: (stage.MSecPerState[enum_value(state)] for stage in h.ForegroundStages),
    )


def _get_all_join_durations_for_heap(hp: ProcessedHeap) -> Result[str, Iterable[float]]:
    return _get_all_state_durations_for_heap(hp, ServerGCState.waiting_in_join)


def _median_or_empty(i: Iterable[float]) -> FailableFloat:
    m = opt_median(i)
    return Err("empty join durations") if m is None else Ok(m)


def _max_or_empty(i: Iterable[float]) -> FailableFloat:
    m = opt_max(i)
    return Err("empty join durations") if m is None else Ok(m)


def _sum_or_empty(i: Iterable[float]) -> FailableFloat:
    s = sum(i)
    return Err("empty join durations") if s == 0 else Ok(s)


def _get_median_individual_join_msec(hp: ProcessedHeap) -> FailableFloat:
    return flat_map_ok(_get_all_join_durations_for_heap(hp), _median_or_empty)


def _get_max_individual_join_msec(hp: ProcessedHeap) -> FailableFloat:
    return flat_map_ok(_get_all_join_durations_for_heap(hp), _max_or_empty)


def _get_total_join_msec(hp: ProcessedHeap) -> FailableFloat:
    return flat_map_ok(_get_all_join_durations_for_heap(hp), _sum_or_empty)


PER_HEAP_JOIN_GETTERS: Mapping[SingleHeapMetric, PerHeapGetter] = {
    SingleHeapMetric("MedianIndividualJoinMSec"): _get_median_individual_join_msec,
    SingleHeapMetric("MaxIndividualJoinMSec"): _get_max_individual_join_msec,
    SingleHeapMetric("TotalJoinMSec"): _get_total_join_msec,
}


def _per_heap_getter(cb: Callable[[AbstractJoinInfoForHeap], float]) -> PerHeapGetter:
    return lambda hp: map_ok(hp.join_info, cb)


def _getter_for_total_time_in_state(state: ServerGCState) -> PerHeapGetter:
    return _per_heap_getter(lambda i: i.TotalMSecInState(enum_value(state)))


def _getter_for_total_time_in_stage(stage: GcJoinStage) -> PerHeapGetter:
    return _per_heap_getter(lambda i: i.TotalMSecInStage(enum_value(stage)))


def _getter_for_total_time_in_phase(phase: GcJoinPhase) -> PerHeapGetter:
    return _per_heap_getter(lambda i: i.TotalMSecInPhase(enum_value(phase)))


STATE_TIMES_GETTERS: Mapping[SingleHeapMetric, PerHeapGetter] = {
    SingleHeapMetric(state.name): _getter_for_total_time_in_state(state) for state in ServerGCState
}

STAGE_TIMES_GETTERS: Mapping[SingleHeapMetric, PerHeapGetter] = {
    SingleHeapMetric(stage.name): _getter_for_total_time_in_stage(stage)
    for stage in GcJoinStage
    if stage != GcJoinStage.restart
}

PHASE_TIMES_GETTERS: Mapping[SingleHeapMetric, PerHeapGetter] = {
    SingleHeapMetric(phase.name): _getter_for_total_time_in_phase(phase) for phase in GcJoinPhase
}

ALL_GETTERS_FROM_JOIN_ANALYSIS: Mapping[SingleHeapMetric, PerHeapGetter] = combine_mappings(
    PER_HEAP_JOIN_GETTERS, STATE_TIMES_GETTERS, STAGE_TIMES_GETTERS, PHASE_TIMES_GETTERS
)

_PER_HEAP_JOIN_TIME_METRICS: Sequence[str] = [m.name for m in PER_HEAP_JOIN_GETTERS]
_PER_HEAP_JOIN_STATE_METRICS: Sequence[str] = [m.name for m in STATE_TIMES_GETTERS]
_PER_HEAP_JOIN_STAGE_METRICS: Sequence[str] = [m.name for m in STAGE_TIMES_GETTERS]
_PER_HEAP_JOIN_PHASE_METRICS: Sequence[str] = [m.name for m in PHASE_TIMES_GETTERS]

JOIN_PER_HEAP_METRICS_ALIASES: Mapping[str, Sequence[str]] = {
    "joinTimes": _PER_HEAP_JOIN_TIME_METRICS,
    "states": _PER_HEAP_JOIN_STATE_METRICS,
    "stages": _PER_HEAP_JOIN_STAGE_METRICS,
    "phases": _PER_HEAP_JOIN_PHASE_METRICS,
}


def means(names: Sequence[str]) -> Sequence[str]:
    return [f"{name}_Mean" for name in names]


_PER_GC_JOIN_STATE_METRICS = means(_PER_HEAP_JOIN_STATE_METRICS)
_PER_GC_JOIN_STAGE_METRICS = means(_PER_HEAP_JOIN_STAGE_METRICS)
_PER_GC_JOIN_PHASE_METRICS = means(_PER_HEAP_JOIN_PHASE_METRICS)

JOIN_PER_GC_METRICS_ALIASES: Mapping[str, Sequence[str]] = {
    "joinTimes": ("TotalJoinMSec_Max", "TotalJoinMSec_Mean"),
    "states": _PER_GC_JOIN_STATE_METRICS,
    "stages": _PER_GC_JOIN_STAGE_METRICS,
    "phases": _PER_GC_JOIN_PHASE_METRICS,
}

JOIN_RUN_METRICS_ALIASES: Mapping[str, Sequence[str]] = {
    "joinTimes": ("TotalJoinMSec_Max_Mean", "TotalJoinMSec_Mean_Mean"),
    "states": means(_PER_GC_JOIN_STATE_METRICS),
    "stages": means(_PER_GC_JOIN_STAGE_METRICS),
    "phases": means(_PER_GC_JOIN_PHASE_METRICS),
}
