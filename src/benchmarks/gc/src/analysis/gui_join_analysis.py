# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

"""
This is OLD analysis code that is not as good as the analysis from managed-lib.
This exists just because I don't have time to update the gui code.
"""

# pylint:disable=line-too-long,too-many-branches,unused-argument

from collections import defaultdict
from dataclasses import dataclass
from math import inf, isclose
from statistics import median
from typing import Dict, Iterable, Mapping, Optional, Sequence, Set, Tuple

from result import Err, Ok, Result

from ..commonlib.collection_util import (
    add,
    invert_multi_mapping,
    is_empty,
    make_multi_mapping,
    map_mapping,
    map_mapping_keys,
)
from ..commonlib.option import non_null, optional_to_iter
from ..commonlib.result_utils import map_ok
from ..commonlib.type_utils import with_slots
from ..commonlib.util import lazy_property

from .clr import Clr
from .clr_types import AbstractServerGcHistory, AbstractTraceGC
from .enums import GcJoinPhase, GcJoinStage, GcJoinTime, GcJoinType, ServerGCThreadState


# This dictionary documents different join stages that are fired within a GC, whether concurrent
# or not. These are listed in the chronological order in which they should be fired based on gc.cpp
# and are taken advantage of for marking the start/end of their respective phases. It's worth
# noting that these don't line up perfectly with the actual phase start/end, but are our best
# source of information without using CPU samples. Furthermore, it's worth noting that depending
# on ETW traces we receive from customers, arbitrary combinations of these stage events may be
# dropped in the trace or, in rarer circumstances, re-ordered.
#
# One should also note that not all of these joins will fire per GC. For example, a GC event trace
# should not contain join events fired for both the sweep phase and relocate phase. By chronological
# order, I mean that:
#
#   1) Within a GC phase, a join stage is listed in the order it would occur relative to other
#      join phases in that GC phase, *if* it occurs at all (most do). Similarly,
#   2) A GC phase is listed in the order it would occur relative to other
#      phases, *if* it occurs.
GC_JOIN_STAGES_BY_GC_PHASE: Mapping[GcJoinPhase, Sequence[GcJoinStage]] = {
    GcJoinPhase.mark: [
        GcJoinStage.begin_mark_phase,
        GcJoinStage.scan_sizedref_done,
        GcJoinStage.update_card_bundles,
        GcJoinStage.scan_dependent_handles,
        GcJoinStage.null_dead_short_weak,
        GcJoinStage.scan_finalization,
        GcJoinStage.null_dead_long_weak,
        GcJoinStage.null_dead_syncblk,
    ],
    GcJoinPhase.plan: [GcJoinStage.decide_on_compaction],  # Slightly past end of plan phase
    GcJoinPhase.relocate: [GcJoinStage.begin_relocate_phase, GcJoinStage.relocate_phase_done],
    GcJoinPhase.compact: [
        GcJoinStage.rearrange_segs_compaction,
        GcJoinStage.adjust_handle_age_compact,  # Marks end of compact phase
    ],
    GcJoinPhase.sweep: [GcJoinStage.adjust_handle_age_sweep],  # Marks end of sweep phase
    GcJoinPhase.heap_verify: [GcJoinStage.verify_copy_table, GcJoinStage.verify_objects_done],
    GcJoinPhase.post_gc: [GcJoinStage.done],  # Marks start of post gc work
}
_GC_JOIN_STAGE_TO_PHASE = invert_multi_mapping(GC_JOIN_STAGES_BY_GC_PHASE)


def _try_get_join_phase(stage: GcJoinStage) -> Optional[GcJoinPhase]:
    return _GC_JOIN_STAGE_TO_PHASE.get(stage)


@with_slots
@dataclass(frozen=True)
class JoinEvent:
    type: GcJoinType
    time: GcJoinTime
    stage: GcJoinStage
    processor_number: int
    time_msec: float

    def _is_restart_either(self) -> bool:
        return self.type == GcJoinType.restart and self.stage == GcJoinStage.restart

    def is_restart_start(self) -> bool:
        return self._is_restart_either() and self.time == GcJoinTime.start

    def is_restart_end(self) -> bool:
        return self._is_restart_either() and self.time == GcJoinTime.end

    def is_join_start(self) -> bool:
        return self.type == GcJoinType.join and self.time == GcJoinTime.start

    def is_join_end(self) -> bool:
        return self.type == GcJoinType.join and self.time == GcJoinTime.end


# Mapping will only have an entry for non-empty values
JoinTimesForHeap = Mapping[GcJoinStage, Sequence[float]]


