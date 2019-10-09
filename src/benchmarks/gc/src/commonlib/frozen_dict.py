# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from collections import Mapping as Mapping_ABC
from typing import AbstractSet, Dict, Generic, Iterable, Iterator, Mapping, Tuple, TypeVar, Union

K = TypeVar("K")
V = TypeVar("V")

# Like frozendict on pip, but generic so it can be used as a type.
class FrozenDict(Generic[K, V], Mapping[K, V]):
    _inner: Mapping[K, V]

    def __init__(self, items: Union[Mapping[K, V], Iterable[Tuple[K, V]]] = ()) -> None:
        inner: Dict[K, V] = {}
        iter_items = items.items() if isinstance(items, Mapping_ABC) else items
        for k, v in iter_items:
            assert k not in inner
            inner[k] = v
        self._inner = inner

    def __contains__(self, o: object) -> bool:
        return self._inner.__contains__(o)

    def __getitem__(self, key: K) -> V:
        return self._inner.__getitem__(key)

    def __iter__(self) -> Iterator[K]:
        return self._inner.__iter__()

    def __len__(self) -> int:
        return self._inner.__len__()

    def __repr__(self) -> str:
        return self._inner.__repr__()

    def __hash__(self) -> int:
        res = 0
        for key, value in self._inner.items():
            res ^= hash((key, value))
        return res

    def items(self) -> AbstractSet[Tuple[K, V]]:
        return self._inner.items()
