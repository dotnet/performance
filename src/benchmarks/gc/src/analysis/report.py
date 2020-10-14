# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Iterable, Mapping, Optional, Sequence, Tuple, List, Dict

from result import Err, Ok, Result

from ..commonlib.bench_file import (
    BenchFileAndPath,
    get_test_paths_for_each_iteration,
    get_this_machine,
    iter_test_combinations,
    MAX_ITERATIONS_FOR_ANALYZE_DOC,
    parse_bench_file,
    ProcessQuery,
    SingleTestCombination,
    TestPaths,
    Vary,
    VARY_DOC,
)
from ..commonlib.collection_util import (
    add,
    cat_unique,
    empty_sequence,
    flatten,
    is_empty,
    items_sorted_by_key,
    make_multi_mapping,
    repeat_list,
    sort_high_to_low,
    unique_preserve_order,
    zip_check,
    zip_with_is_first,
)
from ..commonlib.command import Command, CommandKind, CommandsMapping
from ..commonlib.document import (
    Align,
    Cell,
    DocOutputArgs,
    Document,
    handle_doc,
    HeaderGroup,
    output_options_from_args,
    print_document,
    Row,
    Section,
    Table,
)
from ..commonlib.option import map_option, non_null, optional_to_iter, option_or
from ..commonlib.result_utils import all_non_err, as_err, map_ok, match, unwrap
from ..commonlib.score_spec import ScoreElement
from ..commonlib.type_utils import argument, check_cast, T, with_slots
from ..commonlib.util import (
    float_to_str,
    get_factor_diff,
    get_max_factor_diff,
    get_percent,
    percent_to_fraction,
)

from .core_analysis import PROCESS_DOC
from .diffable import (
    DIFFABLE_PATHS_DOC,
    Diffables,
    get_diffables,
    get_test_combinations,
    SingleDiffable,
    SingleDiffed,
    TEST_WHERE_DOC,
)
from .parse_metrics import parse_run_metrics_arg, get_score_metrics
from .process_trace import ProcessedTraces
from .types import (
    Better,
    FailableFloat,
    FailableMetricValue,
    FailableValue,
    get_regression_kind,
    invert_mechanisms,
    MechanismsAndReasons,
    MetricValue,
    MetricValuesForSingleIteration,
    RegressionKind,
    RunMetric,
    RunMetrics,
    run_metric_must_exist_for_name,
    RUN_METRICS_DOC,
    SampleKind,
    ScoreRunMetric,
    SAMPLE_KIND_DOC,
    union_all_mechanisms,
)


@with_slots
@dataclass(frozen=True)
class _ComparisonStatDiff:
    metric: RunMetric
    # None if the whole test failed, as opposed to just not being able to get that particular metric
    base: Optional[FailableMetricValue]
    new: Optional[FailableMetricValue]

    def has_both(self) -> bool:
        return self.base is not None and self.new is not None

    def base_new_factor_diff_abs_diff(
        self,
    ) -> Tuple[FailableMetricValue, FailableMetricValue, Optional[float], Optional[float]]:
        assert self.base is not None and self.new is not None
        if self.base.is_err() or self.new.is_err():
            return self.base, self.new, None, None
        else:
            bs = self.base.unwrap().sample
            ns = self.new.unwrap().sample
            return (self.base, self.new, get_factor_diff(bs, ns), ns - bs)

    @property
    def factor_diff(self) -> Optional[float]:
        return (
            None
            if self.base is None or self.new is None or self.base.is_err() or self.new.is_err()
            else get_factor_diff(self.base.unwrap().sample, self.new.unwrap().sample)
        )

    @property
    def regression_kind(self) -> RegressionKind:
        return (
            RegressionKind.STALE
            if self.factor_diff is None
            else get_regression_kind(self.factor_diff, self.metric.better)
        )

    def max_stdev_fraction(self) -> Optional[float]:
        return (
            None
            if self.base is None or self.new is None or self.base.is_err() or self.new.is_err()
            else max(self.base.unwrap().stdev_frac, self.new.unwrap().stdev_frac)
        )


