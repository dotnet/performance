# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from __future__ import annotations  # InfoAtCodeAddress circularly references itself.
from abc import ABC, abstractmethod
from dataclasses import dataclass
from pathlib import Path
from re import compile as compile_regexp, IGNORECASE
from typing import Any, cast, Dict, Iterable, Mapping, Optional, Pattern, Sequence, Tuple

from overrides import overrides

from ..commonlib.bench_file import get_trace_kind, TraceKind
from ..commonlib.collection_util import find_only, is_empty, try_find_only, TryFindOnlyFailure
from ..commonlib.option import map_option, non_null, optional_to_iter
from ..commonlib.type_utils import check_cast, with_slots

from .clr import Clr, SYMBOL_PATH
from .clr_types import (
    AbstractEtlTrace,
    AbstractTraceCallStack,
    AbstractTraceEvent,
    AbstractTraceGC,
    AbstractTraceLog,
    AbstractTraceProcess,
    AbstractTracedProcesses,
    AbstractTraceLoadedDotNetRuntime,
)
from .enums import GCType
from .types import ProcessInfo, ThreadToProcessToName, ProcessQuery


def get_etl_trace(clr: Clr, etl_path: Path) -> AbstractEtlTrace:
    return clr.EtlTrace(str(etl_path), str(etl_path), SYMBOL_PATH)


def get_trace_log(clr: Clr, trace_path: Path) -> AbstractTraceLog:
    assert is_etl(trace_path), "OpenOrConvert only works with .etl apparently"
    return cast(AbstractTraceLog, clr.TraceLog.OpenOrConvert(str(trace_path)))


def is_etl(trace_path: Path) -> bool:
    return trace_path.name.endswith(".etl")


def get_traced_processes(
    clr: Clr, trace_path: Path, collect_event_names: bool = False
) -> AbstractTracedProcesses:
    return clr.Analysis.GetTracedProcesses(str(trace_path), collect_event_names, True)


class ProcessPredicate(ABC):
    @abstractmethod
    def match(self, process: AbstractTraceProcess) -> bool:
        raise NotImplementedError()

    @abstractmethod
    def describe(self) -> str:
        raise NotImplementedError()

    @abstractmethod
    def get_parts(self) -> ProcessQuery:
        raise NotImplementedError()


class _RegexProcessPredicate(ProcessPredicate):
    def __init__(self, parts: Sequence[str]):
        name = None
        args = None
        proc_id = None
        for part in parts:
            colon_sep = part.split(":")
            assert len(colon_sep) == 2, f"Expected to split '{part}' into two by a ':'"
            key, value = colon_sep
            if key == "name":
                name = value
            elif key == "args":
                args = value
            else:
                assert key == "id"
                proc_id = int(value)

        def cmp(st: Optional[str]) -> Optional[Pattern[str]]:
            return map_option(st, lambda s: compile_regexp(s, IGNORECASE))

        self.id = proc_id
        self.name_regex = cmp(name)
        self.args_regex = cmp(args)
        self.parts = parts

        if self.id is not None:
            assert self.name_regex is None and self.args_regex is None
        else:
            assert self.name_regex is not None or self.args_regex is not None

    @overrides
    def match(self, process: AbstractTraceProcess) -> bool:
        if self.id is None:

            def test(r: Optional[Pattern[str]], s: str) -> bool:
                return r is None or r.search(s) is not None

            return test(self.name_regex, process.Name) and test(
                self.args_regex, process.CommandLine
            )
        else:
            return process.ProcessID == self.id

    @overrides
    def describe(self) -> str:
        if self.id is None:

            def show(key: str, r: Optional[Pattern[str]]) -> Optional[str]:
                return None if r is None else f"{key}:{check_cast(str, r.pattern)}"

            return " ".join(
                [
                    x
                    for s in [show("name", self.name_regex), show("args", self.args_regex)]
                    for x in optional_to_iter(s)
                ]
            )
        else:
            return f"id:{self.id}"

    @overrides
    def get_parts(self) -> ProcessQuery:
        return self.parts


def process_predicate_from_parts(parts: Sequence[str]) -> ProcessPredicate:
    return _RegexProcessPredicate(parts)


def process_predicate_from_id(process_id: int) -> ProcessPredicate:
    return _RegexProcessPredicate((f"id:{process_id}",))


SINGLE_PATH_DOC = """
Path to the trace file to analyze
(or test result '.yaml' test file referencing it, which provides the process ID)
"""

