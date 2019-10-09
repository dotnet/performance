from typing import Optional, Tuple

class Machine:
    pass

class Key:
    pass

class Registry:
    pass

def ConnectRegistry(computer_name: Optional[str], key: Machine) -> Registry: ...

HKEY_LOCAL_MACHINE: Machine

def OpenKey(key: Registry, sub_key: str) -> Key: ...
def QueryValueEx(key: Key, sub_key: str) -> Tuple[object, int]: ...
