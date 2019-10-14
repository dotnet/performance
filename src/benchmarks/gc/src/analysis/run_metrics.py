# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from statistics import mean
from typing import Callable, Dict, List, Mapping, Sequence, Tuple

from result import Err, Ok, Result

from ..commonlib.bench_file import GCPerfSimResult, TestRunStatus
from ..commonlib.collection_util import (
    combine_mappings,
    DequeWithSum,
    indices,
    is_empty,
    make_mapping,
    map_mapping_values,
)
from ..commonlib.option import map_option, non_null
from ..commonlib.result_utils import (
    all_non_err,
    flat_map_ok,
    fn2_to_ok,
    map_ok,
    match,
    option_to_result,
    unwrap,
)
from ..commonlib.score_spec import ScoreElement
from ..commonlib.type_utils import enum_value
from ..commonlib.util import bytes_to_gb, bytes_to_mb, geometric_mean, get_percent, seconds_to_msec

from .aggregate_stats import get_aggregate_stats
from .enums import Gens, ServerGCThreadState
from .single_gc_metrics import get_bytes_allocated_since_last_gc, SINGLE_GC_METRIC_GETTERS
from .types import (
    Failable,
    FailableFloat,
    fn_of_property,
    FailableInt,
    NamedRunMetric,
    ProcessedGC,
    ProcessInfo,
    ProcessedTrace,
    RunMetric,
    RunMetrics,
    run_metric_must_exist_for_name,
    ScoreRunMetric,
    FailableValue,
    FailableValues,
)

_GCPERFSIM_RESULT_GETTERS: Mapping[NamedRunMetric, Callable[[GCPerfSimResult], FailableValue]] = {
    NamedRunMetric("InternalSecondsTaken", is_from_test_status=True): lambda g: Ok(g.seconds_taken),
    NamedRunMetric("FinalHeapSizeGB", is_from_test_status=True): lambda g: Err(
        "final_heap_size_bytes was not in test result, this can happen on runtimes < 3.0"
    )
    if g.final_heap_size_bytes is None
    else Ok(bytes_to_gb(g.final_heap_size_bytes)),
    NamedRunMetric("FinalFragmentationGB", is_from_test_status=True): lambda g: Err(
        "final_fragmentation_bytes was not in test result, this can happen on runtimes < 3.0"
    )
    if g.final_fragmentation_bytes is None
    else Ok(bytes_to_gb(g.final_fragmentation_bytes)),
    NamedRunMetric("FinalTotalMemoryGB", is_from_test_status=True): lambda g: Ok(
        bytes_to_gb(g.final_total_memory_bytes)
    ),
    NamedRunMetric("Gen0CollectionCount", is_from_test_status=True): lambda g: Ok(
        g.collection_counts[0]
    ),
    NamedRunMetric("Gen1CollectionCount", is_from_test_status=True): lambda g: Ok(
        g.collection_counts[1]
    ),
    NamedRunMetric("Gen2CollectionCount", is_from_test_status=True): lambda g: Ok(
        g.collection_counts[2]
    ),
}


def _gcperfsim_getter(
    cb: Callable[[GCPerfSimResult], FailableValue]
) -> Callable[[TestRunStatus], FailableValue]:
    return (
        lambda ts: Err("No gcperfsim_result")
        if ts.gcperfsim_result is None
        else cb(ts.gcperfsim_result)
    )


_TEST_STATUS_METRIC_GETTERS: Mapping[
    NamedRunMetric, Callable[[TestRunStatus], FailableValue]
] = combine_mappings(
    {
        NamedRunMetric("TotalSecondsTaken", is_from_test_status=True): lambda ts: Ok(
            ts.seconds_taken
        ),
        NamedRunMetric("Gen0Size", is_from_test_status=True): lambda ts: option_to_result(
            ts.test.config.config.complus_gcgen0size, lambda: "Gen0size not specified in config"
        ),
        NamedRunMetric("ThreadCount", is_from_test_status=True): lambda ts: option_to_result(
            map_option(ts.test.benchmark.benchmark.get_argument("-tc"), int),
            lambda: "tc not specified in benchmark",
        ),
    },
    map_mapping_values(_gcperfsim_getter, _GCPERFSIM_RESULT_GETTERS),
)

