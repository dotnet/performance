# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from operator import sub
from typing import Callable, Mapping, Optional, Sequence

from result import Err, Ok, Result

from ..commonlib.collection_util import combine_mappings, map_mapping_values
from ..commonlib.option import non_null
from ..commonlib.result_utils import fn_to_ok, fn3_to_ok, map_ok, map_ok_2, option_to_result, unwrap
from ..commonlib.type_utils import enum_value
from ..commonlib.util import bytes_to_mb, mb_to_gb, msec_to_seconds

from .aggregate_stats import get_aggregate_stats
from .clr_types import AbstractBGCRevisitInfo
from .enums import (
    BGCRevisitState,
    gc_heap_compact_reason,
    gc_heap_expand_mechanism,
    gc_reason,
    Gens,
    try_get_gc_heap_compact_reason,
    try_get_gc_heap_expand_mechanism,
    HeapType,
)
from .single_heap_metrics import SINGLE_HEAP_METRIC_GETTERS
from .types import (
    FailableBool,
    FailableFloat,
    fn_of_property,
    MetricType,
    GenInfoGetter,
    ok_of_property,
    PerHeapGetter,
    ProcessedGC,
    ProcessedHeap,
    ProcessedTrace,
    SingleGCMetric,
    FailableValue,
)


def _get_non_concurrent_revisit_info(
    gc: ProcessedGC, heap_type: HeapType
) -> AbstractBGCRevisitInfo:
    assert gc.IsBackground
    return gc.trace_gc.BGCRevisitInfoArr[enum_value(BGCRevisitState.NonConcurrent)][
        enum_value(heap_type.value)
    ]


def _get_max_bgc_wait_msec(gc: ProcessedGC) -> float:
    if gc.IsBackground:
        loh_wait = gc.trace_gc.LOHWaitThreads
        if loh_wait is not None:
            return max(
                bwi.WaitStopRelativeMSec - bwi.WaitStartRelativeMSec for bwi in loh_wait.Values
            )
    return 0


GC_PREDICATES = Mapping[SingleGCMetric, Callable[[ProcessedGC], FailableBool]]


GC_TYPE_METRICS: GC_PREDICATES = {
    SingleGCMetric("IsNonConcurrent", type=MetricType.bool): ok_of_property(
        ProcessedGC.IsNonConcurrent
    ),
    SingleGCMetric("IsBackground", type=MetricType.bool): ok_of_property(ProcessedGC.IsBackground),
    SingleGCMetric("IsForeground", type=MetricType.bool): ok_of_property(ProcessedGC.IsForeground),
}

GC_MECHANISM_METRICS: GC_PREDICATES = {
    SingleGCMetric("IsConcurrent", type=MetricType.bool): fn_of_property(ProcessedGC.IsConcurrent),
    SingleGCMetric("UsesCompaction", type=MetricType.bool): fn_of_property(
        ProcessedGC.UsesCompaction
    ),
    SingleGCMetric("UsesPromotion", type=MetricType.bool): fn_of_property(
        ProcessedGC.UsesPromotion
    ),
    SingleGCMetric("UsesDemotion", type=MetricType.bool): fn_of_property(ProcessedGC.UsesDemotion),
    SingleGCMetric("UsesCardBundles", type=MetricType.bool): fn_of_property(
        ProcessedGC.UsesCardBundles
    ),
    SingleGCMetric("UsesElevation", type=MetricType.bool): fn_of_property(
        ProcessedGC.UsesElevation
    ),
    SingleGCMetric("UsesLOHCompaction", type=MetricType.bool): fn_of_property(
        ProcessedGC.UsesLOHCompaction
    ),
}


def _pred_for_heap_compact_reason(reason: gc_heap_compact_reason,) -> Callable[[ProcessedGC], bool]:
    return lambda gc: any(
        try_get_gc_heap_compact_reason(unwrap(phh.compact_mechanisms)) == reason for phh in gc.heaps
    )


GC_HEAP_COMPACT_REASON_METRICS: GC_PREDICATES = {
    SingleGCMetric(f"CompactsBecause_{reason.name}", type=MetricType.bool): fn_to_ok(
        _pred_for_heap_compact_reason(reason)
    )
    for reason in gc_heap_compact_reason
}


def _pred_for_heap_expand_reason(
    reason: gc_heap_expand_mechanism,
) -> Callable[[ProcessedGC], bool]:
    return lambda gc: any(
        try_get_gc_heap_expand_mechanism(unwrap(phh.expand_mechanisms)) == reason
        for phh in gc.heaps
    )


