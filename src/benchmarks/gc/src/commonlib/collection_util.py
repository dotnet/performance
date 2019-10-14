# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from enum import Enum
from functools import partial
from operator import eq
from typing import (
    Callable,
    cast,
    Dict,
    FrozenSet,
    Generic,
    Iterable,
    List,
    Mapping,
    Optional,
    Sequence,
    Set,
    Tuple,
    Union,
)

from .frozen_dict import FrozenDict
from .option import optional_to_iter
from .type_utils import K, T, U, V, with_slots


def empty_mapping() -> FrozenDict[K, V]:
    return FrozenDict()


def empty_sequence() -> Sequence[T]:
    return ()


def empty_set() -> FrozenSet[T]:
    return frozenset()


# mapping


def add(d: Dict[K, V], k: K, v: V) -> None:
    assert k not in d, f"Duplicate key {k}"
    d[k] = v


def combine_mappings(*dicts: Mapping[K, V]) -> Mapping[K, V]:
    return make_mapping(pair for d in dicts for pair in d.items())


def group_by(values: Sequence[V], get_k: Callable[[V], K]) -> Mapping[K, Sequence[V]]:
    return make_multi_mapping((get_k(value), value) for value in values)


def map_mapping_keys(f: Callable[[K], U], m: Mapping[K, V]) -> Mapping[U, V]:
    return make_mapping((f(k), v) for k, v in m.items())


def map_mapping_values(f: Callable[[V], U], m: Mapping[K, V]) -> Mapping[K, U]:
    return make_mapping((k, f(v)) for k, v in m.items())


def map_mapping(f: Callable[[K, V], Tuple[T, U]], m: Mapping[K, V]) -> Mapping[T, U]:
    return make_mapping(f(k, v) for k, v in m.items())


def make_mapping(pairs: Iterable[Tuple[K, V]]) -> Mapping[K, V]:
    out: Dict[K, V] = {}
    for k, v in pairs:
        add(out, k, v)
    return out


def map_to_mapping(keys: Iterable[K], get_value: Callable[[K], V]) -> Mapping[K, V]:
    return make_mapping((k, get_value(k)) for k in keys)


def map_to_mapping_optional(
    keys: Iterable[K], get_value: Callable[[K], Optional[V]]
) -> Mapping[K, V]:
    return make_mapping((k, v) for k in keys for v in optional_to_iter(get_value(k)))


def items_sorted_by_key(d: Mapping[K, V]) -> Iterable[Tuple[K, V]]:
    return ((k, d[k]) for k in sorted(d.keys()))


def make_multi_mapping(pairs: Iterable[Tuple[K, V]]) -> Mapping[K, Sequence[V]]:
    out: Dict[K, List[V]] = {}
    for k, v in pairs:
        out.setdefault(k, []).append(v)
    return out


def invert_multi_mapping(m: Mapping[K, Sequence[V]]) -> Mapping[V, K]:
    out: Dict[V, K] = {}
    for k, vs in m.items():
        for v in vs:
            add(out, v, k)
    return out


def optional_mapping(key: K, value: Optional[V]) -> Mapping[K, V]:
    return empty_mapping() if value is None else {key: value}


# sequence


def filter_together(
    a: Iterable[T], b: Iterable[U], keep_if: Callable[[T, U], bool]
) -> Tuple[Sequence[T], Sequence[U]]:
    out_a: List[T] = []
    out_b: List[U] = []
    for x, y in zip_check(a, b):
        if keep_if(x, y):
            out_a.append(x)
            out_b.append(y)
    return out_a, out_b


def indices(s: Sequence[T]) -> Sequence[int]:
    return range(len(s))


def index_of_max(s: Sequence[T]) -> int:
    return max(indices(s), key=lambda i: s[i])


def is_empty(l: Union[FrozenSet[T], Mapping[K, V], Sequence[T]]) -> bool:
    return len(l) == 0