TEST_STATUS_METRICS: Sequence[NamedRunMetric] = tuple(_TEST_STATUS_METRIC_GETTERS.keys())


def stat_for_proc(proc: ProcessedTrace, metric: RunMetric) -> FailableValue:
    res = (
        _RUN_METRIC_GETTERS[metric](proc)
        if isinstance(metric, NamedRunMetric)
        else _value_for_score_metric(proc, metric)
    )
    # Type system not always reliable, so use this to report a bad metric
    assert isinstance(res, Result) and (
        res.is_err() or any(isinstance(res.unwrap(), t) for t in (bool, int, float))
    ), f"Getter for metric {metric} returned a {type(res)}"
    return res


def _value_for_score_metric(proc: ProcessedTrace, metric: ScoreRunMetric) -> FailableValue:
    all_par_set = all(x.par is not None for x in metric.spec.values())
    # If scorer.par is set, we take the mean of all fractional differences (* weight) from the par.
    # Else we take the geometric mean. (In this case, only the sign of weight matters.)
    def get_value(name: str, scorer: ScoreElement) -> FailableFloat:
        scored_metric = run_metric_must_exist_for_name(name)

        def handle_success(value: float) -> FailableFloat:
            if all_par_set:
                return Ok(get_signed_diff_fraction(value, non_null(scorer.par)) * scorer.weight)
            else:
                return Ok(value * scorer.weight)

        return match(
            _RUN_METRIC_GETTERS[scored_metric](proc),
            handle_success,
            lambda e: Err(f"In {scored_metric.name}: {e}"),
        )

    def f(values: Sequence[float]) -> float:
        if all_par_set:
            return sum(values) * 100
        else:
            # For negative values, lower is better.
            # So for geometric mean, use 1 / that.
            return geometric_mean([1 / -v if v < 0 else v for v in values])

    return map_ok(all_non_err([get_value(name, scorer) for name, scorer in metric.spec.items()]), f)


def _stats_list_for_proc(proc: ProcessedTrace, run_metrics: RunMetrics) -> FailableValues:
    if is_empty(proc.gcs):
        return Err("no gcs")
    else:
        return Ok([stat_for_proc(proc, metric) for metric in run_metrics])


# TODO: use new join analysis instead
def _get_stolen_cpu_times_getters() -> Mapping[
    NamedRunMetric, Callable[[ProcessedTrace], FailableFloat]
]:
    def get_value(
        gcs: Sequence[ProcessedGC], state: ServerGCThreadState, is_max: bool
    ) -> FailableFloat:
        # TODO: lazily compute (not more than once)
        by_state: Dict[ServerGCThreadState, List[float]] = {
            state: [] for state in ServerGCThreadState
        }
        for gc in gcs:
            for hp in gc.heaps:
                lost_cpu = gc.clr.Analysis.GetLostCpuBreakdownForHeap(
                    gc.trace_gc, unwrap(hp.server_gc_history)
                )
                for kv in lost_cpu:  # This is a C# dict, so no .items()
                    thread_state = ServerGCThreadState(kv.Key)
                    stolen_time = kv.Value.Item1
                    total_time = kv.Value.Item2
                    assert stolen_time <= total_time
                    by_state[thread_state].append(
                        0 if total_time == 0 else stolen_time / total_time
                    )

        values = by_state[state]
        return Ok(max(values) if is_max else mean(values))

    def get_metric(
        state_and_is_max: Tuple[ServerGCThreadState, bool]
    ) -> Tuple[NamedRunMetric, Callable[[ProcessedTrace], FailableFloat]]:
        state, is_max = state_and_is_max
        return (
            NamedRunMetric(f"{state.name}_stolen_cpu_fraction_{'max' if is_max else 'mean'}"),
            lambda proc: get_value(proc.gcs, state, is_max),
        )

    return make_mapping(
        map(
            get_metric,
            ((state, is_max) for state in ServerGCThreadState for is_max in (False, True)),
        )
    )


