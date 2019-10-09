# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from enum import Enum
from pathlib import Path
from typing import FrozenSet, Mapping, Optional, Sequence

from ..commonlib.collection_util import indices, is_empty, map_to_mapping_optional
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
    single_table_document,
    Table,
)
from ..commonlib.type_utils import argument, enum_count, enum_value, with_slots

from .clr import get_clr
from .clr_types import AbstractGCCondemnedReasons
from .core_analysis import GC_NUMBER_DOC, PROCESS_DOC, SINGLE_PATH_DOC
from .enums import Gens, gen_short_name
from .process_trace import get_processed_trace, test_result_from_path
from .single_gc_metrics import get_gc_with_number
from .types import ProcessedGC, ProcessedTrace, ProcessQuery

# From TraceManagedProcess.cs in PerfView
class _CondemnedReasonGroup(Enum):
    # The first 4 will have values of a number which is the generation.
    # Note that right now these 4 have the exact same value as what's in
    # Condemned_Reason_Generation.
    initial_generation = 0
    final_generation = 1
    alloc_exceeded = 2
    time_tuning = 3

    # The following are either true(1) or false(0). They are not
    # a 1:1 mapping from
    induced = 4
    low_ephemeral = 5
    expand_heap = 6
    fragmented_ephemeral = 7
    fragmented_gen1_to_gen2 = 8
    fragmented_gen2 = 9
    fragmented_gen2_high_mem = 10
    gc_before_oom = 11
    too_small_for_bgc = 12
    ephemeral_before_bgc = 13
    internal_tuning = 14


# An enum I wrote. These are the elements in CondemnedReasonGroup that are Gens
class GenCondemnedReason(Enum):
    initial_generation = 0
    final_generation = 1
    alloc_exceeded = 2
    time_tuning = 3


# An enum I wrote. These are the elements in CondemendReasonGroup that are bools
class BoolCondemnedReason(Enum):
    induced = 0
    low_ephemeral = 1
    expand_heap = 2
    fragmented_ephemeral = 3
    fragmented_gen1_to_gen2 = 4
    fragmented_gen2 = 5
    fragmented_gen2_high_mem = 6
    gc_before_oom = 7
    too_small_for_bgc = 8
    ephemeral_before_bgc = 9
    internal_tuning = 10


def _condemned_reason_group_of_gen_condemned_reason(g: GenCondemnedReason) -> _CondemnedReasonGroup:
    res = _CondemnedReasonGroup(enum_value(g))
    assert res.name == g.name
    return res


def _condemned_reason_group_of_bool_condemned_reason(
    b: BoolCondemnedReason
) -> _CondemnedReasonGroup:
    res = _CondemnedReasonGroup(enum_value(b) + enum_count(GenCondemnedReason))
    assert res.name == b.name
    return res


@with_slots
@dataclass(frozen=True)
class CondemnedReasonsForHeap:
    # Only has non-Gen0 entries
    gen_reasons: Mapping[GenCondemnedReason, Gens]
    bool_reasons: FrozenSet[BoolCondemnedReason]

    def is_empty(self) -> bool:
        return is_empty(self.gen_reasons) and is_empty(self.bool_reasons)

    def get_gen(self) -> Gens:
        return self.gen_reasons.get(GenCondemnedReason.final_generation, Gens.Gen0)

    @property
    def initial_generation(self) -> Optional[Gens]:
        return self.gen_reasons.get(GenCondemnedReason.initial_generation)

    @property
    def final_generation(self) -> Optional[Gens]:
        return self.gen_reasons[GenCondemnedReason.final_generation]

    @property
    def alloc_exceeded(self) -> Optional[Gens]:
        return self.gen_reasons[GenCondemnedReason.alloc_exceeded]

    @property
    def time_tuning(self) -> Optional[Gens]:
        return self.gen_reasons[GenCondemnedReason.time_tuning]

    def has_reason(self, b: BoolCondemnedReason) -> bool:
        return b in self.bool_reasons

    @property
    def induced(self) -> bool:
        return self.has_reason(BoolCondemnedReason.induced)

    @property
    def low_ephemeral(self) -> bool:
        return self.has_reason(BoolCondemnedReason.low_ephemeral)

    @property
    def expand_heap(self) -> bool:
        return self.has_reason(BoolCondemnedReason.expand_heap)

    @property
    def fragmented_ephemeral(self) -> bool:
        return self.has_reason(BoolCondemnedReason.fragmented_ephemeral)

    @property
    def fragmented_gen1_to_gen2(self) -> bool:
        return self.has_reason(BoolCondemnedReason.fragmented_gen1_to_gen2)

    @property
    def fragmented_gen2(self) -> bool:
        return self.has_reason(BoolCondemnedReason.fragmented_gen2)

    @property
    def fragmented_gen2_high_mem(self) -> bool:
        return self.has_reason(BoolCondemnedReason.fragmented_gen2_high_mem)

    @property
    def gc_before_oom(self) -> bool:
        return self.has_reason(BoolCondemnedReason.gc_before_oom)

    @property
    def too_small_for_bgc(self) -> bool:
        return self.has_reason(BoolCondemnedReason.too_small_for_bgc)

    @property
    def ephemeral_before_bgc(self) -> bool:
        return self.has_reason(BoolCondemnedReason.ephemeral_before_bgc)

    @property
    def internal_tuning(self) -> bool:
        return self.has_reason(BoolCondemnedReason.internal_tuning)


