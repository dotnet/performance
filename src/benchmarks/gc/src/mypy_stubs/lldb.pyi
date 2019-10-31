from pathlib import Path

# Apparently lldb requires inputs to be of type List, not just any Sequence
from typing import Any, List, Optional

LLDB_INVALID_PROCESS_ID: int

class SBDebugger:
    @staticmethod
    def Create() -> SBDebugger: ...
    def GetCommandInterpreter(self) -> Any: ...
    def CreateTarget(self, path: str) -> SBTarget: ...

class SBError:
    pass

# https://lldb.llvm.org/python_reference/lldb.SBLaunchInfo-class.html
class SBLaunchInfo:
    def __init__(self, argv: List[str]): ...

class SBListener:
    pass

class SBTarget:
    def Launch(self, listener: Optional[SBListener], argv: List[str]) -> None:
        pass