PROCESS_DOC = """
This may be:
* `id:123` to specify a process ID
* `name:abc` to specify a regular expression to match a process name.
* `args:abc` to specify a regular expression to match process arguments.
"""


GC_NUMBER_DOC = "Number of the GC to analyze."

TRACE_PATH_DOC = "Path to a trace file."


def find_process(
    clr: Clr, processes: Iterable[AbstractTraceProcess], predicate: ProcessPredicate
) -> AbstractTraceProcess:
    res = try_find_only(predicate.match, processes)
    if isinstance(res, TryFindOnlyFailure):
        processes_with_gcs: Sequence[AbstractTraceProcess] = (
            [
                p
                for p in processes
                for mang in optional_to_iter(try_get_runtime(clr, p))
                if not is_empty(mang.GC.GCs)
            ]
            if res == TryFindOnlyFailure.NotFound
            else tuple(processes)
        )

        # This will fail
        return find_only(
            predicate.match,
            processes_with_gcs,
            show=lambda p: f"{p.ProcessID} {p.Name} {p.CommandLine}",
            show_predicate=lambda: f"{predicate.describe()} (including processes with GCs only)",
        )
    else:
        return res


def get_process_names_and_process_info(
    clr: Clr,
    trace_path: Path,
    show_name: str,
    process_predicate: ProcessPredicate,
    collect_event_names: bool = False,
) -> Tuple[ThreadToProcessToName, ProcessInfo]:
    p = get_traced_processes(clr, trace_path, collect_event_names)
    kind = get_trace_kind(trace_path)
    if kind == TraceKind.Etl:
        process = find_process(clr, p.processes, process_predicate)
    else:
        processes = tuple(p.processes)
        assert len(processes) == 1
        process = processes[0]

    return (
        _get_process_names(p),
        get_process_info_from_process(clr, p, trace_path, process, show_name),
    )


def _get_process_names(pr: AbstractTracedProcesses) -> ThreadToProcessToName:
    return ThreadToProcessToName(pr.thread_id_to_process_id, pr.process_id_to_process_name)


def get_process_info(
    clr: Clr,
    trace_path: Path,
    show_name: str,
    process_predicate: ProcessPredicate,
    collect_event_names: bool = False,
) -> ProcessInfo:
    return get_process_names_and_process_info(
        clr, trace_path, show_name, process_predicate, collect_event_names
    )[1]


def try_get_runtime(
    clr: Clr, process: AbstractTraceProcess
) -> Optional[AbstractTraceLoadedDotNetRuntime]:
    return clr.TraceLoadedDotNetRuntimeExtensions.LoadedDotNetRuntime(process)


def get_gcs_from_process(clr: Clr, p: AbstractTraceProcess) -> Sequence[AbstractTraceGC]:
    return _get_gcs_from_mang(non_null(try_get_runtime(clr, p)), p.Name)


def _get_gcs_from_mang(
    mang: AbstractTraceLoadedDotNetRuntime, name: str
) -> Sequence[AbstractTraceGC]:
    # Skip the first two GCs which often have incomplete events
    unfiltered_gcs = mang.GC.GCs[2:]

    def flt(i: int, gc: AbstractTraceGC) -> bool:
        gc_type = GCType(gc.Type)
        if not gc.IsComplete:
            if gc_type == GCType.BackgroundGC and i == len(unfiltered_gcs) - 1:
                pass  # print("(note: final bgc is incomplete, ignoring)")
            else:
                print(
                    f"WARN: In {name}, ignoring incomplete gc number {gc.Number}. "
                    + f"It's a {GCType(gc.Type)}"
                )
        return gc.IsComplete

    return [gc for i, gc in enumerate(unfiltered_gcs) if flt(i, gc)]


def get_process_info_from_process(
    clr: Clr,
    p: AbstractTracedProcesses,
    trace_path: Path,
    process: AbstractTraceProcess,
    show_name: str,
) -> ProcessInfo:
    mang = non_null(try_get_runtime(clr, process))

    return ProcessInfo(
        event_names=p.event_names,
        name=show_name,
        trace_path=trace_path,
        process=process,
        mang=mang,
        all_gcs_including_incomplete=tuple(mang.GC.GCs),
        gcs=_get_gcs_from_mang(mang, show_name),
        stats=mang.GC.Stats(),
        events_time_span=p.events_time_span,
        per_heap_history_times=p.per_heap_history_times,
    )


