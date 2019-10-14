# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from pathlib import Path
from typing import Mapping, Optional, Sequence, Tuple

from result import Ok

from ..commonlib.bench_file import (
    BenchFileAndPath,
    Benchmark,
    BenchmarkAndName,
    get_test_paths_for_each_iteration,
    parse_bench_file,
    parse_machines_arg,
    PartialConfigAndName,
    PartialTestCombination,
    SingleTestCombination,
    Vary,
)
from ..commonlib.collection_util import (
    cat_unique,
    find_common,
    is_empty,
    make_mapping,
    make_multi_mapping,
)
from ..commonlib.option import non_null
from ..commonlib.result_utils import all_non_err, flat_map_ok, map_ok
from ..commonlib.type_utils import with_slots

from .parse_metrics import get_score_metrics
from .process_trace import ProcessedTraces, test_result_from_path
from .types import (
    MaybeMetricStatisticsFromAllIterations,
    MaybeMetricValuesForSingleIteration,
    MetricStatisticsFromAllIterations,
    FailableMetricValue,
    MetricValuesForSingleIteration,
    metric_value_of,
    RunMetric,
    RunMetrics,
    SampleKind,
)
from .where import get_where_doc, get_where_filter, WhereMapping


@with_slots
@dataclass(frozen=True)
class SingleDiffed:
    name: str
    test: Optional[SingleTestCombination]
    stats: MaybeMetricStatisticsFromAllIterations

    def get_value(self, run_metric: RunMetric) -> FailableMetricValue:
        return flat_map_ok(self.stats, lambda s: s[run_metric])


def _name_for_vary(test: SingleTestCombination, vary: Vary) -> str:
    return {
        Vary.machine: test.machine_name,
        Vary.coreclr: test.coreclr_name,
        Vary.config: test.config_name,
        Vary.benchmark: test.benchmark_name,
    }[vary]


@with_slots
@dataclass(frozen=True)
class SingleDiffable:
    # Things that are constant across all the things diffed
    common: PartialTestCombination
    # Optional because the test may have failed
    diff_us: Sequence[SingleDiffed]
    # This includes score metrics if there is a common benchmark
    run_metrics: RunMetrics

    @property
    def name(self) -> str:
        return self.common.name


@with_slots
@dataclass(frozen=True)
class Diffables:
    bench_and_path: Optional[BenchFileAndPath]
    vary: Optional[Vary]
    diffed_names: Sequence[str]
    common: PartialTestCombination
    diffables: Sequence[SingleDiffable]

    @property
    def n_to_diff(self) -> int:
        return len(self.diffed_names)

    def __post_init__(self) -> None:
        for d in self.diffables:
            assert len(d.diff_us) == self.n_to_diff


def iterations_to_statistics(
    iterations: Sequence[MaybeMetricValuesForSingleIteration],
    run_metrics: RunMetrics,
    sample_kind: SampleKind,
) -> MaybeMetricStatisticsFromAllIterations:
    def f(
        non_failure: Sequence[MetricValuesForSingleIteration]
    ) -> MetricStatisticsFromAllIterations:
        assert not is_empty(non_failure)
        return {
            metric: metric_value_of([i[metric] for i in non_failure], sample_kind)
            for metric in run_metrics
        }

    return map_ok(all_non_err(iterations), f)


def _get_for_single_test(
    traces: ProcessedTraces,
    bench_and_path: BenchFileAndPath,
    run_metrics: RunMetrics,
    sample_kind: SampleKind,
    max_iterations: Optional[int],
    test: SingleTestCombination,
) -> Tuple[SingleTestCombination, SingleDiffed]:
    iterations: Sequence[MaybeMetricValuesForSingleIteration] = [
        traces.get_run_metrics(iteration.to_test_result(), run_metrics)
        for iteration in get_test_paths_for_each_iteration(bench_and_path, test, max_iterations)
    ]
    stats = iterations_to_statistics(iterations, run_metrics, sample_kind)
    return test, SingleDiffed(name=test.name, test=test, stats=stats)


DIFFABLE_PATHS_DOC = """
Either
* Single path to a benchfile. All output traces will be included.
* Paths to individual traces.
"""


def get_diffables(
    traces: ProcessedTraces,
    # Either a benchfile, or a list of test status or trace files
    paths: Sequence[Path],
    run_metrics: RunMetrics,
    machines_arg: Optional[Sequence[str]],
    vary: Optional[Vary],
    test_where: Optional[Sequence[str]],
    sample_kind: SampleKind,
    max_iterations: Optional[int],
) -> Diffables:
    if len(paths) == 1:
        return get_diffables_from_bench_file(
            traces=traces,
            bench_file_path=paths[0],
            run_metrics=run_metrics,
            machines_arg=machines_arg,
            arg_vary=vary,
            test_where=test_where,
            sample_kind=sample_kind,
            max_iterations=max_iterations,
        )
    else:
        assert (
            machines_arg is None and vary is None and test_where is None and max_iterations is None
        )

        def get_single_diffed(path: Path) -> SingleDiffed:
            return SingleDiffed(
                name=str(path),
                test=None,
                stats=iterations_to_statistics(
                    (traces.get_run_metrics(test_result_from_path(path), run_metrics),),
                    run_metrics,
                    sample_kind,
                ),
            )

        diff_us = [get_single_diffed(path) for path in paths]
        diffable = SingleDiffable(
            common=PartialTestCombination(), diff_us=diff_us, run_metrics=run_metrics
        )
        return Diffables(
            bench_and_path=None,
            vary=None,
            diffed_names=[str(p) for p in paths],
            common=PartialTestCombination(),
            diffables=(diffable,),
        )