@with_slots
@dataclass(frozen=True)
class _ComparisonDataForSingleDiff:
    by_metric: Mapping[RunMetric, _ComparisonStatDiff]
    by_regression_kind: Mapping[RegressionKind, Sequence[_ComparisonStatDiff]]

    def __post_init__(self) -> None:
        for v in self.by_regression_kind.values():
            assert not is_empty(v)


def _report_diff(
    diffables: Diffables,
    min_factor_diff: float,
    show_stdev: bool,
    sample_kind: SampleKind,
    include_summary: bool,
) -> Document:
    assert diffables.n_to_diff >= 2
    if diffables.n_to_diff == 2:
        return _diff_two(
            diffables,
            lambda sd: sd.factor_diff is None or abs(sd.factor_diff) >= min_factor_diff,
            show_stdev,
            sample_kind,
            include_summary,
        )
    else:
        return _table_for_more_than_two(diffables, min_factor_diff, show_stdev)


_SUMMARY_METRICS_STRINGS: Sequence[Sequence[str]] = (
    # No more than 3 per group or it is too wide.
    ("PctTimePausedInGC", "FirstToLastGCSeconds"),
    ("HeapSizeBeforeMB_Mean", "HeapSizeAfterMB_Mean"),
    ("PauseDurationMSec_95PWhereIsGen0", "PauseDurationMSec_95PWhereIsGen1"),
    ("PauseDurationMSec_95PWhereIsBackground", "PauseDurationMSec_95PWhereIsBlockingGen2"),
)
_SUMMARY_METRICS: Sequence[RunMetrics] = [
    [run_metric_must_exist_for_name(name) for name in group] for group in _SUMMARY_METRICS_STRINGS
]
_SUB_HEADERS: Sequence[str] = ("Base", "New", "% Diff")


def _cell_for_optional_metric_value(value: Optional[FailableMetricValue]) -> Cell:
    if value is None:
        return Cell("<test failed>")
    else:
        return _cell_for_metric_value(value)


def _cell_for_metric_value(value: FailableMetricValue) -> Cell:
    return match(value, lambda v: Cell(v.sample), Cell)


def _cell_for_value(value: FailableValue) -> Cell:
    return match(value, Cell, Cell)


def _value_and_stdev(s: FailableMetricValue, show_stdev: bool) -> Sequence[Cell]:
    return match(
        s,
        # https://github.com/python/mypy/issues/6898#issuecomment-495753076
        cb_ok=lambda s: s.cells_sample_stdev_pct() if show_stdev else [Cell(s.sample)],
        cb_err=lambda e: [Cell(e), Cell()] if show_stdev else [Cell(e)],
    )


def _show_sample_kind(sample_kind: SampleKind) -> str:
    if isinstance(sample_kind, int):
        return f"run {sample_kind}"
    else:
        return sample_kind.name


