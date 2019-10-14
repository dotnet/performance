# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from re import compile as compile_regexp, IGNORECASE
from typing import Iterable, List, Mapping, Optional, Sequence

from ..commonlib.bench_file import BenchFile, BenchFileAndPath
from ..commonlib.collection_util import (
    cat_unique,
    combine_mappings,
    empty_mapping,
    empty_sequence,
    is_empty,
)
from ..commonlib.option import non_null
from ..commonlib.type_utils import T
from ..commonlib.util import did_you_mean, get_or_did_you_mean, try_remove_str_start

from .join_analysis import (
    JOIN_PER_HEAP_METRICS_ALIASES,
    JOIN_PER_GC_METRICS_ALIASES,
    JOIN_RUN_METRICS_ALIASES,
)
from .run_metrics import TEST_STATUS_METRICS
from .single_gc_metrics import (
    GC_HEAP_COMPACT_REASON_METRICS,
    GC_HEAP_EXPAND_REASON_METRICS,
    GC_MECHANISM_METRICS,
    GC_REASON_METRICS,
    GC_TYPE_METRICS,
)
from .single_heap_metrics import MARK_ROOT_PROMOTED_METRICS, MARK_ROOT_TIME_METRICS
from .types import (
    NAME_TO_RUN_METRIC,
    NAME_TO_SINGLE_GC_METRIC,
    NAME_TO_SINGLE_HEAP_METRIC,
    RunMetric,
    RunMetrics,
    ScoreRunMetric,
    SingleGCMetric,
    SingleGCMetrics,
    SingleHeapMetrics,
    run_metric_must_exist_for_name,
)


def get_parsed_and_score_metrics(
    bench: Optional[BenchFileAndPath], metrics: Optional[Sequence[str]], default_to_important: bool
) -> RunMetrics:
    score_metrics = empty_sequence() if bench is None else tuple(get_score_metrics(bench.content))
    if metrics is None and not is_empty(score_metrics):
        return score_metrics
    return cat_unique(parse_run_metrics_arg(metrics, default_to_important), score_metrics)


_SINGLE_GC_REASONS_METRICS: Mapping[str, Sequence[str]] = {
    "mechanisms": [m.name for m in (*GC_TYPE_METRICS, *GC_MECHANISM_METRICS)],
    "reasons": [m.name for m in GC_REASON_METRICS],
    "compact-reasons": [m.name for m in GC_HEAP_COMPACT_REASON_METRICS],
    "expand-reasons": [m.name for m in GC_HEAP_EXPAND_REASON_METRICS],
}

_RUN_REASONS_METRICS: Mapping[str, Sequence[str]] = {
    k: [f"Pct{v}" for v in vs] for k, vs in _SINGLE_GC_REASONS_METRICS.items()
}


def parse_run_metrics_arg(
    metrics: Optional[Sequence[str]], default_to_important: bool = False
) -> RunMetrics:
    return _parse_metrics_arg(
        names=metrics,
        name_to_metric=NAME_TO_RUN_METRIC,
        metric_type_name="run",
        important=_IMPORTANT_RUN_METRICS,
        special=combine_mappings(
            _RUN_REASONS_METRICS,
            JOIN_RUN_METRICS_ALIASES,
            {
                "markTime": [
                    m
                    for metric in MARK_ROOT_TIME_METRICS
                    for m in (f"{metric.name}_Max_Mean", f"{metric.name}_Mean_Mean")
                ]
            },
            {"just-test-status": [m.name for m in TEST_STATUS_METRICS]},
        ),
        default_to_important=default_to_important,
    )


def parse_run_metric_arg(metric: str) -> RunMetric:
    return _parse_metric_arg(metric, NAME_TO_RUN_METRIC, "run metric")


_IMPORTANT_RUN_METRICS: Sequence[str] = (
    "TotalNumberGCs",
    "CountUsesLOHCompaction",
    "CountIsGen0",
    "CountIsGen1",
    "CountIsBackground",
    "CountIsBlockingGen2",
    "PctIsEphemeral",
    "FirstToLastGCSeconds",
    "TotalNonGCSeconds",
    "NumHeaps",
    "HeapSizeBeforeMB_Max",
    "HeapSizeAfterMB_Max",
    "HeapSizeBeforeMB_Mean",
    "HeapSizeAfterMB_Mean",
    "FirstToLastEventSeconds",
    "FirstEventToFirstGCSeconds",
    "PctTimeInGC_WhereIsNonBackground",
    "PctTimePausedInGC",
    "PctUsesCompaction",
    "PauseDurationSeconds_SumWhereIsGen1",
    "PauseDurationSeconds_SumWhereIsBackground",
    "PauseDurationSeconds_SumWhereIsBlockingGen2",
    "PauseDurationSeconds_SumWhereUsesLOHCompaction",
    "TotalAllocatedMB",
    "TotalLOHAllocatedMB",
    "PauseDurationMSec_Mean",
    "PauseDurationMSec_MeanWhereIsEphemeral",
    "PauseDurationMSec_95PWhereIsGen0",
    "PauseDurationMSec_95PWhereIsGen1",
    "PauseDurationMSec_95PWhereIsBackground",
    "PauseDurationMSec_95PWhereIsBlockingGen2",
    "PauseDurationSeconds_Sum",
    "PauseDurationSeconds_SumWhereIsNonBackground",
    "PctReductionInHeapSize_Mean",
    "PromotedMB_MeanWhereIsBlockingGen2",
    "PromotedMB_MeanWhereIsGen0",
    "PromotedMB_MeanWhereIsGen1",
)


