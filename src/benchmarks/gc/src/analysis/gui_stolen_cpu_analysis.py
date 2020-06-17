# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

"""
This is OLD analysis code that is not as good as the analysis from managed-lib.
This exists just because I don't have time to update the gui code.
"""

from dataclasses import dataclass
from typing import Iterable, Mapping, Sequence

from ..commonlib.type_utils import with_slots

from .clr import Clr
from .clr_types import (
    AbstractServerGcHistory,
    AbstractThreadWorkSpan,
    AbstractTraceEvents,
    AbstractTraceGC,
)
from .enums import ServerGCThreadState


@with_slots
@dataclass(frozen=True)
class StolenAndTotal:
    stolen: float
    total: float


# TODO:NAME
@with_slots
@dataclass(frozen=True)
class StolenForHeap:
    heap_number: int
    # Maps ServerGCThreadState name to stolen time in that state
    stolen_cpu_breakdown: Mapping[str, StolenAndTotal]


@with_slots
@dataclass(frozen=True)
class StolenForGc:
    gc_number: int
    heaps: Sequence[StolenForHeap]


# TODO:NAME
@with_slots
@dataclass(frozen=True)
class StolenTimeForProc:
    pid: int
    process_name: str
    gc_occurrences: Sequence[StolenForGc]


def _get_stolen_time(clr: Clr, gc_index: int, gc: AbstractTraceGC) -> StolenForGc:
    def get_stolen_time_for_heap(heap: AbstractServerGcHistory) -> StolenForHeap:
        heap_stolen_time_breakdown = {
            ServerGCThreadState(kv.Key).name: StolenAndTotal(kv.Value.Item1, kv.Value.Item2)
            for kv in clr.Analysis.GetLostCpuBreakdownForHeap(gc, heap)
        }
        return StolenForHeap(heap.HeapId, heap_stolen_time_breakdown)

    return StolenForGc(
        gc_index, [get_stolen_time_for_heap(heap) for heap in gc.ServerGcHeapHistories]
    )


def get_stolen_cpu_times(clr: Clr, gcs: Iterable[AbstractTraceGC]) -> Sequence[StolenForGc]:
    return [_get_stolen_time(clr, index, gc) for index, gc in enumerate(gcs)]


def get_gc_stolen_cpu_times(
    clr: Clr, process_id: int, process_name: str, gcs: Sequence[AbstractTraceGC]
) -> StolenTimeForProc:
    return StolenTimeForProc(process_id, process_name, get_stolen_cpu_times(clr, gcs))


@with_slots
@dataclass(frozen=True)
class StolenTimeInstance:
    # Note: this is serialized to json, so must update gui if renaming any fields
    pid: int
    timestamp: float
    interrupting_thread_duration_ms: float
    process_name: str
    processor: int
    tid: int
    priority: int


@with_slots
@dataclass(frozen=True)
class StolenCpuInstancesForHeap:
    heap_number: int
    stolen_cpu_instances: Mapping[str, Sequence[StolenTimeInstance]]


def get_gc_stolen_cpu_instances(
    clr: Clr, gc: AbstractTraceGC, trace_events: AbstractTraceEvents
) -> Sequence[StolenCpuInstancesForHeap]:
    def get_stolen_time_instance(instance: AbstractThreadWorkSpan) -> StolenTimeInstance:
        if instance.DurationMsc > gc.DurationMSec:
            print("gc number", gc.Number)
            print("gc duration", gc.DurationMSec)
            print("stolen time instance duration", instance.DurationMsc)
            raise Exception("Huh? Thread stole more time than the GC took")

        return StolenTimeInstance(
            pid=instance.ProcessId,
            timestamp=instance.AbsoluteTimestampMsc - gc.PauseStartRelativeMSec,
            interrupting_thread_duration_ms=instance.DurationMsc,
            process_name=instance.ProcessName,
            processor=instance.ProcessorNumber,
            tid=instance.ThreadId,
            priority=instance.Priority,
        )

    def get_stolen_cpu_instances_for_heap(
        heap: AbstractServerGcHistory,
    ) -> StolenCpuInstancesForHeap:
        lost_cpu = clr.Analysis.GetLostCpuInstancesForHeap(gc, heap, trace_events)
        heap_stolen_time_instances_per_phase = {
            ServerGCThreadState(kv.Key).name: [get_stolen_time_instance(i) for i in kv.Value]
            for kv in lost_cpu
        }
        return StolenCpuInstancesForHeap(heap.HeapId, heap_stolen_time_instances_per_phase)

    return [get_stolen_cpu_instances_for_heap(heap) for heap in gc.ServerGcHeapHistories]
