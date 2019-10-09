# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from math import inf
from pathlib import Path
from typing import Iterable, List, Optional, Sequence, Tuple

import matplotlib.pyplot as plt
from matplotlib.axes._subplots import SubplotBase
from matplotlib.lines import Line2D

from ..commonlib.bench_file import (
    BenchFileAndPath,
    MACHINE_DOC,
    MAX_ITERATIONS_FOR_ANALYZE_DOC,
    PartialTestCombination,
    Vary,
    VARY_DOC,
)
from ..commonlib.collection_util import (
    empty_sequence,
    FloatRange,
    is_empty,
    min_max_float,
    unzip3,
    XYRanges,
    zip_check,
)
from ..commonlib.command import Command, CommandKind, CommandsMapping
from ..commonlib.option import map_option, non_null, optional_to_iter, option_or_3
from ..commonlib.result_utils import unwrap
from ..commonlib.type_utils import argument, with_slots

from .chart_utils import OUT_SVG_DOC, set_axes, show_or_save, subplots, zip_with_marker_styles
from .diffable import DIFFABLE_PATHS_DOC, Diffables, get_diffables, SingleDiffable, TEST_WHERE_DOC
from .parse_metrics import parse_run_metric_arg, parse_run_metrics_arg
from .process_trace import ProcessedTraces
from .types import (
    floats,
    FloatsOrStrs,
    RunMetric,
    RunMetrics,
    RUN_METRICS_DOC,
    SampleKind,
    SAMPLE_KIND_DOC,
    strs,
)


@with_slots
@dataclass(frozen=True)
class ChartConfigurationsArgs:
    paths: Sequence[Path] = argument(name_optional=True, doc=DIFFABLE_PATHS_DOC)

    y_run_metrics: Sequence[str] = argument(
        doc=f"""
    {RUN_METRICS_DOC}

    A chart will be made for each of these metrics, where the metric is the y axis.
    """
    )

    vary: Optional[Vary] = argument(default=None, doc=VARY_DOC)

    x_run_metric: Optional[str] = argument(
        default=None,
        doc="""
    x-value to associate with each configuarion.
    If missing, will use a bar chart.
    """,
    )

    machine: Optional[Sequence[str]] = argument(
        default=None,
        doc=f"""
    Machine to look for test results.
    Specify multiple machines to compare test output from different machines.

    {MACHINE_DOC}
    """,
    )
    sample_kind: SampleKind = argument(default=0, doc=SAMPLE_KIND_DOC)
    max_iterations: Optional[int] = argument(default=None, doc=MAX_ITERATIONS_FOR_ANALYZE_DOC)
    out: Optional[Path] = argument(default=None, doc=OUT_SVG_DOC)

    test_where: Optional[Sequence[str]] = argument(default=None, doc=TEST_WHERE_DOC)

    # TODO: these should be in ChartCommonArgs
    zero_x: bool = argument(
        default=False, doc="If set, the chart will begin at 0 instead of at the smallest x value."
    )
    zero_y: bool = argument(
        default=False, doc="If set, the chart will begin at 0 instead of at the smallest y value."
    )
    width_factor: Optional[float] = argument(
        default=None, hidden=True, doc="If set, will shrink the output SVG's width."
    )
    adjust: bool = argument(
        default=False, hidden=True, doc="If set, will shrink the generated chart."
    )


@with_slots
@dataclass(frozen=True)
class Errors:
    err_neg: Sequence[float]
    err_pos: Sequence[float]

    def to_tuple(self) -> Tuple[Sequence[float], Sequence[float]]:
        return self.err_neg, self.err_pos


@with_slots
@dataclass(frozen=True)
class _ConfigurationsDataForDiffable:
    name: str
    x_axis: FloatsOrStrs
    x_axis_default: Optional[float]
    # For each metric, for each x value, there is a y value. (Note x value may just be a name.)
    y_axes: Sequence[Sequence[float]]
    y_errs: Sequence[Errors]

    @property
    def n_configs(self) -> int:
        return len(self.x_axis)

    def __post_init__(self) -> None:
        n_configs = self.n_configs
        assert len(self.x_axis) == n_configs
        for y in self.y_axes:
            assert len(y) == n_configs
        for e in self.y_errs:
            assert len(e.err_neg) == n_configs
            assert len(e.err_pos) == n_configs