_STOLEN_CPU_TIMES_GETTERS: Mapping[
    NamedRunMetric, Callable[[ProcessedTrace], FailableFloat]
] = _get_stolen_cpu_times_getters()


def get_final_youngest_desired_bytes(proc: ProcessedTrace) -> int:
    return get_final_youngest_desired_bytes_for_process(non_null(proc.process_info))


def get_final_youngest_desired_bytes_for_process(proc: ProcessInfo) -> int:
    ghhs = [gc.GlobalHeapHistory for gc in proc.gcs if gc.GlobalHeapHistory is not None]
    assert (
        len(ghhs) > 10
    ), f"There are {len(proc.gcs)} gcs and only {len(ghhs)} have GlobalHeapHistory"

    # Since it takes time to stabilize,
    # use the last 5 GCs, which should all have the same stable value.
    fyd = ghhs[-1].FinalYoungestDesired
    for gc in ghhs[-5:]:
        its_fyd = gc.FinalYoungestDesired
        # May vary by a few bytes
        assert get_unsigned_diff_fraction(its_fyd, fyd) < 0.1

    return fyd


def get_unsigned_diff_fraction(a: float, b: float) -> float:
    assert a != 0 and b != 0
    return max(abs((a / b) - 1), abs((b / a) - 1))


def get_signed_diff_fraction(a: float, b: float) -> float:
    return (a / b) - 1


def _get_gcs_per_second(proc: ProcessedTrace, filtered_gcs: Sequence[ProcessedGC]) -> FailableFloat:
    return map_ok(proc.FirstToLastGCSeconds, lambda seconds: len(filtered_gcs) / seconds)


def _get_percent_time_in_gc(
    proc: ProcessedTrace, filtered_gcs: Sequence[ProcessedGC]
) -> FailableFloat:
    return map_ok(
        proc.FirstToLastGCSeconds,
        lambda seconds: get_percent(
            sum(gc.DurationMSec for gc in filtered_gcs) / seconds_to_msec(seconds)
        ),
    )


def _get_percent_time_paused_in_gc(proc: ProcessedTrace) -> FailableFloat:
    return map_ok(
        proc.FirstToLastGCSeconds,
        lambda seconds: get_percent(
            sum(
                gc.PauseDurationMSec
                for gc in non_null(proc.process_info).all_gcs_including_incomplete
            )
            / seconds_to_msec(seconds)
        ),
    )


def _get_gcs_without_outliers(proc: ProcessedTrace) -> Failable[Sequence[ProcessedGC]]:
    gen_infos = [_PrevGcInfo() for _ in range(3)]

    def is_outlier(gc: ProcessedGC) -> bool:
        gen_info = gen_infos[enum_value(gc.Generation)]
        prev_pause_time_mean = gen_info.prev_pause_times.mean()
        res = gc.PauseDurationMSec > 10 * prev_pause_time_mean and _is_within_50pct(
            gc.PromotedMB, gen_info.prev_promoted_mbs.mean()
        )
        if not res:
            gen_info.prev_promoted_mbs.push(gc.PromotedMB)
            gen_info.prev_pause_times.push(gc.PauseDurationMSec)
        return res

    return Ok([gc for gc in proc.gcs if not is_outlier(gc)])


def _get_allocation_speed(_proc: ProcessedTrace, gcs: Sequence[ProcessedGC]) -> float:
    # TODO: PromotedMB isn't all allocation, so this needs a better name.
    promoted_mb = sum(gc.PromotedMB for gc in gcs)
    pause_ms = sum(gc.PauseDurationMSec for gc in gcs)
    return promoted_mb / pause_ms


