# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from operator import add
from typing import Callable, Mapping, Sequence

from result import Err, Ok, Result

from ..commonlib.collection_util import combine_mappings, identity
from ..commonlib.result_utils import map_ok, map_ok_2, unwrap
from ..commonlib.type_utils import enum_value
from ..commonlib.util import bytes_to_mb, get_percent

from .enums import Gens, MarkRootType
from .join_analysis import ALL_GETTERS_FROM_JOIN_ANALYSIS
from .types import (
    fn_of_property,
    PerHeapGetter,
    ProcessedGenData,
    ProcessedHeap,
    SingleHeapMetric,
    FailableValue,
)


def _get_gen0_heap_fullness_pct(phh: ProcessedHeap) -> Result[str, float]:
    return map_ok(phh.gen_result(Gens.Gen0), lambda g0: get_percent(g0.size_before / g0.budget))


def get_gen_name(gen: Gens) -> str:
    return "Loh" if gen == Gens.GenLargeObj else f"Gen{enum_value(gen)}"


_PER_HEAP_HISTORY_NON_SIZE_GETTERS: Mapping[str, Callable[[ProcessedGenData], int]] = {
    "SurvRate": lambda g: g.surv_rate,
    "PinnedSurv": lambda g: g.pinned_surv,
    "NonePinnedSurv": lambda g: g.non_pinned_surv,
}
_PER_HEAP_HISTORY_SIZE_GETTERS: Mapping[str, Callable[[ProcessedGenData], int]] = {
    "SizeBefore": lambda g: g.size_before,
    "SizeAfter": lambda g: g.size_after,
    "ObjSpaceBefore": lambda g: g.obj_space_before,
    "Fragmentation": lambda g: g.fragmentation,
    "ObjSizeAfter": lambda g: g.obj_size_after,
    "FreeListSpaceBefore": lambda g: g.free_list_space_before,
    "FreeObjSpaceBefore": lambda g: g.free_obj_space_before,
    "FreeListSpaceAfter": lambda g: g.free_list_space_after,
    "FreeObjSpaceAfter": lambda g: g.free_obj_space_after,
    "In": lambda g: g.in_bytes,
    "Out": lambda g: g.out_bytes,
    "Budget": lambda g: g.budget,
}


def _per_heap_history_getters_from_gen_data(gen: Gens) -> Mapping[SingleHeapMetric, PerHeapGetter]:
    def get_getter(
        cb: Callable[[ProcessedGenData], int], get_mb: Callable[[int], float]
    ) -> PerHeapGetter:
        # NOTE: gcs don't seem to have gendata for generations other than their own
        return (
            lambda hp: map_ok(hp.gen_result(gen), lambda g: get_mb(cb(g)))
            if hp.gc.collects_generation(gen)
            else Err("gc is wrong gen")
        )

    return combine_mappings(
        {
            SingleHeapMetric(f"{get_gen_name(gen)}{name}"): get_getter(cb, get_mb=identity)
            for name, cb in _PER_HEAP_HISTORY_NON_SIZE_GETTERS.items()
        },
        {
            SingleHeapMetric(f"{get_gen_name(gen)}{name}MB"): get_getter(cb, get_mb=bytes_to_mb)
            for name, cb in _PER_HEAP_HISTORY_SIZE_GETTERS.items()
        },
    )


# TODO:MOVE
ALL_GENS: Sequence[Gens] = (Gens.Gen0, Gens.Gen1, Gens.Gen2, Gens.GenLargeObj)
ALL_GC_GENS: Sequence[Gens] = (Gens.Gen0, Gens.Gen1, Gens.Gen2)


def _per_heap_history_getters_for_all_gens() -> Mapping[SingleHeapMetric, PerHeapGetter]:
    def get_getter(cb: Callable[[ProcessedGenData], int]) -> PerHeapGetter:
        return lambda phh: map_ok(phh.gens, lambda gens: sum(bytes_to_mb(cb(gen)) for gen in gens))

    return {
        SingleHeapMetric(f"{name}MB"): get_getter(cb)
        for name, cb in _PER_HEAP_HISTORY_SIZE_GETTERS.items()
    }