def map_non_null_together(
    ts: Iterable[T], get_u: Callable[[T], Optional[U]], get_v: Callable[[T], Optional[V]]
) -> Tuple[Sequence[U], Sequence[V]]:
    def get_u_v(t: T) -> Optional[Tuple[U, V]]:
        u = get_u(t)
        v = get_v(t)
        return None if u is None or v is None else (u, v)

    return unzip(uv for t in ts for uv in optional_to_iter(get_u_v(t)))


def repeat_list(l: Sequence[str], times: int) -> Sequence[str]:
    return cast(List[str], l) * times


def sort_high_to_low(l: Iterable[T], compare_by: Callable[[T], float]) -> Sequence[T]:
    return list(reversed(sorted(l, key=compare_by)))


def split_once(xs: Sequence[T], cb: Callable[[T], bool]) -> Tuple[Sequence[T], Sequence[T]]:
    idx = None
    for i, x in enumerate(xs):
        if cb(x):
            assert idx is None, "Two (or more) elements match the predicate"
            idx = i
    assert idx is not None, "Did not find anything matching the predicate"
    return xs[:idx], xs[idx + 1 :]


def split(xs: Sequence[T], cb: Callable[[T], bool]) -> Sequence[Sequence[T]]:
    out: List[List[T]] = [[]]
    for x in xs:
        if cb(x):
            out.append([])
        else:
            out[-1].append(x)
    return out


def split_and_keep_splitter(
    xs: Iterable[T], cb: Callable[[T], Optional[U]]
) -> Tuple[Sequence[T], Sequence[Tuple[U, Sequence[T]]]]:
    before_first_splitter: List[T] = []
    split_groups: List[Tuple[U, Sequence[T]]] = []

    cur_group: Optional[Tuple[U, List[T]]] = None

    for x in xs:
        u = cb(x)
        if u is None:
            if cur_group is None:
                before_first_splitter.append(x)
            else:
                cur_group[1].append(x)
        else:
            if cur_group is not None:
                split_groups.append(cur_group)
            cur_group = u, []

    if cur_group is not None:
        split_groups.append(cur_group)

    return before_first_splitter, split_groups


def cat_unique(*seqs: Iterable[T]) -> Sequence[T]:
    return unique_preserve_order(flatten(seqs))


def unique(s: Sequence[T]) -> Sequence[T]:
    out = []
    prev = None
    for x in sorted(s):
        assert x is not None
        if x != prev:
            out.append(x)
            prev = x
    return out


def unique_preserve_order(s: Iterable[T]) -> Sequence[T]:
    seen: Set[T] = set()
    res: List[T] = []
    for x in s:
        if x not in seen:
            seen.add(x)
            res.append(x)
    return res


def unzip(pairs: Iterable[Tuple[T, U]]) -> Tuple[Sequence[T], Sequence[U]]:
    res = tuple(zip(*pairs))
    if is_empty(res):
        return (), ()
    else:
        assert len(res) == 2
        return cast(Tuple[Sequence[T], Sequence[U]], res)


def unzip3(triplets: Iterable[Tuple[T, U, V]]) -> Tuple[Sequence[T], Sequence[U], Sequence[V]]:
    return cast(Tuple[Sequence[T], Sequence[U], Sequence[V]], tuple(zip(*triplets)))


# iterable


def count(i: Iterable[Tuple[()]]) -> int:
    assert not isinstance(i, list) or isinstance(i, tuple)
    return sum(1 for _ in i)


def repeat(value: T, times: int) -> Sequence[T]:
    assert times >= 0
    return [value for _ in range(times)]


def find_common(get_u: Callable[[T], U], ts: Sequence[T]) -> Optional[U]:
    """
    Returns the result of `get_u` if it is the same for all `ts`.
    (Returns `None` on empty input.)
    """
    res = None
    for t in ts:
        u = get_u(t)
        if res is None:
            res = u
        elif res != u:
            return None
    return res


def flatten(s: Iterable[Iterable[T]]) -> Iterable[T]:
    return (x for sub in s for x in sub)


