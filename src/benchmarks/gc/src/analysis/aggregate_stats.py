# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from statistics import mean, stdev
from typing import Callable, Iterable, List, Mapping, Sequence, Tuple, Type, TypeVar

from result import Err, Ok

from ..commonlib.collection_util import (
    combine_mappings,
    empty_mapping,
    indices,
    is_empty,
    make_mapping,
)
from ..commonlib.result_utils import all_non_err, as_err, fn_to_ok, flat_map_ok, map_ok
from ..commonlib.type_utils import check_cast, T
from ..commonlib.util import get_95th_percentile, get_percent

from .types import (
    Failable,
    FailableFloat,
    FailableInt,
    MetricType,
    TMetric,
    TMetricB,
    TNamedMetric,
    FailableValue,
)

TElement = TypeVar("TElement")
Filter = Callable[[T], Failable[Sequence[TElement]]]

# We'll include a filter named "" that always returns True.
_Filters = Mapping[str, Filter[T, TElement]]

_AdditionalAggregates = Mapping[str, Callable[[T, Sequence[TElement]], FailableValue]]

_MakeMetric = Callable[[str], TMetricB]

_GetElements = Callable[[T], Failable[Sequence[TElement]]]
_GetValueForElement = Callable[[T, Sequence[TElement], int], FailableValue]
_GetFloatValueForElement = Callable[[T, Sequence[TElement], int], FailableFloat]

_Getter = Callable[[T], FailableValue]
_IntGetter = Callable[[T], FailableInt]
_FloatGetter = Callable[[T], FailableFloat]


def get_aggregate_stats(
    aggregate_metric_cls: Type[TNamedMetric],
    get_elements: _GetElements[T, TElement],
    element_metric_to_get_value: Mapping[
        TMetric, Callable[[T, Sequence[TElement], int], FailableValue]
    ],
    additional_filters: Mapping[str, Filter[T, TElement]] = empty_mapping(),
    additional_aggregates: _AdditionalAggregates[T, TElement] = empty_mapping(),
) -> Mapping[TNamedMetric, _Getter[T]]:
    def make_metric(name: str) -> TNamedMetric:
        return aggregate_metric_cls(name, is_aggregate=True)

    filters: Mapping[str, Filter[T, TElement]] = combine_mappings(
        {"": get_elements},
        additional_filters,
        make_mapping(
            flt
            for element_metric, get_value_for_element in element_metric_to_get_value.items()
            for flt in _get_filters(element_metric, get_elements, get_value_for_element)
        ),
    )

    a = make_mapping(
        pair
        for element_metric, get_value_for_element in element_metric_to_get_value.items()
        for pair in _get_for_element_metric(
            element_metric, get_value_for_element, filters, make_metric
        )
    )
    b = make_mapping(_get_for_special_aggregates(filters, additional_aggregates, make_metric))
    return combine_mappings(a, b)


def _get_filters(
    element_metric: TMetric,
    get_elements: _GetElements[T, TElement],
    get_value_for_element: Callable[[T, Sequence[TElement], int], FailableValue],
) -> Iterable[Tuple[str, Filter[T, TElement]]]:
    if element_metric.type == MetricType.bool:

        def flt(t: T) -> Failable[Sequence[TElement]]:
            def g(elements: Sequence[TElement]) -> Failable[Sequence[TElement]]:
                out: List[TElement] = []
                for i, em in enumerate(elements):
                    b = get_value_for_element(t, elements, i)
                    if b.is_err():
                        return Err(as_err(b))
                    elif check_cast(bool, b.unwrap()):
                        out.append(em)
                return Ok(out)

            return flat_map_ok(get_elements(t), g)

        yield element_metric.name, flt


