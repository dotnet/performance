# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from math import inf
from pathlib import Path
from typing import Callable, List, Optional, Sequence

from ..commonlib.bench_file import try_find_benchfile_from_trace_file_path
from ..commonlib.collection_util import cat_unique, identity, is_empty, items_sorted_by_key
from ..commonlib.command import Command, CommandKind, CommandsMapping
from ..commonlib.document import (
    Cell,
    Document,
    handle_doc,
    OutputOptions,
    OutputWidth,
    OUTPUT_WIDTH_DOC,
    Row,
    Section,
    Table,
    TABLE_INDENT_DOC,
    TXT_DOC,
)
from ..commonlib.option import map_option, non_null, optional_to_iter
from ..commonlib.result_utils import match
from ..commonlib.type_utils import argument, with_slots

from .analyze_joins import show_time_span_start_end
from .clr import get_clr
from .core_analysis import GC_NUMBER_DOC, PROCESS_DOC, SINGLE_PATH_DOC, TRACE_PATH_DOC
from .parse_metrics import (
    get_parsed_and_score_metrics,
    parse_single_gc_metric_arg,
    parse_single_gc_metrics_arg,
    parse_single_heap_metrics_arg,
)
from .process_trace import get_processed_trace, test_result_from_path
from .single_gc_metrics import get_gc_with_number
from .types import (
    EventNames,
    MetricBase,
    ProcessedGC,
    ProcessQuery,
    ProcessedTrace,
    RunMetrics,
    RUN_METRICS_DOC,
    SingleGCMetric,
    SingleGCMetrics,
    SINGLE_GC_METRICS_DOC,
    SingleHeapMetrics,
    SINGLE_HEAP_METRICS_DOC,
    AnyValue,
    FailableValue,
)
from .where import get_where_filter_for_gcs, GC_WHERE_DOC


@with_slots
@dataclass(frozen=True)
class _AnalyzeSingleArgs:
    path: Path = argument(doc=SINGLE_PATH_DOC, name_optional=True)
    process: ProcessQuery = argument(default=None, doc=PROCESS_DOC)
    run_metrics: Optional[Sequence[str]] = argument(default=None, doc=RUN_METRICS_DOC)
    single_gc_metrics: Optional[Sequence[str]] = argument(default=None, doc=SINGLE_GC_METRICS_DOC)
    single_heap_metrics: Optional[Sequence[str]] = argument(
        default=None, doc=SINGLE_HEAP_METRICS_DOC
    )
    show_events: bool = argument(
        default=False, doc="If true, prints the event names available in this trace file."
    )
    gc_where: Optional[Sequence[str]] = argument(default=None, doc=GC_WHERE_DOC)
    sort_gcs_ascending: Optional[str] = argument(
        default=None, doc="Name of a single-gc metric to sort GCs by."
    )
    sort_gcs_descending: Optional[str] = argument(
        default=None,
        doc="""
    Name of a single-gc- metric to sort GCs by.
    Should not be set if '--sort-gcs-ascending' is.
    """,
    )
    show_first_n_gcs: Optional[int] = argument(
        default=None,
        doc="""
    We will print at most this many gcs (that match '--gc-where').
    Default is 10.
    """,
    )
    show_last_n_gcs: Optional[int] = argument(
        default=None,
        doc="""
    We will print at most this many gcs (that match '--gc-where'), starting from the last.
    Should not be set if '--show-first-n-gcs' is.
    """,
    )

    output_width: Optional[OutputWidth] = argument(default=None, doc=OUTPUT_WIDTH_DOC)
    table_indent: Optional[int] = argument(default=None, doc=TABLE_INDENT_DOC)
    txt: Optional[Path] = argument(default=None, doc=TXT_DOC)


def analyze_single(args: _AnalyzeSingleArgs) -> None:
    handle_doc(
        _get_analyze_single_document(args),
        OutputOptions(width=args.output_width, table_indent=args.table_indent, txt=args.txt),
    )


@with_slots
@dataclass(frozen=True)
class _AnalyzeSingleGcArgs:
    trace_path: Path = argument(name_optional=True, doc=TRACE_PATH_DOC)
    gc_number: int = argument(doc=GC_NUMBER_DOC)
    process: Optional[Sequence[str]] = argument(default=None, doc=PROCESS_DOC)
    single_gc_metrics: Optional[Sequence[str]] = argument(default=None, doc=SINGLE_GC_METRICS_DOC)


