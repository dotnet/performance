from typing import Mapping, Optional, Sequence, Tuple, Union

from .axes._subplots import Color, SubplotBase

# If 'squeeze' is true, return type depends on the values of 'nrows' and 'ncols'
def subplots(
    nrows: int,
    ncols: int,
    figsize: Tuple[float, float] = (8, 6),
    squeeze: bool = True,
    gridspec_kw: Optional[Mapping[str, object]] = None,
) -> Tuple[Figure, Union[SubplotBase, Sequence[SubplotBase], Sequence[Sequence[SubplotBase]]]]: ...

class Figure:
    def add_subplot(self, nrows: int, ncols: int, index: int) -> SubplotBase:
        pass
    def suptitle(self, s: str) -> None: ...

def figure() -> Figure: ...
def title(s: str) -> None: ...
def xlabel(s: str) -> None: ...
def ylabel(s: str) -> None: ...
def xticks(ticks: Sequence[float], labels: Sequence[str]) -> None: ...
def show() -> None: ...
def plot(
    x_data: Sequence[float],
    y_data: Sequence[float],
    marker: str = "",
    linestyle: str = "",
    color: Color = "",
    label: str = "",
) -> None: ...
def legend(
    loc: str = "best",
    bbox_to_anchor: Optional[Tuple[float, float]] = None,
    fancybox: bool = False,
    shadow: bool = False,
) -> None: ...
def tight_layout() -> None: ...
def savefig(
    file_name: str, bbox_inches: Optional[str] = None, pad_inches: Optional[int] = None
) -> None: ...
def subplots_adjust(
    left: Optional[float] = None,
    bottom: Optional[float] = None,
    right: Optional[float] = None,
    top: Optional[float] = None,
    wspace: Optional[float] = None,
    hspace: Optional[float] = None,
) -> None: ...
