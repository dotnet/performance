from pathlib import Path
from typing import Callable

def parse_gitignore(path: Path) -> Callable[[Path], bool]: ...
