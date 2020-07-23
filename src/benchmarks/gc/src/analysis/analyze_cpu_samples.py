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
from ..commonlib.type_utils import with_slots, doc_field


@with_slots
@dataclass(frozen=True)
class GCAndCPUSamples:
    gc_index: int
    inclusive_count: float
    exclusive_count: float
    inclusive_metric_percent: float
    exclusive_metric_percent: float
    first_time_msec: float
    last_time_msec: float


def _print_node(node: AbstractCallTreeNodeBase) -> None:
    print(f"Name: {node.Name}")
    print(f"Inclusive Metric %: {node.InclusiveMetricPercent}")
    print(f"Exclusive Metric %: {node.ExclusiveMetricPercent}")
    print(f"Inclusive Count: {node.InclusiveCount}")
    print(f"Exclusive Count: {node.ExclusiveCount}")
    print(f"First Time Relative MSec: {node.FirstTimeRelativeMSec}")
    print(f"Last Time Relative MSec: {node.LastTimeRelativeMSec}")


# Summary: Generates a list of TimeSpan objects, which contain the start and end
# times of each individual GC from the list provided as parameter.
#
# The TimeSpan class is part of GCPerf and is defined in managed-lib/Analysis.cs.
#
# Parameters:
#   gcs: List of ProcessedGC objects, which contain all the information of
#        each individual GC.
#
# Returns:
#    List with the time ranges of each GC. This list is later used in
#    _get_cpu_samples_from_trace() to fetch each GC's sample metrics.

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


# Summary: Filters the GC's (if a filter is specified) of the given trace,
#          and retrieves all the samples metrics of each one, for each of
#          the specified functions. The metrics of each GC in each function
#          create a set of "points", which is then appended to the 'all_data_to_chart'
#          list. Chart_cpu_samples_per_gcs() uses this list to draw the final chart.
#
# Parameters:
#   clr: Clr object which provides the capability to call GCPerf from here.
#   ptrace: ProcessedTrace object with all the trace's data.
#   symbol_reader: SymbolReader object used by GCPerf to lookup symbols.
#   functions: List with the names of the runtime functions to analyze.
#   all_data_to_chart: List where Trace objects with the points to chart
#       are appended. This class is defined in chart_utils.py.
#   gc_filter: Function used to filter the trace's GC's. This parameter is
#       optional and can be omitted to chart all the GC's samples.
#
# Returns: Nothing

def _get_cpu_samples_from_trace(
    clr: Clr,
    ptrace: ProcessedTrace,
    symbol_reader: AbstractSymbolReader,
    functions: Sequence[str],
    all_data_to_chart: List[Trace[GCAndCPUSamples]],
    gc_filter: Optional[Callable[[ProcessedGC], bool]] = None,
) -> None:
    trace_log = clr.Analysis.GetOpenedTraceLog(str(ptrace.test_result.trace_path))

    # Make sure we have the process name, otherwise GCPerf won't be able to
    # filter the stacks.

    assert ptrace.process_name is not None, (
        "Unknown error occurred. Was not able to get the process name ",
        "from test status yaml file."
    )
    process_name =  ptrace.process_name

    stack_source = clr.Analysis.GetProcessFullStackSource(
        trace_log,
        symbol_reader,
        process_name,
    )

    # Filter the GC's we want and obtain their time lapses.

    gcs_to_analyze = list(filter(gc_filter, ptrace.gcs))
    gcs_time_ranges = _get_gcs_time_ranges(gcs_to_analyze)

    # Read each GC's metrics for each of the given functions and create a new
    # GCAndCPUSamples object with this data and add it to a list of points.
    #
    # Finally, wrap this list in a Trace object and append it to the result list.
    # The reason it is done this way instead of returning the result, is because
    # when one wants to chart from more than one trace, chart_cpu_samples_per_gcs()
    # would end up with a list of lists, instead of a list of traces, which is
    # what the charting utilities require.

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
                    inclusive_count=node_metrics.InclusiveCount,
                    exclusive_count=node_metrics.ExclusiveCount,
                    inclusive_metric_percent=node_metrics.InclusiveMetricPercent,
                    exclusive_metric_percent=node_metrics.ExclusiveMetricPercent,
                    first_time_msec=node_metrics.FirstTimeRelativeMSec,
                    last_time_msec=node_metrics.LastTimeRelativeMSec,
                )
            )
        all_data_to_chart.append(Trace(name=f"{ptrace.name}---{func}", data=sample_points))