# Reads all events and outputs a pair for each stage's time.
# Note the same stage can occur multiple times.
def _process_events_for_heap(heap: AbstractServerGcHistory) -> Iterable[Tuple[GcJoinStage, float]]:
    joins = tuple(heap.GcJoins)
    i = 0

    def peek_join() -> Optional[JoinEvent]:
        if i == len(joins):
            return None
        else:
            j = joins[i]
            return JoinEvent(
                GcJoinType(j.Type),
                GcJoinTime(j.Time),
                GcJoinStage(j.JoinID),
                # NOTE: The 'Heap' property is actually the PRocessorNumber
                j.Heap,
                j.AbsoluteTimestampMsc,
            )

    def try_slurp_join() -> Optional[JoinEvent]:
        nonlocal i
        while True:
            if i == len(joins):
                return None
            join = non_null(peek_join())
            i += 1
            # if True:
            #    print(join)
            return join

    def try_slurp_join_skip_restarts() -> Optional[JoinEvent]:
        j = try_slurp_join()
        if j is None:
            return None
        elif j.is_restart_start():
            j2 = try_slurp_join()
            if j2 is None:
                return None
            elif j2.is_restart_end():
                return try_slurp_join()
            else:
                return j2
        else:
            return j

    def maybe_skip_restart_end() -> None:
        j = try_slurp_join()
        if j is not None and not j.is_restart_end():
            unslurp()

    def slurp_join() -> JoinEvent:
        return non_null(try_slurp_join())

    def unslurp() -> None:
        # if True:
        #    print("unslurp")
        nonlocal i
        i -= 1

    def get_join_end(j: JoinEvent, stage: GcJoinStage) -> float:
        assert j.is_join_end()
        assert j.stage == stage
        return j.time_msec

    def slurp_join_end(stage: GcJoinStage) -> float:
        return get_join_end(slurp_join(), stage)

    # Because the gc_join_generation_determined join is fired *before*
    # `do_pre_gc` which fires the gcstart etw event,
    # we possibly won't see the start of that join.
    # (And since we don't know whether this was started with Join or LastJoin,
    # we don't know whether there should be a JoinEnd.)
    def skip_initial() -> None:
        join = try_slurp_join()
        if join is None:
            pass
        elif join.is_restart_start():
            while True:
                j2 = slurp_join()
                if j2.is_join_end():
                    assert j2.stage == GcJoinStage.generation_determined
                elif not j2.is_restart_end():
                    unslurp()
                    break
        elif join.is_join_start():
            assert join.stage == GcJoinStage.generation_determined
            slurp_join_end(join.stage)
        else:
            assert join.stage == GcJoinStage.generation_determined and join.is_join_end()

    # Returns the start time
    def slurp_restart_start_msec() -> float:
        start = slurp_join()
        assert start.is_restart_start()
        return start.time_msec

    # Returns the end time
    def slurp_restart_start_end() -> float:
        slurp_restart_start_msec()
        return slurp_restart_end()

    def slurp_restart_end() -> float:
        end = slurp_join()
        assert end.is_restart_end()
        return end.time_msec

    skip_initial()

    def slurp_to_end_time(start_join: JoinEvent) -> Optional[float]:
        assert start_join.time == GcJoinTime.start

        if start_join.type == GcJoinType.last_join:
            # assert start_join.processor_number == heap.HeapId
            return slurp_restart_start_end()

        elif start_join.type == GcJoinType.join:
            # assert start_join.processor_number == heap.HeapId
            # note: apparently restart events Heap doesn't come from the raw heap, but from data.ProcessorNumber ... of the heap that did the restart, which may not be this one
            # Ignore restart time, that is handled by the one in last_join
            # slurp_restart_start_msec()
            # end_msec = slurp_join_end_and_restart_end(first_join.stage)
            end_join = try_slurp_join_skip_restarts()
            if end_join is None:
                # Similar to how we may get only the latter half of the gc_join_generation_determined,
                # we may get only the latter half of gc_join_done
                assert start_join.stage == GcJoinStage.done
                return None

            # We may see a restart that was from another heap. Just ignore that.
            # NOTE: since this is from another heap, it could come before or after our own end_join event.
            end = get_join_end(end_join, start_join.stage)
            maybe_skip_restart_end()
            return end

        elif start_join.type == GcJoinType.first_r_join:
            # assert start_join.processor_number == heap.HeapId
            # Right-join. The thread that does the work is the one that fires this event.
            # That one will not see a regular Join event, it will just see the restart events.
            # So no slurp_join_end here.
            return slurp_restart_start_end()

        elif start_join.type == GcJoinType.restart:
            # Restart was done by a different heap, skip
            raise Exception()  # Should be handled by caller

        else:
            raise Exception(start_join.type)

    while True:
        first_join = try_slurp_join()
        if first_join is None:
            break
        elif first_join.is_restart_start():
            # This must be a restart event from another heap -- otherwise we'd see last_join before this.
            # The restart end may be immediate or may come later (it's on another thread so we aren't synchronized)
            maybe_skip_restart_end()
        else:
            end_msec = slurp_to_end_time(first_join)
            if end_msec is None:
                break
            else:
                yield (first_join.stage, end_msec - first_join.time_msec)