@with_slots
@dataclass(frozen=True)
class _ConfigurationsData:
    run_metrics: RunMetrics
    diffables_data: Diffables
    diffables: Sequence[_ConfigurationsDataForDiffable]

    def __post_init__(self) -> None:
        for d in self.diffables:
            assert d.x_axis.is_floats == self.x_axis_is_floats
            # TODO: these are duplicate
            assert len(d.y_axes) == len(self.run_metrics)
            assert len(d.y_errs) == len(self.run_metrics)
            for lst in (
                d.y_axes,
                [err for errs in d.y_errs for err in (errs.err_neg, errs.err_pos)],
            ):
                for values in lst:
                    assert len(values) == d.n_configs

    @property
    def x_axis_is_floats(self) -> bool:
        return self.diffables[0].x_axis.is_floats


def _get_chart_configs_data(
    args: ChartConfigurationsArgs, sample_kind: SampleKind, max_iterations: Optional[int]
) -> _ConfigurationsData:
    x_run_metric = map_option(args.x_run_metric, parse_run_metric_arg)
    y_run_metrics = parse_run_metrics_arg(args.y_run_metrics, default_to_important=False)
    # Need an explicit type for this due to https://github.com/python/mypy/issues/6751
    x_run_metric_iter: Iterable[RunMetric] = optional_to_iter(x_run_metric)
    all_run_metrics: RunMetrics = (*x_run_metric_iter, *y_run_metrics)
    diffables = get_diffables(
        traces=ProcessedTraces(),
        paths=args.paths,
        run_metrics=all_run_metrics,
        # TODO: don't hardcode these, make them args
        machines_arg=args.machine,
        vary=args.vary,
        test_where=args.test_where,
        sample_kind=sample_kind,
        max_iterations=max_iterations,
    )

    def get_for_diffable(diffable: SingleDiffable) -> _ConfigurationsDataForDiffable:
        if x_run_metric is None:
            x_axis = strs([d.name for d in diffable.diff_us])
        else:
            # TODO: handle failure (no unwrap)
            x_axis = floats(
                [unwrap(unwrap(d.stats)[x_run_metric]).sample for d in diffable.diff_us]
            )

        y_axes: List[Sequence[float]] = []
        y_errs: List[Errors] = []
        for run_metric in y_run_metrics:
            y_axis, err_neg, err_pos = unzip3(
                # TODO: handle failure (no unwrap)
                unwrap(d.get_value(run_metric)).sample_and_errors().to_tuple()
                for d in diffable.diff_us
            )
            y_axes.append(y_axis)
            y_errs.append(Errors(err_neg, err_pos))

        configs_vary_by = (
            None
            if diffables.bench_and_path is None
            else diffables.bench_and_path.content.configs_vary_by
        )
        default_values = None if configs_vary_by is None else configs_vary_by.default_values
        # TODO: support getting default from more than just coreclr?
        x_axis_default = (
            None
            if default_values is None
            else default_values[non_null(diffable.common.coreclr_name)]
        )

        return _ConfigurationsDataForDiffable(
            name=diffable.name,
            x_axis=x_axis,
            x_axis_default=x_axis_default,
            y_axes=y_axes,
            y_errs=y_errs,
        )

    return _ConfigurationsData(
        run_metrics=y_run_metrics,
        diffables_data=diffables,
        diffables=[get_for_diffable(d) for d in diffables.diffables],
    )


def chart_configs(args: ChartConfigurationsArgs) -> None:
    data: _ConfigurationsData = _get_chart_configs_data(
        args, sample_kind=args.sample_kind, max_iterations=args.max_iterations
    )

    # TODO: dup code above
    fig, axes = subplots(len(data.run_metrics))

    x_axis_name = option_or_3(
        args.x_run_metric, map_option(data.diffables_data.vary, lambda v: v.name), "traces"
    )

    for metric_index, (metric, ax) in enumerate(zip_check(data.run_metrics, axes)):
        ax.set_xlabel(x_axis_name)
        ax.set_ylabel(metric.name)

        if data.x_axis_is_floats:
            _show_line(data, ax, metric_index, zero_x=args.zero_x, zero_y=args.zero_y)
        else:
            _show_bar(data, ax, metric_index)

    fig.suptitle(
        _chart_configs_title(
            x_axis_name, data.diffables_data.common, args, data.diffables_data.bench_and_path
        )
    )
    # wspace is so y-axis labels don't go on top of the chart to the left
    # hspace is irrelevant because they're horizontally stacked
    # top is lower to make room for the title

    if args.adjust:
        plt.subplots_adjust(left=0, bottom=0, right=1, top=0.75, wspace=0.18)
    show_or_save(args.out, width_factor=args.width_factor)