def _analyze_single_gc(args: _AnalyzeSingleGcArgs) -> None:
    single_gc_metrics = parse_single_gc_metrics_arg(
        args.single_gc_metrics, default_to_important=True
    )

    trace = get_processed_trace(
        clr=get_clr(),
        test_result=test_result_from_path(args.trace_path),
        process=args.process,
        need_mechanisms_and_reasons=False,
        need_join_info=False,
    ).unwrap()

    handle_doc(
        analyze_single_gc_for_processed_trace_file(
            trace=trace, gc_number=args.gc_number, single_gc_metrics=single_gc_metrics
        )
    )


def analyze_single_gc_for_processed_trace_file(
    trace: ProcessedTrace, gc_number: int, single_gc_metrics: SingleGCMetrics
) -> Document:
    gc = get_gc_with_number(trace.gcs, gc_number)
    rows = [
        (Cell(metric.name), _value_cell(metric, gc.metric(metric))) for metric in single_gc_metrics
    ]
    name = f"GC {gc_number} ({show_time_span_start_end(gc.StartRelativeMSec, gc.EndRelativeMSec)})"
    section = Section(name=name, tables=(Table(rows=rows),))
    return Document(sections=(section,))


@with_slots
@dataclass(frozen=True)
class SortGCsBy:
    metric: SingleGCMetric
    sort_reverse: bool


def _get_sort_gcs_by(args: _AnalyzeSingleArgs) -> Optional[SortGCsBy]:
    assert args.sort_gcs_ascending is None or args.sort_gcs_descending is None
    if args.sort_gcs_ascending is not None:
        return SortGCsBy(parse_single_gc_metric_arg(args.sort_gcs_ascending), False)
    elif args.sort_gcs_descending is not None:
        return SortGCsBy(parse_single_gc_metric_arg(args.sort_gcs_descending), True)
    else:
        return None


def _get_analyze_single_document(args: _AnalyzeSingleArgs) -> Document:
    # Find the bench for this test
    bench = try_find_benchfile_from_trace_file_path(args.path)

    run_metrics = get_parsed_and_score_metrics(bench, args.run_metrics, default_to_important=True)

    gc_where_filter = get_where_filter_for_gcs(args.gc_where)

    single_gc_metrics = parse_single_gc_metrics_arg(
        args.single_gc_metrics, default_to_important=True
    )

    sort_gcs_by = _get_sort_gcs_by(args)

    single_heap_metrics = parse_single_heap_metrics_arg(
        args.single_heap_metrics, default_to_important=False
    )

    trace = get_processed_trace(
        clr=get_clr(),
        test_result=test_result_from_path(args.path),
        process=args.process,
        need_mechanisms_and_reasons=False,
        need_join_info=False,
    ).unwrap()

    return analyze_single_for_processed_trace(
        trace,
        print_events=args.show_events,
        run_metrics=run_metrics,
        gc_where_filter=gc_where_filter,
        sort_gcs_by=sort_gcs_by,
        single_gc_metrics=single_gc_metrics,
        show_first_n_gcs=args.show_first_n_gcs,
        show_last_n_gcs=args.show_last_n_gcs,
        single_heap_metrics=single_heap_metrics,
    )


def analyze_single_for_processed_trace(
    trace: ProcessedTrace,
    print_events: bool,
    run_metrics: RunMetrics,
    gc_where_filter: Callable[[ProcessedGC], bool],
    sort_gcs_by: Optional[SortGCsBy],
    single_gc_metrics: SingleGCMetrics,
    single_heap_metrics: SingleHeapMetrics,
    show_first_n_gcs: Optional[int],
    show_last_n_gcs: Optional[int],
) -> Document:

    sections: List[Section] = []

    sections.extend(optional_to_iter(_get_run_metrics_section(trace, run_metrics)))

    if print_events:
        sections.append(_get_events_section(non_null(trace.event_names)))

    gcs = [gc for gc in trace.gcs if gc_where_filter(gc)]
    all_single_gc_metrics = cat_unique(
        optional_to_iter(map_option(sort_gcs_by, lambda s: s.metric)), single_gc_metrics
    )

    gcs_to_print = _get_sorted_gcs_to_print(show_first_n_gcs, show_last_n_gcs, gcs, sort_gcs_by)

    if is_empty(gcs_to_print.gcs):
        if not is_empty(all_single_gc_metrics) or not is_empty(single_heap_metrics):
            sections.append(Section(text="No GCs in trace"))
    else:
        if not is_empty(all_single_gc_metrics):
            sections.append(_get_single_gcs_section(gcs_to_print, all_single_gc_metrics))
        if not is_empty(single_heap_metrics):
            sections.extend(_get_single_heaps_sections(gcs_to_print.gcs, single_heap_metrics))

    return Document(sections=sections)