def _diff_two(
    diffables: Diffables,
    filter_comparison: Callable[[_ComparisonStatDiff], bool],
    show_stdev: bool,
    sample_kind: SampleKind,
    include_summary: bool,
) -> Document:
    assert diffables.n_to_diff == 2
    base, new = diffables.diffed_names

    def if_stdev(x: T) -> Sequence[T]:
        return [x] if show_stdev else empty_sequence()

    column_titles: Sequence[str] = (
        "Metric",
        f"Base ({_show_sample_kind(sample_kind)})",
        *if_stdev("Stdev %"),
        f"New ({_show_sample_kind(sample_kind)})",
        *if_stdev("Stdev %"),
        "% Diff",
        "Abs Diff",
    )

    def row_cells(c: _ComparisonStatDiff) -> Row:
        base, new, factor_diff, abs_diff = c.base_new_factor_diff_abs_diff()
        return (
            Cell(c.metric.name, Align.left),
            *_value_and_stdev(base, show_stdev),
            *_value_and_stdev(new, show_stdev),
            Cell(map_option(factor_diff, get_percent)),  # TODO:COLOR
            Cell(abs_diff),
        )

    def get_section(d: SingleDiffable) -> Optional[Section]:
        def get_table(
            rk: RegressionKind, stat_diffs: Sequence[_ComparisonStatDiff]
        ) -> Optional[Table]:
            # If one failed, don't bother with the table
            if not any(sd.has_both() for sd in stat_diffs):
                return None
            else:
                used_stat_diffs = [sd for sd in stat_diffs if filter_comparison(sd)]
                sorted_stat_diffs = sorted(used_stat_diffs, key=lambda sd: sd.metric.name)
                rows = [row_cells(sd) for sd in sorted_stat_diffs]
                if is_empty(rows):
                    return None
                else:
                    return Table(name=rk.title(), headers=column_titles, rows=rows)

        comparison_data = _get_comparison_data(d)
        if comparison_data is None:
            return None
        else:
            tables = [
                table
                for rk, stat_diffs in items_sorted_by_key(comparison_data.by_regression_kind)
                for table in optional_to_iter(get_table(rk, stat_diffs))
            ]

            return None if is_empty(tables) else Section(name=d.name, tables=tables)

    summary_sections = _get_summary_sections(diffables) if include_summary else empty_sequence()

    return Document(
        comment=f"Diff of base = {base} and new = {new}",
        sections=(
            *summary_sections,
            *(section for d in diffables.diffables for section in optional_to_iter(get_section(d))),
        ),
    )


def _get_summary_sections(diffables: Diffables) -> Sequence[Section]:
    def get_summary_row(diffable: SingleDiffable, group: RunMetrics) -> Row:
        data = _get_comparison_data(diffable)

        def cells_for_metric(metric: RunMetric) -> Sequence[Cell]:
            r = data.by_metric[metric]

            return [
                _cell_for_optional_metric_value(r.base),
                _cell_for_optional_metric_value(r.new),
                Cell()
                if r.factor_diff is None
                else _diff_percent_cell(r.factor_diff, metric.better),
            ]

        return [
            Cell(diffable.name, Align.left),
            *(cell for metric in group for cell in cells_for_metric(metric)),
        ]

    def get_section(is_first: bool, group: RunMetrics) -> Section:
        return Section(
            name=f"Summary of important stats" if is_first else None,
            tables=(
                Table(
                    header_groups=(
                        HeaderGroup("", 1),
                        *(HeaderGroup(metric.name, 3) for metric in group),
                    ),
                    headers=("name", *repeat_list(_SUB_HEADERS, len(group))),
                    rows=[get_summary_row(x, group) for x in diffables.diffables],
                ),
            ),
        )

    return [get_section(is_first, group) for is_first, group in zip_with_is_first(_SUMMARY_METRICS)]


@with_slots
@dataclass(frozen=True)
class _DiffRow:
    metric: RunMetric
    max_factor_diff: Optional[float]
    stat_values: Sequence[FailableMetricValue]


def _name_and_stdev_pct(name: str, show_stdev: bool) -> Sequence[str]:
    if show_stdev:
        return name, "stdev%"
    else:
        return (name,)


def _get_max_factor_diff_for_metric_values(
    metric_values: Sequence[FailableMetricValue],
) -> Optional[float]:
    return get_max_factor_diff(s.unwrap().sample for s in metric_values if s.is_ok())