GC_HEAP_EXPAND_REASON_METRICS: GC_PREDICATES = {
    SingleGCMetric(f"ExpandsBecause_{reason.name}", type=MetricType.bool): fn_to_ok(
        _pred_for_heap_expand_reason(reason)
    )
    for reason in gc_heap_expand_mechanism
}


def _pred_for_reason(reason: gc_reason) -> Callable[[ProcessedGC], bool]:
    return lambda gc: gc.reason == reason


GC_REASON_METRICS: GC_PREDICATES = {
    SingleGCMetric(f"Reason_Is_{reason.name}", type=MetricType.bool): fn_to_ok(
        _pred_for_reason(reason)
    )
    for reason in gc_reason
}


GC_BOOLEAN_METRICS_GETTERS: GC_PREDICATES = combine_mappings(
    GC_TYPE_METRICS,
    GC_MECHANISM_METRICS,
    GC_REASON_METRICS,
    GC_HEAP_COMPACT_REASON_METRICS,
    GC_HEAP_EXPAND_REASON_METRICS,
    {
        SingleGCMetric("IsGen0", type=MetricType.bool): ok_of_property(ProcessedGC.IsGen0),
        SingleGCMetric("IsGen1", type=MetricType.bool): ok_of_property(ProcessedGC.IsGen1),
        SingleGCMetric("IsGen2", type=MetricType.bool): ok_of_property(ProcessedGC.IsGen2),
        SingleGCMetric("IsBlockingGen2", type=MetricType.bool): ok_of_property(
            ProcessedGC.IsBlockingGen2
        ),
        SingleGCMetric("IsEphemeral", type=MetricType.bool): ok_of_property(
            ProcessedGC.IsEphemeral
        ),
        SingleGCMetric("IsNonBackground", type=MetricType.bool): lambda gc: Ok(not gc.IsBackground),
    },
)
GC_BOOLEAN_METRICS = tuple(GC_BOOLEAN_METRICS_GETTERS.keys())
for bool_metric in GC_BOOLEAN_METRICS:
    assert bool_metric.type == MetricType.bool, f"Metric {bool_metric} should be type bool"


def _get_pinned_object_percentage(gc: ProcessedGC) -> FailableFloat:
    pct = gc.PinnedObjectPercentage
    assert pct is None or 0 <= pct <= 100
    return option_to_result(pct, lambda: "GetPinnedObjectPercentage() failed")


def _get_total_gc_time(gc: ProcessedGC) -> FailableFloat:
    t = gc.TotalGCTime
    if t is None:
        return Err("Needs CPU samples")
    else:
        assert t > 0
        return Ok(t)


_PER_GEN_METRIC_GETTERS: Mapping[str, Callable[[GenInfoGetter], FailableFloat]] = {
    # NOTE: these names will be prefixed with the generation name
    "SurvivalPercent": fn_of_property(GenInfoGetter.SurvivalPct),
    "SizeBeforeMB": ok_of_property(GenInfoGetter.SizeBeforeMB),
    "SizeAfterMB": ok_of_property(GenInfoGetter.SizeAfterMB),
    # TODO: this is just the heap's FragmentationMB_Sum
    "FragmentationMB": ok_of_property(GenInfoGetter.FragmentationMB),
    "FragmentationPercent": ok_of_property(GenInfoGetter.FragmentationPct),
    # TODO: this is just GenData[gen].In, already exposed elsewhere?
    "InMB": ok_of_property(GenInfoGetter.InMB),
    "PromotedMB": ok_of_property(GenInfoGetter.PromotedMB),
    "BudgetMB": ok_of_property(GenInfoGetter.BudgetMB),
    "ObjSizeAfterMB": ok_of_property(GenInfoGetter.ObjSizeAfterMB),
}


def _per_gen_stat_getter(
    gen: Gens, getter: Callable[[GenInfoGetter], FailableFloat]
) -> Callable[[ProcessedGC], FailableFloat]:
    return lambda gc: getter(gc.gen_info(gen))


def _convert_heap_getter(
    g: PerHeapGetter
) -> Callable[[ProcessedGC, Sequence[ProcessedHeap], int], FailableValue]:
    return lambda gc, hps, i: g(hps[i])


