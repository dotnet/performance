# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from ..commonlib.collection_util import is_empty, unzip

from .clr_types import AbstractTraceGC
from .enums import (
    GCGlobalMechanisms,
    gc_reason,
    GCType,
    try_get_gc_heap_compact_reason,
    try_get_gc_heap_expand_mechanism,
)
from .types import (
    EMPTY_MECHANISMS_AND_REASONS,
    MechanismsAndReasons,
    ProcessInfo,
    union_all_mechanisms,
)


def get_mechanisms_and_reasons_for_process_info(proc: ProcessInfo) -> MechanismsAndReasons:
    res = union_all_mechanisms(
        _get_seen_mechanisms_and_reasons_for_single_gc(gc) for gc in proc.gcs
    )
    assert res.is_empty() == is_empty(proc.gcs)
    return res


def _get_seen_mechanisms_and_reasons_for_single_gc(gc: AbstractTraceGC) -> MechanismsAndReasons:
    ghh = gc.GlobalHeapHistory
    if ghh is None:
        return EMPTY_MECHANISMS_AND_REASONS
    else:
        expand, compact = unzip(
            (
                try_get_gc_heap_expand_mechanism(phh.ExpandMechanisms),
                try_get_gc_heap_compact_reason(phh.CompactMechanisms),
            )
            for phh in gc.PerHeapHistories
        )
        return MechanismsAndReasons(
            types=frozenset((GCType(gc.Type),)),
            mechanisms=GCGlobalMechanisms(ghh.GlobalMechanisms),
            reasons=frozenset((gc_reason(gc.Reason),)),
            # TODO: these aren't available on the individual GC.
            # See comment in 'GetTracedProcesses' in managed-lib/Analysis.cs
            heap_expand=frozenset(x for x in expand if x is not None),
            heap_compact=frozenset(x for x in compact if x is not None),
        )
