# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from enum import Enum
from pathlib import Path
from typing import Callable, Optional, Sequence

from result import Result

from ..commonlib.collection_util import indices, is_empty, min_max_float
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
from ..commonlib.result_utils import unwrap
from ..commonlib.type_utils import argument, enum_value, T, with_slots
from ..commonlib.util import float_to_str, get_percent

from .clr import Clr, get_clr
from .clr_types import (
    AbstractJoinInfoForGC,
    AbstractJoinInfoForHeap,
    AbstractJoinStageOrPhaseInfo,
    AbstractStolenTimeInstance,
    AbstractStolenTimeInstanceWithGcNumber,
    AbstractTimeSpan,
    AbstractWorstJoinInstance,
)
from .core_analysis import GC_NUMBER_DOC, PROCESS_DOC, TRACE_PATH_DOC
from .enums import GcJoinPhase, GcJoinStage, Gens, ServerGCState
from .process_trace import get_processed_trace, test_result_from_path
from .single_gc_metrics import get_gc_index_with_number
from .types import ProcessedTrace, ThreadToProcessToName, ProcessQuery


class StagesOrPhases(Enum):
    stages = 0
    phases = 1
    both = 2


_DOC_N_WORST_STOLEN_TIME_INSTANCES = """
Show the top N instances of stolen time.
Only available if 'collect: cswitch' was specified in the BenchOptions.
"""

_DOC_N_WORST_JOINS = """
Shows the top N worst individual joins.
"""


@with_slots
@dataclass(frozen=True)
class AnalyzeJoinsAllGcsArgs:
    trace_path: Path = argument(name_optional=True, doc=TRACE_PATH_DOC)
    process: ProcessQuery = argument(default=None, doc=PROCESS_DOC)
    show_n_worst_stolen_time_instances: int = argument(
        default=10, doc=_DOC_N_WORST_STOLEN_TIME_INSTANCES
    )
    show_n_worst_joins: int = argument(default=10, doc=_DOC_N_WORST_JOINS)
    txt: Optional[Path] = argument(default=None, doc=TXT_DOC)


def analyze_joins_all_gcs(args: AnalyzeJoinsAllGcsArgs) -> None:
    trace = unwrap(
        _get_processed_trace_with_just_join_info(
            clr=get_clr(), path=args.trace_path, process=args.process
        )
    )
    handle_doc(
        analyze_joins_all_gcs_for_jupyter(
            trace,
            show_n_worst_stolen_time_instances=args.show_n_worst_stolen_time_instances,
            show_n_worst_joins=args.show_n_worst_joins,
        ),
        OutputOptions(txt=args.txt),
    )


def analyze_joins_all_gcs_for_jupyter(
    trace: ProcessedTrace, show_n_worst_stolen_time_instances: int, show_n_worst_joins: int
) -> Document:
    _check_join_analysis_ready()
    _check_server_gc(trace)

    join_info = unwrap(trace.join_info)

    sections = (
        _get_worst_stolen_times_section_for_process(
            trace.process_names,
            tuple(join_info.WorstStolenTimeInstances)[:show_n_worst_stolen_time_instances],
        ),
        _get_worst_joins_section(tuple(join_info.WorstForegroundJoins)[:show_n_worst_joins]),
    )
    if is_empty(sections):
        return Document(comment="Nothing to show (no stolen time instances or worst joins)")
    else:
        return Document(sections=sections)


@with_slots
@dataclass(frozen=True)
class _AnalyzeJoinsSingleGcArgs:
    trace_path: Path = argument(name_optional=True, doc=TRACE_PATH_DOC)
    gc_number: int = argument(doc=GC_NUMBER_DOC)
    process: ProcessQuery = argument(default=None, doc=PROCESS_DOC)
    kind: StagesOrPhases = argument(
        default=StagesOrPhases.phases,
        doc="""
    phases: These group several stages together. Better for a quick overview.
    stages: Show every individual stage -- there may be many of these.
    both: Show phases, then show stages.
    """,
    )
    only_stages_with_percent_time: Optional[float] = argument(
        default=None, doc="Only show stages that take up at least this percentage of total time."
    )
    show_n_worst_stolen_time_instances: int = argument(
        default=10, doc=_DOC_N_WORST_STOLEN_TIME_INSTANCES
    )
    max_heaps: Optional[int] = argument(default=None, doc="Only show this many heaps")

    txt: Optional[Path] = argument(default=None, doc=TXT_DOC)
    output_width: Optional[OutputWidth] = argument(default=None, doc=OUTPUT_WIDTH_DOC)
    table_indent: Optional[int] = argument(default=None, doc=TABLE_INDENT_DOC)


