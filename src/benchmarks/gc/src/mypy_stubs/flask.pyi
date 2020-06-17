from typing import Any, Callable, Dict, List, Mapping, Optional, Sequence, Union

AnyFunc = Callable[..., object]

class Flask:
    config: Dict[str, object]
    def __init__(self, name: str): ...
    def route(
        self, path: str, methods: Optional[Sequence[str]] = None
    ) -> Callable[[AnyFunc], AnyFunc]: ...
    def errorhandler(self, cls: Any) -> Callable[[AnyFunc], AnyFunc]: ...
    def run(self, port: int = 5000) -> None: ...

class Args(Dict[str, str]):
    def getlist(self, key: str) -> Sequence[str]: ...

class Request:
    args: Args
    form: Mapping[str, str]

class Response:
    status_code: int

def jsonify(d: Union[Sequence[Any], Mapping[str, Any]]) -> Response: ...
def send_from_directory(dir: str, path: str) -> Response: ...

request: Request
