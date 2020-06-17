# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

"""
WARN: The server code is still here but the JS gui code is not in this repo
because we need to know if it's OK to include vendored requirements.
(Or, we could update it to get them from NPM).
"""

from dataclasses import dataclass
from math import inf
from pathlib import Path
import socket
from threading import Lock
from typing import Any, Callable, cast, Dict, Optional, Sequence, Tuple

from flask import Flask, jsonify, request, Request, Response, send_from_directory

from ..commonlib.collection_util import find_only_matching
from ..commonlib.config import CWD, GC_PATH
from ..commonlib.parse_and_serialize import to_serializable
from ..commonlib.result_utils import match
from ..commonlib.type_utils import with_slots

from .clr import get_clr
from .clr_types import AbstractTraceGC, AbstractTraceLog, AbstractTraceProcess
from .core_analysis import (
    find_process,
    get_cpu_stack_frame_summary,
    get_etl_trace,
    get_gcs_from_process,
    get_trace_log,
    get_traced_processes,
    load_module_symbols_for_events,
    ProcessPredicate,
)
from .gui_join_analysis import (
    GC_JOIN_STAGES_BY_GC_PHASE,
    GcJoinStatistics,
    get_gc_join_duration_statistics,
    get_gc_join_timeframes,
    get_gc_thread_state_timeline,
    StatsOverAllJoins,
)
from .gui_stolen_cpu_analysis import get_gc_stolen_cpu_instances, get_gc_stolen_cpu_times


def _j(value: object) -> Response:
    return jsonify(cast(Any, to_serializable(value)))


@with_slots
@dataclass(frozen=True)
class App:
    app: Flask
    # Can't make this directly be a Callable: https://github.com/python/mypy/issues/708
    load_etl_and_get_process_id: Tuple[Callable[[Path, ProcessPredicate], int],]


_GUI_DIR = GC_PATH / "gui"


