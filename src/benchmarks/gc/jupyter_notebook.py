# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# See `docs/jupyter notebook.md` for how to use this file.

#%% setup cell (must run this)

from dataclasses import dataclass
from pathlib import Path
from typing import List, Optional, Sequence


from src.analysis.analyze_cpu_samples import (
    chart_cpu_samples_per_gcs,
    show_cpu_samples_metrics,
    TraceReadAndParseUtils,
)
from src.analysis.analyze_joins import (
    analyze_joins_all_gcs_for_jupyter,
    analyze_joins_single_gc_for_jupyter,
    StagesOrPhases,
)
from src.analysis.analyze_single import (
    analyze_single_for_processed_trace,
    analyze_single_gc_for_processed_trace_file,
    SortGCsBy,
)
from src.analysis.chart_individual_gcs import (
    chart_individual_gcs_for_jupyter,
    chart_individual_gcs_histogram_for_jupyter,
)
from src.analysis.chart_utils import (
    basic_chart,
    BasicHistogram,
    BasicLine,
    BasicLineChart,
    chart_histograms_from_fields,
    chart_lines_from_fields,
    chart_heaps,
    Trace,
)
from src.analysis.condemned_reasons import (
    show_brief_condemned_reasons_for_gc,
    show_condemned_reasons_for_jupyter,
    show_condemned_reasons_for_gc_for_jupyter,
)
from src.analysis.enums import Gens
from src.analysis.parse_metrics import (
    parse_run_metrics_arg,
    parse_single_gc_metric_arg,
    parse_single_gc_metrics_arg,
    parse_single_heap_metrics_arg,
)
from src.analysis.process_trace import ProcessedTraces, test_result_from_path
from src.analysis.report import diff_for_jupyter, report_reasons_for_jupyter
from src.analysis.single_gc_metrics import get_bytes_allocated_since_last_gc
from src.analysis.single_heap_metrics import ALL_GC_GENS
from src.analysis.trace_commands import print_events_for_jupyter
from src.analysis.types import GCKind, get_gc_kind, ProcessedTrace, SpecialSampleKind

from src.commonlib.bench_file import ProcessQuery, Vary
from src.commonlib.collection_util import repeat
from src.commonlib.document import Cell, handle_doc, Row, single_table_document, Table
from src.commonlib.option import non_null
from src.commonlib.result_utils import unwrap
from src.commonlib.type_utils import enum_value, with_slots
from src.commonlib.util import add_extension, bytes_to_mb, get_percent


ALL_TRACES = ProcessedTraces()


def get_trace_with_everything(
    path: Path, process: ProcessQuery = None, dont_cache: bool = False
) -> ProcessedTrace:
    return unwrap(
        ALL_TRACES.get(
            test_result=test_result_from_path(path, process),
            # TODO: disabling mechanisms and join info for now
            # as it doesn't work without updating TraceEvent
            need_mechanisms_and_reasons=False,
            need_join_info=False,
            dont_cache=dont_cache,
        )
    )


def show_summary(trace: ProcessedTrace) -> None:
    print(f"{trace.NumberGCs} GCs")
    for k, v in trace.number_gcs_in_each_generation.items():
        print(f"  {k.name}: {v}")
    print(f"{trace.HeapCount} heaps")
    for m in ("HeapSizeAfterMB_Mean", "HeapSizeAfterMB_Max"):
        print(f"{m}: {trace.unwrap_metric_from_name(m)}")
    # TODO: how to get mean/max memory load? Is it possible?

    metrics: Sequence[str] = ("PauseDurationMSec", "PromotedMBPerSec", "HeapSizeAfterMB")
    num_metrics = len(metrics)
    kind_to_metric_to_values: List[List[List[float]]] = [
        [[] for _ in range(num_metrics)] for _ in range(4)
    ]
    for gc in trace.gcs:
        gc_kind = get_gc_kind(gc)
        for metric_index, metric in enumerate(metrics):
            metric_index_to_values = kind_to_metric_to_values[enum_value(gc_kind)]
            values = metric_index_to_values[metric_index]
            values.append(gc.unwrap_metric_from_name(metric))

    for kind in GCKind:
        histograms: List[BasicHistogram] = []
        for metric_index, metric in enumerate(metrics):
            histograms.append(
                BasicHistogram(
                    values=kind_to_metric_to_values[enum_value(kind)][metric_index],
                    name=metric,
                    x_label=kind.name,
                )
            )

        basic_chart(histograms)