def _analyze_joins_single_gc(args: _AnalyzeJoinsSingleGcArgs) -> None:
    _check_join_analysis_ready()
    trace = unwrap(
        _get_processed_trace_with_just_join_info(get_clr(), args.trace_path, args.process)
    )
    doc = analyze_joins_single_gc_for_jupyter(
        trace,
        gc_number=args.gc_number,
        kind=args.kind,
        only_stages_with_percent_time=args.only_stages_with_percent_time,
        show_n_worst_stolen_time_instances=args.show_n_worst_stolen_time_instances,
        max_heaps=args.max_heaps,
    )
    handle_doc(
        doc, OutputOptions(width=args.output_width, table_indent=args.table_indent, txt=args.txt)
    )


def _get_processed_trace_with_just_join_info(
    clr: Clr, path: Path, process: ProcessQuery
) -> Result[str, ProcessedTrace]:
    return get_processed_trace(
        clr=clr,
        test_result=test_result_from_path(path),
        process=process,
        need_mechanisms_and_reasons=False,
        need_join_info=True,
    )


def analyze_joins_single_gc_for_jupyter(
    trace: ProcessedTrace,
    gc_number: int,
    kind: StagesOrPhases,
    only_stages_with_percent_time: Optional[float],
    show_n_worst_stolen_time_instances: int,
    max_heaps: Optional[int] = None,
) -> Document:
    _check_join_analysis_ready()
    _check_server_gc(trace)

    gc_index = get_gc_index_with_number(trace.gcs, gc_number)
    gc = trace.gcs[gc_index]
    join_info = unwrap(gc.join_info)
    assert join_info.GC.Number == gc_number

    has_update_card_bundles = (
        enum_value(GcJoinStage.update_card_bundles) in join_info.ForegroundGCJoinStages
    )
    assert has_update_card_bundles == (gc.Generation != Gens.Gen2)

    return _get_analyze_joins_single_gc_document(
        process_names=trace.process_names,
        gc_duration_msec=gc.DurationMSec,
        join_info_for_gc=join_info,
        kind=kind,
        only_stages_with_percent_time=only_stages_with_percent_time,
        show_n_worst_stolen_time_instances=show_n_worst_stolen_time_instances,
        heap_indices=heap_indices_from_max_heaps(max_heaps),
    )


def _check_join_analysis_ready() -> None:
    # Comment this out when testing out join analysis
    # raise Exception("Not ready yet, won't work without updated TraceEvent")
    pass


def _check_server_gc(trace: ProcessedTrace) -> None:
    if not trace.UsesServerGC:
        raise Exception("Can't analyze joins for non-server GC")


@with_slots
@dataclass(frozen=True)
class HeapIndices:
    max: Optional[int]

    def has(self, hp: int) -> bool:
        return self.max is None or hp < self.max

    def choose(self, seq: Sequence[T]) -> Sequence[T]:
        return [x for i, x in enumerate(seq) if self.has(i)]


def heap_indices_from_max_heaps(max_heaps: Optional[int]) -> HeapIndices:
    return HeapIndices(max_heaps)


def _get_analyze_joins_single_gc_document(
    process_names: ThreadToProcessToName,
    gc_duration_msec: float,
    join_info_for_gc: AbstractJoinInfoForGC,
    kind: StagesOrPhases,
    only_stages_with_percent_time: Optional[float],
    show_n_worst_stolen_time_instances: int,
    heap_indices: HeapIndices,
) -> Document:
    heap_tids = _heap_tids_section(join_info_for_gc)
    stages_and_phases = _sections_for_stages_and_phases(
        gc_duration_msec, join_info_for_gc, kind, only_stages_with_percent_time, heap_indices
    )
    stolen_times = _get_worst_stolen_times_section_for_gc(
        process_names,
        tuple(join_info_for_gc.WorstStolenTimeInstances)[:show_n_worst_stolen_time_instances],
    )
    comment = (
        "Percentages will add up to a little over 100% "
        + "as they take the first heap's begin to the last heap's end."
    )
    return Document(comment=comment, sections=(heap_tids, *stages_and_phases, stolen_times))


def _heap_tids_section(join_info_for_gc: AbstractJoinInfoForGC) -> Section:
    table = Table(
        headers=("heap number", "foreground TID", "background TID"),
        rows=[
            (Cell(hp.HeapID), Cell(hp.ForegroundThreadID), Cell(hp.BackgroundThreadID))
            for hp in join_info_for_gc.Heaps
        ],
    )
    return Section(tables=(table,))


