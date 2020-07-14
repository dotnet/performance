# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from pathlib import Path

from ..commonlib.command import Command, CommandKind, CommandsMapping

from .clr import Clr
from .clr_types import (
    AbstractCallTreeNodeBase,
    AbstractStackView,
)


def cpu_samples_draft(
    clr: Clr,
    trace_path: Path,
    symbol_path: Path,
    node_name: str,
) -> None:
    stack_view = clr.Analysis.GetStackViewForInfra(str(trace_path), str(symbol_path), "CoreRun")
    node = stack_view.FindNodeByName(node_name)
    # print(node.ToString())
    print(f"Name: {node.Name}")
    print(f"Inclusive Metric %: {node.InclusiveMetricPercent}")
    print(f"Exclusive Metric %: {node.ExclusiveMetricPercent}")
    print(f"Inclusive Count: {node.InclusiveCount}")
    print(f"Exclusive Count: {node.ExclusiveCount}")
    print(f"First Time Relative MSec: {node.FirstTimeRelativeMSec}")
    print(f"Last Time Relative MSec: {node.LastTimeRelativeMSec}")


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