def _table_for_more_than_two(
    diffables: Diffables, min_factor_diff: float, show_stdev: bool
) -> Document:
    # We will sort the stats by the greatest % difference between the various tests.

    def get_table(diffable: SingleDiffable) -> Table:
        def get_row(metric: RunMetric) -> Optional[_DiffRow]:
            metric_values = [d.get_value(metric) for d in diffable.diff_us]
            max_factor_diff = _get_max_factor_diff_for_metric_values(metric_values)
            if max_factor_diff is None or max_factor_diff >= min_factor_diff:
                return _DiffRow(metric, max_factor_diff, metric_values)
            else:
                return None

        rows: Sequence[_DiffRow] = sort_high_to_low(
            [r for m in diffable.run_metrics for r in optional_to_iter(get_row(m))],
            lambda r: option_or(r.max_factor_diff, -1),
        )

        def render_row(row: _DiffRow) -> Row:
            return [
                Cell(row.metric.name, Align.left),
                Cell(map_option(row.max_factor_diff, get_percent)),
                *(
                    cell
                    for values in row.stat_values
                    for cell in _value_and_stdev(values, show_stdev)
                ),
            ]

        return Table(
            name=diffable.name,
            headers=[
                "stat name",
                "Greatest % diff",
                *(
                    x
                    for diffed in diffables.diffed_names
                    for x in _name_and_stdev_pct(diffed, show_stdev)
                ),
            ],
            rows=[
                render_row(r)
                for r in sorted(
                    rows, key=lambda row: option_or(row.max_factor_diff, 0), reverse=True
                )
            ],
        )

    return Document(
        sections=[Section(name=d.name, tables=(get_table(d),)) for d in diffables.diffables]
    )


def _diff_percent_cell(factor_diff: float, better: Better) -> Cell:
    return Cell(
        get_percent(factor_diff), color=get_regression_kind(factor_diff, better).text_color()
    )


def _get_comparison_data(d: SingleDiffable) -> _ComparisonDataForSingleDiff:
    assert len(d.diff_us) == 2
    base, new = d.diff_us

    by_metric = {
        metric: _ComparisonStatDiff(metric, base.get_value(metric), new.get_value(metric))
        for metric in d.run_metrics
    }
    by_regression_kind = make_multi_mapping((r.regression_kind, r) for r in by_metric.values())
    return _ComparisonDataForSingleDiff(by_metric, by_regression_kind)


@with_slots
@dataclass(frozen=True)
class ReportReasonsArgs:
    bench_file_path: Path = argument(
        name_optional=True,
        doc="""
    Path to a benchfile.
    All traces produced from this will have reasons reported.
    """,
    )
    max_iterations: Optional[int] = argument(default=None, doc=MAX_ITERATIONS_FOR_ANALYZE_DOC)


@with_slots
@dataclass(frozen=True)
class DiffArgs(DocOutputArgs):
    trace_paths: Sequence[Path] = argument(name_optional=True, doc=DIFFABLE_PATHS_DOC)

    vary: Optional[Vary] = argument(default=None, doc=VARY_DOC)

    metrics_as_columns: bool = argument(
        default=False, doc="Show metrics on columns and tests on rows (default is the reverse)"
    )
    sort_by_run_metric: Optional[str] = argument(
        default=None, doc="For --metrics-as-columns, sort rows by this metric"
    )

    machines: Optional[Sequence[str]] = argument(
        default=None,
        doc="Machine the test results are on (if different from the machine the benchfile is on).",
    )

    test_where: Optional[Sequence[str]] = argument(default=None, doc=TEST_WHERE_DOC)

    run_metrics: Optional[Sequence[str]] = argument(default=None, doc=RUN_METRICS_DOC)

    min_difference_pct: float = argument(
        default=0,
        doc="Only show metrics where there is this much difference between configurations.",
    )

    sample_kind: SampleKind = argument(default=0, doc=SAMPLE_KIND_DOC)
    max_iterations: Optional[int] = argument(default=None, doc=MAX_ITERATIONS_FOR_ANALYZE_DOC)

    no_summary: bool = argument(default=False, doc="Don't emit the 'summary' section.")
    process: ProcessQuery = argument(default=None, doc=PROCESS_DOC)