def _get_join_times_for_all_heaps_worker(
    gc: AbstractTraceGC,
) -> Result[str, Sequence[JoinTimesForHeap]]:
    assert gc.HeapCount > 1  # Join durations only valid for server gc
    if is_empty(gc.ServerGcHeapHistories):
        return Err("empty ServerGcHeapHistories")
    else:
        return Ok(
            [
                make_multi_mapping(_process_events_for_heap(heap))
                for heap in gc.ServerGcHeapHistories
            ]
        )


def _get_join_times_for_all_heaps(gc: AbstractTraceGC) -> Result[str, Sequence[JoinTimesForHeap]]:
    return lazy_property(gc, _get_join_times_for_all_heaps_worker)


def _get_all_join_durations_for_all_heaps(gc: AbstractTraceGC) -> Result[str, Iterable[float]]:
    return map_ok(
        _get_join_times_for_all_heaps(gc),
        lambda all_times: (
            time
            for stage_to_times in all_times
            for times in stage_to_times.values()
            for time in times
        ),
    )


def _all_gc_join_ids_valid(gc: AbstractTraceGC) -> bool:
    for heap in gc.ServerGcHeapHistories:
        for join in heap.GcJoins:
            if join.JoinID == -256:
                return False
            else:
                GcJoinStage(join.JoinID)  # Should not fail
    return True


@with_slots
@dataclass(frozen=True)
class StartEnd:
    start_msec: float
    end_msec: float

    def __post_init__(self) -> None:
        assert self.start_msec <= self.end_msec

    @property
    def span_msec(self) -> float:
        return self.end_msec - self.start_msec


@with_slots
@dataclass(frozen=True)
class OptionalStartEnd:
    start_msec: Optional[float]
    end_msec: Optional[float]

    @property
    def span_msec(self) -> Optional[float]:
        if self.start_msec is None or self.end_msec is None:
            return None
        else:
            return self.end_msec - self.start_msec

    def to_tuple(self) -> Tuple[Optional[float], Optional[float]]:
        return self.start_msec, self.end_msec


@with_slots
@dataclass(frozen=False)
class MutOptionalStartEnd:
    # TODO: only one of abs or rel should actually be needed
    n_starts: int = 0
    n_ends: int = 0
    start_abs_msec: Optional[float] = None
    end_abs_msec: Optional[float] = None
    start_rel_msec: Optional[float] = None
    end_rel_msec: Optional[float] = None

    def finish(self, join_id: GcJoinStage, hp_num: int) -> OptionalStartEnd:
        if self.start_rel_msec is not None and self.end_rel_msec is not None:
            diff_abs = non_null(self.end_abs_msec) - non_null(self.start_abs_msec)
            diff_rel = non_null(self.end_rel_msec) - non_null(self.start_rel_msec)
            assert isclose(diff_abs, diff_rel)
        return OptionalStartEnd(self.start_abs_msec, self.end_abs_msec)


HeapId = int
TimeForStageByHeap = Mapping[GcJoinStage, Mapping[HeapId, OptionalStartEnd]]
MutTimeForStageByHeap = Dict[GcJoinStage, Dict[HeapId, MutOptionalStartEnd]]


def get_join_stage_start_end_times_for_heaps(gc: AbstractTraceGC) -> TimeForStageByHeap:
    return lazy_property(gc, get_join_stage_start_end_times_for_heaps_worker)