@with_slots
@dataclass(frozen=True)
class HeapAndReasons:
    heap: int
    reasons: CondemnedReasonsForHeap

    def __post_init__(self) -> None:
        assert not self.reasons.is_empty()


@with_slots
@dataclass(frozen=True)
class CondemnedReasonsForGC:
    # Find the heaps that caused this.
    # E.g., heap 3 has foo gen_reasons.
    reasons: Sequence[HeapAndReasons]

    # May be empty if the GC was gen0
    def is_empty(self) -> bool:
        return is_empty(self.reasons)


def get_condemned_reasons_for_each_heap(gc: ProcessedGC) -> Sequence[CondemnedReasonsForHeap]:
    return [
        _get_condemned_reasons_for_heap(reasons_for_heap)
        for reasons_for_heap in gc.trace_gc.PerHeapCondemnedReasons
    ]


def get_condemned_reasons_for_gc(gc: ProcessedGC) -> CondemnedReasonsForGC:
    res = CondemnedReasonsForGC(
        [
            HeapAndReasons(i, hp)
            for i, hp in enumerate(get_condemned_reasons_for_each_heap(gc))
            if hp.get_gen() == gc.Generation
        ]
    )
    if res.is_empty():
        assert gc.IsGen0
    return res


def _get_condemned_reasons_for_heap(reasons: AbstractGCCondemnedReasons) -> CondemnedReasonsForHeap:
    # x = reasons_for_heap.EncodedReasons
    # x.Reasons # int -- what enum does this correspond to?
    # x.ReasonsEx # more reasons

    def get_for_generation(gen_reason: GenCondemnedReason) -> Optional[Gens]:
        idx = enum_value(_condemned_reason_group_of_gen_condemned_reason(gen_reason))
        res = Gens(reasons.CondemnedReasonGroups[idx])
        return None if res == Gens.Gen0 else res

    def get_for_bool(bool_reason: BoolCondemnedReason) -> bool:
        idx = enum_value(_condemned_reason_group_of_bool_condemned_reason(bool_reason))
        return {0: False, 1: True}[reasons.CondemnedReasonGroups[idx]]

    gen_reasons = map_to_mapping_optional(GenCondemnedReason, get_for_generation)
    bool_reasons = frozenset(
        bool_reason for bool_reason in BoolCondemnedReason if get_for_bool(bool_reason)
    )

    return CondemnedReasonsForHeap(gen_reasons=gen_reasons, bool_reasons=bool_reasons)


@with_slots
@dataclass(frozen=True)
class _ShowCondemnedReasonsArgs:
    path: Path = argument(name_optional=True, doc=SINGLE_PATH_DOC)
    process: ProcessQuery = argument(default=None, doc=PROCESS_DOC)
    max_gcs: int = argument(default=32, doc="Maximum number of GCs to show.")


