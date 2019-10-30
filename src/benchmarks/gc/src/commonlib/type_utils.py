# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from __future__ import annotations  # Allow subscripting Field

from collections.abc import Mapping as abc_Mapping, Sequence as abc_Sequence
from dataclasses import dataclass, field, fields, Field, is_dataclass
from enum import Enum
from functools import total_ordering
from inspect import isclass
from math import isnan
from pathlib import Path
from typing import Any, Callable, cast, Iterable, Mapping, Optional, Sequence, Type, TypeVar, Union

from .option import map_option, non_null, option_or


E = TypeVar("E")
F = TypeVar("F")
K = TypeVar("K")
V = TypeVar("V")
T = TypeVar("T")
U = TypeVar("U")
W = TypeVar("W")


def todo(reason: Optional[str] = None) -> T:
    raise Exception("TODO" if reason is None else f"TODO: {reason}")


def unindent_doc(doc: str) -> str:
    lines = doc.strip().split("\n")
    return "\n".join(line.strip() for line in lines)


# In addition to reducing space, __slots__ also helps us serialize in a consistent order
def with_slots(cls: Type[T]) -> Type[T]:
    cls_dict = dict(cls.__dict__)  # creates a copy
    slots = tuple(f.name for f in fields(cls))
    cls_dict["__slots__"] = slots
    for slot in slots:
        cls_dict.pop(slot, None)  # This will be in the cls_dict if the slot has a default
    if "__dict__" in cls_dict:
        del cls_dict["__dict__"]
    if "__weakref__" in cls_dict:
        del cls_dict["__weakref__"]
    res = cast(Type[T], type(cls.__name__, cls.__bases__, cls_dict))
    res.__qualname__ = getattr(cls, "__qualname__")
    return res


@with_slots
@dataclass(frozen=True)
class FieldInfo:
    doc: Optional[str]
    hidden: bool


def _get_field_info_name(field_name: str) -> str:
    return f"__field_info__{field_name}"


def doc_field(
    prop_name: str, doc: Optional[str], hidden: bool = False
) -> Callable[[Type[T]], Type[T]]:
    """
    Adds docstring associated with a property,
    which can be retrieved with `get_property_info`.
    """

    def f(cls: Type[T]) -> Type[T]:
        assert hasattr(cls, prop_name), f"Class {cls.__name__} has no such property {prop_name}"
        name = _get_field_info_name(prop_name)
        assert not hasattr(cls, name)
        setattr(cls, name, FieldInfo(doc, hidden))
        return cls

    return f


def get_field_info(cls: Type[Any], fld: Field[Any]) -> FieldInfo:
    return get_field_info_from_name(cls, fld.name)


def get_field_info_from_name(cls: Type[Any], fld_name: str) -> FieldInfo:
    try:
        return check_cast(FieldInfo, getattr(cls, _get_field_info_name(fld_name)))
    except AttributeError:
        raise Exception(f"You forgot to document the field {cls.__name__}#{fld_name}") from None


# Unlike dataclass.MISSING,
# python won't object if a field with default of _NO_DEFAULT follows one with a default.
# Normally you can just sort the default-less fields first,
# but if we inherit from another class, those fields always go first.
class _NO_DEFAULT:
    pass


NO_DEFAULT = _NO_DEFAULT()


# Shorthand for creating a field in an arguments dataclass.
def argument(
    doc: str, default: Any = NO_DEFAULT, hidden: bool = False, name_optional: bool = False
) -> Any:
    return field(
        default=default,
        metadata={
            "hidden": hidden,
            "doc": map_option(doc, unindent_doc),
            "name_optional": name_optional,
        },
    )


@with_slots
@dataclass(frozen=True)
class ArgumentFieldInfo:
    hidden: bool
    # TODO: non-optional
    doc: Optional[str]
    name_optional: bool


def get_field_argument_info(fld: Field[Any]) -> ArgumentFieldInfo:
    return non_null(try_get_field_argument_info(fld))


def try_get_field_argument_info(fld: Field[Any]) -> Optional[ArgumentFieldInfo]:
    md = fld.metadata
    if md == {}:
        return None
    else:
        return ArgumentFieldInfo(
            hidden=md["hidden"], doc=md["doc"], name_optional=md["name_optional"]
        )