# For each stage, for each heap, get the first and last events in that stage.
def get_join_stage_start_end_times_for_heaps_worker(gc: AbstractTraceGC) -> TimeForStageByHeap:
    start_end_for_stage_by_heap: MutTimeForStageByHeap = {}

    def get_start_end(stage: GcJoinStage, heap: int) -> MutOptionalStartEnd:
        return start_end_for_stage_by_heap.setdefault(stage, {}).setdefault(
            heap, MutOptionalStartEnd()
        )

    all_heaps: Set[int] = set()

    for i, heap in enumerate(gc.ServerGcHeapHistories):
        assert heap.HeapId == i
        all_heaps.add(heap.HeapId)

        single_threaded_join_stage: Optional[GcJoinStage] = None
        for join in heap.GcJoins:
            stage = GcJoinStage(join.JoinID)
            time = GcJoinTime(join.Time)
            join_type = GcJoinType(join.Type)
            if time == GcJoinTime.start:
                if stage != GcJoinStage.restart:
                    start_end = get_start_end(stage, heap.HeapId)
                    start_end.n_starts += 1
                    start_end.start_rel_msec = join.RelativeTimestampMsc
                    start_end.start_abs_msec = join.AbsoluteTimestampMsc
                    if join_type == GcJoinType.last_join:
                        single_threaded_join_stage = GcJoinStage(stage)
            elif time == GcJoinTime.end:
                if stage == GcJoinStage.restart:
                    # assert False
                    if single_threaded_join_stage is not None:
                        # if True:
                        #    assert False  # TODO
                        start_end = start_end_for_stage_by_heap[single_threaded_join_stage][
                            heap.HeapId
                        ]
                        start_end.end_abs_msec = join.AbsoluteTimestampMsc
                        start_end.end_rel_msec = join.RelativeTimestampMsc
                        single_threaded_join_stage = None
                else:
                    # TODO: The time should only be there once, shouldn't it?
                    # Why are we joining the same stage multiple times?
                    # assert start_end_for_stage_by_heap[stage][heap.HeapId].end == 0
                    # start_end_for_stage_by_heap.setdefault(stage, {})
                    start_end = get_start_end(stage, heap.HeapId)
                    start_end.n_ends += 1
                    start_end.end_rel_msec = join.RelativeTimestampMsc
                    start_end.end_abs_msec = join.AbsoluteTimestampMsc
            else:
                raise Exception(time)

    print("ALLHEAPS", all_heaps)

    for stage, heap_to_start_end in start_end_for_stage_by_heap.items():
        hp_num = 3
        if hp_num in heap_to_start_end:
            start_end = heap_to_start_end[3]
            # print(
            #    f"{stage}: heap {hp_num} start_end is {start_end}, span is {start_end.finish(stage, hp_num).span_msec}"
            # )
        else:
            print(f"heap {hp_num} has no stage {stage}")

    def finish_stage(
        stage: GcJoinStage, heap_to_start_end: Mapping[HeapId, MutOptionalStartEnd]
    ) -> Tuple[GcJoinStage, Mapping[HeapId, OptionalStartEnd]]:

        # TODO: apparently one of the heaps may randomly be missing. Don't know why.
        # seen_heaps = sorted(stage.keys())
        # all_heaps = tuple(range(gc.HeapCount))
        # assert seen_heaps == all_heaps, f"Expected heaps {all_heaps}, only got {seen_heaps}"
        return (
            stage,
            map_mapping(lambda hp_num, se: (hp_num, se.finish(stage, hp_num)), heap_to_start_end),
        )

    return map_mapping(finish_stage, start_end_for_stage_by_heap)


@with_slots
@dataclass(frozen=True)
class StatsOverAllJoins:
    median_join_msec: float
    maximum_join_msec: float
    minimum_join_msec: float


@with_slots
@dataclass(frozen=True)
class AbsPct:
    absolute: float
    percentage: float


@with_slots
@dataclass(frozen=True)
class ForHeap:
    deviation_from_median_join_stage_duration: AbsPct


@with_slots
@dataclass(frozen=True)
class IndividualJoinStats:
    join_stage_name: str
    median_heap_join_msec: float
    minimum_heap_join_msec: float  # TODO: never used?
    maximum_heap_join_msec: float
    Heaps: Mapping[int, ForHeap]


@with_slots
@dataclass(frozen=True)
class StatsOverIndividualGcPhase:
    median_phase_join_msec: float
    max_phase_join_msec: float  # TODO: never used?
    min_phase_join_msec: float
    deviation_from_median_join_msec: AbsPct


@with_slots
@dataclass(frozen=True)
class GcJoinStatistics:
    statistics_over_all_joins: StatsOverAllJoins
    statistics_over_individual_joins: Mapping[GcJoinStage, IndividualJoinStats]
    statistics_over_individual_gc_phases: Mapping[GcJoinPhase, StatsOverIndividualGcPhase]


def _get_join_duration_by_heap(gc: AbstractTraceGC) -> Mapping[GcJoinStage, Mapping[HeapId, float]]:
    all_times = _get_join_times_for_all_heaps(gc).unwrap()  # TODO: handle err in unwrap
    res: Dict[GcJoinStage, Dict[HeapId, float]] = {}
    for hp_num, stage_to_times in enumerate(all_times):
        for stage, times in stage_to_times.items():
            # TODO: old gui assumed a given stage only happened once. That isn't true and this throws away data.
            res.setdefault(stage, {})[hp_num] = times[-1]
    return res


