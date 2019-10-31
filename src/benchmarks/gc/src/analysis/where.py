# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from enum import Enum
from operator import eq, le, ge, lt, gt, ne
from typing import Any, Callable, cast, Generic, Mapping, Optional, Sequence, Tuple, Type, Union

from result import Result

from ..commonlib.collection_util import is_empty, split
from ..commonlib.result_utils import match
from ..commonlib.type_utils import T, with_slots

from .parse_metrics import parse_single_gc_metric_arg
from .types import ProcessedGC


@with_slots
@dataclass(frozen=True)
class EnumWhereMapping(Generic[T]):
    enum: Type[object]
    # Wrapping in a tuple due to https://github.com/python/mypy/issues/708
    get_value: Tuple[Callable[[T], object]]


WhereValue = Union[bool, float, str]

WhereMappingValue = Union[Callable[[T], Result[str, WhereValue]], EnumWhereMapping[T]]
WhereMapping = Mapping[str, WhereMappingValue[T]]


@with_slots
@dataclass(frozen=True)
class _ParsedWherePart:
    variable: str
    op: str
    value: str


def get_where_filter(
    where: Optional[Sequence[str]], mapping: WhereMapping[T]
) -> Callable[[T], bool]:
    p = _parse_where(where)

    def eval_part(part: _ParsedWherePart, t: T) -> bool:
        var = mapping[part.variable]
        if isinstance(var, EnumWhereMapping):
            l_value = var.get_value[0](t)
            r_value = cast(Enum, cast(Any, var.enum)[part.value])
            assert part.op == "="
            return l_value == r_value
        else:
            l_value = var(t)
            if l_value.is_ok():
                return _OPERATORS[part.op](l_value.value, _parse_where_value(part.value))
            else:
                # TODO: raise an exception here? This silently filters out failures
                return False

    return lambda t: _evaluate(p, lambda p: eval_part(p, t))


@with_slots
@dataclass(frozen=True)
class _ParsedWhere:
    # Inner sequences are combined with `and`, then those are combined with `or`.
    and_clauses: Sequence[Sequence[_ParsedWherePart]]


def _evaluate(where: _ParsedWhere, eval_part: Callable[[_ParsedWherePart], bool]) -> bool:
    return is_empty(where.and_clauses) or any(
        all(eval_part(part) for part in and_clause) for and_clause in where.and_clauses
    )


def _parse_where(where: Optional[Sequence[str]]) -> _ParsedWhere:
    if where is None:
        return _ParsedWhere(())
    else:
        return _ParsedWhere(
            [
                [_parse_where_part(w) for w in and_clause]
                for and_clause in split(where, lambda s: s == "or")
            ]
        )


def _parse_where_part(part: str) -> _ParsedWherePart:
    for op in _OPERATORS:
        if op in part:
            lr = part.split(op)
            l, r = lr
            return _ParsedWherePart(l, op, r)
    raise Exception(f"Did not find any operator in {part}")


def _parse_where_value(s_in: str) -> WhereValue:
    s = s_in.lower()
    if s == "true":
        return True
    elif s == "false":
        return False
    else:
        try:
            return float(s)
        except ValueError:
            return s


# Note: put longer operators first so we try parsing them first
# (and don't parse the '<' out of '<=')
_OPERATORS: Mapping[str, Callable[[WhereValue, WhereValue], bool]] = {
    "<=": le,
    ">=": ge,
    "!=": ne,
    "=": eq,
    "<": lt,
    ">": gt,
}


# Returns the metrics it will need to filter GCs.
def get_where_filter_for_gcs(where: Optional[Sequence[str]],) -> Callable[[ProcessedGC], bool]:
    p = _parse_where(where)

    def f(part: _ParsedWherePart, gc: ProcessedGC) -> bool:
        l_res = gc.metric(parse_single_gc_metric_arg(part.variable))
        # TODO: throw on result failure?
        return match(l_res, lambda l: _OPERATORS[part.op](l, float(part.value)), lambda _: False)

    return lambda gc: _evaluate(p, lambda part: f(part, gc))


def _get_where_doc_worker(keys: str) -> str:
    return f"""
    Read about 'where' syntax in `docs/commands_syntax.md`.
    
    Possible variables are: {keys}
    """


def get_where_doc(mapping: WhereMapping[T]) -> str:
    return _get_where_doc_worker(", ".join(mapping.keys()))


GC_WHERE_DOC = """
Read about 'where' syntax in `docs/commands_syntax.md`.

Possible variables are any single-gc metric. (See `docs/metrics.md` for a list.)
"""