def _get_for_element_metric(
    element_metric: TMetric,
    get_value_for_element: _GetValueForElement[T, TElement],
    filters: _Filters[T, TElement],
    make_metric: _MakeMetric[TMetricB],
) -> Iterable[Tuple[TMetricB, _Getter[T]]]:
    if element_metric.type == MetricType.bool:
        for filter_name, flt in filters.items():
            get_count, get_pct = _get_get_count_get_pct(get_value_for_element, flt)
            base_name = (
                element_metric.name
                if filter_name == ""
                else f"{element_metric.name}Where{filter_name}"
            )
            yield make_metric(f"Count{base_name}"), get_count
            yield make_metric(f"Pct{base_name}"), get_pct
    elif element_metric.type == MetricType.float:
        for filter_name, flt in filters.items():
            for aggregate_name, get_aggregate in AGGREGATE_FLOAT_STATISTICS.items():
                base_name = f"{element_metric.name}_{aggregate_name}"
                name = base_name if filter_name == "" else f"{base_name}Where{filter_name}"
                yield make_metric(name), _getter_for_aggregate_value(
                    get_value_for_element, get_aggregate, flt
                )
    else:
        raise Exception(element_metric.type)


def _get_get_count_get_pct(
    get_value_for_element: _GetValueForElement[T, TElement], flt: Filter[T, TElement]
) -> Tuple[_IntGetter[T], _FloatGetter[T]]:
    def get_count_from_elements(t: T, elements: Sequence[TElement]) -> FailableInt:
        count = 0
        for i in indices(elements):
            v = get_value_for_element(t, elements, i)
            if v.is_ok():
                if check_cast(bool, v.value):
                    count += 1
            else:
                return Err(as_err(v))
        return Ok(count)

    def get_count(t: T) -> FailableInt:
        elements = flt(t)
        return flat_map_ok(elements, lambda ems: get_count_from_elements(t, ems))

    def get_pct(t: T) -> FailableFloat:
        def f(elements: Sequence[TElement]) -> FailableFloat:
            count = get_count_from_elements(t, elements)
            total_count = len(elements)
            if total_count == 0:
                return Err("Can't get pct with no elements")
            else:
                return map_ok(count, lambda cnt: get_percent(cnt / total_count))

        return flat_map_ok(flt(t), f)

    return get_count, get_pct


def _getter_for_aggregate_value(
    get_value_for_element: _GetFloatValueForElement[T, TElement],
    get_aggregate: Callable[[Sequence[float]], FailableFloat],
    flt: Filter[T, TElement],
) -> Callable[[T], FailableFloat]:
    def f(t: T) -> FailableFloat:
        return flat_map_ok(
            flt(t),
            lambda filtered_elements: flat_map_ok(
                all_non_err(
                    [
                        get_value_for_element(t, filtered_elements, i)
                        for i in indices(filtered_elements)
                    ]
                ),
                get_aggregate,
            ),
        )

    return f


def _getter_for_special_aggregate(
    flt: Filter[T, TElement], get_aggregate: Callable[[T, Sequence[TElement]], FailableFloat]
) -> Callable[[T], FailableFloat]:
    return lambda t: flat_map_ok(
        flt(t), lambda filtered_elements: get_aggregate(t, filtered_elements)
    )


def _get_for_special_aggregates(
    filters: _Filters[T, TElement],
    additional_aggregates: _AdditionalAggregates[T, TElement],
    make_metric: _MakeMetric[TMetricB],
) -> Iterable[Tuple[TMetricB, _FloatGetter[T]]]:
    for filter_name, flt in filters.items():
        for aggregate_name, get_aggregate in additional_aggregates.items():
            name = aggregate_name if filter_name == "" else f"{aggregate_name}_Where{filter_name}"
            yield make_metric(name), _getter_for_special_aggregate(flt, get_aggregate)


def _fail_if_empty(
    cb: Callable[[Sequence[float]], float]
) -> Callable[[Sequence[float]], FailableFloat]:
    return lambda xs: Err(f"<no values>") if is_empty(xs) else Ok(cb(xs))


def _stdev(values: Sequence[float]) -> FailableFloat:
    if len(values) <= 1:
        return Err("Not enough values for stdev")
    else:
        return Ok(stdev(values))


AGGREGATE_FLOAT_STATISTICS: Mapping[str, Callable[[Sequence[float]], FailableFloat]] = {
    "Mean": _fail_if_empty(mean),
    "Max": _fail_if_empty(max),
    "Min": _fail_if_empty(min),
    "Sum": fn_to_ok(sum),
    "95P": get_95th_percentile,
    "Stdev": _stdev,
}