@with_slots
@dataclass()  # not frozen because num_samples is mutable
class InfoAtCodeAddress:
    callees: Dict[str, InfoAtCodeAddress]
    method_name: str
    num_samples: int


# process id -> thread id -> stack frame id (code address, stored as a hex string) -> Foo
CpuStackFrameSummary = Mapping[int, Mapping[int, Mapping[str, InfoAtCodeAddress]]]
_MutCpuStackFrameSummary = Dict[int, Dict[int, Dict[str, InfoAtCodeAddress]]]


# Only used inside get_cpu_stack_frame_summary, but mypy complains if I nest class
@with_slots
@dataclass(frozen=True)
class _CallStack:
    code_address: str
    method_name: str
    callee: Any  # Optional[_CallStack] (recursive types not yet supported)


def get_cpu_stack_frame_summary(
    clr: Clr,
    log: AbstractTraceLog,
    cpu_id: int,
    proc_ids: Iterable[int],
    start_time: float,
    end_time: float,
) -> CpuStackFrameSummary:
    process_list: Dict[int, Dict[int, Dict[str, InfoAtCodeAddress]]] = {}

    def add_stack_to_tree(stack: _CallStack, event: AbstractTraceEvent) -> None:
        call_stack_breakdown_by_thread = process_list.setdefault(event.ProcessID, {})
        call_stack_breakdown_by_code_address = call_stack_breakdown_by_thread.setdefault(
            event.ThreadID, {}
        )

        cur_stack_frame_to_add = stack
        cur_stack_tree_root = call_stack_breakdown_by_code_address

        while cur_stack_frame_to_add is not None:
            cur_stack_frame_id = cur_stack_frame_to_add.code_address
            cur_stack_frame_method_name = cur_stack_frame_to_add.method_name

            if cur_stack_frame_id not in cur_stack_tree_root:
                cur_stack_tree_root[cur_stack_frame_id] = InfoAtCodeAddress(
                    {}, cur_stack_frame_method_name, 1
                )
            else:
                cur_stack_tree_root[cur_stack_frame_id].num_samples += 1

            cur_stack_tree_root = cur_stack_tree_root[cur_stack_frame_id].callees
            cur_stack_frame_to_add = cur_stack_frame_to_add.callee

    for ev in log.Events.ByEventType[
        clr.TraceEvent
    ]():  # Plain enumerator does not work, must use this instead
        if not (
            ev.ProcessorNumber == cpu_id
            and "Sample" in ev.EventName
            and ev.ProcessID in proc_ids
            and start_time <= ev.TimeStampRelativeMSec <= end_time
        ):
            continue

        cur_frame = log.GetCallStackForEvent(ev)
        if cur_frame is None:
            continue

        stack_frames = _CallStack(
            hex(cur_frame.CodeAddress.Address),
            cur_frame.CodeAddress.FullMethodName or "<unknown method name>",
            None,
        )

        cur_frame_depth = 0
        max_frame_depth = 10
        while cur_frame is not None and cur_frame_depth < max_frame_depth:
            stack_frames = _CallStack(
                hex(cur_frame.CodeAddress.Address),
                cur_frame.CodeAddress.FullMethodName,
                stack_frames,
            )
            cur_frame = cast(AbstractTraceCallStack, cur_frame.Caller)

        if cur_frame_depth >= max_frame_depth:
            stack_frames = _CallStack("...", "Truncated stack frame", stack_frames)
        add_stack_to_tree(stack_frames, ev)
    return process_list


def load_module_symbols_for_events(
    clr: Clr, sym_path: str, trace_log: AbstractTraceLog, symbol_log_path: Path
) -> None:
    sym_log_writer = clr.File.CreateText(str(symbol_log_path))
    sym_reader = clr.SymbolReader(sym_log_writer, sym_path)
    modules_to_load = ("clr", "kernel32", "ntdll", "ntoskrnl", "mscorlib")
    # pythonnet can't resolve `GetEnumerator()` correctly, so access the private array
    module_files_object = trace_log.ModuleFiles
    module_files_arr = (
        module_files_object.GetType()
        .GetField("moduleFiles", clr.BindingFlags.Instance | clr.BindingFlags.NonPublic)
        .GetValue(module_files_object)
    )
    for i in range(module_files_arr.Count):
        module = module_files_arr[i]
        m_name = module.Name.lower()
        if any(m in m_name for m in modules_to_load):
            trace_log.CodeAddresses.LookupSymbolsForModule(sym_reader, module)
            print(f"Loaded symbols for module {module}")
    sym_log_writer.Close()