SINGLE_GC_METRIC_GETTERS_SIMPLE: Mapping[
    SingleGCMetric, Callable[[ProcessedGC], FailableValue]
] = combine_mappings(
    {
        SingleGCMetric(f"{gen.name}{name}"): _per_gen_stat_getter(gen, getter)
        for gen in Gens
        for name, getter in _PER_GEN_METRIC_GETTERS.items()
    },
    {
        SingleGCMetric("Generation"): lambda gc: Ok(enum_value(gc.Generation)),
        SingleGCMetric("BGCSohConcurrentRevisitedPages"): lambda gc: Ok(
            _get_non_concurrent_revisit_info(gc, HeapType.SOH).PagesRevisited
        ),
        SingleGCMetric("BGCLohConcurrentRevisitedPages"): lambda gc: Ok(
            _get_non_concurrent_revisit_info(gc, HeapType.LOH).PagesRevisited
        ),
        SingleGCMetric("MaxBGCWaitMSec"): fn_to_ok(_get_max_bgc_wait_msec),
        SingleGCMetric("DurationSeconds"): lambda gc: Ok(msec_to_seconds(gc.DurationMSec)),
        SingleGCMetric("Number"): lambda gc: Ok(gc.Number),
        SingleGCMetric("Index"): lambda gc: Ok(gc.index),
        SingleGCMetric("PauseDurationSeconds"): lambda gc: Ok(msec_to_seconds(gc.DurationMSec)),
        **GC_BOOLEAN_METRICS_GETTERS,
        SingleGCMetric("PctReductionInHeapSize"): ok_of_property(
            ProcessedGC.PctReductionInHeapSize
        ),
        SingleGCMetric("PinnedObjectSizes"): lambda gc: Ok(gc.PinnedObjectSizes),
        SingleGCMetric("PinnedObjectPercentage"): _get_pinned_object_percentage,
        SingleGCMetric(
            "TotalGCTime", doc="WARN: Only works in increments of 1MS, may error on smaller GCs"
        ): _get_total_gc_time,
        SingleGCMetric("PromotedGBPerSec"): lambda gc: Ok(gc.PromotedGBPerSec),
        SingleGCMetric("PromotedMBPerSec"): lambda gc: Ok(gc.PromotedMBPerSec),
        SingleGCMetric("SecondsPerPromotedGB"): lambda gc: Ok(
            msec_to_seconds(gc.DurationMSec) / mb_to_gb(gc.PromotedMB)
        ),
        SingleGCMetric("PauseStartMSec", do_not_use_scientific_notation=True): lambda gc: Ok(
            gc.PauseStartRelativeMSec
        ),
        SingleGCMetric("SuspendToGCStartMSec", do_not_use_scientific_notation=True): lambda gc: Ok(
            gc.SuspendToGCStartMSec
        ),
        SingleGCMetric("PauseEndMSec", do_not_use_scientific_notation=True): lambda gc: Ok(
            gc.PauseStartRelativeMSec + gc.PauseDurationMSec
        ),
        SingleGCMetric("StartMSec", do_not_use_scientific_notation=True): lambda gc: Ok(
            gc.StartRelativeMSec
        ),
        SingleGCMetric("EndMSec", do_not_use_scientific_notation=True): lambda gc: Ok(
            gc.StartRelativeMSec + gc.DurationMSec
        ),
        SingleGCMetric("Type", doc="Value of GCType enum"): lambda gc: Ok(enum_value(gc.Type)),
        SingleGCMetric("AllocedSinceLastGCMB"): ok_of_property(ProcessedGC.AllocedSinceLastGCMB),
        SingleGCMetric("AllocedMBAccumulated"): ok_of_property(ProcessedGC.AllocedMBAccumulated),
        SingleGCMetric("AllocRateMBSec"): ok_of_property(ProcessedGC.AllocRateMBSec),
        SingleGCMetric("BGCFinalPauseMSec"): ok_of_property(ProcessedGC.BGCFinalPauseMSec),
        SingleGCMetric("DurationMSec"): ok_of_property(ProcessedGC.DurationMSec),
        SingleGCMetric("DurationSinceLastRestartMSec"): ok_of_property(
            ProcessedGC.DurationSinceLastRestartMSec
        ),
        SingleGCMetric("GCCpuMSec"): ok_of_property(ProcessedGC.GCCpuMSec),
        # Should be the same across all GCs -- used for debugging
        SingleGCMetric("HeapCount"): ok_of_property(ProcessedGC.HeapCount),
        SingleGCMetric("HeapSizeAfterMB"): ok_of_property(ProcessedGC.HeapSizeAfterMB),
        SingleGCMetric("HeapSizeBeforeMB"): ok_of_property(ProcessedGC.HeapSizeBeforeMB),
        SingleGCMetric("HeapSizePeakMB"): ok_of_property(ProcessedGC.HeapSizePeakMB),
        SingleGCMetric("PauseDurationMSec"): ok_of_property(ProcessedGC.PauseDurationMSec),
        SingleGCMetric("PauseTimePercentageSinceLastGC"): ok_of_property(
            ProcessedGC.PauseTimePercentageSinceLastGC
        ),
        SingleGCMetric("ProcessCpuMSec"): ok_of_property(ProcessedGC.ProcessCpuMSec),
        SingleGCMetric("PromotedMB"): ok_of_property(ProcessedGC.PromotedMB),
        SingleGCMetric("RatioPeakAfter"): ok_of_property(ProcessedGC.RatioPeakAfter),
        SingleGCMetric("SuspendDurationMSec"): ok_of_property(ProcessedGC.suspend_duration_msec),
        SingleGCMetric("MemoryPressure"): fn_of_property(ProcessedGC.MemoryPressure),
    },
    {
        # Unrelated to the total PercentTimeInGc which is for the entire process
        # TODO: this requires CPU samples, fail otherwise
        SingleGCMetric("PctTimeInThisGcSinceLastGc"): lambda gc: Ok(gc.PercentTimeInGC)
    },
    get_aggregate_stats(
        aggregate_metric_cls=SingleGCMetric,
        get_elements=lambda gc: Ok(gc.heaps),
        element_metric_to_get_value=map_mapping_values(
            _convert_heap_getter, SINGLE_HEAP_METRIC_GETTERS
        ),
    ),
)

