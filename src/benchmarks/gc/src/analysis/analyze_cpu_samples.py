# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from os import getcwd
from pathlib import Path
from typing import Sequence, Callable, List, Optional

from .chart_utils import chart_lines_from_fields, Trace
from .types import ProcessedTrace, ProcessedGC

from .clr import get_clr, Clr
from .clr_types import (
    AbstractCallTreeNodeBase,
    AbstractTimeSpan,
    AbstractSymbolReader,
    AbstractTraceLog,
    AbstractStackSource,
    AbstractStackView)
from ..commonlib.type_utils import with_slots, doc_field


@doc_field(
    "trace_info", "ProcessedTrace object with all the trace's data. Definition is in types.py."
)
@doc_field(
    "trace_log",
    "TraceLog object associated with the trace. This is part of TraceEvent, and "
    "contains all the streams of events in the trace and is used by GCPerf.",
)
@doc_field(
    "symbol_reader",
    "SymbolReader object in charge of finding and applying the PDB's to resolve "
    "the trace's symbols. This is part of TraceEvent.",
)
@doc_field(
    "stack_source",
    "StackSource object that contains a list of all the samples captured in the "
    "trace. This is part of TraceEvent.",
)
@with_slots
@dataclass(frozen=False)
class TraceReadAndParseUtils:
    trace_info: ProcessedTrace
    trace_log: AbstractTraceLog
    symbol_reader: AbstractSymbolReader
    stack_source: AbstractStackSource
    gcs_stack_views: List[AbstractStackView]

    def __init__(self, ptrace: ProcessedTrace, symbol_path: Path, init_gcs_samples: bool = True):
        clr = get_clr()
        workdir = Path(getcwd())

        # Make sure we have the process name, otherwise GCPerf won't be able to
        # filter the stacks.

        assert ptrace.process_name is not None, (
            "Unknown error occurred. Was not able to get the process name ",
            "from test status yaml file.",
        )
        process_to_analyze = ptrace.process_name

        # Create and fetch the TraceLog, SymbolReader, and StackSource objects
        # associated with this trace.

        self.trace_info = ptrace
        self.trace_log = clr.Analysis.GetOpenedTraceLog(str(ptrace.test_result.trace_path))
        self.symbol_reader = clr.Analysis.GetSymbolReader(
            str(workdir / "GCPerf-Symbols-Log.txt"), str(symbol_path)
        )
        self.stack_source = clr.Analysis.GetProcessFullStackSource(
            self.trace_log, self.symbol_reader, process_to_analyze
        )

        self.gcs_stack_views = []
        if (init_gcs_samples):
            self.init_gcs_samples_data()

    # Clean up resources and free the usage on the symbols log file, as future
    # calls to CPU Samples Analysis functions with different traces would fail
    # otherwise, due to the resource being used.
    def __del__(self) -> None:
        self.symbol_reader.Dispose()
        self.symbol_reader.Log.Close()

    # Since the ProcessTrace object stores the test's duration in seconds,
    # multiply by 1000 to get the msec GCPerf requires.
    @property
    def trace_duration_msec(self) -> float:
        return float(self.trace_info.TotalSecondsTaken.value) * 1000.0

    # Property for easy access to the trace's processed GC's.
    @property
    def trace_processed_gcs(self) -> Sequence[ProcessedGC]:
        return self.trace_info.gcs

    # Property for easy access to the trace's test name.
    @property
    def trace_name(self) -> str:
        return self.trace_info.name

    # Retrieve the corresponding StackView object with all the samples data
    # for each GC in the trace. This collection is later accessed by the
    # charting functions to read the metrics values and create the points.
    def init_gcs_samples_data(self) -> None:
        clr = get_clr()

        # Ensure GC sample data has not been initialized.

        assert not self.gcs_stack_views, (
            "This trace already has the GC Samples data. Can't initialize "
            "it again."
        )

        gcs_time_ranges = _get_gcs_time_ranges(self.trace_processed_gcs)

        # Call GCPerf to get the samples metrics for each GC, by passing the
        # time range where each one occurred.

        for gc_trange in gcs_time_ranges:
            gc_stack_view = clr.Analysis.GetSamplesDataWithinTimeRange(
                self.trace_log,
                self.symbol_reader,
                self.stack_source,
                gc_trange,
            )
            self.gcs_stack_views.append(gc_stack_view)


@doc_field("gc_index", "Index/Number/ID of the GC.")
@doc_field("function", "Function to analyze samples of.")
@doc_field("inclusive_count", "Number of CPU Samples of the analyzed function and its callees.")
@doc_field("exclusive_count", "Number of CPU Samples of the analyzed function only.")
@doc_field(
    "inclusive_metric_percent", "Percent of CPU Samples belonging to this function and its callees."
)
@doc_field("exclusive_metric_percent", "Percent of CPU Samples belonging to this function only.")
@doc_field("first_time_msec", "Timestamp in msec where this function's first sample was found.")
@doc_field("last_time_msec", "Timestamp in msec where this function's last sample was found.")
@with_slots
@dataclass(frozen=True)
class GCAndCPUSamples:
    gc_index: int
    function: str
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