def is_field_name_optional(fld: Field[Any]) -> bool:
    return get_field_argument_info(fld).name_optional


def is_a(o: object, t: Type[T]) -> bool:
    def instance(cls: Type[Any]) -> bool:
        return isinstance(o, cls)

    return match_type(
        t,
        default_handler=instance,
        # Allow ints to be treated as floats
        handle_primitive=lambda p: instance(p) or (p is float and isinstance(o, int)),
        handle_union=lambda u: any(is_a(o, x) for x in u),
        handle_sequence=lambda element_type: hasattr(o, "__iter__")
        and all(is_a(x, element_type) for x in cast(Sequence[object], o)),
        handle_tuple=lambda element_types: isinstance(o, tuple)
        and len(o) == len(element_types)
        and all(is_a(element, element_type) for element, element_type in zip(o, element_types)),
        handle_mapping=lambda key_type, value_type: hasattr(o, "items")
        and all(
            is_a(k, key_type) and is_a(v, value_type)
            for k, v in cast(Mapping[object, object], o).items()
        ),
    )


def check_cast(t: Type[T], o: object) -> T:
    assert is_a(o, t), f"Expected a {t}, got {o}"
    if t is float:
        assert not isnan(cast(float, o))
    return cast(T, o)


def _get_origin(t: Type[Any]) -> object:
    return cast(object, getattr(t, "__origin__", None))


def _try_extract_union_members(t: Type[Any]) -> Optional[Sequence[Type[Any]]]:
    return cast(Any, t).__args__ if _get_origin(t) is Union else None


# Returns whether it's an Optional, and returns the unwrapped type (or false and the original type)
def _try_extract_optional_type(t: Type[Any]) -> Optional[Type[Any]]:
    u = _try_extract_union_members(t)
    if u is not None:
        for i, member in enumerate(u):
            if member is type(None):
                return cast(Type[Any], Union[(*u[:i], *u[i + 1 :])])
    return None


def non_optional_type(t: Type[Any]) -> Type[Any]:
    return option_or(_try_extract_optional_type(t), t)


def construct_class_from_fields(cls: Type[T], field_values: Sequence[object]) -> T:
    return check_cast(cls, cast(Any, cls)(*field_values))


def match_type(
    t: Type[Any],
    default_handler: Callable[[Type[Any]], T],
    handle_primitive: Optional[Callable[[Type[Any]], T]] = None,
    handle_union: Optional[Callable[[Sequence[Type[Any]]], T]] = None,
    handle_enum: Optional[Callable[[Sequence[str]], T]] = None,
    handle_sequence: Optional[Callable[[Type[Any]], T]] = None,
    handle_tuple: Optional[Callable[[Sequence[Type[Any]]], T]] = None,
    handle_mapping: Optional[Callable[[Type[Any], Type[Any]], T]] = None,
    handle_dataclass: Optional[Callable[[Type[Any]], T]] = None,
) -> T:
    origin = _get_origin(t)

    union = _try_extract_union_members(t)
    if union is not None:
        return default_handler(t) if handle_union is None else handle_union(union)
    elif isclass(t) and issubclass(t, Enum):
        enum_members = cast(Sequence[str], tuple(t.__members__.keys()))
        return default_handler(t) if handle_enum is None else handle_enum(enum_members)
    elif origin is abc_Sequence:
        args = cast(Any, t).__args__
        assert len(args) == 1
        element_type = args[0]
        return default_handler(t) if handle_sequence is None else handle_sequence(element_type)
    elif origin is tuple:
        element_types = cast(Sequence[Type[Any]], cast(Any, t).__args__)
        return default_handler(t) if handle_tuple is None else handle_tuple(element_types)
    elif origin is abc_Mapping or abc_Mapping in getattr(origin, "__bases__", ()):
        args = cast(Any, t).__args__
        assert len(args) == 2
        return default_handler(t) if handle_mapping is None else handle_mapping(*args)
    elif t in (bool, int, float, type(None), Path, str):
        return default_handler(t) if handle_primitive is None else handle_primitive(t)
    elif is_dataclass(t):
        return default_handler(t) if handle_dataclass is None else handle_dataclass(t)
    else:
        return default_handler(t)