def find_index(predicate: Callable[[T], bool], sequence: Sequence[T]) -> Optional[int]:
    for index, x in enumerate(sequence):
        if predicate(x):
            return index
    return None


def find(predicate: Callable[[T], bool], sequence: Iterable[T]) -> Optional[T]:
    for x in sequence:
        if predicate(x):
            return x
    return None


def find_only_matching(
    property_to_match: Callable[[T], U],
    expected: U,
    seq: Sequence[T],
    show: Callable[[T], str] = str,
) -> T:
    res = try_find_only(lambda x: property_to_match(x) == expected, seq)
    if not isinstance(res, TryFindOnlyFailure):
        return res
    elif res == TryFindOnlyFailure.NotFound:
        available = [f"{show(x)} -- {property_to_match(x)}" for x in seq]
        raise Exception(f"No item matches {expected}. Available: {available}")
    elif res == TryFindOnlyFailure.MoreThanOne:
        matches = [show(x) for x in seq if property_to_match(x) == expected]
        raise Exception(f"More than one item matches {expected}: {matches}")
    else:
        raise Exception(res)


def find_only_or_only_matching(
    property_to_match: Callable[[T], U], property_name: str, expected: Optional[U], seq: Sequence[T]
) -> T:
    if expected is None:
        seq = tuple(seq)
        if len(seq) == 1:
            return seq[0]
        else:
            raise Exception(
                f"Must provide {property_name}. Available: {[property_to_match(x) for x in seq]}"
            )
    else:
        return find_only_matching(property_to_match, expected, seq)


class TryFindOnlyFailure(Enum):
    NotFound = 0
    MoreThanOne = 1


def find_only(
    predicate: Callable[[T], bool],
    seq: Sequence[T],
    show: Callable[[T], str],
    show_predicate: Callable[[], str],
) -> T:
    res = find_only_or_none(predicate, seq, show, show_predicate)
    assert res is not None, f"No item matches {show_predicate()} in {[show(x) for x in seq]}"
    return res


def find_only_or_none(
    predicate: Callable[[T], bool],
    seq: Sequence[T],
    show: Callable[[T], str],
    showPredicate: Callable[[], str],
) -> Optional[T]:
    res = try_find_only(predicate, seq)
    if not isinstance(res, TryFindOnlyFailure):
        return res
    elif res == TryFindOnlyFailure.NotFound:
        return None
    elif res == TryFindOnlyFailure.MoreThanOne:
        raise Exception(
            f"Multiple items match {showPredicate()}: {[show(x) for x in seq if predicate(x)]}"
        )
    else:
        raise Exception(res)


def try_find_only(predicate: Callable[[T], bool], seq: Iterable[T]) -> Union[TryFindOnlyFailure, T]:
    res = None
    for s in seq:
        assert s is not None
        if predicate(s):
            if res is not None:
                return TryFindOnlyFailure.MoreThanOne
            res = s
    return TryFindOnlyFailure.NotFound if res is None else res


def partition(predicate: Callable[[T], bool], i: Iterable[T]) -> Tuple[Sequence[T], Sequence[T]]:
    """Returns (values where preditace is true, values where predicate is false)"""
    a: List[T] = []
    b: List[T] = []
    for x in i:
        (a if predicate(x) else b).append(x)
    return a, b


@with_slots
@dataclass(frozen=True)
class FloatRange:
    min: float
    # Inclusive
    max: float

    def __post_init__(self) -> None:
        assert self.min <= self.max

    def to_pair(self) -> Tuple[float, float]:
        return self.min, self.max


@with_slots
@dataclass(frozen=True)
class XYRanges:
    x_range: FloatRange
    y_range: FloatRange

    @property
    def x_min(self) -> float:
        return self.x_range.min

    @property
    def x_max(self) -> float:
        return self.x_range.max

    @property
    def y_min(self) -> float:
        return self.y_range.min

    @property
    def y_max(self) -> float:
        return self.y_range.max