def _get_stats_for_join_phase(gc: AbstractTraceGC, join_stage: GcJoinStage) -> IndividualJoinStats:
    heap_to_join_duration = _get_join_duration_by_heap(gc)[join_stage]
    join_durations_for_stage = tuple(heap_to_join_duration.values())
    median_join_msec_for_stage = median(join_durations_for_stage)
    max_join_msec_for_stage = max(join_durations_for_stage)
    min_join_msec_for_stage = min(join_durations_for_stage)

    join_stage_name = GcJoinStage(join_stage).name
    return IndividualJoinStats(
        join_stage_name=join_stage_name,
        median_heap_join_msec=median_join_msec_for_stage,
        minimum_heap_join_msec=min_join_msec_for_stage,
        maximum_heap_join_msec=max_join_msec_for_stage,
        Heaps=_get_for_heaps(heap_to_join_duration),
    )


def _get_for_heap(median_join_msec_for_stage: float, heap_join_duration: float) -> ForHeap:
    absolute_deviation_from_median = abs(heap_join_duration - median_join_msec_for_stage)
    percent_deviation_from_median = (
        absolute_deviation_from_median / median_join_msec_for_stage * 100.0
    )
    return ForHeap(AbsPct(absolute_deviation_from_median, percent_deviation_from_median))


def _get_for_heaps(heap_to_join_duration: Mapping[int, float]) -> Mapping[int, ForHeap]:
    median_join_msec_for_stage = median(heap_to_join_duration.values())
    return {
        heap_number: _get_for_heap(median_join_msec_for_stage, heap_join_duration)
        for heap_number, heap_join_duration in heap_to_join_duration.items()
    }


def get_gc_join_duration_statistics(gc: AbstractTraceGC) -> Result[str, GcJoinStatistics]:
    assert _all_gc_join_ids_valid(gc)

    join_duration_by_heap = _get_join_duration_by_heap(gc)
    join_stage_to_individual_join_stats = {
        join_stage: _get_stats_for_join_phase(gc, join_stage)
        for join_stage in join_duration_by_heap
    }

    def f(all_join_durations_iter: Iterable[float]) -> GcJoinStatistics:
        all_join_durations = tuple(all_join_durations_iter)
        median_join_duration_all = median(all_join_durations)
        all_join_stats = StatsOverAllJoins(
            median_join_msec=median_join_duration_all,
            maximum_join_msec=max(all_join_durations),
            minimum_join_msec=min(all_join_durations),
        )
        join_duration_list_by_gc_phase = _get_join_duration_list_by_gc_phase(gc)
        return GcJoinStatistics(
            all_join_stats,
            join_stage_to_individual_join_stats,
            _get_stats_for_individual_phases(
                join_duration_list_by_gc_phase, median_join_duration_all
            ),
        )

    return map_ok(_get_all_join_durations_for_all_heaps(gc), f)


def _get_join_duration_list_by_gc_phase(
    gc: AbstractTraceGC,
) -> Mapping[GcJoinPhase, Sequence[float]]:
    return make_multi_mapping(
        (gc_phase_for_stage, join_duration)
        for join_stage, heap_to_join_duration in _get_join_duration_by_heap(gc).items()
        for gc_phase_for_stage in optional_to_iter(_try_get_join_phase(join_stage))
        for join_duration in heap_to_join_duration.values()
    )


def _get_stats_for_individual_phases(
    join_duration_list_by_gc_phase: Mapping[GcJoinPhase, Sequence[float]],
    all_joins_median_duration: float,
) -> Mapping[GcJoinPhase, StatsOverIndividualGcPhase]:
    join_stats_by_gc_phase: Dict[GcJoinPhase, StatsOverIndividualGcPhase] = {}
    for phase in GC_JOIN_STAGES_BY_GC_PHASE:
        join_durations_for_all_joins_in_phase = join_duration_list_by_gc_phase.get(phase)
        if join_durations_for_all_joins_in_phase is not None:
            assert not is_empty(join_durations_for_all_joins_in_phase)
            median_join_duration = median(join_durations_for_all_joins_in_phase)
            absolute_deviation_from_median = abs(median_join_duration - all_joins_median_duration)
            percent_deviation_from_median = (
                absolute_deviation_from_median / all_joins_median_duration * 100.0
            )

            s = StatsOverIndividualGcPhase(
                median_phase_join_msec=median_join_duration,
                max_phase_join_msec=max(join_durations_for_all_joins_in_phase),
                min_phase_join_msec=min(join_durations_for_all_joins_in_phase),
                deviation_from_median_join_msec=AbsPct(
                    absolute_deviation_from_median, percent_deviation_from_median
                ),
            )
            add(join_stats_by_gc_phase, phase, s)
    return join_stats_by_gc_phase