def _sections_for_stages_and_phases(
    gc_duration_msec: float,
    join_info_for_gc: AbstractJoinInfoForGC,
    kind: StagesOrPhases,
    only_stages_with_percent_time: Optional[float],
    heap_indices: HeapIndices,
) -> Sequence[Section]:
    if kind in (StagesOrPhases.phases, StagesOrPhases.stages):
        return (
            _sections_for_stages(join_info_for_gc, only_stages_with_percent_time, heap_indices)
            if kind == StagesOrPhases.stages
            else tuple(
                optional_to_iter(
                    _section_for_phases(gc_duration_msec, join_info_for_gc, heap_indices)
                )
            )
        )
    elif kind == StagesOrPhases.both:
        return (
            *_sections_for_stages(join_info_for_gc, only_stages_with_percent_time, heap_indices),
            *optional_to_iter(
                _section_for_phases(gc_duration_msec, join_info_for_gc, heap_indices)
            ),
        )
    else:
        raise Exception(kind)


def _sections_for_stages(
    join_info_for_gc: AbstractJoinInfoForGC,
    only_stages_with_percent_time: Optional[float],
    heap_indices: HeapIndices,
) -> Sequence[Section]:
    def should_show_helper(span: AbstractTimeSpan) -> bool:
        if only_stages_with_percent_time is None:
            return True
        else:
            total_duration = join_info_for_gc.GC.DurationMSec
            duration = span.DurationMSec
            return (duration / total_duration) * 100.0 >= only_stages_with_percent_time

    def should_show_foreground(stage_index: int) -> bool:
        return should_show_helper(join_info_for_gc.TimeSpanForForegroundStage(stage_index))

    def should_show_background(stage_index: int) -> bool:
        return should_show_helper(join_info_for_gc.TimeSpanForBackgroundStage(stage_index))

    # Group by phases. Get stages within that phase.
    total_stage_index = 0

    def get_for_foreground_phase(phase_index: int, stages: Sequence[int]) -> Optional[Section]:
        nonlocal total_stage_index
        prev_total_stage_index = total_stage_index
        total_stage_index += len(stages)
        section_name = f"Stages of {_get_title_for_phase(join_info_for_gc, phase_index)}"
        return _section_for_stages_or_phases(
            approx_total_msec=join_info_for_gc.TimeSpanForForegroundPhase(phase_index).DurationMSec,
            join_info_for_gc=join_info_for_gc,
            name=section_name,
            stages_or_phases=stages,
            should_show=lambda i: should_show_foreground(prev_total_stage_index + i),
            get_name=lambda i: _get_title_for_stage(
                join_info_for_gc,
                join_info_for_gc.ForegroundGCJoinStages,
                join_info_for_gc.TimeSpanForForegroundStage(prev_total_stage_index + i),
                prev_total_stage_index + i,
            ),
            get_is_ee_suspended=lambda i: join_info_for_gc.IsEESuspendedForForegroundStage(
                prev_total_stage_index + i
            ),
            get_for_heap=lambda h: h.ForegroundStages[prev_total_stage_index:total_stage_index],
            heap_indices=heap_indices,
        )

    fg_sections = [
        s
        for phase_index, phase_and_stages in enumerate(join_info_for_gc.ForegroundStagesByPhase())
        for s in optional_to_iter(get_for_foreground_phase(phase_index, phase_and_stages.Stages))
    ]

    bg_section = _section_for_stages_or_phases(
        approx_total_msec=join_info_for_gc.TimeSpanForAllBackgroundStages().DurationMSec,
        join_info_for_gc=join_info_for_gc,
        name="Background stages",
        stages_or_phases=join_info_for_gc.BackgroundGCJoinStages,
        should_show=should_show_background,
        get_name=lambda i: _get_title_for_stage(
            join_info_for_gc,
            join_info_for_gc.BackgroundGCJoinStages,
            join_info_for_gc.TimeSpanForBackgroundStage(i),
            i,
        ),
        get_is_ee_suspended=join_info_for_gc.IsEESuspendedForBackgroundStage,
        get_for_heap=lambda h: h.BackgroundStages,
        heap_indices=heap_indices,
    )
    return (*fg_sections, *optional_to_iter(bg_section))


def _get_title_for_phase(join_info_for_gc: AbstractJoinInfoForGC, phase_index: int) -> str:
    phase_name = GcJoinPhase(join_info_for_gc.ForegroundGCJoinPhases[phase_index]).name
    time_span = join_info_for_gc.TimeSpanForForegroundPhase(phase_index)
    show_span = show_time_span_and_pct(time_span, join_info_for_gc.GC.PauseDurationMSec)
    return f"{phase_name} -- approx. {show_span}"


