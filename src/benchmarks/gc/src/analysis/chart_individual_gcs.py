# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from pathlib import Path
from typing import Callable, List, Mapping, Optional, Sequence

from matplotlib.axes._subplots import SubplotBase
from matplotlib.lines import Line2D

from ..commonlib.collection_util import is_empty, map_non_null_together, zip_check
from ..commonlib.command import Command, CommandKind, CommandsMapping
from ..commonlib.option import non_null
from ..commonlib.result_utils import all_non_err, ignore_err
from ..commonlib.type_utils import argument, with_slots

from .chart_utils import Color, OUT_SVG_DOC, set_axes, show_or_save, subplots, zip_with_colors
from .clr import get_clr
from .core_analysis import PROCESS_DOC, TRACE_PATH_DOC
from .enums import GCType, Gens
from .parse_metrics import (
    parse_single_gc_metric_arg,
    parse_single_gc_metrics_arg,
    parse_single_heap_metrics_arg,
)
from .process_trace import get_processed_trace, test_result_from_path
from .types import (
    ProcessedGC,
    ProcessedTrace,
    ProcessQuery,
    SingleGCMetric,
    SingleGCMetrics,
    SINGLE_GC_METRICS_DOC,
    SingleHeapMetric,
    SINGLE_HEAP_METRICS_DOC,
    SingleHeapMetrics,
)
from .where import get_where_filter_for_gcs, GC_WHERE_DOC


def _gen_to_str(gc: ProcessedGC) -> Optional[str]:
    bg = {GCType.NonConcurrentGC: "", GCType.ForegroundGC: "F", GCType.BackgroundGC: "B"}[gc.Type]
    # Too noisy if we show this for all GCs, just show for gen2
    return f"{gc.Generation.name}{bg}" if gc.Generation == Gens.Gen2 else None


@with_slots
@dataclass(frozen=True)
class ChartIndividualGcsArgs:
    trace_files: Sequence[Path] = argument(
        name_optional=True,
        doc="""
        Paths to individual trace file(s) to chart GCs for.
        When there are multiple of these you can compare different traces.
        """,
    )
    process: ProcessQuery = argument(default=None, doc=PROCESS_DOC)
    x_single_gc_metric: str = argument(
        default="Number", doc="What single-gc metric to plot on the x axis."
    )
    show_gen_as_xticks: bool = argument(
        default=False, doc="If set, markers will be put on the x axis showing GC generations."
    )
    y_single_gc_metrics: Optional[Sequence[str]] = argument(
        default=None,
        doc=f"""
    {SINGLE_GC_METRICS_DOC}

    Each of these will be given its own chart where it is plotted on the y-axis.
    """,
    )
    y_single_heap_metrics: Optional[Sequence[str]] = argument(
        default=None,
        doc=f"""
        {SINGLE_HEAP_METRICS_DOC}

        Each of these will be given its own chart where each heaps is plotted as a separate line.
        This may be noisy so consider using '--show-n-heaps'.
        """,
    )
    show_n_heaps: Optional[int] = argument(
        default=None,
        doc="When '--single-heap-metrics' is set, only chart the first N heaps to reduce clutter.",
    )
    out: Optional[Path] = argument(default=None, doc=OUT_SVG_DOC)
    gc_where: Optional[Sequence[str]] = argument(default=None, doc=GC_WHERE_DOC)


@with_slots
@dataclass(frozen=True)
class ProcAndGCs:
    proc: ProcessedTrace
    # These are the GCs filtered by '--gc-where'.
    # `proc.gcs` is all the GCs.
    gcs: Sequence[ProcessedGC]


def chart_individual_gcs(args: ChartIndividualGcsArgs) -> None:
    single_heap_metrics = parse_single_heap_metrics_arg(
        args.y_single_heap_metrics, default_to_important=False
    )
    x_single_gc_metric = parse_single_gc_metric_arg(args.x_single_gc_metric)
    y_single_gc_metrics = parse_single_gc_metrics_arg(
        args.y_single_gc_metrics, default_to_important=is_empty(single_heap_metrics)
    )

    gc_where_filter = get_where_filter_for_gcs(args.gc_where)

    clr = get_clr()
    traces = all_non_err(
        [
            get_processed_trace(
                clr=clr,
                test_result=test_result_from_path(trace_path),
                process=args.process,
                need_mechanisms_and_reasons=False,
                need_join_info=False,
            )
            for trace_path in args.trace_files
        ]
    ).unwrap()

    chart_individual_gcs_for_jupyter(
        traces=traces,
        gc_where_filter=gc_where_filter,
        x_single_gc_metric=x_single_gc_metric,
        y_single_gc_metrics=y_single_gc_metrics,
        single_heap_metrics=single_heap_metrics,
        show_gen_as_xticks=args.show_gen_as_xticks,
        show_n_heaps=args.show_n_heaps,
    )
    show_or_save(args.out)