def get_gc_join_timeframes(
    clr: Clr, gc: AbstractTraceGC
) -> Tuple[bool, Mapping[str, StartEnd], Mapping[str, StartEnd], Mapping[int, StartEnd]]:
    phase_time_frames: Dict[GcJoinPhase, StartEnd] = {}
    join_stage_time_frames: Dict[GcJoinStage, StartEnd] = {}
    join_index_time_frames: Dict[int, StartEnd] = {}

    can_determine_join_stages = _all_gc_join_ids_valid(gc)

    if can_determine_join_stages:
        times = _get_gc_join_stage_timeframes(gc)

        # At this point, we now have the start/end times of each granular join stage. We want to
        # turn this detailed information into high-level start/stop times for different GC phases,
        # e.g. the mark phase or compact phase (if it occurs). From here, based on the join stage
        # sequence observed in each phase, we can come up with some heuristics to determine the
        # start of a phase, the end, or both.
        start_for_phase: Dict[GcJoinPhase, float] = defaultdict(lambda: inf)
        end_for_phase: Dict[GcJoinPhase, float] = defaultdict(float)
        for phase, stages_within_phase in GC_JOIN_STAGES_BY_GC_PHASE.items():
            # In general, we can always mark the end of a phase by enumerating all join stages in
            # that stage and tracking the latest join end fired for a stage within that phase.
            # Similarly, we can track the stage with the earliest join start fired within a phase
            # and declare that as the start of our phase as an approximation.
            for stage in stages_within_phase:
                if (
                    stage in times.start_for_stage
                    and times.start_for_stage[stage].time < start_for_phase[phase]
                ):
                    start_for_phase[phase] = times.start_for_stage[stage].time
                if (
                    stage in times.end_for_stage
                    and times.end_for_stage[stage].time > end_for_phase[phase]
                ):
                    end_for_phase[phase] = times.end_for_stage[stage].time

        # The above is a good generalization/first step, but there are a few exceptions we need to
        # make for certain GC phases:
        #   1. The join within the plan phase is actually at the end of the plan phase.
        #      The beginning of the plan phase should be defined as the end of the mark phase.
        #   2. The join within the sweep phase is also a marker for the end of the sweep phase. The
        #      beginning of the sweep phase should be defined as the end of the plan phase.
        #   3. The post-GC work join is for the start of post-gc work. The above loop will actually
        #      mark the end of post-GC work as the end of this join which is incorrect; the actual
        # end of this phase should be marked as the start of the EE restart.

        start_for_phase[GcJoinPhase.plan] = end_for_phase[GcJoinPhase.mark]
        if GcJoinPhase.sweep in start_for_phase:
            start_for_phase[GcJoinPhase.sweep] = end_for_phase[GcJoinPhase.plan]

        # I'll deal with the special case for post-GC work properly later.
        # For now I'll define the end of post-GC work as the end of the GC entirely
        # (which shouldn't be a horrible approximation anyways)
        end_for_phase[GcJoinPhase.post_gc] = gc.PauseDurationMSec

        for phase in start_for_phase:
            phase_time_frames[phase] = StartEnd(start_for_phase[phase], end_for_phase[phase])

        for stage in times.start_for_stage:
            if stage in times.end_for_stage:
                join_stage_time_frames[stage] = StartEnd(
                    times.start_for_stage[stage].time, times.end_for_stage[stage].time
                )
            else:
                join_stage_time_frames[stage] = StartEnd(times.start_for_stage[stage].time, inf)

        for stage in times.end_for_stage:
            if stage in times.start_for_stage:
                pass  # We already covered this case in the loop above
            else:
                join_stage_time_frames[stage] = StartEnd(-inf, times.end_for_stage[stage].time)
    else:
        join_index_start_times, join_index_end_times = _get_join_index_start_end_times_for_heaps(
            clr, gc
        )

        for join_index in join_index_start_times:
            try:
                min_join_start_time = min(join_index_start_times[join_index].values())
            except ValueError:
                min_join_start_time = -inf

            try:
                max_join_end_time = max(join_index_end_times[join_index].values())
            except ValueError:
                max_join_end_time = inf

            join_index_time_frames[join_index] = StartEnd(min_join_start_time, max_join_end_time)

    return (
        can_determine_join_stages,
        map_mapping_keys(lambda phase: phase.name, phase_time_frames),
        map_mapping_keys(lambda stage: stage.name, join_stage_time_frames),
        join_index_time_frames,
    )


@with_slots
@dataclass(frozen=True)
class TimeAndHeap:
    # For a start time, this has the time of the *first* heap's start, and that heap number.
    # For an end time, this has the time of the *last* heap's end, and that heap number
    time: float
    heap_num: int


@with_slots
@dataclass(frozen=True)
class StartEndTimesForStages:
    start_for_stage: Mapping[GcJoinStage, TimeAndHeap]
    end_for_stage: Mapping[GcJoinStage, TimeAndHeap]