def _get_gcs_time_ranges(gcs: Sequence[ProcessedGC]) -> List[AbstractTimeSpan]:
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
#   trace_utils: TraceReadAndParseUtils object with the trace's associated
#       ProcessedTrace, TraceLog, SymbolReader, and StackSource objects.
#   functions: List with the names of the runtime functions to analyze.
#   all_data_to_chart: List where Trace objects with the points to chart
#       are appended. This class is defined in chart_utils.py.
#   gc_filter: Function used to filter the trace's GC's. This parameter is
#       optional and can be omitted to chart all the GC's samples.
#
# Returns: Nothing


def _get_cpu_samples_from_trace(
    ptrace_utils: TraceReadAndParseUtils,
    functions: Sequence[str],
    all_data_to_chart: List[Trace[GCAndCPUSamples]],
    gc_filter: Optional[Callable[[ProcessedGC], bool]],
) -> None:

    # Filter the GC's we want. Here is something interesting happening.
    # We are applying the received GC Filter, and retrieving a list with the
    # matching GC's indices. This is because when the TraceReadAndParseUtils
    # object was initialized, it stored each GC's StackView with the samples
    # in an array, where each index matches the GC index.

    gcs_to_analyze = list(map(
        lambda filtered: filtered.index,
        list(filter(gc_filter, ptrace_utils.trace_processed_gcs))
    ))

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

        # Retrieve the GC information from the array in the Trace Utils object
        # using the GC index. This is explained more thoroughly above.

        for gc_num in gcs_to_analyze:
            node_metrics = ptrace_utils.gcs_stack_views[gc_num].FindNodeByName(func)
            sample_points.append(
                GCAndCPUSamples(
                    gc_index=gc_num,
                    function=func,
                    inclusive_count=node_metrics.InclusiveCount,
                    exclusive_count=node_metrics.ExclusiveCount,
                    inclusive_metric_percent=node_metrics.InclusiveMetricPercent,
                    exclusive_metric_percent=node_metrics.ExclusiveMetricPercent,
                    first_time_msec=node_metrics.FirstTimeRelativeMSec,
                    last_time_msec=node_metrics.LastTimeRelativeMSec,
                )
            )
        all_data_to_chart.append(
            Trace(name=f"{ptrace_utils.trace_name}---{func}:", data=sample_points)
        )


# Summary: Reads all the given traces' data and calls _get_cpu_samples_from_trace()
#          accordingly to get all the samples data to chart. Then, calls
#          chart_lines_from_fields() to display the chart.
#
# Parameters:
#   ptraces_utils: List with the TraceReadAndParseUtils objects containing
#       all the information and helper objects for each trace to analyze.
#   functions_to_chart: List with the names of the runtime functions to analyze.
#   x_property_name: Metric to chart on the X-Axis.
#   y_property_name: Metric(s) to chart on the Y-Axis.
#   gc_filter: Function used to filter the trace's GC's. This parameter is
#       optional and can be omitted to chart all the GC's samples.
#
# Returns: Nothing


def chart_cpu_samples_per_gcs(
    ptraces_utils: Sequence[TraceReadAndParseUtils],
    functions_to_chart: Sequence[str],
    x_property_name: str,
    y_property_names: Sequence[str],
    gc_filter: Optional[Callable[[ProcessedGC], bool]] = None,
) -> None:

    # Make sure the samples data has been initialized for all the given traces,
    # or there will be nothing to chart.

    for ptrace in ptraces_utils:
        assert ptrace.gcs_stack_views, (
            f"The Trace Utils for {ptrace.trace_name} does not have the GC samples "
            "data. Make sure you pass `init_gcs_samples=True` when creating the "
            "object or call `init_gcs_samples_data() on it."
        )

    all_data_to_chart: List[Trace[GCAndCPUSamples]] = []

    # Read each trace's information and get the samples data to chart,
    # using the received GC filter, if any.

    for ptrace in ptraces_utils:
        _get_cpu_samples_from_trace(
            ptrace_utils=ptrace,
            functions=functions_to_chart,
            all_data_to_chart=all_data_to_chart,
            gc_filter=gc_filter,
        )

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
#   trace_utils: TraceReadAndParseUtils object with the trace's associated
#       ProcessedTrace, TraceLog, SymbolReader, and StackSource objects.
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
    ptrace_utils: TraceReadAndParseUtils,
    function: str,
    start_time_msec: float = 0.0,
    end_time_msec: float = 0.0,
) -> None:
    clr = get_clr()

    # If end time was not specified, search for the trace's length to set
    # this boundary.

    if end_time_msec == 0.0:
        end_time_msec = ptrace_utils.trace_duration_msec

    # Make sure we have a positive time range to look at.

    assert start_time_msec < end_time_msec, (
        f"Check the timestamp values. Start time is {start_time_msec}, ",
        f"which is later than End time {end_time_msec}.",
    )

    # Get the samples metrics data.

    samples_data = clr.Analysis.GetSamplesDataWithinTimeRange(
        ptrace_utils.trace_log,
        ptrace_utils.symbol_reader,
        ptrace_utils.stack_source,
        clr.TimeSpanUtil.FromStartEndMSec(start_time_msec, end_time_msec),
    )

    # Ask for the metrics related to the function of interest and print them.

    node_metrics = samples_data.FindNodeByName(function)
    _print_node(node_metrics)
