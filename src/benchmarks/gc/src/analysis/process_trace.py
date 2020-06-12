# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from gc import collect as garbage_collect
from pathlib import Path
from typing import cast, Dict, Iterable, Sequence

from result import Err, Ok, Result

from ..commonlib.bench_file import (
    is_trace_path,
    load_test_status,
    ProcessQuery,
    TestResult,
    TestRunStatus,
)
from ..commonlib.collection_util import indices, map_to_mapping, repeat, zip_check, zip_check_3
from ..commonlib.option import map_option, non_null
from ..commonlib.result_utils import map_err, map_ok, match, option_to_result, unwrap
from ..commonlib.util import show_size_bytes

from .clr import Clr, get_clr
from .clr_types import (
    AbstractGCPerHeapHistory,
    AbstractJoinInfoForGC,
    AbstractJoinInfoForHeap,
    AbstractMarkInfo,
    AbstractServerGcHistory,
    AbstractTraceGC,
    AbstractTraceLoadedDotNetRuntime,
    AbstractTraceProcess,
    AbstractTracedProcesses,
    cs_result_to_result,
)
from .core_analysis import (
    get_process_info_from_mang,
    get_process_names_and_process_info,
    process_predicate_from_id,
    process_predicate_from_parts,
)
from .join_analysis import get_join_info_for_all_gcs
from .mechanisms import get_mechanisms_and_reasons_for_process_info
from .types import (
    Failable,
    get_gc_kind_for_abstract_trace_gc,
    MaybeMetricValuesForSingleIteration,
    ProcessedGC,
    ProcessedHeap,
    ProcessInfo,
    ProcessedTrace,
    RunMetrics,
    ThreadToProcessToName,
)


def test_result_from_path(path: Path, process: ProcessQuery) -> TestResult:
    if path.name.endswith(".yaml"):
        # Try to find trace path
        assert (
            process is None
        ), f"When working with .yaml files, --process is not needed. Check {path}."
        trace_file_name = load_test_status(path).trace_file_name
        return TestResult(
            test_status_path=path, trace_path=map_option(trace_file_name, lambda n: path.parent / n)
        )
    else:
        assert is_trace_path(path), f"{path} should be a '.yaml' test output file or a trace file."
        return TestResult(
            test_status_path=None, trace_path=path, process=_convert_to_tuple(process)
        )


def get_processed_trace(
    clr: Clr, test_result: TestResult, need_mechanisms_and_reasons: bool, need_join_info: bool
) -> Result[str, ProcessedTrace]:
    test_status = option_to_result(
        test_result.load_test_status(), lambda: "Need a test status file"
    )

    if test_result.trace_path is None:
        if need_join_info:
            return Err("Can't get join info without a trace.")
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
        return Ok(
            _get_processed_trace_from_process(
                clr,
                test_status,
                test_result,
                need_join_info=need_join_info,
                need_mechanisms_and_reasons=need_mechanisms_and_reasons,
            )
        )


def get_processed_trace_from_just_process(
    clr: Clr,
    trace_path: Path,
    p: AbstractTracedProcesses,
    process: AbstractTraceProcess,
    mang: AbstractTraceLoadedDotNetRuntime,
) -> ProcessedTrace:
    proc_info = get_process_info_from_mang(p, trace_path, process, trace_path.name, mang)
    return _init_processed_trace(
        ProcessedTrace(
            clr=clr,
            test_result=TestResult(trace_path=trace_path),
            test_status=Err("get_processed_trace_from_just_process has no test status"),
            process_info=proc_info,
            process_names=cast(ThreadToProcessToName, None),
            process_query=None,
            join_info=Err("did not request join info"),
            mechanisms_and_reasons=None,
            gcs_result=Err("temp"),
        ),
        proc_info,
    )