def _get_gc_join_stage_timeframes(gc: AbstractTraceGC) -> StartEndTimesForStages:
    start_end_for_stage_by_heap = get_join_stage_start_end_times_for_heaps(gc)

    # Finding the start/end of GC join stages is effectively a matter of finding the first
    # join start fired and the last join end fired. Obviously there are some cases where this isn't
    # going to be 100% correct, e.g. when the restart end happens after all other threads call
    # join end and *especially* in cases if a thread gets switched out during single-threaded
    # mode. However, we would still expect someone to find this relatively easily in a timeline
    # view despite this error.
    start_for_stage = {}
    end_for_stage = {}
    for join_stage, heap_to_start_end_time in start_end_for_stage_by_heap.items():
        min_join_start_time = inf
        max_join_end_time = -inf
        for heap_id, join_stage_start_end_time_for_heap in heap_to_start_end_time.items():
            start, end = join_stage_start_end_time_for_heap.to_tuple()
            if start is not None and start < min_join_start_time:
                start_for_stage[join_stage] = TimeAndHeap(start, heap_id)
            if end is not None and end > max_join_end_time:
                end_for_stage[join_stage] = TimeAndHeap(end, heap_id)

    return StartEndTimesForStages(start_for_stage, end_for_stage)


# Maps join index -> heap id -> transition time.
# Note the first keys are just indices 0, 1, 2... not GcJoinStage
JoinIndexTimesForHeaps = Mapping[int, Mapping[int, float]]
MutJoinIndexTimesForHeaps = Dict[int, Dict[int, float]]