#%%

_BENCH = Path("bench")
_SUITE = Path("bench") / "suite"

_LOW_MEMORY_CONTAINER = _SUITE / "low_memory_container.yaml"
_OUT = add_extension(_LOW_MEMORY_CONTAINER, ".out")

_TRACE = get_trace_with_everything(_OUT / "a__only_config__tlgb0.2__0.yaml")
_TRACE2 = get_trace_with_everything(_OUT / "b__only_config__tlgb0.2__0.yaml")

#%% Load and read trace with CPU Samples

# This example will only work if the normal_server scenario was run with "collect"
# set to, either "cpu_samples" or "thread_times". If you captured your trace
# elsewhere, or ran another scenario, feel free to use that here instead. Just
# make sure to not commit those changes in case you modify this codebase further.

_BENCH = Path("bench")
_SUITE = Path("bench") / "suite"

_NORMAL_SERVER_WSAMPLES = add_extension(_SUITE / "normal_server", "yaml.out")
_SAMPLES_TRACE = get_trace_with_everything(_NORMAL_SERVER_WSAMPLES / "a__only_config__2gb__0.yaml")

#%% Set up the trace, symbols, etc and get it ready for CPU Samples Analysis.

# The "symbol_path" value set here is just a placeholder. Change it to point to
# where you have your PDB's stored.

_SAMPLES_TRACE_ALL_DATA = TraceReadAndParseUtils(
    ptrace=_SAMPLES_TRACE,
    symbol_path=Path("C:/runtime/artifacts/bin/coreclr/Windows_NT.x64.Release/PDB"),
)

#%% Example: Chart the number of samples per individual GC's, for all Gen1 GC's,
# for the functions "gc_heap::plan_phase" and "gc_heap::mark_phase", and their callees.

chart_cpu_samples_per_gcs(
    ptraces_utils=(_SAMPLES_TRACE_ALL_DATA,),
    functions_to_chart=("gc_heap::plan_phase", "gc_heap::mark_phase"),
    x_property_name="gc_index",
    y_property_names=("inclusive_count",),
    gc_filter=lambda gc: gc.Generation == Gens.Gen1,
)

#%% Example: Show CPU Samples metrics within a specified interval of time (1-5 secs),
# for the function "gc_heap::plan_phase".

show_cpu_samples_metrics(
    ptrace_utils=_SAMPLES_TRACE_ALL_DATA,
    function="gc_heap::plan_phase",
    start_time_msec=1000.00,
    end_time_msec=5000.00,
)

#%% show summary

show_summary(_TRACE)


#%% analyze-single

handle_doc(
    analyze_single_for_processed_trace(
        _TRACE,
        print_events=False,
        run_metrics=parse_run_metrics_arg(("important",)),
        gc_where_filter=lambda gc: True,
        sort_gcs_by=SortGCsBy(metric=parse_single_gc_metric_arg("Number"), sort_reverse=False),
        single_gc_metrics=parse_single_gc_metrics_arg(
            ("DurationMSec", "Generation", "Number", "StartMSec")
        ),
        single_heap_metrics=parse_single_heap_metrics_arg(("InMB", "OutMB")),
        show_first_n_gcs=5,
        show_last_n_gcs=None,
        show_reasons=False,
    )
)

#%% analyze-single-gc

handle_doc(
    analyze_single_gc_for_processed_trace_file(
        _TRACE,
        gc_number=42,
        single_gc_metrics=parse_single_gc_metrics_arg(
            ("DurationMSec", "Generation", "Number", "StartMSec")
        ),
    )
)

#%% analyze-joins-all-gcs

handle_doc(
    analyze_joins_all_gcs_for_jupyter(
        _TRACE, show_n_worst_stolen_time_instances=10, show_n_worst_joins=10
    )
)


#%% analyze-joins-single-gc

handle_doc(
    analyze_joins_single_gc_for_jupyter(
        trace=_TRACE,
        gc_number=42,
        kind=StagesOrPhases.both,
        only_stages_with_percent_time=5,
        show_n_worst_stolen_time_instances=10,
    )
)

#%% chart-individual-gcs