def show_type_for_command(t: Type[Any]) -> str:
    def handle_primitive(p: Type[Any]) -> str:
        txt = {
            bool: "'true' or 'false",
            float: "number",
            int: "integer",
            Path: "path",
            str: "any string",
        }.get(p)
        return todo(str(p)) if txt is None else txt

    def handle_union(u: Sequence[Type[Any]]) -> str:
        return "one of:\n" + "\n".join(show_type_for_command(t2) for t2 in u)

    def handle_enum(enum_members: Sequence[str]) -> str:
        return "one of:\n" + "\n".join(f"'{m}'" for m in enum_members)

    def handle_sequence(element_type: Type[Any]) -> str:
        return f"space-separated list of: {show_type_for_command(element_type)}"

    def handle_tuple(element_types: Sequence[Type[Any]]) -> str:
        assert len(element_types) == 2
        a, b = element_types
        assert a == b
        return f"two {show_type_for_command(a)} separated by a space"

    return match_type(
        t,
        default_handler=lambda cls: todo(str(cls)),
        handle_primitive=handle_primitive,
        handle_union=handle_union,
        handle_enum=handle_enum,
        handle_sequence=handle_sequence,
        handle_tuple=handle_tuple,
    )


# Extract out any union / mapping / etc. types, leaving just the classes
def iter_classes_in_type(t: Type[Any]) -> Iterable[Type[Any]]:
    drop = lambda _: ()
    flat: Callable[[Sequence[Type[Any]]], Iterable[Type[Any]]] = lambda types: (
        cls for t in types for cls in iter_classes_in_type(t)
    )

    return match_type(
        t,
        default_handler=lambda cls: todo(str(cls)),
        handle_primitive=drop,
        handle_union=flat,
        handle_enum=drop,
        handle_sequence=iter_classes_in_type,
        handle_tuple=flat,
        handle_mapping=lambda *kv: flat(kv),
        handle_dataclass=lambda cls: (cls,),
    )


TOrderedEnum = TypeVar("TOrderedEnum", bound="OrderedEnum")


@total_ordering
class OrderedEnum(Enum):
    def __lt__(self: TOrderedEnum, other: TOrderedEnum) -> bool:
        return check_cast(bool, self.value < other.value)

    def __gt__(self: TOrderedEnum, other: TOrderedEnum) -> bool:
        return check_cast(bool, self.value > other.value)

    def __le__(self: TOrderedEnum, other: TOrderedEnum) -> bool:
        return check_cast(bool, self.value <= other.value)

    def __ge__(self: TOrderedEnum, other: TOrderedEnum) -> bool:
        return check_cast(bool, self.value >= other.value)

    @classmethod
    def count(cls) -> int:
        c: Optional[int] = getattr(cls, "_MAX", None)
        if c is None:
            cnt = sum(1 for _ in cls)
            setattr(cls, "_MAX", cnt)
            return cnt
        else:
            return c


def enum_value(e: Enum) -> int:
    return check_cast(int, e.value)


def enum_count(e: Type[Enum]) -> int:
    xs = sorted(enum_value(x) for x in e)
    assert xs == list(range(len(xs))), "Enum values should be 0..N"
    return len(xs)


def combine_dataclasses_with_optional_fields(t: Type[T], a: T, b: Optional[T]) -> T:
    if b is None:
        return a
    else:

        def combiner(field_name: str, from_a: object, from_b: object) -> object:
            if from_a is None:
                return from_b
            elif from_b is None:
                return from_a
            else:
                raise Exception(f"Conflicting values for field '{field_name}'")

        return combine_dataclasses(t, a, b, combiner)


def combine_dataclasses(
    t: Type[T], a: T, b: T, combiner: Callable[[str, object, object], object]
) -> T:
    """
    Combines dataclass instances 'a' and 'b' by calling 'combiner' on corresponding fields.
    'combiner' should be a generic function (str, U, U) -> U
    """

    return t(*(combiner(f.name, getattr(a, f.name), getattr(b, f.name)) for f in fields(t)))