def chart_individual_gcs_for_jupyter(
    traces: Sequence[ProcessedTrace],
    gc_where_filter: Callable[[ProcessedGC], bool],
    x_single_gc_metric: SingleGCMetric,
    y_single_gc_metrics: SingleGCMetrics,
    single_heap_metrics: SingleHeapMetrics,
    show_gen_as_xticks: bool,
    show_n_heaps: Optional[int],
) -> None:
    # TODO: this will fail if two processes happen to have the same id -- use an index instead
    procs_and_gcs = [
        ProcAndGCs(trace, [gc for gc in trace.gcs if gc_where_filter(gc)]) for trace in traces
    ]

    for proc_and_gcs in procs_and_gcs:
        if is_empty(proc_and_gcs.gcs):
            print(f"WARNING: no gcs (after filtering) for process {proc_and_gcs.proc.name}")
        if len(proc_and_gcs.gcs) > 400:
            print(
                f"Got {len(proc_and_gcs.gcs)} gcs -- matplotlib will likely freeze.\n"
                + "Consider passing a filter like `--gc-where Index<400`."
            )

    n_subplots = len(y_single_gc_metrics) + len(traces) * len(single_heap_metrics)
    _fig, axes = subplots(n_subplots, individual_figure_size=(8, 4))
    subplot_idx = 0

    def get_subplot() -> SubplotBase:
        nonlocal subplot_idx
        res = axes[subplot_idx]
        subplot_idx += 1
        return res

    # For each single_gc_metric, plot for each process together
    for y_single_gc_metric in y_single_gc_metrics:
        _plot_single_gc_metric(
            ax=get_subplot(),
            procs_and_gcs=procs_and_gcs,
            x_single_gc_metric=x_single_gc_metric,
            y_single_gc_metric=y_single_gc_metric,
            show_gen_as_xticks=show_gen_as_xticks,
        )

    if not is_empty(single_heap_metrics):
        # For single-heap metrics, plot each process separately
        for proc_and_gcs in procs_and_gcs:
            _plot_single_heap_metrics(
                proc_and_gcs, x_single_gc_metric, single_heap_metrics, get_subplot, show_n_heaps
            )

    assert subplot_idx == n_subplots


def _plot_single_gc_metric(
    ax: SubplotBase,
    procs_and_gcs: Sequence[ProcAndGCs],
    x_single_gc_metric: SingleGCMetric,
    y_single_gc_metric: SingleGCMetric,
    show_gen_as_xticks: bool,
) -> None:
    ax.set_xlabel(x_single_gc_metric.name)
    ax.set_ylabel(y_single_gc_metric.name)

    def get_line(proc_and_gcs: ProcAndGCs, color: Color) -> Line2D:
        xs, ys = map_non_null_together(
            proc_and_gcs.gcs,
            lambda gc: ignore_err(gc.metric(x_single_gc_metric)),
            lambda gc: ignore_err(gc.metric(y_single_gc_metric)),
        )
        (p,) = ax.plot(
            xs,
            ys,
            marker=".",
            linestyle="-",
            color=color,
            label=f"{proc_and_gcs.proc.name}\\{y_single_gc_metric.name}",
        )

        if show_gen_as_xticks:
            assert len(procs_and_gcs) == 1
            _set_xticks_for_gcs(ax, proc_and_gcs.gcs, x_single_gc_metric)

        return p

    plots = [
        get_line(proc_and_gcs, color) for proc_and_gcs, color in zip_with_colors(procs_and_gcs)
    ]
    ax.legend(handles=plots, loc="upper center", bbox_to_anchor=(0.5, 1.25))

    set_axes(ax, zero_x=False, zero_y=False, ranges=None)


