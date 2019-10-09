# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from enum import Enum, IntEnum
from typing import Optional, Sequence

from ..commonlib.collection_util import empty_sequence
from ..commonlib.type_utils import OrderedEnum, with_slots

# These are based on enums from PerfView, must match.


def _has_flag(a: int, b: int) -> bool:
    return bool(a & b)


class Gens(Enum):
    Gen0 = 0
    Gen1 = 1
    Gen2 = 2
    GenLargeObj = 3


def gen_short_name(gen: Gens) -> str:
    return {Gens.Gen0: "0", Gens.Gen1: "1", Gens.Gen2: "2", Gens.GenLargeObj: "LOH"}[gen]


# See 'gc.h' in coreclr
# This is the same as GCReason from PerfView
class gc_reason(OrderedEnum):
    alloc_soh = 0
    induced = 1
    lowmemory = 2
    empty = 3
    alloc_loh = 4
    oos_soh = 5
    oos_loh = 6
    induced_noforce = 7
    gcstress = 8
    lowmemory_blocking = 9
    induced_compacting = 10
    lowmemory_host = 11
    pm_full_gc = 12
    lowmemory_host_blocking = 13


# See gc_heap_expand_mechanism in gcrecords.h
# NOTE: TraceEvent gives 1024 for not_specified. We will use Optional instead.
class gc_heap_expand_mechanism(OrderedEnum):
    expand_reuse_normal = 0
    expand_reuse_bestfit = 1
    expand_new_set_ep = 2  # new seg with ephemeral promotion
    expand_new_seg = 3
    expand_no_memory = 4  # we can't get a new seg
    expand_next_full_gc = 5


def try_get_gc_heap_expand_mechanism(i: int) -> Optional[gc_heap_expand_mechanism]:
    return None if i == 1024 else gc_heap_expand_mechanism(i)


# NOTE: TraceEvent gives 1024 for not_specified. We will use Optional instead.
class gc_heap_compact_reason(OrderedEnum):
    low_ephemeral = 0
    high_frag = 1
    no_gaps = 2
    loh_forced = 3
    last_gc = 4
    induced_compacting = 5
    fragmented_gen0 = 6
    high_mem_load = 7
    high_mem_frag = 8
    vhigh_mem_frag = 9
    no_gc_mode = 10


def try_get_gc_heap_compact_reason(i: int) -> Optional[gc_heap_compact_reason]:
    return None if i == 1024 else gc_heap_compact_reason(i)


@with_slots
@dataclass(frozen=True)
class StartupFlags:
    _f: int

    def _has(self, f: int) -> bool:
        return _has_flag(self._f, f)

    @property
    def using_concurrent(self) -> bool:
        return self._has(0x000001)

    @property
    def using_server(self) -> bool:
        return self._has(0x001000)


class GCGlobalMechanisms:
    def __init__(self, f: int):
        self.f = f

    def _has(self, f: int) -> bool:
        return _has_flag(self.f, f)

    @property
    def concurrent(self) -> bool:
        return self._has(1 << 0)

    @property
    def compaction(self) -> bool:
        return self._has(1 << 1)

    @property
    def promotion(self) -> bool:
        return self._has(1 << 2)

    @property
    def demotion(self) -> bool:
        return self._has(1 << 3)

    @property
    def cardbundles(self) -> bool:
        return self._has(1 << 4)

    @property
    def elevation(self) -> bool:
        return self._has(1 << 5)

    @property
    def loh_compaction(self) -> bool:
        # TODO: This flag needs to be implemented. It was causing errors when I tried adding it.
        return self._has(1 << 6)

    def __eq__(self, other: object) -> bool:
        assert isinstance(other, GCGlobalMechanisms)
        return self.f == other.f

    def names(self) -> Sequence[str]:
        def f(b: bool, s: str) -> Sequence[str]:
            return [s] if b else empty_sequence()

        return (
            *f(self.concurrent, "concurrent"),
            *f(self.compaction, "compaction"),
            *f(self.promotion, "promotion"),
            *f(self.demotion, "demotion"),
            *f(self.cardbundles, "cardbundles"),
            *f(self.elevation, "elevation"),
            *f(self.loh_compaction, "loh_compaction"),
        )

    def __str__(self) -> str:
        return " ".join(self.names())


EMPTY_GC_GLOBAL_MECHANISMS = GCGlobalMechanisms(0)

_MAX_GC_GLOBAL_MECHANISMS = 1 << 7


def invert_gc_global_mechanisms(a: GCGlobalMechanisms) -> GCGlobalMechanisms:
    return GCGlobalMechanisms((_MAX_GC_GLOBAL_MECHANISMS - 1) & ~a.f)


def union_gc_global_mechanisms(a: GCGlobalMechanisms, b: GCGlobalMechanisms) -> GCGlobalMechanisms:
    return GCGlobalMechanisms(a.f | b.f)