def _metrics_as_columns_table(
    diffables: Diffables, sort_by_metric: Optional[RunMetric], show_stdev: bool
) -> Document:
    def get_table(dd: SingleDiffable) -> Table:
        diffeds_and_values = zip_check(diffables.diffed_names, dd.diff_us)
        if sort_by_metric is not None:
            non_null_metric = sort_by_metric

            def key(t: Tuple[str, SingleDiffed]) -> float:
                # TODO: handle failure
                return check_cast(MetricValue, t[1].get_value(non_null_metric)).sample

            diffeds_and_values = sorted(diffeds_and_values, key=key)
        return Table(
            name=dd.name,
            headers=[
                "test name",
                *(x for s in dd.run_metrics for x in _name_and_stdev_pct(s.name, show_stdev)),
            ],
            rows=[
                [
                    Cell(diffed, Align.left),
                    *(
                        cell
                        for metric in dd.run_metrics
                        for cell in _value_and_stdev(d.get_value(metric), show_stdev=show_stdev)
                    ),
                ]
                for diffed, d in diffeds_and_values
            ],
        )

    return Document(sections=(Section(tables=[get_table(dd) for dd in diffables.diffables]),))


def diff(args: DiffArgs) -> None:
    sort_by_metric = map_option(args.sort_by_run_metric, run_metric_must_exist_for_name)
    doc = diff_for_jupyter(
        traces=ProcessedTraces(),
        trace_paths=args.trace_paths,
        run_metrics=parse_run_metrics_arg(args.run_metrics, default_to_important=True),
        machines=args.machines,
        vary=args.vary,
        test_where=args.test_where,
        sample_kind=args.sample_kind,
        max_iterations=args.max_iterations,
        metrics_as_columns=args.metrics_as_columns,
        no_summary=args.no_summary,
        sort_by_metric=sort_by_metric,
        min_difference_pct=args.min_difference_pct,
        process=args.process,
    )
    handle_doc(doc, output_options_from_args(args))


def diff_for_jupyter(
    traces: ProcessedTraces,
    trace_paths: Sequence[Path],
    run_metrics: RunMetrics,
    machines: Optional[Sequence[str]],
    vary: Optional[Vary],
    test_where: Optional[Sequence[str]],
    sample_kind: SampleKind,
    max_iterations: Optional[int],
    metrics_as_columns: bool,
    no_summary: bool,
    sort_by_metric: Optional[RunMetric],
    min_difference_pct: float,
    process: ProcessQuery,
) -> Document:
    include_summary = not no_summary and not metrics_as_columns
    all_run_metrics = get_run_metrics_for_diff(
        include_summary=include_summary, sort_by_metric=sort_by_metric, run_metrics=run_metrics
    )

    diffables = get_diffables(
        traces=traces,
        paths=trace_paths,
        run_metrics=all_run_metrics,
        machines_arg=machines,
        vary=vary,
        test_where=test_where,
        sample_kind=sample_kind,
        max_iterations=max_iterations,
        process=process,
    )
    return show_diff_from_diffables(
        diffables,
        metrics_as_columns=metrics_as_columns,
        sort_by_metric=sort_by_metric,
        min_difference_pct=min_difference_pct,
        sample_kind=sample_kind,
        include_summary=include_summary,
    )


def get_run_metrics_for_diff(
    include_summary: bool, sort_by_metric: Optional[RunMetric], run_metrics: RunMetrics
) -> RunMetrics:
    # Need an explicit type for this due to https://github.com/python/mypy/issues/6751
    sort_by_metric_iter: Iterable[RunMetric] = optional_to_iter(sort_by_metric)
    return unique_preserve_order(
        (
            *(flatten(_SUMMARY_METRICS) if include_summary else empty_sequence()),
            *sort_by_metric_iter,
            *run_metrics,
        )
    )


@with_slots
@dataclass(frozen=True)
class _MetricAndStdevFrac:
    metric: RunMetric
    stdev: float


@with_slots
@dataclass(frozen=True)
class _ScoreDiff:
    factor_diff: float
    max_stdev: _MetricAndStdevFrac