# matplotlib automatically outputs to jupyter notebook through magic
chart_individual_gcs_for_jupyter(
    traces=(_TRACE,),
    gc_where_filter=lambda gc: gc.index < 100,
    x_single_gc_metric=parse_single_gc_metric_arg("Number"),
    y_single_gc_metrics=parse_single_gc_metrics_arg(("DurationMSec",)),
    single_heap_metrics=parse_single_heap_metrics_arg(()),
    show_gen_as_xticks=True,
    show_n_heaps=4,
)

#%% chart-individual-gcs-histogram

chart_individual_gcs_histogram_for_jupyter(
    trace=_TRACE,
    single_gc_metrics=parse_single_gc_metrics_arg(("DurationMSec",)),
    gc_where_filter=lambda gc: gc.Generation == Gens.Gen0,
    bins=16,
)

#%% diff

handle_doc(
    diff_for_jupyter(
        traces=ALL_TRACES,
        trace_paths=(_LOW_MEMORY_CONTAINER,),
        run_metrics=parse_run_metrics_arg(("important",)),
        machines=None,
        vary=Vary.config,
        test_where=None,
        sample_kind=SpecialSampleKind.median,
        max_iterations=None,
        metrics_as_columns=False,
        no_summary=False,
        # Only for metrics_as_columns
        sort_by_metric=None,
        min_difference_pct=5,
        process=("name:corerun",),
    )
)


#%% report-reasons

handle_doc(
    report_reasons_for_jupyter(
        traces=ALL_TRACES, bench_file_path=_LOW_MEMORY_CONTAINER, max_iterations=None
    )
)

#%% show-condemned-reasons

handle_doc(
    show_condemned_reasons_for_jupyter(
        trace=_TRACE,
        gc_where_filter=lambda gc: ((gc.Generation != Gens.Gen0) and (gc.Number < 1000)),
    )
)

#%% show-condemned-reasons-for-gc


handle_doc(show_condemned_reasons_for_gc_for_jupyter(trace=_TRACE, gc_number=42))


#%% show-condemned-reasons custom


def _show_condemned_reasons_for_gen2(trace: ProcessedTrace) -> None:
    gcs = [gc for gc in trace.gcs if gc.IsGen2]
    for gc in gcs:
        print(f"gc {gc.Number}")
        print(show_brief_condemned_reasons_for_gc(gc))


_show_condemned_reasons_for_gen2(_TRACE)


#%% print-events

print_events_for_jupyter(
    path=non_null(_TRACE.test_result.trace_path), time_span_msec=(0, 100), include="thread_times"
)


#%% custom analysis

total_duration = sum(gc.DurationMSec for gc in _TRACE.gcs)
assert total_duration == _TRACE.unwrap_metric_from_name("DurationMSec_Sum")
print(
    f"{len(_TRACE.gcs)} gcs * {total_duration / len(_TRACE.gcs)} msec avg = {total_duration} msec"
)


#%% custom charting


def _custom_chart() -> None:
    xs = tuple(range(8))
    basic_chart(
        (
            BasicLineChart(
                lines=(
                    BasicLine(name="linear", xs=xs, ys=xs),
                    BasicLine(name="quadratic", xs=xs, ys=[x ** 2 for x in xs]),
                ),
                x_label="x",
                y_label="y",
            ),
            BasicHistogram(values=[x for n in range(4) for x in repeat(n, n)], x_label="number"),
        )
    )


_custom_chart()

#%% more custom charting

# gen size of gen2 after a bgc
# free list space before and after a bgc


# Create a 'trace' which contains the data we want
@with_slots
@dataclass(frozen=True)
class MyGCData:
    Number: int
    start_time: float
    duration_msec: float
    alloced_mb: float
    Gen2SizeBeforeMB: float
    Gen2SizeAfterMB: float
    Gen2FreeListSpaceBeforeMB: float
    Gen2FreeListSpaceAfterMB: float


def get_data(trace: ProcessedTrace) -> Trace[MyGCData]:
    data = []
    for gc in _TRACE.gcs:
        if gc.Generation == Gens.Gen2:
            assert gc.IsConcurrent
            data.append(
                MyGCData(
                    Number=gc.Number,
                    start_time=gc.StartRelativeMSec,
                    duration_msec=gc.DurationMSec,
                    alloced_mb=gc.AllocedSinceLastGCMB,
                    Gen2SizeBeforeMB=gc.Gen2SizeBeforeMB,
                    Gen2SizeAfterMB=gc.Gen2SizeAfterMB,
                    Gen2FreeListSpaceBeforeMB=gc.Gen2FreeListSpaceBeforeMB,
                    Gen2FreeListSpaceAfterMB=gc.Gen2FreeListSpaceAfterMB,
                )
            )

    return Trace(name=trace.name, data=data)