# Using OrderedEnum so we can sort
class GCType(OrderedEnum):
    NonConcurrentGC = 0
    BackgroundGC = 1
    ForegroundGC = 2


# See TraceManagedProcess.cs in PerfView
class HeapType(Enum):
    SOH = 0
    LOH = 1


# See ClrPrivateTraceEventParser.cs in PerfView
class BGCPhase(Enum):
    BGC1stNonConcurrent = 0
    BGC1stConcurrent = 1
    BGC2ndNonConcurrent = 2
    BGC2ndConcurrent = 3


# See TraceManagedProcess.cs in PerfView
class BGCRevisitState(Enum):
    Concurrent = 0
    NonConcurrent = 1


class ServerGCThreadState(IntEnum):
    unknown = 0
    ready = 1
    waiting_in_join = 2
    single_threaded = 3
    waiting_in_restart = 4


class GcJoinTime(Enum):
    start = 0
    end = 1


# See gc_join_stage in gc.cpp in coreclr. PerfView just exposes this as an int.
class GcJoinStage(Enum):
    restart = -1
    init_cpu_mapping = 0
    done = 1
    generation_determined = 2
    begin_mark_phase = 3
    scan_dependent_handles = 4
    rescan_dependent_handles = 5
    scan_sizedref_done = 6
    null_dead_short_weak = 7
    scan_finalization = 8
    null_dead_long_weak = 9
    null_dead_syncblk = 10
    decide_on_compaction = 11
    rearrange_segs_compaction = 12
    adjust_handle_age_compact = 13
    adjust_handle_age_sweep = 14
    begin_relocate_phase = 15
    relocate_phase_done = 16
    verify_objects_done = 17
    start_bgc = 18
    restart_ee = 19
    concurrent_overflow = 20
    suspend_ee = 21
    bgc_after_ephemeral = 22
    allow_fgc = 23
    bgc_sweep = 24
    suspend_ee_verify = 25
    restart_ee_verify = 26
    set_state_free = 27
    # This is the only r_join
    update_card_bundles = 28
    after_absorb = 29
    verify_copy_table = 30
    after_reset = 31
    after_ephemeral_sweep = 32
    after_profiler_heap_walk = 33
    minimal_gc = 34
    after_commit_soh_no_gc = 35
    expand_loh_no_gc = 36
    final_no_gc = 37
    disable_software_write_watch = 38


# See managed-lib/MoreAnalysis.cs
class ServerGCState(Enum):
    working = 0
    single_threaded = 1
    restarting = 2
    waiting_in_join = 3
    stolen = 4
    idle_for_no_good_reason = 5


# See `enum join_type` in gc.cpp in coreclr
class GcJoinType(Enum):
    last_join = 0
    join = 1
    restart = 2
    first_r_join = 3
    # r_join = 3 # Looked in gc.cpp, this is never fired


# From ClrTraceEventParser.cs in PerfView
class MarkRootType(OrderedEnum):
    MarkStack = 0
    MarkFQ = 1
    MarkHandles = 2
    MarkOlder = 3
    MarkSizedRef = 4
    MarkOverflow = 5


# See TraceManagedProcess.cs in PerfView
# Used by TraceGc.GetCondemnedReasons
class CondemnedReasonsGroup(Enum):
    # The first 4 will have values of a number which is the generation.
    # Note that right now these 4 have the exact same value as what's in
    # Condemned_Reason_Generation.
    Initial_Generation = 0
    Final_Generation = 1
    Alloc_Exceeded = 2
    Time_Tuning = 3

    # The following are either true(1) or false(0). They are not
    # a 1:1 mapping from
    # ... we'll never know what this comment was supposed to say ...
    Induced = 4
    Low_Ephemeral = 5
    Expand_Heap = 6
    Fragmented_Ephemeral = 7
    Fragmented_Gen1_To_Gen2 = 8
    Fragmented_Gen2 = 9
    Fragmented_Gen2_High_Mem = 10
    GC_Before_OOM = 11
    Too_Small_For_BGC = 12
    Ephemeral_Before_BGC = 13
    Internal_Tuning = 14
    Max = 15


# From MoreAnalysis.cs
class GcJoinPhase(Enum):
    init = 0
    mark = 1
    plan = 2
    relocate = 3
    compact = 4
    sweep = 5
    heap_verify = 6
    post_gc = 7


# System.DIagnostics.ThreadWaitReason
class ThreadWaitReason(Enum):
    Executive = 0
    FreePage = 1
    PageIn = 2
    SystemAllocation = 3
    ExecutionDelay = 4
    Suspended = 5
    UserRequest = 6
    EventPairHigh = 7
    EventPairLow = 8
    LpcReceive = 9
    LpcReply = 10
    VirtualMemory = 11
    PageOut = 12
    Unknown = 13