def get_app() -> App:
    app = Flask(__name__)
    app.config["JSONIFY_PRETTYPRINT_REGULAR"] = False
    symbol_log_path = CWD / "sym_loader.txt"

    clr = get_clr()

    class InvalidUsage(Exception):
        def __init__(self, message: str, status_code: int = 400, payload: Any = None):
            Exception.__init__(self)
            self.message = message
            self.status_code = status_code
            self.payload = payload

        def to_dict(self) -> Dict[str, Any]:
            rv = dict(self.payload or ())
            rv["message"] = self.message
            return rv

    def handle_invalid_usage(error: InvalidUsage) -> Response:
        response = _j(error.to_dict())
        response.status_code = error.status_code
        return response

    app.errorhandler(InvalidUsage)(handle_invalid_usage)

    def all_exception_handler(error: Exception) -> Response:
        raise error

    app.errorhandler(Exception)(all_exception_handler)

    def route(path: str, fn: Callable[..., Response]) -> None:
        assert not path.endswith(
            "/"
        )  # flasks seems to automatically add '/' to all requests. TODO: or is this jquery?
        app.route(path)(fn)
        app.route(path + "/")(fn)

    def send_file(path: str) -> Response:
        return send_from_directory(str(_GUI_DIR), path)

    app.route("/gui/<path:path>")(send_file)

    def find_proc_by_id(proc_id: int) -> AbstractTraceProcess:
        return find_only_matching(lambda p: p.ProcessID, proc_id, all_processes)

    proc_id_and_gcs: Optional[Tuple[int, Sequence[AbstractTraceGC]]] = None

    get_gcs_lock = Lock()

    def get_gcs(proc_id: int) -> Sequence[AbstractTraceGC]:
        nonlocal proc_id_and_gcs
        with get_gcs_lock:
            if proc_id_and_gcs is None:
                proc_id_and_gcs = (proc_id, get_gcs_from_process(clr, find_proc_by_id(proc_id)))
        assert proc_id_and_gcs[0] == proc_id
        return proc_id_and_gcs[1]

    def get_gc(proc_id: int, gc_id: int) -> AbstractTraceGC:
        return get_gcs(proc_id)[gc_id]

    def get_gc_basic_info(proc_id: int, gc_id: int) -> Response:
        gc = get_gc(proc_id, gc_id)
        return _j(
            {
                "Pause Start Time": gc.PauseStartRelativeMSec,
                "GC Start Time": gc.StartRelativeMSec,
                "GC End Time": gc.StartRelativeMSec + gc.DurationMSec,
                "Pause End Time": gc.PauseStartRelativeMSec + gc.PauseDurationMSec,
                "Generation": gc.Generation,
                "Type": gc.Type,
                "Reason": gc.Reason,
            }
        )

    route("/stats/proc/<int:proc_id>/gc/<int:gc_id>", get_gc_basic_info)

    @with_slots
    @dataclass(frozen=True)
    class GcPauseTime:
        DurationMSec: float
        PauseDurationMSec: float
        gc_number: int

    def get_gc_pause_durations() -> Response:
        proc_id = _must_fetch_int(request, "proc_id")

        gcs = get_gcs(proc_id)
        gc_durations = [
            GcPauseTime(gc.DurationMSec, gc.PauseDurationMSec, idx) for idx, gc in enumerate(gcs)
        ]
        s = sorted(gc_durations, key=lambda gc: gc.DurationMSec)
        return _j(s)

    route("/stats/gc/pause_time", get_gc_pause_durations)

    def get_gc_stolen_cpu_time() -> Response:
        proc_id = _must_fetch_int(request, "proc_id")
        # TODO: it used to be a list in case pid wasn't provided, but it always is...
        return _j([get_gc_stolen_cpu_times(clr, proc_id, "proc_name", get_gcs(proc_id))])

    route("/stats/gc/stolen_cpu/per_phase", get_gc_stolen_cpu_time)

    def get_gc_join_imbalance_stats() -> Response:
        proc_id = _must_fetch_int(request, "proc_id")
        gc_id = _must_fetch_int(request, "gc_id")

        def get_gc_join_duration_statistics_not_none(gc: AbstractTraceGC) -> GcJoinStatistics:
            return match(
                get_gc_join_duration_statistics(gc),
                cb_ok=lambda x: x,
                # TODO: gui should handle this
                cb_err=lambda _: GcJoinStatistics(
                    statistics_over_all_joins=StatsOverAllJoins(0, 0, 0),
                    statistics_over_individual_joins={},
                    statistics_over_individual_gc_phases={},
                ),
            )

        return _j(get_gc_join_duration_statistics_not_none(get_gc(proc_id, gc_id)))

    route("/stats/gc/join_imbalance", get_gc_join_imbalance_stats)

    def get_stolen_cpu_per_phase() -> Response:
        proc_id = _must_fetch_int(request, "proc_id")
        gc_id = _must_fetch_int(request, "gc_id")
        return _j(get_gc_stolen_cpu_instances(clr, get_gc(proc_id, gc_id), trace_log.Events))

    route("/chrono/gc/stolen_cpu/per_phase", get_stolen_cpu_per_phase)

    def get_gc_heap_join_states() -> Response:
        proc_id = _try_fetch_int(request, "proc_id")
        gc_id = _try_fetch_int(request, "gc_id")
        heap_id = _try_fetch_int(request, "heap_id")

        if proc_id is None or gc_id is None:
            raise InvalidUsage(
                "USAGE: GET\t/chrono/gc/join/per_thread_state\tARGS\tproc_id: "
                "Process ID\tgc_id: GC number for this process",
                status_code=422,
            )

        return _j(get_gc_thread_state_timeline(clr, get_gc(proc_id, gc_id), heap_id))

    route("/chrono/gc/join/per_thread_state", get_gc_heap_join_states)

    def get_gc_join_time_ranges() -> Response:
        proc_id = _must_fetch_int(request, "proc_id")
        gc_id = _must_fetch_int(request, "gc_id")

        if None in [proc_id, gc_id]:
            raise InvalidUsage(
                "USAGE: GET\t/chrono/gc/join/stages\tARGS\tproc_id: "
                "Process ID\tgc_id: GC number for this process",
                status_code=422,
            )

        stages_phases_known, phase_times, stage_times, join_index_times = get_gc_join_timeframes(
            clr, get_gc(proc_id, gc_id)
        )

        for stage in stage_times:
            st = stage_times[stage]
            for st_time in ("start", "end"):
                if getattr(st, st_time) in [inf, -inf]:
                    setattr(st, st_time, str(getattr(st, st_time)))

        return _j(
            {
                "Join Stages Known": stages_phases_known,
                "Join Phases Known": stages_phases_known,
                "Timeframes By GC Phase": phase_times,
                "Timeframes By Join Stage": stage_times,
                "Timeframes By Join Index": join_index_times,
            }
        )

    route("/chrono/gc/join/stages", get_gc_join_time_ranges)

    def get_stack_frame_summary_for_cpu(cpu_id: int) -> Response:
        proc_ids = [int(arg) for arg in request.args.getlist("proc_id")]
        start_time = _must_fetch_float(request, "start_time")
        end_time = _must_fetch_float(request, "end_time")
        return _j(
            get_cpu_stack_frame_summary(clr, trace_log, cpu_id, proc_ids, start_time, end_time)
        )

    route("/chrono/cpu/<int:cpu_id>/stack_frames/summary", get_stack_frame_summary_for_cpu)

    etl_path: Path
    trace_log: AbstractTraceLog
    all_processes: Sequence[AbstractTraceProcess]

    def load_etl_trace() -> Response:
        assert "etl_path" in request.form  # if "etl_path" in request.form:
        etl_path: str = request.form["etl_path"]
        do_load_etl(Path(etl_path))
        return _j(etl_path)

    # Note: I do not allow POST requests to the route without a trailing slash because Flask will
    # re-route POST /trace to GET /trace/, instead of POST /trace/ as expected.
    # Instead of introducing potential debugging confusion to users of this API,
    # it seems better to allow a small amount of inconsistency.
    app.route("/trace/", methods=["POST"])(load_etl_trace)

    def do_load_etl(the_etl_path: Path) -> None:
        nonlocal all_processes, etl_path, trace_log
        etl_path = the_etl_path
        sym_path = get_etl_trace(
            clr, etl_path
        ).SymPath  # TODO: this is only used for .SymPath, seems inefficient
        all_processes = tuple(get_traced_processes(clr, etl_path).processes)
        trace_log = get_trace_log(clr, etl_path)
        load_module_symbols_for_events(clr, sym_path, trace_log, symbol_log_path)

    route(
        "/processes",
        lambda: _j(
            [
                {"Process Name": p.Name, "PID": p.ProcessID, "Parent PID": p.ParentID}
                for p in all_processes
            ]
        ),
    )

    def get_join_stages_per_phase() -> Response:
        return _j(
            {
                gc_phase: [{"name": stage.name, "id": stage.value} for stage in join_stages]
                for gc_phase, join_stages in GC_JOIN_STAGES_BY_GC_PHASE.items()
            }
        )

    route("/info/gc/phases/join_stages_per_phase", get_join_stages_per_phase)

    def load_etl_and_get_process_id(etl_path: Path, process_predicate: ProcessPredicate) -> int:
        do_load_etl(etl_path)
        return find_process(clr, all_processes, process_predicate).ProcessID

    return App(app, (load_etl_and_get_process_id,))


def is_port_used(port: int) -> bool:
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try:
        s.bind(("127.0.0.1", port))
    except socket.error:
        return True
    return False


def _must_fetch_int(rq: Request, arg_name: str) -> int:
    return int(rq.args[arg_name])


def _must_fetch_float(rq: Request, arg_name: str) -> float:
    return float(rq.args[arg_name])


def _try_fetch_str(rq: Request, arg_name: str) -> Optional[str]:
    return rq.args.get(arg_name, None)


def _try_fetch_int(rq: Request, arg_name: str) -> Optional[int]:
    s = _try_fetch_str(rq, arg_name)
    return None if s is None else int(s)
