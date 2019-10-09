# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from typing import Callable, cast, Iterable, Optional, Sequence, Union

from result import Err, Ok, Result

from .type_utils import E, T, U, V, W


def all_non_err(xs: Iterable[Result[E, T]]) -> Result[E, Sequence[T]]:
    out = []
    for x in xs:
        if x.is_err():
            return Err(as_err(x))
        else:
            out.append(x.unwrap())
    return Ok(out)


def as_err(r: Result[E, T]) -> E:
    assert r.is_err()
    return cast(E, r.value)


def unwrap(r: Result[E, T]) -> T:
    if r.is_ok():
        return r.unwrap()
    else:
        err = as_err(r)
        if isinstance(err, BaseException):
            raise err
        else:
            raise Exception(str(err))


def ignore_err(r: Result[E, T]) -> Optional[T]:
    return match(r, lambda x: x, lambda _: None)


def map_ok(r: Result[E, T], cb: Callable[[T], U]) -> Result[E, U]:
    return flat_map_ok(r, lambda t: Ok(cb(t)))


def map_ok_2(a: Result[E, T], b: Result[E, U], cb: Callable[[T, U], V]) -> Result[E, V]:
    return flat_map_ok(a, lambda a_ok: map_ok(b, lambda b_ok: cb(a_ok, b_ok)))


def option_to_result(o: Optional[T], cb: Callable[[], E]) -> Result[E, T]:
    if o is None:
        return Err(cb())
    else:
        return Ok(o)


def flat_map_ok(r: Result[E, T], cb: Callable[[T], Result[E, U]]) -> Result[E, U]:
    return match(r, cb, Err)


# Giving cb_ok and cb_err two different return types due to
# https://github.com/python/mypy/issues/6898#issuecomment-521063239
def match(r: Result[E, T], cb_ok: Callable[[T], U], cb_err: Callable[[E], V]) -> Union[U, V]:
    if r.is_ok():
        return cb_ok(r.unwrap())
    else:
        return cb_err(as_err(r))


def fn_to_ok(f: Callable[[T], U]) -> Callable[[T], Result[E, U]]:
    return lambda t: Ok(f(t))


def fn2_to_ok(f: Callable[[T, U], V]) -> Callable[[T, U], Result[E, V]]:
    return lambda t, u: Ok(f(t, u))


def fn3_to_ok(f: Callable[[T, U, V], W]) -> Callable[[T, U, V], Result[E, W]]:
    return lambda t, u, v: Ok(f(t, u, v))
