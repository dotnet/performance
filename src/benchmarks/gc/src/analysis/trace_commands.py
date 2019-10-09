# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from itertools import islice
from pathlib import Path
from re import compile as compile_regexp, IGNORECASE
from typing import Mapping, Optional, Tuple

from ..commonlib.collection_util import map_to_mapping
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
    TXT_DOC,
)
from ..commonlib.option import map_option, non_null, option_or
from ..commonlib.type_utils import argument, with_slots
from ..commonlib.util import opt_max, seconds_to_msec

from .clr import Clr, get_clr
from .clr_types import AbstractEtlxTraceProcess, AbstractTracedProcesses
from .core_analysis import get_traced_processes, TRACE_PATH_DOC, try_get_runtime
from .process_trace import test_result_from_path


@with_slots
@dataclass(frozen=True)
class _PrintEventsArgs:
    trace_path: Path = argument(name_optional=True, doc=TRACE_PATH_DOC)
    time_span_msec: Optional[Tuple[float, float]] = argument(
        default=None, doc="Only print events within this time span."
    )
    include: Optional[str] = argument(
        default=None, doc="Only events whose names match this regex will be included"
    )
    exclude: Optional[str] = argument(
        default=None, doc="Events whose names match this regex will be excluded"
    )
    thread_id: Optional[int] = argument(default=None, doc="Only print events with this thread ID")
    max_events: Optional[int] = argument(default=None, doc="Stop after this many events.")


def _print_events(args: _PrintEventsArgs) -> None:
    print_events_for_jupyter(
        args.trace_path,
        args.time_span_msec,
        args.include,
        args.exclude,
        args.thread_id,
        args.max_events,
    )


def print_events_for_jupyter(
    path: Path,
    time_span_msec: Optional[Tuple[float, float]] = None,
    include: Optional[str] = None,
    exclude: Optional[str] = None,
    thread_id: Optional[int] = None,
    max_events: Optional[int] = None,
) -> None:
    clr = get_clr()
    time = map_option(time_span_msec, lambda t: clr.TimeSpanUtil.FromStartEndMSec(*t))
    trace_path = non_null(test_result_from_path(path).trace_path)
    clr.Analysis.PrintEvents(str(trace_path), time, include, exclude, thread_id, max_events, False)


@with_slots
@dataclass(frozen=True)
class _PrintProcessesArgs:
    trace_path: Path = argument(name_optional=True, doc=TRACE_PATH_DOC)
    command_line_regex: Optional[str] = argument(
        default=None,
        doc="Regular expression used to filter processes by their command-line arguments",
    )
    clr_only: bool = argument(default=False, doc="Only include CLR processes")
    hide_threads: bool = argument(default=False, doc="Don't show threads for each process")
    txt: Optional[Path] = argument(default=None, doc=TXT_DOC)
    output_width: Optional[OutputWidth] = argument(default=None, doc=OUTPUT_WIDTH_DOC)


def _print_processes(args: _PrintProcessesArgs) -> None:
    trace_path = non_null(test_result_from_path(args.trace_path).trace_path)
    clr = get_clr()
    processes = get_traced_processes(clr, trace_path)
    rgx = map_option(args.command_line_regex, lambda s: compile_regexp(s, IGNORECASE))
    proc_to_peak_heap_size = map_to_mapping(
        processes.processes, lambda p: _get_peak_clr_heap_size(clr, p)
    )
    filtered_processes = [
        p
        for p in processes.processes
        if (rgx is None or rgx.search(p.CommandLine) is not None)
        and (not args.clr_only or proc_to_peak_heap_size[p] is not None)
    ]

    table = Table(
        headers=(
            "pid",
            "name",
            "peak heap size (MB)",
            *([] if args.hide_threads else ["threads", "threads (my version)"]),
            "command-line args",
        ),
        rows=[
            _process_row(p, processes, proc_to_peak_heap_size, args.hide_threads)
            for p in sorted(
                filtered_processes,
                key=lambda p: option_or(proc_to_peak_heap_size[p], 0),
                reverse=True,
            )
        ],
    )
    handle_doc(
        Document(sections=(Section(tables=(table,)),)),
        OutputOptions(txt=args.txt, width=args.output_width),
    )


def _process_row(
    p: AbstractEtlxTraceProcess,
    processes: AbstractTracedProcesses,
    proc_to_peak_heap_size: Mapping[AbstractEtlxTraceProcess, Optional[float]],
    hide_threads: bool,
) -> Row:
    return (
        Cell(p.ProcessID),
        Cell(p.Name),
        Cell(proc_to_peak_heap_size[p]),
        *(
            []
            if hide_threads
            else [
                Cell(
                    sorted(
                        t.ThreadID
                        for t in processes.thread_id_to_process_id.ProcessIDToThreadIDsAndTimes(
                            p.ProcessID
                        )
                    )
                ),
                Cell(
                    sorted(
                        t.ThreadID
                        for t in processes.my_thread_id_to_process_id.ProcessIDToThreadIDsAndTimes(
                            p.ProcessID
                        )
                    )
                ),
            ]
        ),
        Cell(p.CommandLine),
    )


def _get_peak_clr_heap_size(clr: Clr, p: AbstractEtlxTraceProcess) -> Optional[float]:
    return map_option(
        try_get_runtime(clr, p),
        lambda rt: option_or(opt_max(gc.HeapSizePeakMB for gc in rt.GC.GCs), 0),
    )


@with_slots
@dataclass(frozen=True)
class SliceTraceFileArgs:
    input: Path = argument(doc="Input trace file.")
    output: Path = argument(doc="Path to write the output trace file to.")
    time_span_seconds: Tuple[float, float] = argument(
        doc="Output trace file will only contain events within this time span."
    )


def slice_trace_file(args: SliceTraceFileArgs) -> None:
    clr = get_clr()
    time_span = clr.TimeSpanUtil.FromStartEndMSec(
        seconds_to_msec(args.time_span_seconds[0]), seconds_to_msec(args.time_span_seconds[1])
    )
    clr.Analysis.SliceTraceFile(str(args.input), str(args.output), time_span)
    print(f"Wrote to {args.output}")


@with_slots
@dataclass(frozen=True)
class HeadArgs:
    path: Path = argument(name_optional=True, doc="Text file path.")


def head(args: HeadArgs) -> None:
    with open(args.path) as f:
        for line in islice(f, 10):
            print(type(line))
            print(line)


TRACE_COMMANDS: CommandsMapping = {
    "head": Command(
        kind=CommandKind.trace, hidden=True, fn=head, doc="Print the first few lines of a file."
    ),
    "print-events": Command(
        kind=CommandKind.trace,
        fn=_print_events,
        doc="Print all events from a trace file within a time range.",
    ),
    "print-processes": Command(
        kind=CommandKind.trace,
        fn=_print_processes,
        doc="Print all process PIDs and names from a trace file.",
    ),
    "slice-trace-file": Command(
        kind=CommandKind.trace,
        fn=slice_trace_file,
        doc="Given an input trace file, output a trace file containing "
        "only events in a given time range.",
    ),
}