def _get_score_diff(
    score_metric: ScoreRunMetric, l: SingleDiffed, r: SingleDiffed
) -> Result[str, _ScoreDiff]:
    max_stdev: Optional[_MetricAndStdevFrac] = None

    def get_element_value(name: str, el: ScoreElement) -> FailableFloat:
        metric = run_metric_must_exist_for_name(name)
        lvr = l.get_value(metric)
        rvr = r.get_value(metric)
        if lvr.is_err():
            return Err(f"{l.name} failed: {as_err(lvr)}")
        elif rvr.is_err():
            return Err(f"{r.name} failed: {as_err(rvr)}")
        else:
            lv = lvr.unwrap()
            rv = rvr.unwrap()
            nonlocal max_stdev
            if max_stdev is None or lv.stdev_frac > max_stdev.stdev:
                max_stdev = _MetricAndStdevFrac(metric, lv.stdev_frac)
            if rv.stdev_frac > max_stdev.stdev:
                max_stdev = _MetricAndStdevFrac(metric, rv.stdev_frac)
            return Ok(get_factor_diff(lv.sample, rv.sample) * el.weight)

    def use_elements(xs: Sequence[float]) -> _ScoreDiff:
        total = sum(xs)
        total_weight = sum(el.weight for el in score_metric.spec.values())
        return _ScoreDiff(total / total_weight, non_null(max_stdev))

    return map_ok(
        all_non_err([get_element_value(name, el) for name, el in score_metric.spec.items()]),
        use_elements,
    )


def _get_score_diffs(diffable: SingleDiffable) -> Mapping[ScoreRunMetric, Result[str, _ScoreDiff]]:
    score_metrics: Sequence[ScoreRunMetric] = [
        m for m in diffable.run_metrics if isinstance(m, ScoreRunMetric)
    ]
    assert len(diffable.diff_us) == 2
    l, r = diffable.diff_us
    return {score_metric: _get_score_diff(score_metric, l, r) for score_metric in score_metrics}


# For diffing two scores: we compare the difference of one to the other
def print_diff_score_summary(diffables: Diffables) -> None:
    def get_table(diffable: SingleDiffable) -> Table:
        def get_row(
            score_metric: ScoreRunMetric, score_diff_result: Result[str, _ScoreDiff]
        ) -> Row:
            return match(
                score_diff_result,
                cb_ok=lambda score_diff: (
                    Cell(score_metric.name),
                    Cell(get_percent(score_diff.factor_diff)),
                    Cell(_show_metric_and_stdev(score_diff.max_stdev)),
                ),
                cb_err=lambda err: (Cell(score_metric.name), Cell(err), Cell()),
            )

        rows = [
            get_row(score_metric, score_diff)
            for score_metric, score_diff in _get_score_diffs(diffable).items()
        ]
        return Table(
            name=diffable.name,
            headers=("score", "% diff of scores (lower is better)", "max stdev"),
            rows=rows,
        )

    doc = Document(sections=(Section(tables=[get_table(d) for d in diffables.diffables]),))
    print_document(doc)


def _show_metric_and_stdev(m: _MetricAndStdevFrac) -> str:
    return f"{float_to_str(get_percent(m.stdev))}% at {m.metric.name}"


def show_diff_from_diffables(
    diffables: Diffables,
    metrics_as_columns: bool,
    sort_by_metric: Optional[RunMetric],
    min_difference_pct: float,
    sample_kind: SampleKind,
    include_summary: bool,
) -> Document:
    show_stdev = _should_show_stdev(diffables)
    if metrics_as_columns:
        assert min_difference_pct is None
        assert sample_kind is None
        return _metrics_as_columns_table(diffables, sort_by_metric, show_stdev)
    else:
        assert sort_by_metric is None
        return _report_diff(
            diffables,
            percent_to_fraction(min_difference_pct),
            show_stdev,
            sample_kind,
            include_summary,
        )


def _should_show_stdev(diffables: Diffables) -> bool:
    # Only show stdev if there was more than one iteration
    return any(
        metric_value.unwrap().n_samples > 1
        for diffable in diffables.diffables
        for values in diffable.diff_us
        if values.stats.is_ok()
        for metric_value in values.stats.unwrap().values()
        if metric_value.is_ok()
    )