def supports_config(benchmark: BenchmarkAndName, config: PartialConfigAndName) -> bool:
    only_configs = benchmark.benchmark.only_configs
    return only_configs is None or config.name in only_configs


def get_diffables_from_bench_file(
    traces: ProcessedTraces,
    bench_file_path: Path,
    run_metrics: RunMetrics,
    machines_arg: Optional[Sequence[str]],
    arg_vary: Optional[Vary],
    test_where: Optional[Sequence[str]],
    sample_kind: SampleKind,
    max_iterations: Optional[int],
) -> Diffables:
    machines = parse_machines_arg(machines_arg)
    bench_and_path = parse_bench_file(bench_file_path)
    bench = bench_and_path.content

    vary = non_null(bench.vary, "Must provide --vary") if arg_vary is None else arg_vary

    unfiltered_all_combinations = [
        SingleTestCombination(machine=machine, coreclr=coreclr, config=config, benchmark=benchmark)
        for machine in machines
        for coreclr in bench.coreclrs_and_names
        for config in bench.partial_configs_and_names
        for benchmark in bench.benchmarks_and_names
        if supports_config(benchmark, config)
    ]

    filtered_all_combinations = _filter_test_combinations(unfiltered_all_combinations, test_where)

    common = PartialTestCombination(
        machine=find_common(lambda c: c.machine, filtered_all_combinations),
        coreclr_and_name=find_common(lambda c: c.coreclr, filtered_all_combinations),
        config_and_name=find_common(lambda c: c.config, filtered_all_combinations),
        benchmark_and_name=find_common(lambda c: c.benchmark, filtered_all_combinations),
    )

    all_run_metrics: Sequence[RunMetric] = cat_unique(run_metrics, get_score_metrics(bench))

    all_runs: Mapping[SingleTestCombination, SingleDiffed] = make_mapping(
        _get_for_single_test(
            traces, bench_and_path, all_run_metrics, sample_kind, max_iterations, t
        )
        for t in filtered_all_combinations
    )

    diffed_names, diffables = _get_diffables_by_vary(vary, all_run_metrics, all_runs)
    return Diffables(
        bench_and_path=bench_and_path,
        vary=vary,
        diffed_names=diffed_names,
        common=common,
        diffables=diffables,
    )


_TEST_WHERE_MAPPING: WhereMapping[SingleTestCombination] = {
    "coreclr": lambda c: Ok(c.coreclr_name),
    "config": lambda c: Ok(c.config_name),
    "benchmark": lambda b: Ok(b.benchmark_name),
}

TEST_WHERE_DOC = get_where_doc(_TEST_WHERE_MAPPING)


def _filter_test_combinations(
    all_combinations: Sequence[SingleTestCombination], test_where: Optional[Sequence[str]]
) -> Sequence[SingleTestCombination]:
    flt = get_where_filter(test_where, _TEST_WHERE_MAPPING)
    return [c for c in all_combinations if flt(c)]


def _get_diffables_by_vary(
    vary: Vary, all_run_metrics: RunMetrics, all_runs: Mapping[SingleTestCombination, SingleDiffed]
) -> Tuple[Sequence[str], Sequence[SingleDiffable]]:
    def get_run_metrics(_benchmark: Optional[Benchmark]) -> RunMetrics:
        # TODO: Filter using benchmark.only_score
        return all_run_metrics

    res = make_multi_mapping(
        (_rm_varied(test_combination, vary), single_diffed)
        for test_combination, single_diffed in all_runs.items()
    )

    diffables = [
        SingleDiffable(key, value, get_run_metrics(key.benchmark)) for key, value in res.items()
    ]
    assert not is_empty(diffables), "No tests matched the criteria"
    diffed_names = non_null(
        find_common(
            lambda diffable: tuple(
                _name_for_vary(non_null(du.test), vary) for du in diffable.diff_us
            ),
            diffables,
        )
    )

    return diffed_names, diffables


def _rm_varied(c: SingleTestCombination, vary: Vary) -> PartialTestCombination:
    return PartialTestCombination(
        machine=None if vary == Vary.machine else c.machine,
        coreclr_and_name=None if vary == Vary.coreclr else c.coreclr,
        config_and_name=None if vary == Vary.config else c.config,
        benchmark_and_name=None if vary == Vary.benchmark else c.benchmark,
    )