def _get_gc_aggregate_stats() -> Mapping[NamedRunMetric, Callable[[ProcessedTrace], FailableValue]]:
    return get_aggregate_stats(
        aggregate_metric_cls=NamedRunMetric,
        get_elements=lambda proc: proc.gcs_result,
        element_metric_to_get_value=SINGLE_GC_METRIC_GETTERS,
        additional_filters={"NoOutliers": _get_gcs_without_outliers},
        additional_aggregates={
            "PctTimeInGC": _get_percent_time_in_gc,
            "TotalNumberGCs": lambda _, gcs: Ok(len(gcs)),
            "GcsPerSecond": _get_gcs_per_second,
            "AllocationSpeed": fn2_to_ok(_get_allocation_speed),
        },
    )


def _get_num_heaps(proc: ProcessedTrace) -> FailableInt:
    def f(gcs: Sequence[ProcessedGC]) -> int:
        n_heaps = proc.gcs[0].trace_gc.HeapCount
        for i, gc in enumerate(gcs):
            assert gc.trace_gc.HeapCount == n_heaps
            if gc.trace_gc.GlobalHeapHistory is None:
                print(f"WARN: GC{i} has null GlobalHeapHistory. It's a {gc.Type}")
            phh_count = gc.HeapCount
            if n_heaps != phh_count:
                print(
                    f"WARN: GC{i} has {phh_count} PerHeapHistories but {n_heaps} heaps. "
                    + f"It's a {gc.Type}"
                )
        return n_heaps

    return map_ok(proc.gcs_result, f)


_RUN_METRIC_GETTERS: Mapping[
    NamedRunMetric, Callable[[ProcessedTrace], FailableFloat]
] = combine_mappings(
    {
        NamedRunMetric("NumHeaps"): _get_num_heaps,
        NamedRunMetric("FirstToLastEventSeconds"): fn_of_property(
            ProcessedTrace.FirstToLastEventSeconds
        ),
        NamedRunMetric("TotalNonGCSeconds"): fn_of_property(ProcessedTrace.TotalNonGCSeconds),
        NamedRunMetric("FirstToLastGCSeconds"): fn_of_property(ProcessedTrace.FirstToLastGCSeconds),
        NamedRunMetric("FirstEventToFirstGCSeconds"): fn_of_property(
            ProcessedTrace.FirstEventToFirstGCSeconds
        ),
        (NamedRunMetric("FinalYoungestDesiredMB")): lambda proc: Ok(
            bytes_to_mb(get_final_youngest_desired_bytes(proc))
        ),
        NamedRunMetric("TotalAllocatedMB"): fn_of_property(ProcessedTrace.TotalAllocatedMB),
        NamedRunMetric("TotalLOHAllocatedMB"): lambda proc: map_ok(
            flat_map_ok(proc.gcs_result, _get_total_loh_allocated_bytes), bytes_to_mb
        ),
        NamedRunMetric("PctTimePausedInGC"): _get_percent_time_paused_in_gc,
    },
    # This includes startup time so not a great metric
    # {NamedRunMetric("ProcessDurationMSec"): lambda proc: proc.stats.ProcessDuration},
    _STOLEN_CPU_TIMES_GETTERS,
    _get_gc_aggregate_stats(),
    map_mapping_values(
        lambda test_status_getter: lambda trace: Err("no test status")
        if trace.test_status is None
        else test_status_getter(trace.test_status),
        _TEST_STATUS_METRIC_GETTERS,
    ),
)

ALL_RUN_METRICS: Sequence[RunMetric] = tuple(_RUN_METRIC_GETTERS.keys())


# Note: excludes bytes allocated after the last GC
def _get_total_loh_allocated_bytes(gcs: Sequence[ProcessedGC]) -> Result[str, int]:
    gen2_gcs = [gc for gc in gcs if gc.Generation == Gens.Gen2]
    return map_ok(
        all_non_err(
            get_bytes_allocated_since_last_gc(gen2_gcs, i, Gens.GenLargeObj)
            for i in indices(gen2_gcs)
        ),
        sum,
    )


class _PrevGcInfo:
    prev_promoted_mbs = DequeWithSum(max_len=3)
    prev_pause_times = DequeWithSum(max_len=3)


def _is_within_50pct(a: float, b: float) -> bool:
    return b * 0.5 <= a <= b * 1.5