def report_reasons(args: ReportReasonsArgs) -> None:
    handle_doc(
        report_reasons_for_jupyter(
            traces=ProcessedTraces(),
            bench_file_path=args.bench_file_path,
            max_iterations=args.max_iterations,
        )
    )


def report_reasons_for_jupyter(
    traces: ProcessedTraces, bench_file_path: Path, max_iterations: Optional[int]
) -> Document:
    bench_and_path = parse_bench_file(bench_file_path)

    tests = tuple(iter_test_combinations(bench_and_path.content, (get_this_machine(),)))

    test_results: Sequence[Tuple[SingleTestCombination, MechanismsAndReasons]] = [
        _get_mechanisms_and_reasons_for_test(traces, bench_and_path, test, max_iterations)
        for test in tests
    ]

    def rows_for_mechanisms(mechanisms: MechanismsAndReasons) -> Sequence[Row]:
        return [(Cell(s),) for s in mechanisms.to_strs()]

    all_mechanisms = union_all_mechanisms(m for _, m in test_results)
    sections = (
        Section(
            name="Mechanisms per test",
            tables=[
                Table(
                    name=test.name,
                    rows=[[Cell("no mechanisms (should mean no gcs)")]]
                    if is_empty(mechanisms.to_strs())
                    else rows_for_mechanisms(mechanisms),
                )
                for test, mechanisms in test_results
            ],
        ),
        Section(
            name="All reached mechanisms", tables=(Table(rows=rows_for_mechanisms(all_mechanisms)),)
        ),
        Section(
            name="Missing mechanisms",
            tables=(Table(rows=rows_for_mechanisms(invert_mechanisms(all_mechanisms))),),
        ),
    )
    return Document(sections=sections)


def _get_mechanisms_and_reasons_for_test(
    traces: ProcessedTraces,
    bench_and_path: BenchFileAndPath,
    test: SingleTestCombination,
    max_iterations: Optional[int],
) -> Tuple[SingleTestCombination, MechanismsAndReasons]:
    processed_traces = unwrap(
        all_non_err(
            [
                traces.get(
                    iteration.to_test_result(),
                    need_mechanisms_and_reasons=True,
                    need_join_info=False,
                )
                for iteration in get_test_paths_for_each_iteration(
                    bench_and_path, test, max_iterations
                )
            ]
        )
    )
    return (
        test,
        union_all_mechanisms(non_null(trace.mechanisms_and_reasons) for trace in processed_traces),
    )


@with_slots
@dataclass(frozen=True)
class _PrintAllRunsArgs(DocOutputArgs):
    bench_file_path: Path = argument(name_optional=True, doc="Path to a benchfile")
    run_metrics: Sequence[str] = argument(doc=RUN_METRICS_DOC)


def _print_all_runs(args: _PrintAllRunsArgs) -> None:
    handle_doc(
        print_all_runs_for_jupyter(
            traces=ProcessedTraces(),
            bench_file_path=args.bench_file_path,
            run_metrics=parse_run_metrics_arg(args.run_metrics, default_to_important=False),
        ),
        output_options_from_args(args),
    )


def print_all_runs_for_jupyter(
    traces: ProcessedTraces, bench_file_path: Path, run_metrics: Sequence[RunMetric]
) -> Document:
    bench_and_path = parse_bench_file(bench_file_path)
    iters = [
        iteration
        for test in (tuple(iter_test_combinations(bench_and_path.content, (get_this_machine(),))))
        for iteration in get_test_paths_for_each_iteration(
            bench_and_path, test, max_iterations=None
        )
    ]

    def get_table(it: TestPaths) -> Table:
        name = str(it.test_status_path)
        return match(
            traces.get_run_metrics(it.to_test_result(), run_metrics=run_metrics),
            lambda metric_values: Table(
                name=name,
                rows=[
                    (Cell(metric.name), _cell_for_value(value))
                    for metric, value in metric_values.items()
                ],
            ),
            lambda err: Table(name=name, text=err),
        )

    tables = [get_table(it) for it in iters]
    return Document(sections=(Section(tables=tables),))


