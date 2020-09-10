# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from __future__ import annotations  # PropPath circularly references itself
from collections import OrderedDict, Mapping as Mapping_ABC
from dataclasses import dataclass, fields, Field, MISSING
from enum import Enum
from inspect import isclass
from json import dumps
from pathlib import Path
from typing import Any, Callable, cast, Dict, Mapping, Optional, Sequence, Tuple, Type, List

from overrides import overrides
from result import Err, Ok, Result
from ruamel.yaml import dump, SafeDumper, safe_load, ScalarNode

from .collection_util import zip_check
from .frozen_dict import FrozenDict
from .option import non_null, optional_to_iter
from .result_utils import all_non_err, as_err, map_ok, map_ok_2, unwrap
from .type_utils import (
    check_cast,
    construct_class_from_fields,
    match_type,
    NO_DEFAULT,
    T,
    with_slots,
)


# Parse the first member to succeed; else return all failure messages
def try_for_each_union_member(
    members: Sequence[Type[Any]], try_get: Callable[[Type[Any]], Result[str, T]]
) -> Result[str, T]:
    for member in members:
        res = try_get(member)
        if res.is_ok():
            return Ok(res.unwrap())
    reasons = "\n".join(as_err(try_get(member)) for member in members)
    return Err(f"Failed on all union members. Failures:\n{reasons}")


SerializeMappings = Mapping[str, Callable[[Any], object]]


def to_serializable(value: object) -> object:
    if isinstance(value, Enum):
        return check_cast(str, getattr(value, "name"))
    elif any(isinstance(value, t) for t in [int, str, float, type(None)]):
        return value
    elif isinstance(value, Path):
        return str(value)
    elif isinstance(value, Mapping_ABC):
        # Use an OrderedDict to keep output pretty
        return OrderedDict(
            (k, s) for k, v in value.items() for s in optional_to_iter(to_serializable(v))
        )
    elif isinstance(value, (list, tuple)):
        return [to_serializable(v) for v in value]
    else:
        slots = cast(Sequence[str], value.__slots__)
        serialize_mappings: SerializeMappings = getattr(value, "serialize_mappings", lambda: {})()
        return OrderedDict(
            (k, serialized_value)
            for k in slots
            for v in (getattr(value, k),)
            for serialized_value in optional_to_iter(serialize_mappings.get(k, to_serializable)(v))
        )


def to_json(value: object) -> str:
    return dumps(to_serializable(value), indent=4)


def load_yaml(cls: Type[T], path: Path, all_optional: bool = False) -> T:
    """
    all_optional: Artificially treats all members of T as optional.
    """
    assert path.name.endswith(".yaml"), f"Expected {path} to be a '.yaml' file"
    try:
        with path.open() as f:
            return _yaml_to_typed(
                cls, safe_load(f), path, PropPath.root(f"load_yaml {cls}"), all_optional
            )
    except:
        print(f"Error in {path}")
        raise


def parse_yaml(cls: Type[T], s: str) -> T:
    return _yaml_to_typed(
        cls, safe_load(s), yaml_file_path=None, desc=PropPath.root(f"parse_yaml {cls}")
    )


@with_slots
@dataclass(frozen=True)
class PropPath:
    parent: Optional[PropPath]
    name: str

    @staticmethod
    def root(name: str) -> PropPath:
        return PropPath(None, name)

    def child(self, name: str) -> PropPath:
        return PropPath(self, name)

    def __str__(self) -> str:
        return self.name if self.parent is None else f"{self.parent}.{self.name}"


def _make_path_absolute(yaml_file_path: Optional[Path], path: str) -> Path:
    p = Path(path)
    if p.is_absolute():
        return p
    else:
        return non_null(yaml_file_path, f"Unexpected non-absolute path {p}").parent / p


