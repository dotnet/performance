# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from pathlib import Path
from typing import Sequence, Callable, List, Optional

from .chart_utils import chart_lines_from_fields, Trace
from .types import ProcessedTrace, ProcessedGC

from .clr import get_clr, Clr
from .clr_types import (
    AbstractCallTreeNodeBase,
    AbstractTimeSpan, AbstractSymbolReader)
from ..commonlib.type_utils import with_slots


@with_slots
@dataclass(frozen=True)
class GCAndCPUSamples:
    gc_index: int
    inclusive_metric_percent: float
    exclusive_metric_percent: float
    inclusive_count: float
    exclusive_count: float
    start_msec: float
    end_msec: float


def _print_node(node: AbstractCallTreeNodeBase) -> None:
    # print(node.ToString())
    print(f"Name: {node.Name}")
    print(f"Inclusive Metric %: {node.InclusiveMetricPercent}")
    print(f"Exclusive Metric %: {node.ExclusiveMetricPercent}")
    print(f"Inclusive Count: {node.InclusiveCount}")
    print(f"Exclusive Count: {node.ExclusiveCount}")
    print(f"First Time Relative MSec: {node.FirstTimeRelativeMSec}")
    print(f"Last Time Relative MSec: {node.LastTimeRelativeMSec}")


def _get_gcs_time_ranges(
        gcs: Sequence[ProcessedGC]
) -> List[AbstractTimeSpan]:
    time_ranges = []
    clr = get_clr()
    for gc in gcs:
        start = gc.StartRelativeMSec
        end = gc.EndRelativeMSec
        time_ranges.append(clr.TimeSpanUtil.FromStartEndMSec(start, end))
    return time_ranges


def _get_cpu_samples_from_trace(
    clr: Clr,
    ptrace: ProcessedTrace,
    symbol_reader: AbstractSymbolReader,
    functions: Sequence[str],
    all_data_to_chart: List[Trace[GCAndCPUSamples]],
    gc_filter: Optional[Callable[[ProcessedGC], bool]] = None,
) -> None:
    trace_log = clr.Analysis.GetOpenedTraceLog(str(ptrace.test_result.trace_path))
    stack_source = clr.Analysis.GetProcessFullStackSource(
        trace_log,
        symbol_reader,
        ptrace.process_info.process.Name
    )

    gcs_to_analyze = list(filter(gc_filter, ptrace.gcs))
    gcs_time_ranges = _get_gcs_time_ranges(gcs_to_analyze)

    for func in functions:
        sample_points = []

        for gc, range in zip(gcs_to_analyze, gcs_time_ranges):
            node_metrics = clr.Analysis.GetFunctionMetricsWithinTimeRange(
                trace_log,
                symbol_reader,
                stack_source,
                range,
                func,
            )
            sample_points.append(
                GCAndCPUSamples(
                    gc_index=gc.index,
                    inclusive_metric_percent=node_metrics.InclusiveMetricPercent,
                    exclusive_metric_percent=node_metrics.ExclusiveMetricPercent,
                    inclusive_count=node_metrics.InclusiveCount,
                    exclusive_count=node_metrics.ExclusiveCount,
                    start_msec=node_metrics.FirstTimeRelativeMSec,
                    end_msec=node_metrics.LastTimeRelativeMSec,
                )
            )
        all_data_to_chart.append(Trace(name=f"{ptrace.name}---{func}", data=sample_points))


def chart_cpu_samples_per_gcs(
    ptraces: Sequence[ProcessedTrace],
    symbol_path: Path,
    functions_to_chart: Sequence[str],
    x_property_name: str,
    y_property_names: Sequence[str],
    gc_filter: Optional[Callable[[ProcessedGC], bool]] = None,
) -> None:
    clr = get_clr()
    symbol_reader = clr.Analysis.GetSymbolReader(
        "C:\\Git\\GCPerf-Symbols-Log.txt",
        str(symbol_path)
    )
    all_data_to_chart: List[Trace[GCAndCPUSamples]] = []

    for ptrace in ptraces:
        _get_cpu_samples_from_trace(
            clr=clr,
            ptrace=ptrace,
            symbol_reader=symbol_reader,
            functions=functions_to_chart,
            all_data_to_chart=all_data_to_chart,
            gc_filter=gc_filter,
        )

    chart_lines_from_fields(
        t=GCAndCPUSamples,
        traces=all_data_to_chart,
        x_property_name=x_property_name,
        y_property_names=y_property_names,
    )