def get_score_metrics(bench: BenchFile) -> Iterable[RunMetric]:
    if bench.scores is None:
        pass
    else:
        for k, v in bench.scores.items():
            for name, _ in v.items():
                yield run_metric_must_exist_for_name(name)
            yield ScoreRunMetric(k, v)


def parse_single_gc_metrics_arg(
    metrics: Optional[Sequence[str]], default_to_important: bool = False
) -> SingleGCMetrics:
    return _parse_metrics_arg(
        names=metrics,
        name_to_metric=NAME_TO_SINGLE_GC_METRIC,
        metric_type_name="single-gc",
        important=_SINGLE_GC_IMPORTANT_METRICS,
        special=combine_mappings(
            _SINGLE_GC_REASONS_METRICS,
            {
                "markTime": [
                    m
                    for metric in MARK_ROOT_TIME_METRICS
                    for m in (f"{metric.name}_Max", f"{metric.name}_Mean")
                ],
                "markPromoted": [
                    m
                    for metric in MARK_ROOT_PROMOTED_METRICS
                    for m in (f"{metric.name}_Max", f"{metric.name}_Mean")
                ],
            },
            JOIN_PER_GC_METRICS_ALIASES,
        ),
        default_to_important=default_to_important,
    )


_SINGLE_GC_IMPORTANT_METRICS: Sequence[str] = (
    "Generation",
    "IsConcurrent",
    "AllocRateMBSec",
    "AllocedSinceLastGCMB",
    "PauseDurationMSec",
    "PromotedMB",
    "HeapSizeBeforeMB",
    "HeapSizeAfterMB",
    # "PctReductionInHeapSize",
    "UsesCompaction",
)


def parse_single_gc_metric_arg(metric: str) -> SingleGCMetric:
    return _parse_metric_arg(metric, NAME_TO_SINGLE_GC_METRIC, "single-gc metric")


def parse_single_heap_metrics_arg(
    metrics: Optional[Sequence[str]], default_to_important: bool = False
) -> SingleHeapMetrics:
    return _parse_metrics_arg(
        names=metrics,
        name_to_metric=NAME_TO_SINGLE_HEAP_METRIC,
        metric_type_name="single-heap",
        important=None,
        special=JOIN_PER_HEAP_METRICS_ALIASES,
        default_to_important=default_to_important,
    )


def _parse_metrics_arg(
    names: Optional[Sequence[str]],
    name_to_metric: Mapping[str, T],
    metric_type_name: str,
    important: Optional[Sequence[str]] = None,
    special: Mapping[str, Sequence[str]] = empty_mapping(),
    default_to_important: bool = False,
) -> Sequence[T]:
    kind_of_metric = f"{metric_type_name} metric"

    def all_important() -> Iterable[T]:
        for name in non_null(important):
            try:
                yield name_to_metric[name]
            except KeyError:
                raise Exception(did_you_mean(name_to_metric, name, name=kind_of_metric)) from None

    def metrics() -> Iterable[T]:
        if names is None and not default_to_important:
            pass
        elif names is None:
            yield from all_important()
        elif len(names) == 1 and names[0] == "all":
            yield from name_to_metric.values()
        elif len(names) == 1 and names[0] == "none":
            pass
        else:
            for m in names:
                if m == "important":
                    yield from all_important()
                elif m in special:
                    for s in special[m]:
                        yield get_or_did_you_mean(name_to_metric, s, kind_of_metric)
                elif m in name_to_metric:
                    yield name_to_metric[m]
                else:
                    rgx_str = try_remove_str_start(m, "rgx:")
                    assert rgx_str is not None, did_you_mean(name_to_metric, m, kind_of_metric)
                    # Try using as a regexp
                    rgx = compile_regexp(rgx_str, IGNORECASE)
                    metrics = [
                        metric
                        for name, metric in name_to_metric.items()
                        if rgx.search(name) is not None
                    ]
                    assert not is_empty(metrics), did_you_mean(name_to_metric, m, kind_of_metric)
                    yield from metrics

    res: List[T] = []
    for met in metrics():
        assert met not in res, f"Duplicate metric {met}"
        res.append(met)
    return res


def _parse_metric_arg(name: str, name_to_metric: Mapping[str, T], metric_type_name: str) -> T:
    try:
        return name_to_metric[name]
    except KeyError:
        raise Exception(did_you_mean(name_to_metric, name, metric_type_name)) from None