# Simpler to use 'object' than 'T' for this private function or we'd need to cast everywhere.
def _try_yaml_to_typed(
    cls: Type[object],
    o: object,
    yaml_file_path: Optional[Path],
    desc: PropPath,
    all_optional: bool = False,
) -> Result[str, object]:
    generic_err: Result[str, object] = Err(f"{desc}: Expected a {cls}, got a {type(o)}")

    child = desc.child

    def handle_primitive(p: Type[Any]) -> Result[str, object]:
        if p in (bool, int, float, str):
            if p is float and isinstance(o, int):
                return Ok(float(o))
            elif isinstance(o, p):
                return Ok(o)
            else:
                return generic_err
        elif p is Path:
            if isinstance(o, str):
                return Ok(_make_path_absolute(yaml_file_path, o))
            else:
                return generic_err
        else:
            raise Exception(f"TODO: handle {p}")

    def handle_sequence(element_type: Type[Any]) -> Result[str, object]:
        # Use tuples instead of lists as only tuples are hashable
        if isinstance(o, str):
            # Allow wrapping a single str into Sequence[str] with one element
            # (do *not* treat each character as an element)
            return map_ok(
                _try_yaml_to_typed(element_type, o, yaml_file_path, child("<element>")),
                lambda s: (s,),
            )
        elif isinstance(o, list):
            return all_non_err(
                _try_yaml_to_typed(element_type, x, yaml_file_path, child("<element>")) for x in o
            )
        else:
            return generic_err

    def handle_tuple(element_types: Sequence[Type[object]]) -> Result[str, object]:
        if isinstance(o, list) and len(o) == len(element_types):
            return all_non_err(
                [
                    _try_yaml_to_typed(em_type, em, yaml_file_path, child(str(i)))
                    for i, (em_type, em) in enumerate(zip_check(element_types, o))
                ]
            )
        else:
            return generic_err

    def handle_mapping(key_type: Type[object], value_type: Type[object]) -> Result[str, object]:
        if isinstance(o, dict):

            def kv(k: object, v: object) -> Result[str, Tuple[object, object]]:
                return map_ok_2(
                    _try_yaml_to_typed(key_type, k, yaml_file_path, child("key")),
                    _try_yaml_to_typed(value_type, v, yaml_file_path, child("value")),
                    lambda k, v: (k, v),
                )

            return map_ok(all_non_err(kv(k, v) for k, v in o.items()), FrozenDict)
        else:
            return generic_err

    def handle_dataclass(_: Type[Any]) -> Result[str, object]:
        if isinstance(o, dict):
            # This should be a dataclass
            assert isclass(cls), f"Unexpected type {cls}"
            d = check_cast(dict, o)
            flds = fields(cls)
            for k in d.keys():
                assert any(
                    f.name == k for f in flds
                ), f"At {desc}: Unexpected field {k}, available: {tuple(f.name for f in flds)}"

            def _get_field(fld: Field[object]) -> Result[str, object]:
                if fld.name in d:
                    return _try_yaml_to_typed(
                        fld.type, d[fld.name], yaml_file_path, child(fld.name)
                    )
                else:
                    if all_optional:
                        return Ok(None)
                    else:
                        assert fld.default not in (
                            MISSING,
                            NO_DEFAULT,
                        ), f"At {desc}: Did not find field {fld.name} (and it has no default)"
                        return Ok(fld.default)

            return map_ok(
                all_non_err(_get_field(fld) for fld in flds),
                lambda field_values: construct_class_from_fields(cls, field_values),
            )
        else:
            return generic_err

    return match_type(
        cls,
        default_handler=lambda _: generic_err,
        handle_primitive=handle_primitive,
        handle_union=lambda members: try_for_each_union_member(
            members, lambda t: _try_yaml_to_typed(t, o, yaml_file_path, desc, all_optional)
        ),
        handle_enum=lambda members: try_get_enum_from_str(cls, members, o)
        if isinstance(o, str)
        else Err(f"Expected to get enum member as a string"),
        handle_sequence=handle_sequence,
        handle_tuple=handle_tuple,
        handle_mapping=handle_mapping,
        handle_dataclass=handle_dataclass,
    )


def _yaml_to_typed(
    cls: Type[T],
    o: object,
    yaml_file_path: Optional[Path],
    desc: PropPath,
    all_optional: bool = False,
) -> T:
    return cast(T, unwrap(_try_yaml_to_typed(cls, o, yaml_file_path, desc, all_optional)))


def try_get_enum_from_str(enum_cls: Type[T], enum_members: Sequence[str], s: str) -> Result[str, T]:
    try:
        return Ok(cast(T, cast(Any, enum_cls)[s]))
    except KeyError:
        return Err(f"Expected {enum_cls.__name__} to be one of: {tuple(enum_members)}, got {s}")


class MyDumper(SafeDumper):  # pylint:disable=too-many-ancestors
    # Default is to represent an ordereddict as a list of pairs
    # but we are just using OrderedDicts as pretty dicts
    @overrides
    # Note: dies if data is typed as OrderedDict (stackoverflow.com/questions/41207128)
    def represent_ordereddict(self, data: Dict[object, object]) -> object:
        self.sort_base_mapping_type_on_output = False
        return super().represent_dict(data)


# https://dustinoprea.com/2018/04/15/python-writing-hex-values-into-yaml/
class HexInt(int):
    pass


def _hex_representer(_: MyDumper, data: HexInt) -> ScalarNode:
    return ScalarNode("tag:yaml.org,2002:int", "0x{:04x}".format(data))


MyDumper.yaml_representers = {
    **MyDumper.yaml_representers,
    OrderedDict: MyDumper.represent_ordereddict,
    HexInt: _hex_representer,
}


def to_yaml(content: object) -> str:
    return dump(to_serializable(content), Dumper=MyDumper, default_flow_style=False)


def write_yaml_file(path: Path, content: object, overwrite: bool = False) -> None:
    if not overwrite:
        assert not path.exists(), f"{path} already exists, maybe you want '--overwrite'?"

    serializable = to_serializable(content)
    with path.open("w") as f:
        dump(serializable, f, Dumper=MyDumper, default_flow_style=False)


def _format_result_yaml_fields(content: object, indent_size: int, result: List[str]) -> None:
    if isinstance(content, OrderedDict):
        for k, v in content.items():
            if isinstance(v, Dict):
                text = f"{k}:\n"
                result.append("{field: >{width}}".format(field=text, width=len(text) + indent_size))
                _format_result_yaml_fields(v.items(), indent_size + 2, result)
            else:
                if k == "stdout":
                    v = v.rstrip("\n").replace("\n", "\n  ")
                    text = f'{k}:\n  "{v}"\n'
                else:
                    text = f"{k}: {v}\n"
                result.append("{field: >{width}}".format(field=text, width=len(text) + indent_size))


def write_test_yaml_file(path: Path, content: object) -> None:
    yaml_items: List[str] = []
    _format_result_yaml_fields(to_serializable(content), 0, yaml_items)

    with path.open("w") as f:
        f.writelines(yaml_items)