def _plot_single_heap_metrics(
    proc_and_gcs: ProcAndGCs,
    x_single_gc_metric: SingleGCMetric,
    y_single_heap_metrics: SingleHeapMetrics,
    get_subplot: Callable[[], SubplotBase],
    show_n_heaps: Optional[int],
) -> None:
    n_heaps_to_show = (
        non_null(proc_and_gcs.gcs[0].HeapCount) if show_n_heaps is None else show_n_heaps
    )
    if n_heaps_to_show > 4:
        print(f"This will show {n_heaps_to_show} heaps. Consider passing `--show-n-heaps`.")

    def plot_for_metric(y_single_heap_metric: SingleHeapMetric) -> None:
        ax = get_subplot()
        _set_xticks_for_gcs(ax, proc_and_gcs.gcs, x_single_gc_metric)
        ax.set_xlabel(x_single_gc_metric.name)
        ax.set_ylabel(y_single_heap_metric.name)

        def plot_line(heap_index: int, color: Color) -> None:
            xs, ys = map_non_null_together(
                proc_and_gcs.gcs,
                lambda gc: ignore_err(gc.metric(x_single_gc_metric)),
                lambda gc: ignore_err(gc.heaps[heap_index].metric(y_single_heap_metric)),
            )
            ax.plot(
                xs,
                ys,
                marker=".",
                linestyle="-",
                color=color,
                label=f"{proc_and_gcs.proc.name}\\{y_single_heap_metric.name}\\{heap_index}",
            )

        for heap_index, color in zip_with_colors(range(n_heaps_to_show)):
            plot_line(heap_index, color)
        # ax.legend()

    for metric in y_single_heap_metrics:
        plot_for_metric(metric)


def _set_xticks_for_gcs(ax: SubplotBase, gcs: Sequence[ProcessedGC], x: SingleGCMetric) -> None:
    # Note: If no gen2 gcs, this results in an empty list
    xs, ys = map_non_null_together(gcs, lambda gc: ignore_err(gc.metric(x)), _gen_to_str)
    ax.set_xticks(xs)
    ax.set_xticklabels(ys)


@with_slots
@dataclass(frozen=True)
class ChartIndividualGcsHistogramArgs:
    trace_path: Path = argument(name_optional=True, doc=TRACE_PATH_DOC)
    process: Optional[Sequence[str]] = argument(default=None, doc=PROCESS_DOC)
    single_gc_metrics: Optional[Sequence[str]] = argument(
        default=None,
        doc=f"""
    {SINGLE_GC_METRICS_DOC}
    
    There will be a separate histogram for each metric.""",
    )
    bins: int = argument(default=16, doc="Show this many bins on the histogram. Default is 16.")
    gc_where: Optional[Sequence[str]] = argument(default=None, doc=GC_WHERE_DOC)
    out: Optional[Path] = argument(default=None, doc=OUT_SVG_DOC)


# TODO: support multiple args.trace_path for comparing histograms
def chart_individual_gcs_histogram(args: ChartIndividualGcsHistogramArgs) -> None:
    single_gc_metrics = parse_single_gc_metrics_arg(
        args.single_gc_metrics, default_to_important=True
    )
    gc_where_filter = get_where_filter_for_gcs(args.gc_where)

    trace = get_processed_trace(
        clr=get_clr(),
        test_result=test_result_from_path(args.trace_path),
        process=args.process,
        need_mechanisms_and_reasons=False,
        need_join_info=False,
    ).unwrap()

    chart_individual_gcs_histogram_for_jupyter(
        trace, single_gc_metrics, gc_where_filter, bins=args.bins
    )
    show_or_save(args.out)


def chart_individual_gcs_histogram_for_jupyter(
    trace: ProcessedTrace,
    single_gc_metrics: SingleGCMetrics,
    gc_where_filter: Callable[[ProcessedGC], bool],
    bins: int,
) -> None:
    gcs = [gc for gc in trace.gcs if gc_where_filter(gc)]

    stat_values: Mapping[SingleGCMetric, List[float]] = {m: [] for m in single_gc_metrics}

    for gc in gcs:
        for single_gc_metric in single_gc_metrics:
            value = ignore_err(gc.metric(single_gc_metric))
            if value is not None:
                stat_values[single_gc_metric].append(value)

    _fig, axes = subplots(len(single_gc_metrics), individual_figure_size=(8, 4))

    for ax, single_gc_metric in zip_check(axes, single_gc_metrics):
        ax.set_xlabel(single_gc_metric.name)
        ax.set_ylabel("# occurrences")
        ax.hist(stat_values[single_gc_metric], bins=bins)


CHART_INDIVIDUAL_GCS_COMMANDS: CommandsMapping = {
    "chart-individual-gcs": Command(
        kind=CommandKind.analysis,
        fn=chart_individual_gcs,
        doc="""
    Plots metrics for each individual GC over time.
    As this will only operate on a few trace files,
    it takes them directly as arguments instead of the benchfile.
    """,
    ),
    "chart-individual-gcs-histogram": Command(
        kind=CommandKind.analysis,
        fn=chart_individual_gcs_histogram,
        doc="""
        Plot a histogram of a metric's value over all GCs in the trace.
        """,
    ),
}