traces: Sequence[Trace[MyGCData]] = [get_data(trace) for trace in (_TRACE, _TRACE2)]
chart_lines_from_fields(
    t=MyGCData,
    traces=traces,
    x_property_name="Number",
    y_property_names=("Gen2FreeListSpaceBeforeMB", "Gen2FreeListSpaceAfterMB"),
)

#%%

chart_histograms_from_fields(
    t=MyGCData, gcs=get_data(_TRACE).data, property_names=("duration_msec", "alloced_mb")
)


#%% per-heap custom charting


@with_slots
@dataclass(frozen=True)
class MyHeapData:
    gen0_in_mb: float
    gen0_out_mb: float


def _custom_chart_heaps(trace: ProcessedTrace) -> None:
    heaps: Sequence[List[MyHeapData]] = [[] for _ in range(trace.HeapCount)]
    for gc in trace.gcs:
        if gc.Generation == Gens.Gen0:
            for hp in gc.heaps:
                gn = hp.gen(Gens.Gen0)
                heaps[hp.index].append(MyHeapData(gen0_in_mb=gn.in_mb, gen0_out_mb=gn.out_mb))

    chart_heaps(
        t=MyHeapData,
        heaps=heaps,
        y_property_names=("gen0_in_mb", "gen0_out_mb"),
        heap_indices=(0, 1),
        xs=None,
        x_label=None,
    )


_custom_chart_heaps(_TRACE)

#%% another per-heap custom charting


@with_slots
@dataclass(frozen=True)
class _HeapData:
    gc_number: int
    budget_mb: float
    allocated_mb: float


def _custom_chart_heaps_2(trace: ProcessedTrace) -> None:
    # A different chart for each heap
    heaps: Sequence[List[_HeapData]] = [[] for _ in range(trace.HeapCount)]
    prev_non_free_size_after: List[int] = [0 for _ in range(trace.HeapCount)]
    for gc in trace.gcs:
        if gc.Generation != Gens.Gen2:
            continue

        budget_per_heap = gc.LOHBudgetMB / len(heaps)

        for hp_i, hp in enumerate(gc.heaps):
            # Want the difference in size before -- get prev gen2 gc
            prev_size_after = prev_non_free_size_after[hp_i]
            gen = hp.gen(Gens.GenLargeObj)
            size_before_now = gen.non_free_size_before
            size_after_now = gen.non_free_size_after
            allocated_bytes = size_before_now - prev_size_after
            prev_non_free_size_after[hp_i] = size_after_now
            # gen.budget is before equalizing
            heaps[hp_i].append(
                _HeapData(
                    gc_number=gc.Number,
                    budget_mb=budget_per_heap,
                    allocated_mb=bytes_to_mb(allocated_bytes),
                )
            )

    # Chart each hp
    lines = []
    for i, hp_data in enumerate(heaps):
        xs = [d.gc_number for d in hp_data]
        line0 = BasicLine(name="budget (MB)", xs=xs, ys=[d.budget_mb for d in hp_data])
        line1 = BasicLine(name="allocated (MB)", xs=xs, ys=[d.allocated_mb for d in hp_data])
        lines.append(BasicLineChart(name=f"hp{i}", lines=(line0, line1)))
    basic_chart(lines)


_custom_chart_heaps_2(_TRACE)


#%% summary

show_summary(_TRACE)


#%% custom analysis


@with_slots
@dataclass(frozen=True)
class _SuspensionData:
    PauseToStartMSec: float
    DurationMSec: float
    PauseDurationMSec: float
    SuspendDurationMSec: float
    PromotedMB: float

    @property
    def SuspensionPercent(self) -> float:
        return get_percent(self.SuspendDurationMSec / self.PauseDurationMSec)

    @property
    def PromotedMBPerSec(self) -> float:
        return self.PromotedMB / (self.PauseDurationMSec / 1000)

    @property
    def PctPauseFromSuspend(self) -> float:
        return get_percent(self.SuspendDurationMSec / self.PauseToStartMSec)