# Summary: Iterates through a set of test runs (currently only available for
#          GCPerfSim tests due to how the infra is consolidated), and fetches
#          all the GC metrics from their respective traces (e.g. PctinGC).
#          Then, saves this information in a list of dictionaries, where each
#          entry contains the metric values of an iteration of the test that
#          was run.
#
# Parameters:
#   traces:
#       ProcessedTraces object which stores tarce file information. This
#       is commonly initialized by calling ProcessedTraces() in Jupyter Notebook.
#   bench_file_path:
#       Path to the test's spec yaml bench file.
#   run_metrics:
#       RunMetrics object which later contains the GC metrics extracted
#       from the trace. Usually initialized with a dummy in Jupyter Notebook.
#       For example: parse_run_metrics_arg(("important",))
#   machines:
#       Optional list with the names of the machines where the tests
#       were run. Usually left blank and then infra reads and uses the current
#       machine's name.
#
# Returns: Nothing


def get_gc_metrics_numbers_for_jupyter(
    traces: ProcessedTraces,
    bench_file_path: Path,
    run_metrics: RunMetrics,
    machines: Optional[Sequence[str]],
) -> List[Dict[str, float]]:
    initial_run_metrics = get_run_metrics_for_diff(
        include_summary=True, sort_by_metric=None, run_metrics=run_metrics
    )

    raw_numbers_data = []
    bench_and_path = parse_bench_file(bench_file_path)
    bench = bench_and_path.content

    all_combinations = get_test_combinations(machines_arg=machines, bench=bench, test_where=None)
    all_run_metrics = cat_unique(initial_run_metrics, get_score_metrics(bench))

    for t in all_combinations:
        iterations = [
            traces.get_run_metrics(iteration.to_test_result(), all_run_metrics)
            for iteration in get_test_paths_for_each_iteration(
                bench=bench_and_path, t=t, max_iterations=None
            )
        ]

        for iteration in iterations:
            # MetricValuesForSingleIteration - Mapping[RunMetric, FailableValue]
            # RunMetric can either be a NamedRunMetric or a ScoreRunMetric. In this case,
            # it is the former.
            # However, it originally comes from a MaybeMetricValuesForSingleIteration,
            # which has to be unwrapped.
            iter_ok_result: MetricValuesForSingleIteration = unwrap(iteration)
            data_map: Dict[str, float] = {}

            for iter_key, iter_value in iter_ok_result.items():
                # iter_key = RunMetric, iter_value = FailableValue(Union(bool, int, float))
                # We are adding an annotation to ask mypy to ignore the type checking.
                # No matter what, it will always complain and make the code unusable.
                # This was the closest way to have functioning code while silencing the
                # least amount of complaints from mypy.
                add(data_map, iter_key.name, iter_value.ok())  # type: ignore
            raw_numbers_data.append(data_map)
    return raw_numbers_data


REPORT_COMMANDS: CommandsMapping = {
    "diff": Command(
        kind=CommandKind.analysis,
        fn=diff,
        doc="""
    Compares run metrics between traces or configs.

    If `--trace-paths` specifies multiple paths, they should be paths to traces to diff.
    (These should be test status `.yaml` files as we need to know the process ID for each trace.)

    Otherwise, `--trace-paths` should specify a benchfile, and you should specify `--vary`.
    If `--vary` is e.g. `config`, it will compare all the different configs from the benchfile.
    """,
    ),
    "print-all-runs": Command(
        kind=CommandKind.analysis,
        fn=_print_all_runs,
        doc="""
    Print run metrics for every trace file,
    considering different iterations of the same test separately.
    """,
    ),
    # TODO: hiding this for now as it needs a PerfView update
    "report-reasons": Command(
        hidden=True,
        kind=CommandKind.analysis,
        fn=report_reasons,
        doc="""
    Print what kinds of GCs occurred in a trace.
    """,
        priority=2,
    ),
}
