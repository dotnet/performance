# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from gc import collect as garbage_collect
from pathlib import Path
from typing import cast, Dict, Iterable, Sequence

from result import Err, Ok, Result

from ..commonlib.bench_file import is_trace_path, load_test_status, TestResult
from ..commonlib.collection_util import indices, map_to_mapping, repeat, zip_check, zip_check_3
from ..commonlib.option import map_option, non_null
from ..commonlib.result_utils import map_ok, match
from ..commonlib.util import change_extension, show_size_bytes

from .clr import Clr, get_clr
from .clr_types import (
    AbstractGCPerHeapHistory,
    AbstractJoinInfoForGC,
    AbstractJoinInfoForHeap,
    AbstractMarkInfo,
    AbstractServerGcHistory,
    AbstractTraceGC,
    cs_result_to_result,
)
from .core_analysis import (
    get_process_names_and_process_info,
    process_predicate_from_id,
    process_predicate_from_parts,
)
from .join_analysis import get_join_info_for_all_gcs
from .mechanisms import get_mechanisms_and_reasons_for_process_info
from .types import (
    Failable,
    MaybeMetricValuesForSingleIteration,
    ProcessedGC,
    ProcessedHeap,
    ProcessedTrace,
    ProcessQuery,
    RunMetrics,
    ThreadToProcessToName,
)


def test_result_from_path(path: Path) -> TestResult:
    if path.name.endswith(".yaml"):
        # Try to find trace path
        trace_file_name = load_test_status(path).trace_file_name
        return TestResult(
            test_status_path=path, trace_path=map_option(trace_file_name, lambda n: path.parent / n)
        )
    else:
        assert is_trace_path(path), f"{path} should be a '.yaml' test output file or a trace file."
        # User might have specified the tracefile when a status file still exists
        test_status = change_extension(path, ".yaml")
        status_exists = test_status.exists()
        assert (not status_exists) or load_test_status(test_status).trace_file_name == path.name
        return TestResult(test_status_path=test_status if status_exists else None, trace_path=path)


def get_processed_trace(
    clr: Clr,
    test_result: TestResult,
    process: ProcessQuery,
    need_mechanisms_and_reasons: bool,
    need_join_info: bool,
) -> Result[str, ProcessedTrace]:
    test_status = test_result.load_test_status()

    if test_result.trace_path is None:
        if need_join_info:
            return Err("Can't get join info without a trace.")
        elif process is not None:
            return Err("Can't get process without a trace.")
        else:
            return Ok(
                ProcessedTrace(
                    clr=clr,
                    test_result=test_result,
                    test_status=test_status,
                    process_info=None,
                    process_names=cast(ThreadToProcessToName, None),
                    process_query=None,
                    join_info=Err("Did not request join info"),
                    mechanisms_and_reasons=None,
                    gcs_result=Err("Did not collect a trace"),
                )
            )
    else:
        if process is None:
            assert test_status is not None, (
                "Didn't specify --process and there's no test status to specify PID\n"
                " (hint: maybe specify the test output '.yaml' file instead of the trace file)"
            )
            process_predicate = process_predicate_from_id(test_status.process_id)
        else:
            assert (
                test_status is None
            ), "'--process' is unnecessary as the test result specifies the PID"
            process_predicate = process_predicate_from_parts(process)

        process_names, proc = get_process_names_and_process_info(
            clr,
            test_result.trace_path,
            str(test_result),
            process_predicate,
            # TODO: make this optional; though the metric FirstEventToFirstGCSeconds needs this too.
            collect_event_names=True,
        )

        # TODO: just do this lazily (getting join info)
        join_info = (
            get_join_info_for_all_gcs(clr, proc)
            if need_join_info
            else Err("Did not request join info")
        )
        res = ProcessedTrace(
            clr=clr,
            test_result=test_result,
            test_status=test_status,
            process_info=proc,
            process_names=process_names,
            process_query=process,
            join_info=join_info,
            # TODO: just do this lazily
            mechanisms_and_reasons=get_mechanisms_and_reasons_for_process_info(proc)
            if need_mechanisms_and_reasons
            else None,
            gcs_result=Err("temporary err, will be overwritten"),
        )
        gc_join_infos: Iterable[Result[str, AbstractJoinInfoForGC]] = match(
            join_info,
            lambda j: [cs_result_to_result(jgc) for jgc in j.GCs],
            lambda e: repeat(Err(e), len(proc.gcs)),
        )
        res.gcs_result = Ok(
            [
                _get_processed_gc(res, i, gc_join_info)
                for i, gc_join_info in zip_check(indices(proc.gcs), gc_join_infos)
            ]
        )
        return Ok(res)