# TODO: end_by_index_and_heap is empty!!!
# Or rather, it's always just PauseDurationMSec.
# Try not using 'defaultdict', that hides errors
def _get_join_index_start_end_times_for_heaps(
    clr: Clr, gc: AbstractTraceGC
) -> Tuple[JoinIndexTimesForHeaps, JoinIndexTimesForHeaps]:
    # join index -> heap id -> transition time
    start_by_index_and_heap: MutJoinIndexTimesForHeaps = defaultdict(lambda: defaultdict(float))
    end_by_index_and_heap: MutJoinIndexTimesForHeaps = defaultdict(
        lambda: defaultdict(lambda: gc.PauseDurationMSec)
    )

    # We take advantage of get_gc_thread_state_timeline(...) here because it already provides
    # parsing of GC join events in a way that is (hopefully!) robust against different types
    # of joins, e.g. r_joins, single-threaded vs. standard join cases, and, to a lesser extent,
    # dropped events.
    #
    # Given a timeline of how a GC thread transitions between join states, we can build several
    # state machines to determine whether a GC thread is starting a new join or ending a
    # current join; the flow of these state machines and the exact markers of whether a thread
    # is entering or leaving a specific join depends on whether a thread is the last to join
    # or not.
    #
    # By far the most common join state pattern we can expect to see is in the case where a
    # thread is *not* the last thread to join (or the first in the case of an r_join).
    # In this situation, the general flow of states is as follows:
    #
    #                    new join
    # ... -------> Ready -------> Wait Join -------> Wait Restart -------+
    #                ^                                                   |
    #                |                                                   |
    #                +---------------------------------------------------+
    #                                end current join
    #
    # I call this state sequence "normal operation" since it is the most likely join state sequence
    # for an arbitrary server GC thread on machines with many cores.
    #
    # Note that when transitioning from Wait Restart to Ready, we consider the current join as
    # complete. (In fact, this transition only occurs when the thread has fired a "Join End" ETW
    # event.) A new join is signaled by a transition to Wait Join from Ready in the typical case
    # where a GC thread was executing and doing some sort of work outside of a join prior to calling
    # join.
    #
    # Similar to the "normal operation" case described above, we can define state machines for what
    # I call "slow operation," where a particular GC thread is the last thread to join for some
    # join (e.g. if the GC thread for heap 2 was the last thread to join for
    # gc_join_begin_mark_phase, then heap 2 is considered to be in a "slow operation" state
    # afterwards).

    def is_start_of_normal_join_from_normal_operation(cur_state: str, last_state: str) -> bool:
        return (
            cur_state == ServerGCThreadState.waiting_in_join.name
            and last_state == ServerGCThreadState.ready.name
        )

    def is_start_of_normal_join_from_slow_operation(cur_state: str, last_state: str) -> bool:
        return (
            cur_state == ServerGCThreadState.waiting_in_join.name
            and last_state == ServerGCThreadState.waiting_in_restart.name
        )

    def is_end_of_normal_join(cur_state: str, last_state: str) -> bool:
        return (
            cur_state == ServerGCThreadState.ready.name
            and last_state == ServerGCThreadState.waiting_in_restart.name
        )

    def is_start_of_last_to_join_from_normal_operation(cur_state: str, last_state: str) -> bool:
        return (
            cur_state == ServerGCThreadState.single_threaded.name
            and last_state == ServerGCThreadState.ready.name
        )

    def is_start_of_last_to_join_from_slow_operation(cur_state: str, last_state: str) -> bool:
        return (
            cur_state == ServerGCThreadState.single_threaded.name
            and last_state == ServerGCThreadState.waiting_in_restart.name
        )

    def is_end_of_last_to_join_from_normal_operation(cur_state: str, last_state: str) -> bool:
        return (
            cur_state == ServerGCThreadState.waiting_in_join.name
            and last_state == ServerGCThreadState.waiting_in_restart.name
        )

    def is_end_of_last_to_join_from_slow_operation(cur_state: str, last_state: str) -> bool:
        return (
            cur_state == ServerGCThreadState.single_threaded.name
            and last_state == ServerGCThreadState.waiting_in_restart.name
        )

    def is_start_of_join(cur_state: str, last_state: str) -> bool:
        return (
            is_start_of_normal_join_from_normal_operation(cur_state, last_state)
            or is_start_of_normal_join_from_slow_operation(cur_state, last_state)
            or is_start_of_last_to_join_from_normal_operation(cur_state, last_state)
            or is_start_of_last_to_join_from_slow_operation(cur_state, last_state)
        )

    # TODO: take ServerGCThreadState instead of str
    def is_end_of_join(cur_state: str, last_state: str) -> bool:
        return (
            is_end_of_normal_join(cur_state, last_state)
            or is_end_of_last_to_join_from_normal_operation(cur_state, last_state)
            or is_end_of_last_to_join_from_slow_operation(cur_state, last_state)
        )

    for heap in gc.ServerGcHeapHistories:
        current_join_index = 0

        heap_thread_states: ThreadStateTransitions = get_gc_thread_state_timeline(
            clr, gc, heap_id=heap.HeapId
        )[0]
        last_gc_thread_state = (
            ServerGCThreadState.unknown.name
        )  # TODO: use the state instead of the string!!!
        for thread_state_transition in heap_thread_states.thread_states:
            cur_gc_thread_state = thread_state_transition.state
            transition_time = thread_state_transition.time_of_transition

            # Note that I check whether a state transition marks the end of a join *before*
            # checking if it marks the start of another join. This order doesn't matter for cases
            # when threads aren't the last to join, but when threads are in single_threaded mode,
            # the end of one join marks the start of another. If we were to reverse the order of
            # checks in this case, we would update the start of the join for the join prior to
            # the one we actually want to mark.
            if is_end_of_join(cur_gc_thread_state, last_gc_thread_state):
                add(end_by_index_and_heap[current_join_index], heap.HeapId, transition_time)
                current_join_index += 1

            if is_start_of_join(cur_gc_thread_state, last_gc_thread_state):
                add(start_by_index_and_heap[current_join_index], heap.HeapId, transition_time)

            last_gc_thread_state = cur_gc_thread_state

    # TODO: were these loops meant to do something?
    # for index in start_by_index_and_heap:
    #    for hp in start_by_index_and_heap[index]:
    #        end_time = end_by_index_and_heap[index][hp]
    # for index in end_by_index_and_heap:
    #    for hp in end_by_index_and_heap[index]:
    #        start_time = start_by_index_and_heap[index][hp]

    return start_by_index_and_heap, end_by_index_and_heap


@with_slots
@dataclass(frozen=True)
class ThreadStateTransition:
    state: str
    time_of_transition: float


@with_slots
@dataclass(frozen=True)
class ThreadStateTransitions:
    heap: int
    thread_states: Sequence[ThreadStateTransition]


def get_gc_thread_state_timeline(
    clr: Clr, gc: AbstractTraceGC, heap_id: Optional[int] = None
) -> Sequence[ThreadStateTransitions]:
    heap_list: Iterable[AbstractServerGcHistory] = gc.ServerGcHeapHistories
    if heap_id is not None:
        heap_list = [heap for heap in heap_list if heap.HeapId == heap_id]

    def get_thread_state_transitions_for_heap(
        heap: AbstractServerGcHistory,
    ) -> ThreadStateTransitions:
        state_transitions = clr.Analysis.GetGcThreadStateTransitionTimes(heap)
        return ThreadStateTransitions(
            heap.HeapId,
            [
                ThreadStateTransition(ServerGCThreadState(tr.Item1).name, tr.Item2)
                for tr in state_transitions
            ],
        )

    return [get_thread_state_transitions_for_heap(heap) for heap in heap_list]