def _show_line(
    data: _ConfigurationsData,
    ax: SubplotBase,
    metric_index: int,
    zero_x: bool = False,
    zero_y: bool = False,
) -> Sequence[Line2D]:
    xticks_for_defaults: List[float] = []
    xtick_labels_for_defaults: List[str] = []

    def draw_line(d: _ConfigurationsDataForDiffable, marker_style: str) -> Line2D:
        line = ax.errorbar(
            x=d.x_axis.as_floats,
            y=d.y_axes[metric_index],
            yerr=d.y_errs[metric_index].to_tuple(),
            marker=marker_style,
            linestyle="-",
            capsize=3,
        )[0]
        if d.x_axis_default is not None:
            # y = _closest_y_value_to_default(c.x_axis, c.x_axis_default, c.y_axes[prop_index])
            ax.axvline(x=d.x_axis_default, color="gray", linewidth=0.5)
            xticks_for_defaults.append(d.x_axis_default)
            xtick_labels_for_defaults.append(d.name)
        return line

    lines = [draw_line(d, m) for d, m in zip_with_marker_styles(data.diffables)]
    ax.legend(handles=lines, labels=[d.name for d in data.diffables])

    # We want thes in addition to the other xticks ...

    # ax.set_xticks([*ax.get_xticks(), *xticks])
    # ax.set_xticklabels([*ax.get_xticklabels(), *xtick_labels])
    # ax.xticks(ax.ticks() + xticks)
    # ax.xticklabels(ax.xticklabels() + xtick_labels)
    # ax.set_xticks(xticks)
    # ax.set_xticklabels(xtick_labels)

    ranges = _get_x_y_ranges(data)
    set_axes(ax, zero_x=zero_x, zero_y=zero_y, ranges=ranges)

    ax2 = ax.twiny()
    ax2.set_xticks(xticks_for_defaults)
    ax2.set_xticklabels(xtick_labels_for_defaults)
    set_axes(ax2, zero_x=zero_x, zero_y=zero_y, ranges=ranges)

    return lines


def _show_bar(data: _ConfigurationsData, ax: SubplotBase, metric_index: int) -> None:
    assert len(data.diffables) == 1  # TODO: how to draw multiple bar charts?
    d = data.diffables[0]
    y_values = d.y_axes[metric_index]
    errs = d.y_errs[metric_index]
    x_indices = range(len(y_values))
    ax.bar(x_indices, y_values, yerr=errs.to_tuple())

    ax.set_xticks(x_indices)
    ax.set_xticklabels(d.x_axis.as_strs)


def _chart_configs_title(
    x_axis_name: str,
    common: PartialTestCombination,
    args: ChartConfigurationsArgs,
    bench: Optional[BenchFileAndPath],
) -> str:
    pfx = "" if bench is None else f"{bench.path}: "
    desc = f"{pfx}Effect of varying {x_axis_name}"
    mach = "" if common.machine is None else f" (on {common.machine.name})"
    where = "" if args.test_where is None else f", where {' '.join(args.test_where)}"
    cc = "" if common.config is None else f"\n{common.config.to_str_pretty()}"
    bm = (
        ""
        if common.benchmark is None
        else f"\n{common.benchmark_name}: {common.benchmark.executable_and_arguments()}"
    )
    iterations = None if common.benchmark is None else common.benchmark.iteration_count
    iters = "" if iterations is None else f" ({iterations} iterations)"
    return f"{desc}{mach}{where}{cc}{bm}{iters}"


def _get_x_y_ranges(data: _ConfigurationsData) -> XYRanges:
    x_range = non_null(
        min_max_float(
            x
            for d in data.diffables
            for x_axis in (d.x_axis.as_floats,)
            for x in (empty_sequence() if is_empty(x_axis) else [x_axis[0], x_axis[-1]])
        )
    )
    y_min = inf
    y_max = -inf
    for d in data.diffables:
        for y_axis, y_errs in zip_check(d.y_axes, d.y_errs):
            for y, err_neg, err_pos in zip(y_axis, y_errs.err_neg, y_errs.err_pos):
                y_min = min(y_min, y - err_neg)
                y_max = max(y_max, y + err_pos)
    assert y_min != inf and y_max != -inf
    y_range = FloatRange(y_min, y_max)
    return XYRanges(x_range, y_range)


CHART_CONFIGS_COMMANDS: CommandsMapping = {
    "chart-configs": Command(
        kind=CommandKind.analysis,
        fn=chart_configs,
        doc="""
    Compares various settings from 'configs' from a benchfile.
    Each config will have an associated x value from 'x_axis' and a y value from 'run_metrics'.
    There will be as many charts as there are 'run_metrics'.
    """,
    )
}