def _get_processed_trace_from_process(
    clr: Clr,
    test_status: Failable[TestRunStatus],
    test_result: TestResult,
    need_join_info: bool,
    need_mechanisms_and_reasons: bool,
) -> ProcessedTrace:
    if test_result.process is None:
        ts = unwrap(
            map_err(
                test_status,
                lambda _: "Didn't specify --process and there's no test status to specify PID.\n"
                " (hint: maybe specify the test output '.yaml' file instead of the trace file)",
            )
        )
        if ts.process_id is not None:
            process_predicate = process_predicate_from_id(ts.process_id)
        else:
            process_predicate = process_predicate_from_parts(ts.get_process_data_tuple())

    else:
        assert (
            test_status.is_err()
        ), "'--process' is unnecessary as the test result specifies the PID"
        process_predicate = process_predicate_from_parts(test_result.process)

    process_names, proc = get_process_names_and_process_info(
        clr,
        non_null(test_result.trace_path),
        str(test_result),
        process_predicate,
        # TODO: make this optional; though the metric FirstEventToFirstGCSeconds needs this too.
        collect_event_names=True,
    )

    assert len(proc.gcs) > 0, (
        f"Process '{proc.process.Name}' in Trace File '{proc.trace_path.name}' "
        "has no GC's to analyze."
    )

    # TODO: just do this lazily (getting join info)
    join_info = (
        get_join_info_for_all_gcs(clr, proc) if need_join_info else Err("Did not request join info")
    )
    res = ProcessedTrace(
        clr=clr,
        test_result=test_result,
        test_status=test_status,
        process_info=proc,
        process_names=process_names,
        process_query=test_result.process,
        join_info=join_info,
        # TODO: just do this lazily
        mechanisms_and_reasons=get_mechanisms_and_reasons_for_process_info(proc)
        if need_mechanisms_and_reasons
        else None,
        gcs_result=Err("temporary err, will be overwritten"),
    )
    return _init_processed_trace(res, proc)


def _init_processed_trace(res: ProcessedTrace, process_info: ProcessInfo) -> ProcessedTrace:
    gc_join_infos: Iterable[Result[str, AbstractJoinInfoForGC]] = match(
        res.join_info,
        lambda j: [cs_result_to_result(jgc) for jgc in j.GCs],
        lambda e: repeat(Err(e), len(process_info.gcs)),
    )
    res.gcs_result = Ok(
        [
            _get_processed_gc(res, i, gc_join_info)
            for i, gc_join_info in zip_check(indices(process_info.gcs), gc_join_infos)
        ]
    )
    return res


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
            print(
                f"WARN: GC {gc.Number} has {gc.HeapCount} heaps, but {n} PerHeapHistories. It's a "
                + f" It's a {get_gc_kind_for_abstract_trace_gc(gc).name}."
            )
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
            print(
                f"WARN: GC {gc.Number} has {gc.HeapCount} heaps, but {n} ServerGcHeapHistories."
                + f" It's a {get_gc_kind_for_abstract_trace_gc(gc).name}."
            )
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
                test_result.process,
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
                need_mechanisms_and_reasons=need_mechanisms_and_reasons,
                need_join_info=need_join_info,
            )
            if updated.is_ok():
                assert _processed_trace_file_has_enough_info(
                    updated.unwrap(),
                    test_result.process,
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
            self.get(test_result, need_mechanisms_and_reasons=False, need_join_info=False),
            lambda trace: map_to_mapping(run_metrics, trace.metric),
        )

        if not already_had:
            del self.path_to_file[test_result]
            # Ensure the trace is freed.
            # Without this, Python won't do a collection, and C# will run out of memory.
            # C# garbage collection isn't enough since Python needs to do GC to release the trace.
            garbage_collect()
        return res


def _convert_to_tuple(process: ProcessQuery) -> ProcessQuery:
    if process is None:
        return None
    else:
        return tuple(process)


def _processed_trace_file_has_enough_info(
    current: ProcessedTrace,
    process: ProcessQuery,
    need_mechanisms_and_reasons: bool,
    need_join_info: bool,
) -> bool:
    return (
        _convert_to_tuple(current.process_query) == _convert_to_tuple(process)
        and current.has_mechanisms_and_reasons >= need_mechanisms_and_reasons
        and current.has_join_info >= need_join_info
    )