def _show_condemned_reasons(args: _ShowCondemnedReasonsArgs) -> None:
    trace = get_processed_trace(
        clr=get_clr(),
        test_result=test_result_from_path(args.path),
        process=args.process,
        need_mechanisms_and_reasons=False,
        need_join_info=False,
    ).unwrap()
    handle_doc(show_condemned_reasons_for_jupyter(trace, args.max_gcs), OutputOptions())


@with_slots
@dataclass(frozen=True)
class _ShowCondemnedReasonsForGCArgs:
    path: Path = argument(name_optional=True, doc=SINGLE_PATH_DOC)
    gc_number: int = argument(doc=GC_NUMBER_DOC)
    process: ProcessQuery = argument(default=None, doc=PROCESS_DOC)
    output_width: Optional[OutputWidth] = argument(default=None, doc=OUTPUT_WIDTH_DOC)


def _show_condemned_reasons_for_gc(args: _ShowCondemnedReasonsForGCArgs) -> None:
    trace = get_processed_trace(
        clr=get_clr(),
        test_result=test_result_from_path(args.path),
        process=args.process,
        need_mechanisms_and_reasons=False,
        need_join_info=False,
    ).unwrap()
    handle_doc(
        show_condemned_reasons_for_gc_for_jupyter(trace, args.gc_number),
        OutputOptions(width=args.output_width),
    )


def show_brief_condemned_reasons_for_gc(gc: ProcessedGC) -> str:
    reasons = get_condemned_reasons_for_gc(gc)
    return "\n".join(f"heap {r.heap}: {_show_brief_reasons(r.reasons)}" for r in reasons.reasons)


def _show_brief_reasons(reasons: CondemnedReasonsForHeap) -> str:
    return ", ".join(
        (
            *(f"{k.name} of {v}" for k, v in reasons.gen_reasons.items()),
            *(r.name for r in reasons.bool_reasons),
        )
    )


def show_condemned_reasons_for_jupyter(trace: ProcessedTrace, max_gcs: int) -> Document:
    gcs = trace.gcs[:max_gcs]
    table = Table(
        headers=("gc number", "condemned reasons"),
        rows=[(Cell(gc.Number), Cell(show_brief_condemned_reasons_for_gc(gc))) for gc in gcs],
    )
    return single_table_document(table)


def show_condemned_reasons_for_gc_for_jupyter(trace: ProcessedTrace, gc_number: int) -> Document:
    gc = get_gc_with_number(trace.gcs, gc_number)
    reasons_for_each_heap = get_condemned_reasons_for_each_heap(gc)
    # Make a table -- columns are heaps, rows are condemned reasons

    gen_reason_rows: Sequence[Row] = [
        (
            Cell(gen_reason.name),
            *(_cell_of_gen(r.gen_reasons.get(gen_reason)) for r in reasons_for_each_heap),
        )
        for gen_reason in GenCondemnedReason
    ]

    bool_reason_rows: Sequence[Row] = [
        (
            Cell(bool_reason.name),
            *(_cell_of_bool(r.has_reason(bool_reason)) for r in reasons_for_each_heap),
        )
        for bool_reason in BoolCondemnedReason
    ]

    table = Table(
        name="Details: (Note: column titles are heap numbers)",
        headers=("name", *(str(i) for i in indices(reasons_for_each_heap))),
        rows=[*gen_reason_rows, *bool_reason_rows],
    )
    section = Section(text=show_brief_condemned_reasons_for_gc(gc), tables=(table,))
    return Document(sections=(section,))


def _cell_of_gen(gen: Optional[Gens]) -> Cell:
    return Cell() if gen is None else Cell(gen_short_name(gen))


def _cell_of_bool(b: bool) -> Cell:
    return Cell("✓") if b else Cell()


CONDEMNED_REASONS_COMMANDS: CommandsMapping = {
    "show-condemned-reasons": Command(
        kind=CommandKind.analysis,
        fn=_show_condemned_reasons,
        doc="Show a brief summary of condemned reasons for each GC.",
    ),
    "show-condemned-reasons-for-gc": Command(
        kind=CommandKind.analysis,
        fn=_show_condemned_reasons_for_gc,
        doc="Show why each heap was condemned in a single GC.",
    ),
}
