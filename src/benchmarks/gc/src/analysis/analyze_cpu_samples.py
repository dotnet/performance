# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from pathlib import Path
from typing import Sequence, Callable

from .enums import Gens
from .process_trace import get_processed_trace, test_result_from_path
from .types import ProcessedTrace, ProcessedGC
from ..commonlib.command import Command, CommandKind, CommandsMapping

from .clr import Clr
from .clr_types import (
    AbstractCallTreeNodeBase,
    AbstractStackView,
)


def _filtering_the_gcs(
        gcs: Sequence[ProcessedGC],
        gc_filter: Callable[[ProcessedGC], bool]
) -> None:
    data = filter(gc_filter, gcs)
    for gc in data:
        index = gc.index
        gen = gc.Generation
        start = gc.StartRelativeMSec
        end = gc.EndRelativeMSec
        total = gc.EndRelativeMSec - gc.StartRelativeMSec
        print(f"GC {index}, {gen}, {end} - {start} = {total}")
    # return


def _print_node(node: AbstractCallTreeNodeBase) -> None:
    # print(node.ToString())
    print(f"Name: {node.Name}")
    print(f"Inclusive Metric %: {node.InclusiveMetricPercent}")
    print(f"Exclusive Metric %: {node.ExclusiveMetricPercent}")
    print(f"Inclusive Count: {node.InclusiveCount}")
    print(f"Exclusive Count: {node.ExclusiveCount}")
    print(f"First Time Relative MSec: {node.FirstTimeRelativeMSec}")
    print(f"Last Time Relative MSec: {node.LastTimeRelativeMSec}")


def cpu_samples_draft(
    ptrace: ProcessedTrace,
    symbol_path: Path,
    node_name: str,
    gc_filter: Callable[[ProcessedGC], bool] = None,
) -> None:
    clr = ptrace.clr
    stack_view = clr.Analysis.GetStackViewForInfra(
        str(ptrace.test_result.trace_path),
        str(symbol_path),
        "CoreRun",
    )

    # _filtering_the_gcs(ptrace.gcs, gc_filter)
    node = stack_view.FindNodeByName(node_name)
    _print_node(node)


def analyze_cpu_samples() -> None:
    print("Under construction!")


ANALYZE_CPU_SAMPLES_COMMANDS: CommandsMapping = {
    "analyze-cpu-samples": Command(
        kind=CommandKind.analysis,
        fn=analyze_cpu_samples,
        doc="""
    Given a single trace, print cpu samples for a set of GC's.
    """
    ),
}
