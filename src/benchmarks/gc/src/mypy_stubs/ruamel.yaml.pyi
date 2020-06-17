from typing import Optional, Mapping, Type

def safe_load(io: object) -> object: ...

# Note: only returns str if 'io' is None
def dump(
    content: object,
    io: Optional[object] = None,
    Dumper: Optional[Type[SafeDumper]] = None,
    default_flow_style: bool = True,
) -> str: ...

class ScalarNode:
    def __init__(self, tag: str, show: str): ...

class SafeDumper:
    def represent_dict(self, data: Mapping[object, object]) -> object: ...
    sort_base_mapping_type_on_output: bool
    yaml_representers: Mapping[Type[object], object]