def _get_per_heap_history_getters_for_gens() -> Mapping[SingleHeapMetric, PerHeapGetter]:
    def get_all_gens_getter(cb: Callable[[ProcessedGenData], int]) -> PerHeapGetter:
        return (
            lambda hp: Err("gc is wrong gen")
            if hp.gc.collects_generation(Gens.Gen2)
            else map_ok(hp.gens, lambda gens: bytes_to_mb(sum(cb(gen) for gen in gens)))
        )

    return combine_mappings(
        *(_per_heap_history_getters_from_gen_data(gen) for gen in ALL_GENS),
        _per_heap_history_getters_for_all_gens(),
        {
            SingleHeapMetric(f"AllGens{name}MB"): get_all_gens_getter(cb)
            for name, cb in _PER_HEAP_HISTORY_SIZE_GETTERS.items()
        },
    )


def _getter_for_mark_root_type_msec(mrt: MarkRootType) -> PerHeapGetter:
    return lambda hp: hp.mark_time(mrt)


def _getter_for_mark_root_type_promoted_mb(mrt: MarkRootType) -> PerHeapGetter:
    return lambda hp: hp.mark_promoted(mrt)


MARK_ROOT_TIME_GETTERS: Mapping[SingleHeapMetric, PerHeapGetter] = combine_mappings(
    {SingleHeapMetric("TotalMarkMSec"): fn_of_property(ProcessedHeap.TotalMarkMSec)},
    {
        SingleHeapMetric(f"{mrt.name}MSec"): _getter_for_mark_root_type_msec(mrt)
        for mrt in MarkRootType
    },
)

MARK_ROOT_PROMOTED_GETTERS: Mapping[SingleHeapMetric, PerHeapGetter] = combine_mappings(
    {SingleHeapMetric("TotalMarkPromotedMB"): ProcessedHeap.TotalMarkPromoted},
    {
        SingleHeapMetric(f"{mrt.name}PromotedMB"): _getter_for_mark_root_type_promoted_mb(mrt)
        for mrt in MarkRootType
    },
)

MARK_ROOT_TIME_METRICS = MARK_ROOT_TIME_GETTERS.keys()
MARK_ROOT_PROMOTED_METRICS = MARK_ROOT_PROMOTED_GETTERS.keys()

# TODO:NEATER
_PER_HEAP_HISTORY_GETTERS: Mapping[SingleHeapMetric, PerHeapGetter] = combine_mappings(
    {
        SingleHeapMetric("FreeListAllocated"): fn_of_property(ProcessedHeap.FreeListAllocated),
        SingleHeapMetric("FreeListRejected"): fn_of_property(ProcessedHeap.FreeListRejected),
        SingleHeapMetric("FreeListConsumed"): lambda phh: map_ok_2(
            phh.FreeListAllocated, phh.FreeListRejected, add
        ),
        SingleHeapMetric("Gen0FullnessPercent"): _get_gen0_heap_fullness_pct,
    },
    MARK_ROOT_TIME_GETTERS,
    MARK_ROOT_PROMOTED_GETTERS,
    ALL_GETTERS_FROM_JOIN_ANALYSIS,
)

SINGLE_HEAP_METRIC_GETTERS: Mapping[SingleHeapMetric, PerHeapGetter] = combine_mappings(
    _PER_HEAP_HISTORY_GETTERS,
    _get_per_heap_history_getters_for_gens(),
    {
        SingleHeapMetric(
            "TotalStolenMSec",
            doc="Sum of each time the processor was stolen for this heap's thread.",
        ): lambda hp:
        # TODO: use new join analysis instead
        Ok(
            sum(
                kv.Value.Item1
                for kv in hp.clr.Analysis.GetLostCpuBreakdownForHeap(
                    hp.gc.trace_gc, unwrap(hp.server_gc_history)
                )
            )
        )
    },
)

ALL_SINGLE_HEAP_METRICS: Sequence[SingleHeapMetric] = tuple(SINGLE_HEAP_METRIC_GETTERS.keys())


def get_single_heap_stat(hp: ProcessedHeap, metric: SingleHeapMetric) -> FailableValue:
    return SINGLE_HEAP_METRIC_GETTERS[metric](hp)
