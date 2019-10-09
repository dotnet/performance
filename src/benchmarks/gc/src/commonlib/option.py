# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from typing import Callable, Iterable, Optional, TypeVar, Union

T = TypeVar("T")
U = TypeVar("U")
V = TypeVar("V")


def non_null(x: Optional[T], assert_msg: Union[None, str, Callable[[], str]] = None) -> T:
    if x is None:
        raise Exception(
            "Expected value to not be None"
            if assert_msg is None
            else assert_msg
            if isinstance(assert_msg, str)
            else assert_msg()
        )
    return x


def map_option(t: Optional[T], cb: Callable[[T], U]) -> Optional[U]:
    return None if t is None else cb(t)


def map_option_2(t: Optional[T], u: Optional[U], cb: Callable[[T, U], V]) -> Optional[V]:
    return None if t is None or u is None else cb(t, u)


def option_or(a: Optional[T], _default: T) -> T:
    return _default if a is None else a


def option_or_3(a: Optional[T], b: Optional[T], _default: T) -> T:
    return option_or(a, option_or(b, _default))


def optional_to_iter(o: Optional[T]) -> Iterable[T]:
    # return () if o is None else (o,)
    if o is None:
        return ()
    else:
        return (o,)