GcValueGetter = Callable[[ProcessedTrace, Sequence[ProcessedGC], int], FailableValue]


def _cnv(cb: Callable[[ProcessedGC], FailableValue]) -> GcValueGetter:
    return lambda _, gcs, i: cb(gcs[i])


def _get_last_per_heap_hist_to_end_msec(
    proc: ProcessedTrace, gcs: Sequence[ProcessedGC], gc_index: int
) -> float:
    phh_times = non_null(non_null(proc.process_info).per_heap_history_times)
    gc = gcs[gc_index]
    gc_end = gc.EndRelativeMSec
    last_time = max(time for time in phh_times if time <= gc_end)
    return gc_end - last_time


SINGLE_GC_METRIC_GETTERS: Mapping[SingleGCMetric, GcValueGetter] = combine_mappings(
    {k: _cnv(v) for k, v in SINGLE_GC_METRIC_GETTERS_SIMPLE.items()},
    {
        SingleGCMetric("LastPerHeapHistToEndMSec"): fn3_to_ok(_get_last_per_heap_hist_to_end_msec),
        SingleGCMetric("MbAllocatedOnSOHSinceLastSameGenGc"): lambda _, gcs, index: map_ok(
            get_bytes_allocated_since_last_gc(gcs, index, gcs[index].Generation), bytes_to_mb
        ),
        SingleGCMetric("MbAllocatedOnLOHSinceLastGen2Gc"): lambda _, gcs, index: map_ok(
            get_bytes_allocated_since_last_gc(gcs, index, Gens.GenLargeObj), bytes_to_mb
        ),
    },
)

ALL_SINGLE_GC_METRICS: Sequence[SingleGCMetric] = tuple(SINGLE_GC_METRIC_GETTERS.keys())


def get_single_gc_stat(
    proc: ProcessedTrace, gcs: Sequence[ProcessedGC], index: int, metric: SingleGCMetric
) -> FailableValue:
    return SINGLE_GC_METRIC_GETTERS[metric](proc, gcs, index)


def get_bytes_allocated_since_last_gc(
    gcs: Sequence[ProcessedGC], index: int, gen: Gens
) -> Result[str, int]:
    prev_index = index - 1
    search_gen = Gens.Gen2 if gen == Gens.GenLargeObj else gen
    prev = None
    while prev_index >= 0:
        if gcs[prev_index].Generation == search_gen:
            prev = gcs[prev_index]
            break
        prev_index -= 1

    cur = gcs[index]
    # Can only use this stat if all GCs are of the same generation
    assert prev is None or prev.Generation == cur.Generation
    return _bytes_allocated_between_gcs(prev, cur, gen)


def _bytes_allocated_between_gcs(
    prev: Optional[ProcessedGC], cur: ProcessedGC, gen: Gens
) -> Result[str, int]:
    after_prev = Ok(0) if prev is None else prev.total_bytes_after(gen)
    before_cur = cur.total_bytes_before(gen)
    return map_ok_2(before_cur, after_prev, sub)


def get_gc_index_with_number(gcs: Sequence[ProcessedGC], gc_number: int) -> int:
    # In some cases we'll drop events, so can't just use an index.
    for i, gc in enumerate(gcs):
        if gc.Number == gc_number:
            return i
    raise Exception(f"No GC has number {gc_number}")


def get_gc_with_number(gcs: Sequence[ProcessedGC], gc_number: int) -> ProcessedGC:
    return gcs[get_gc_index_with_number(gcs, gc_number)]