def _get_title_for_stage(
    join_info_for_gc: AbstractJoinInfoForGC,
    all_stages: Sequence[int],
    time_span: AbstractTimeSpan,
    stage_i: int,
) -> str:
    cur_stage = GcJoinStage(all_stages[stage_i])
    prev = "begin" if stage_i == 0 else _show_join_stage(GcJoinStage(all_stages[stage_i - 1]))
    time_span_str = show_time_span_and_pct(time_span, join_info_for_gc.GC.DurationMSec)
    return f"{prev} to {_show_join_stage(cur_stage)} -- {time_span_str}"


def _show_join_stage(stage: GcJoinStage) -> str:
    return f"{stage.name} ({enum_value(stage)})"


def show_time_span_and_pct(span: AbstractTimeSpan, total_time: float) -> str:
    pct = float_to_str(get_percent(span.DurationMSec / total_time))
    return f"{float_to_str(span.DurationMSec)}ms ({pct}%) (span {show_time_span(span)})"


def show_time_span(span: AbstractTimeSpan) -> str:
    return show_time_span_start_end(span.StartMSec, span.EndMSec)


def show_time_span_start_end(start: float, end: float) -> str:
    return f"{show_point_in_time(start)} {show_point_in_time(end)}"


def show_point_in_time(time_msec: float) -> str:
    # Always show tenths of a millisecond
    # (float_to_str will cut that off when time values are large)
    return "%.1f" % time_msec


def show_time_length(time_msec: float) -> str:
    return "%.1f" % time_msec


def _section_for_phases(
    gc_duration_msec: float, join_info_for_gc: AbstractJoinInfoForGC, heap_indices: HeapIndices
) -> Optional[Section]:
    return _section_for_stages_or_phases(
        approx_total_msec=gc_duration_msec,
        join_info_for_gc=join_info_for_gc,
        name="phases",
        stages_or_phases=join_info_for_gc.ForegroundGCJoinPhases,
        should_show=lambda _: True,
        get_name=lambda i: _get_title_for_phase(join_info_for_gc, i),
        get_is_ee_suspended=join_info_for_gc.IsEESuspendedForForegroundPhase,
        get_for_heap=lambda h: h.ForegroundPhases,
        heap_indices=heap_indices,
    )


def _section_for_stages_or_phases(
    approx_total_msec: float,
    join_info_for_gc: AbstractJoinInfoForGC,
    name: str,
    stages_or_phases: Sequence[int],
    should_show: Callable[[int], bool],
    get_name: Callable[[int], str],
    get_is_ee_suspended: Callable[[int], bool],
    get_for_heap: Callable[[AbstractJoinInfoForHeap], Sequence[AbstractJoinStageOrPhaseInfo]],
    heap_indices: HeapIndices,
) -> Optional[Section]:
    def make_time_cell(time: float, total_time: float) -> Cell:
        return Cell(show_point_in_time(time), color="red" if (time / total_time) > 0.25 else None)

    def make_state_time_cell(state: ServerGCState, time: float, total_time: float) -> Cell:
        state_is_bad = state != ServerGCState.working
        color = "red" if state_is_bad and total_time > 0 and (time / total_time) > 0.25 else None
        return Cell(show_point_in_time(time), color=color)

    heaps = heap_indices.choose(join_info_for_gc.Heaps)

    def table_for_stage_or_phase(stage_or_phase_index: int) -> Table:
        # Rows are states. Also the first row is the total.
        def make_row(state: ServerGCState) -> Row:
            def cell_for_heap(info: AbstractJoinStageOrPhaseInfo) -> Cell:
                return make_state_time_cell(
                    state, info.MSecPerState[enum_value(state)], info.DurationMSec
                )

            infos = [info for hp in heaps for info in (get_for_heap(hp)[stage_or_phase_index],)]
            values = [info.MSecPerState[enum_value(state)] for info in infos]
            return (
                Cell(name_min_max(state.name, values)),
                *(cell_for_heap(info) for info in infos),
            )

        headers = ["heap", *(str(n) for n in indices(heaps))]
        total_times = [get_for_heap(h)[stage_or_phase_index].DurationMSec for h in heaps]
        row_for_total = [
            Cell(name_min_max("total", total_times)),
            *(make_time_cell(v, approx_total_msec) for v in total_times),
        ]
        rows_for_states = [make_row(s) for s in ServerGCState]
        rows = (row_for_total, *rows_for_states)
        is_ee_suspended = get_is_ee_suspended(stage_or_phase_index)
        name = f"{get_name(stage_or_phase_index)} (EE {'' if is_ee_suspended else 'not '}suspended)"
        return Table(name=name, headers=headers, rows=rows)

    tables = [
        t
        for i in range(len(stages_or_phases))
        if should_show(i)
        for t in optional_to_iter(table_for_stage_or_phase(i))
    ]
    return None if is_empty(tables) else Section(name=name, tables=tables)


