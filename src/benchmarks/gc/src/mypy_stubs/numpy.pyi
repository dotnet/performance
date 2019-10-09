from typing import Sequence

def linspace(start: float, stop: float, num: int) -> Sequence[float]: ...

# Note: pct is out of 100, not a fraction
def percentile(data: Sequence[float], pct: float) -> float: ...

class ndarray:
    pass