def _get_processed_gc(
    proc: ProcessedTrace, gc_index: int, join_info: Result[str, AbstractJoinInfoForGC]
) -> ProcessedGC:
    gc = non_null(proc.process_info).gcs[gc_index]
    heap_join_infos: Iterable[Result[str, AbstractJoinInfoForHeap]] = match(
        join_info, lambda j: [Ok(h) for h in j.Heaps], lambda e: repeat(Err(e), gc.HeapCount)
    )
    res = ProcessedGC(proc=proc, index=gc_index, trace_gc=gc, join_info=join_info, heaps=())
    res.heaps = [
        ProcessedHeap(
            gc=res,
            index=hp_i,
            per_heap_history=phh,
            server_gc_history=sgh,
            _mark_times=map_ok(mark_times, lambda m: m.MarkTimes),
            _mark_promoted=map_ok(mark_times, lambda m: m.MarkPromoted),
            join_info=heap_join_info,
        )
        for hp_i, (phh, sgh, heap_join_info) in enumerate(
            zip_check_3(
                _get_per_heap_histories(gc), _get_server_gc_heap_histories(gc), heap_join_infos
            )
        )
        for mark_times in (_get_mark_times(proc.clr, gc, hp_i),)
    ]
    return res


def _get_per_heap_histories(gc: AbstractTraceGC) -> Sequence[Result[str, AbstractGCPerHeapHistory]]:
    if gc.HeapCount == 1:
        return [Err("Workstation GC has no AbstractGCPerHeapHistories")]
    else:
        n = len(gc.PerHeapHistories)
        if n != gc.HeapCount:
            print(f"WARN: GC {gc.Number} has {gc.HeapCount} heaps, but {n} PerHeapHistories")
            return repeat(Err("GC has wrong number of PerHeapHistories"), gc.HeapCount)
        else:
            return [Ok(h) for h in gc.PerHeapHistories]


def _get_server_gc_heap_histories(
    gc: AbstractTraceGC
) -> Sequence[Result[str, AbstractServerGcHistory]]:
    if gc.HeapCount == 1:
        return [Err("Workstation GC has no ServerGcHeapHistories")]
    else:
        n = len(gc.ServerGcHeapHistories)
        if n != gc.HeapCount:
            print(f"WARN: GC {gc.Number} has {gc.HeapCount} heaps, but {n} ServerGcHeapHistories")
            return repeat(Err("GC has wrong number of ServerGcHeapHistories"), gc.HeapCount)
        else:
            return [Ok(h) for h in gc.ServerGcHeapHistories]


def _get_mark_times(clr: Clr, gc: AbstractTraceGC, hp_i: int) -> Failable[AbstractMarkInfo]:
    m = gc.PerHeapMarkTimes
    if m is None:
        return Err("No PerHeapMarkTimes")
    else:
        res = clr.PythonnetUtil.TryGetValue(m, hp_i)
        if res is None:
            return Err(f"PerHeapMarkTimes contains no heap {hp_i}")
        else:
            return Ok(res)


def file_size(path: Path) -> int:
    return path.stat().st_size


# Acts as a cache of ProcessedTraceFile
class ProcessedTraces:
    path_to_file: Dict[TestResult, Result[str, ProcessedTrace]]

    def __init__(self) -> None:
        self.path_to_file = {}
        self.clr = get_clr()

    def get(
        self,
        test_result: TestResult,
        process: ProcessQuery,
        need_mechanisms_and_reasons: bool,
        need_join_info: bool,
        dont_cache: bool = False,
    ) -> Result[str, ProcessedTrace]:
        current = None if dont_cache else self.path_to_file.get(test_result, None)
        if (
            current is not None
            and current.is_ok()
            and _processed_trace_file_has_enough_info(
                current.unwrap(),
                process,
                need_mechanisms_and_reasons=need_mechanisms_and_reasons,
                need_join_info=need_join_info,
            )
        ):
            return current
        else:
            pth = test_result.trace_or_test_status_path
            size = file_size(pth)
            print(f"Processing {pth} ({show_size_bytes(size)})")
            updated = get_processed_trace(
                clr=self.clr,
                test_result=test_result,
                process=process,
                need_mechanisms_and_reasons=need_mechanisms_and_reasons,
                need_join_info=need_join_info,
            )
            if updated.is_ok():
                assert _processed_trace_file_has_enough_info(
                    updated.unwrap(),
                    process,
                    need_mechanisms_and_reasons=need_mechanisms_and_reasons,
                    need_join_info=need_join_info,
                )
            if not dont_cache:
                self.path_to_file[test_result] = updated
            return updated

    def get_run_metrics(
        self, test_result: TestResult, run_metrics: RunMetrics
    ) -> MaybeMetricValuesForSingleIteration:
        """Discards the trace when done to avoid using too much memory."""
        already_had = test_result in self.path_to_file
        # TODO: may need join info
        res = map_ok(
            self.get(
                test_result, process=None, need_mechanisms_and_reasons=False, need_join_info=False
            ),
            lambda trace: map_to_mapping(run_metrics, trace.metric),
        )

        if not already_had:
            del self.path_to_file[test_result]
            # Ensure the trace is freed.
            # Without this, Python won't do a collection, and C# will run out of memory.
            # C# garbage collection isn't enough since Python needs to do GC to release the trace.
            garbage_collect()
        return res


def _processed_trace_file_has_enough_info(
    current: ProcessedTrace,
    process: ProcessQuery,
    need_mechanisms_and_reasons: bool,
    need_join_info: bool,
) -> bool:
    return (
        current.process_query == process
        and current.has_mechanisms_and_reasons >= need_mechanisms_and_reasons
        and current.has_join_info >= need_join_info
    )
