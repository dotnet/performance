from typing import Iterable, Mapping, Optional, Sequence, Tuple, Union

from ..lines import Line2D

Color = Union[str, Tuple[float, float, float, float]]

class SubplotBase:
    def twinx(self) -> SubplotBase: ...
    def twiny(self) -> SubplotBase: ...
    def set_title(self, s: str) -> None: ...
    def set_xlabel(self, s: str) -> None: ...
    def set_ylabel(self, s: str) -> None: ...
    spines: Mapping[str, Spine]
    def plot(
        self,
        x_data: Sequence[float],
        y_data: Sequence[float],
        marker: str = "",
        linestyle: str = "",
        color: Color = "",
        label: str = "",
    ) -> Tuple[Line2D]: ...
    def errorbar(
        self,
        x: Sequence[float],
        y: Sequence[float],
        yerr: Tuple[Sequence[float], Sequence[float]],
        marker: str = "",
        linestyle: str = "",
        elinewidth: Optional[float] = None,
        color: Optional[Color] = None,
        ecolor: Optional[Color] = None,
        label: Optional[str] = None,
        capsize: float = 0,
    ) -> Sequence[Line2D]: ...
    def bar(
        self,
        x_values: Sequence[int],
        y_values: Sequence[float],
        yerr: Optional[Tuple[Sequence[float], Sequence[float]]] = None,
        color: Optional[Sequence[Color]] = None,
        width: Optional[float] = None,
    ) -> None: ...
    def hist(
        self, values: Sequence[float], bins: Optional[int] = None, label: Optional[str] = None
    ) -> None: ...
    def legend(
        self,
        handles: Optional[Sequence[Line2D]] = None,
        loc: Optional[str] = None,
        bbox_to_anchor: Optional[Tuple[float, float]] = None,
        labels: Optional[Iterable[str]] = None,
    ) -> None: ...
    def annotate(
        self,
        default: str,
        xy: Tuple[float, float],
        xytext: Tuple[float, float],
        arrowprops: Mapping[str, object],
    ) -> None: ...
    # Remember to call these after plotting, not before
    def set_xlim(self, left: Optional[float] = None, right: Optional[float] = None) -> None: ...
    def set_ylim(self, bottom: Optional[float] = None, top: Optional[float] = None) -> None: ...
    def get_xticks(self) -> Sequence[float]: ...
    def set_xticks(self, ticks: Sequence[float]) -> None: ...
    def get_xticklabels(self) -> Sequence[str]: ...
    def set_xticklabels(self, labels: Sequence[str]) -> None: ...
    def axvline(
        self,
        x: float,
        color: Optional[Color] = None,
        linewidth: Optional[float] = None,
        ymin: Optional[float] = None,
        ymax: Optional[float] = None,
    ) -> None: ...

class Spine:
    def set_position(self, pair: Tuple[str, int]) -> None: ...