def _custom2(trace: ProcessedTrace) -> None:
    gen_to_suspension_datas: List[List[_SuspensionData]] = [[] for _ in range(3)]

    for gc in trace.gcs:
        if gc.PauseDurationMSec < 50:
            continue

        gen_to_suspension_datas[enum_value(gc.Generation)].append(
            _SuspensionData(
                PauseToStartMSec=gc.SuspendToGCStartMSec,
                DurationMSec=gc.DurationMSec,
                PauseDurationMSec=gc.PauseDurationMSec,
                SuspendDurationMSec=gc.SuspendDurationMSec,
                PromotedMB=gc.PromotedMB,
            )
        )

    for gen in ALL_GC_GENS:
        print(f"\n=== {gen.name} suspensions ===\n")
        rows = []
        for susp_data in sorted(
            gen_to_suspension_datas[0], key=lambda sd: sd.DurationMSec, reverse=True
        ):
            rows.append(
                [
                    Cell(x)
                    for x in (
                        susp_data.PauseDurationMSec,
                        susp_data.DurationMSec,
                        # susp_data.PctPauseFromSuspend,
                        # susp_data.PauseToStartMSec,
                        susp_data.SuspendDurationMSec,
                        # susp_data.SuspensionPercent,
                        susp_data.PromotedMB,
                        susp_data.PromotedMBPerSec,
                    )
                ]
            )
        handle_doc(
            single_table_document(
                Table(
                    headers=(
                        "pause msec",
                        "duration msec",
                        # "pause %",
                        # "pause to start",
                        "suspend msec",
                        # "suspend %",
                        "promoted mb",
                        "promoted mb/sec",
                    ),
                    rows=rows,
                )
            )
        )


_custom2(_TRACE)

#%% another custom analysis


@with_slots
@dataclass(frozen=True)
class _GCData:
    Number: int
    MBSOHSinceLastGen2: Optional[float]
    MBLOHSinceLastGen2: Optional[float]
    Gen2BudgetMB: Optional[float]
    LOHBudgetMB: Optional[float]


def _custom(trace: ProcessedTrace) -> None:
    gen2_gcs = [gc for gc in trace.gcs if gc.IsGen2]
    datas: List[_GCData] = []
    for gc in gen2_gcs:
        bytes_since_last_same_gen_gc = (
            unwrap(
                get_bytes_allocated_since_last_gc(trace.gcs, trace.gcs.index(gc), Gens.GenLargeObj)
            )
            if gc.IsGen2
            else None
        )

        datas.append(
            _GCData(
                Number=gc.Number,
                MBSOHSinceLastGen2=bytes_to_mb(
                    unwrap(
                        get_bytes_allocated_since_last_gc(trace.gcs, trace.gcs.index(gc), Gens.Gen2)
                    )
                )
                if gc.IsGen2
                else None,
                MBLOHSinceLastGen2=bytes_to_mb(non_null(bytes_since_last_same_gen_gc))
                if gc.IsGen2
                else None,
                Gen2BudgetMB=gc.Gen2BudgetMB if gc.IsGen2 else None,
                LOHBudgetMB=gc.LOHBudgetMB if gc.IsGen2 else None,
            )
        )

    rows = []
    for data in datas:
        rows.append(
            [
                Cell(str(int(x))) if x is not None else Cell()
                for x in (
                    data.Number,
                    data.MBSOHSinceLastGen2,
                    data.MBLOHSinceLastGen2,
                    data.Gen2BudgetMB,
                    data.LOHBudgetMB,
                )
            ]
        )

    g2_numbers = ", ".join(str(gc.Number) for gc in gen2_gcs)
    gens = f"Gen2 numbers are: {g2_numbers}"

    doc = single_table_document(
        Table(
            text=gens,
            headers=(
                "number",
                "MB on SOH since last gen2",
                "MB on LOH since last gen2",
                "gen2 budget MB",
                "loh budget MB",
            ),
            rows=rows,
        )
    )
    handle_doc(doc)


_custom(_TRACE)


#%% yet more custom charting


def _more_custom(trace: ProcessedTrace) -> None:
    rows: List[Row] = []
    for gc in trace.gcs:
        if not gc.IsBackground:
            continue
        rows.append((Cell(gc.Number), Cell(str(gc.reason))))

    handle_doc(single_table_document(Table(headers=("number", "reason"), rows=rows)))


_more_custom(_TRACE)


# %%