def identity(x: T) -> T:
    return x


def min_max_float(values: Iterable[float]) -> Optional[FloatRange]:
    mm = min_max(values, identity)
    return None if mm is None else FloatRange(*mm)


def min_max(values: Iterable[T], key: Callable[[T], float]) -> Optional[Tuple[T, T]]:
    itr = iter(values)
    try:
        mn = next(itr)
    except StopIteration:
        return None
    mn_key = key(mn)
    mx = mn
    mx_key = mn_key
    while True:
        try:
            v = next(itr)
            k = key(v)
            if k < mn_key:
                mn = v
                mn_key = k
            elif k > mx_key:
                mx = v
                mx_key = k
        except StopIteration as _:
            break
    return mn, mx


def reverse(s: Iterable[T]) -> Sequence[T]:
    # Python's 'reversed' returns an iterator which is destroyed by using it
    return tuple(reversed(tuple(s)))


def with_is_last(a: Iterable[T]) -> Iterable[Tuple[bool, T]]:
    itr = iter(a)
    try:
        cur = next(itr)
    except StopIteration:
        pass
    else:
        while True:
            try:
                nxt = next(itr)
            except StopIteration:
                yield True, cur
                break
            else:
                yield False, cur
            cur = nxt


def zip_check(a: Iterable[T], b: Iterable[U]) -> Iterable[Tuple[T, U]]:
    """Checks that the iterables have the same length"""
    ia = iter(a)
    ib = iter(b)

    while True:
        try:
            na = next(ia)
        except StopIteration:
            try:
                next(ib)
            except StopIteration:
                break
            else:
                raise Exception("Second argument to zip_check is longer than the first argument")

        try:
            nb = next(ib)
        except StopIteration:
            raise Exception(
                "First argument to zip_check is longer than the second argument"
            ) from None

        yield (na, nb)


def zip_check_3(a: Iterable[T], b: Iterable[U], c: Iterable[V]) -> Iterable[Tuple[T, U, V]]:
    return ((a, *bc) for a, bc in zip_check(a, zip_check(b, c)))


def zip_shorten_former(a: Sequence[T], b: Sequence[U]) -> Iterable[Tuple[T, U]]:
    assert len(a) <= len(b)
    return zip(a, b)


def zip_with_is_first(xs: Sequence[T]) -> Iterable[Tuple[bool, T]]:
    is_first = True
    for x in xs:
        yield is_first, x
        is_first = False


class DequeWithSum:
    """
    Fixed-size deque that maintains a sum of its elements.
    """

    max_len: int
    values: List[float]
    index: int
    sum: float

    def __init__(self, max_len: int):
        assert max_len >= 1
        self.max_len = max_len
        self.values = [0] * max_len
        self.index = 0
        self.sum = 0

    def push(self, value: float) -> None:
        self.sum -= self.values[self.index]
        self.sum += value
        self.values[self.index] = value
        self.index += 1
        if self.index == self.max_len:
            self.index = 0

    def mean(self) -> float:
        return self.sum / self.max_len


@dataclass(frozen=True)
class Diff(Generic[T]):
    corresponding: Sequence[Tuple[T, T]]
    unique_to_a: Sequence[T]
    unique_to_b: Sequence[T]


def get_diff(a: Sequence[T], b: Sequence[T], equal: Callable[[T, T], bool] = eq) -> Diff[T]:
    al = list(a)
    bl = list(b)
    corresponding: List[Tuple[T, T]] = []
    unique_to_a: List[T] = []

    for ax in al:
        bi = find_index(partial(equal, ax), bl)
        if bi is None:
            unique_to_a.append(ax)
        else:
            corresponding.append((ax, b[bi]))
            del bl[bi]

    return Diff(corresponding=corresponding, unique_to_a=unique_to_a, unique_to_b=bl)


def try_index(seq: Sequence[T], value: T) -> Optional[int]:
    try:
        return seq.index(value)
    except ValueError:
        return None