def _get_run_metrics_section(trace: ProcessedTrace, run_metrics: RunMetrics) -> Optional[Section]:
    if is_empty(run_metrics):
        return None
    else:
        run_metrics_table = Table(
            headers=("Name", "Value"),
            rows=[
                (Cell(run_metric.name), _value_cell(run_metric, trace.metric(run_metric)))
                for run_metric in run_metrics
            ],
        )
        return Section(name="Overall metrics", tables=(run_metrics_table,))


def _get_events_section(event_names: EventNames) -> Section:
    events_table = Table(
        headers=("Name", "Occurrences"),
        rows=[
            (Cell(name), Cell(occurrences))
            for name, occurrences in items_sorted_by_key(event_names)
            if not name.startswith("Task(") and not name.startswith("EventID(")
        ],
    )
    return Section(name=f"\nEvent names (excluding Task and EventID):", tables=(events_table,))


@with_slots
@dataclass(frozen=True)
class _GCsAndDescription:
    gcs: Sequence[ProcessedGC]
    descr: Optional[str]


def _get_single_gcs_section(gcs: _GCsAndDescription, single_gc_metrics: SingleGCMetrics) -> Section:
    def get_row_for_gc(gc: ProcessedGC) -> Optional[Row]:
        return [Cell(gc.Number), *(_value_cell(m, gc.metric(m)) for m in single_gc_metrics)]

    gcs_table = Table(
        headers=["gc number", *(metric.name for metric in single_gc_metrics)],
        rows=[row for gc in gcs.gcs for row in optional_to_iter(get_row_for_gc(gc))],
    )
    name = "Single gcs" + ("" if gcs.descr is None else f" ({gcs.descr})")
    return Section(name=name, tables=(gcs_table,))


def _get_single_heaps_sections(
    gcs: Sequence[ProcessedGC], single_heap_metrics: SingleHeapMetrics
) -> Sequence[Section]:
    def section_for_gc(gc: ProcessedGC) -> Section:
        heaps_table = Table(
            headers=("heap", *(metric.name for metric in single_heap_metrics)),
            rows=[
                (Cell(heap_i), *(_value_cell(m, heap.metric(m)) for m in single_heap_metrics))
                for heap_i, heap in enumerate(gc.heaps)
            ],
        )
        return Section(f"GC {gc.Number}", tables=(heaps_table,))

    return [section_for_gc(gc) for gc in gcs]


def _get_sorted_gcs_to_print(
    show_first_n_gcs: Optional[int],
    show_last_n_gcs: Optional[int],
    gcs: Sequence[ProcessedGC],
    sort_gcs_by: Optional[SortGCsBy],
) -> _GCsAndDescription:
    if sort_gcs_by is not None:
        metric = sort_gcs_by.metric
        gcs = sorted(
            gcs, key=lambda gc: _to_comparable(gc, metric), reverse=sort_gcs_by.sort_reverse
        )
    return _get_first_or_last_n_gcs(show_first_n_gcs, show_last_n_gcs, gcs)


def _to_comparable(gc: ProcessedGC, metric: SingleGCMetric) -> AnyValue:
    return match(gc.metric(metric), identity, lambda _: -inf)


def _get_first_or_last_n_gcs(
    show_first_n_gcs: Optional[int],
    show_last_n_gcs: Optional[int],
    sorted_gcs: Sequence[ProcessedGC],
) -> _GCsAndDescription:
    # TODO: support returning both first and last n gcs
    if show_first_n_gcs is not None:
        assert show_last_n_gcs is None
        return _first_n(sorted_gcs, show_first_n_gcs)
    elif show_last_n_gcs is not None:
        assert show_first_n_gcs is None
        return _last_n(sorted_gcs, show_last_n_gcs)
    else:
        return _first_n(sorted_gcs, 10)


def _first_n(gcs: Sequence[ProcessedGC], n: int) -> _GCsAndDescription:
    return _GCsAndDescription(gcs[:n], f"first {n}" if n <= len(gcs) else None)


def _last_n(gcs: Sequence[ProcessedGC], n: int) -> _GCsAndDescription:
    return _GCsAndDescription(gcs[-n:], f"last {n}" if n < len(gcs) else None)


def _value_cell(metric: MetricBase, value: FailableValue) -> Cell:
    return match(
        value,
        cb_ok=lambda v: Cell("%.4f" % v) if metric.do_not_use_scientific_notation else Cell(v),
        cb_err=Cell,
    )


ANALYZE_SINGLE_COMMANDS: CommandsMapping = {
    "analyze-single": Command(
        kind=CommandKind.analysis,
        fn=analyze_single,
        doc="""
    Given a single trace, print run metrics and optionally metrics for individual GCs.
    """,
    ),
    "analyze-single-gc": Command(
        kind=CommandKind.analysis,
        fn=_analyze_single_gc,
        doc="""Print detailed info about a single GC within a single trace.""",
    ),
}
