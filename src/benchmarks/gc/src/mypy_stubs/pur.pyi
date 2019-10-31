from typing import Mapping, Tuple
from typing_extensions import TypedDict

class Updated(TypedDict):
    message: str
    # It has many other properties too, not bothering to list them since we just use `message`

def update_requirements(input_file: str) -> Mapping[str, Tuple[Updated]]: ...