def name_min_max(name: str, values: Sequence[float]) -> str:
    mm = non_null(min_max_float(values))
    return f"{name} ({show_time_length(mm.min)}-{show_time_length(mm.max)})"


def _get_worst_stolen_times_section_for_process(
    process_names: ThreadToProcessToName,
    worst_stolen_time_instances: Sequence[AbstractStolenTimeInstanceWithGcNumber],
) -> Section:
    if is_empty(worst_stolen_time_instances):
        return Section(text="No stolen time instances found")
    else:
        table = Table(
            name=f"Top {len(worst_stolen_time_instances)} worst stolen time instances:",
            headers=("gc number", *_HEADERS_FOR_STOLEN_TIME_INSTANCE),
            rows=[
                (Cell(i.GCNumber), *_get_row_for_stolen_time_instance(process_names, i.Instance))
                for i in worst_stolen_time_instances
            ],
        )
        return Section(tables=(table,))


def _get_worst_joins_section(worst_joins: Sequence[AbstractWorstJoinInstance]) -> Section:
    table = Table(
        name=f"Top {len(worst_joins)} worst (foreground) joins:",
        headers=(
            "time span (ms)",
            "duration (ms)",
            "stage",
            "gc number",
            "heap number"
            # TODO: TID
        ),
        rows=[
            (
                Cell(show_time_span(j.TimeSpan)),
                Cell(j.Join.DurationMSec),
                Cell(GcJoinStage(j.Join.JoinStage).name),
                Cell(j.GCNumber),
                Cell(j.HeapID),
            )
            for j in worst_joins
        ],
    )
    return Section(tables=(table,))


def _get_worst_stolen_times_section_for_gc(
    process_names: ThreadToProcessToName,
    worst_stolen_time_instances: Sequence[AbstractStolenTimeInstance],
) -> Section:
    if is_empty(worst_stolen_time_instances):
        return Section(text="No stolen time instances found.")
    else:
        table = Table(
            name=f"Top {len(worst_stolen_time_instances)} worst stolen time instances:",
            headers=_HEADERS_FOR_STOLEN_TIME_INSTANCE,
            rows=[
                _get_row_for_stolen_time_instance(process_names, w)
                for w in worst_stolen_time_instances
            ],
        )
        return Section(tables=(table,))


_HEADERS_FOR_STOLEN_TIME_INSTANCE: Sequence[str] = (
    "start (ms)",
    "duration (ms)",
    "old TID",
    "new TID",
    "old priority",
    "new priority",
    "new PID",
    "new process name",
    "heap",
    "prior state",
    "stage",
    # "phase",
    "processor",
)


def _get_row_for_stolen_time_instance(
    process_names: ThreadToProcessToName, i: AbstractStolenTimeInstance
) -> Row:
    new_process_id = process_names.get_process_id_for_thread_id(i.NewThreadID, i.StartTimeMSec)

    def get_process_name(process_id: Optional[int]) -> Optional[str]:
        return map_option(
            process_id,
            lambda pid: process_names.get_process_name_for_process_id(pid, i.StartTimeMSec),
        )

    return (
        Cell(show_point_in_time(i.StartTimeMSec)),
        Cell(i.DurationMSec),
        Cell(i.OldThreadID),
        Cell(i.NewThreadID),
        Cell(i.OldPriority),
        Cell(i.NewPriority),
        Cell(new_process_id),
        Cell(get_process_name(new_process_id)),
        Cell(i.HeapID),
        Cell(ServerGCState(i.State).name),
        Cell(GcJoinStage(i.Stage).name),
        # Cell(GcJoinPhase(i.Phase).name),
        Cell(i.Processor),
    )


# TODO: These commands should only be hidden until the new join analysis is enabled
# (which requires some updates to TraceEvent)

ANALYZE_JOINS_COMMANDS: CommandsMapping = {
    "analyze-joins-all-gcs": Command(
        hidden=True,
        kind=CommandKind.analysis,
        fn=analyze_joins_all_gcs,
        doc="Collect CPU stolen times over all GCs and print the worst instances.",
        priority=1,
    ),
    "analyze-joins-single-gc": Command(
        hidden=True,
        kind=CommandKind.analysis,
        fn=_analyze_joins_single_gc,
        doc="Print detailed info on join phases or individual stages for a single GC..",
        priority=1,
    ),
}