# Summary: Reads all the given traces and calls _get_cpu_samples_from_trace()
#          accordingly to get all the samples data to chart. Then, calls
#          chart_lines_from_fields() to display the chart.
#
# Parameters:
#   ptraces: List of ProcessedTrace objects for all traces to look at.
#   symbol_path: Path to the directory with the PDB's containing the symbols.
#   functions_to_chart: List with the names of the runtime functions to analyze.
#   x_property_name: Metric to chart on the X-Axis.
#   y_property_name: Metric(s) to chart on the Y-Axis.
#   gc_filter: Function used to filter the trace's GC's. This parameter is
#       optional and can be omitted to chart all the GC's samples.
#
# Returns: Nothing

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

    # Read each trace and get the samples data to chart.

    for ptrace in ptraces:
        _get_cpu_samples_from_trace(
            clr=clr,
            ptrace=ptrace,
            symbol_reader=symbol_reader,
            functions=functions_to_chart,
            all_data_to_chart=all_data_to_chart,
            gc_filter=gc_filter,
        )

    # Clean up resources. Also to free the usage on the symbols log file,
    # as future calls to CPU Samples Analysis functions would fail
    # otherwise, due to the resource being used.

    symbol_reader.Dispose()
    symbol_reader.Log.Close()

    # Display the chart.

    chart_lines_from_fields(
        t=GCAndCPUSamples,
        traces=all_data_to_chart,
        x_property_name=x_property_name,
        y_property_names=y_property_names,
    )


# Summary: Displays the main CPU samples metrics of the given function, within
#          the specified time range.
#
# Parameters:
#   ptrace: ProcessedTrace object with all the trace's data.
#   symbol_path: Path to the directory with the PDB's containing the symbols.
#   function: Runtime function to look at samples information.
#   start_time_msec: Timestamp in msec where we want to begin observations.
#       This parameter is optional and if omitted, analysis will start at
#       the beginning of the trace.
#   end_time_msec: Timestamp in msec where we want to end observations.
#       This parameter is optional and if omitted, analysis will finish at
#       the end of the trace.
#
# Returns: Nothing

def show_cpu_samples_metrics(
    ptrace: ProcessedTrace,
    symbol_path: Path,
    function: str,
    start_time_msec: float = 0.0,
    end_time_msec: float = 0.0,
) -> None:
    clr = get_clr()
    trace_log = clr.Analysis.GetOpenedTraceLog(str(ptrace.test_result.trace_path))
    symbol_reader = clr.Analysis.GetSymbolReader(
        "C:\\Git\\GCPerf-Symbols-Log.txt",
        str(symbol_path)
    )

    # Make sure we have the process name, otherwise GCPerf won't be able to
    # filter the stacks.

    assert ptrace.process_name is not None, (
        "Unknown error occurred. Was not able to get the process name ",
        "from test status yaml file."
    )
    process_name =  ptrace.process_name

    stack_source = clr.Analysis.GetProcessFullStackSource(
        trace_log,
        symbol_reader,
        process_name,
    )

    # If end time was not specified, search for the trace's length to set
    # this boundary.

    if (end_time_msec == 0.0):
        assert ptrace.TotalSecondsTaken is not None, (
            "No end timestamp found. Make sure to pass the time boundary for the",
            " range you wish to analyze, or specify the test's running time in",
            " the test result yaml file under the 'seconds_taken' field."
        )
        # Since the ProcessTrace object stores the test's duration in seconds,
        # multiply by 1000 to get the msec GCPerf requires.
        end_time_msec = float(ptrace.TotalSecondsTaken.value) * 1000.0

    # Make sure we have a positive time range to look at.

    assert start_time_msec < end_time_msec, (
        f"Check the timestamp values. Start time is {start_time_msec}, ",
        f"which is later than End time {end_time_msec}."
    )

    # Get the samples metrics data.

    node_metrics = clr.Analysis.GetFunctionMetricsWithinTimeRange(
        trace_log,
        symbol_reader,
        stack_source,
        clr.TimeSpanUtil.FromStartEndMSec(start_time_msec, end_time_msec),
        function,
    )

    # Clean up resources and print the metrics data. Also free the usage on
    # the symbols log file, as future calls to CPU Samples Analysis functions
    # would fail otherwise, due to the resource being used.

    symbol_reader.Dispose()
    symbol_reader.Log.Close()
    _print_node(node_metrics)
