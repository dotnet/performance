# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass, fields, is_dataclass
from pathlib import Path
from typing import cast, Generic, Iterable, List, Optional, Sequence, Tuple, Type, Union
from xml.etree import ElementTree

from matplotlib.axes._subplots import SubplotBase
from matplotlib.cm import get_cmap
from matplotlib.lines import lineStyles, lineMarkers
import matplotlib.pyplot as plt

from ..commonlib.collection_util import flatten, indices, XYRanges, zip_check, zip_shorten_latter
from ..commonlib.option import map_option, option_or
from ..commonlib.type_utils import check_cast, T, with_slots
from ..commonlib.util import change_extension, ensure_dir, get_command_line, remove_str_end

# TODO: add more styles if necessary
# Solid, dashed with dots, dotted
LINE_STYLES: Sequence[str] = ["-", "-.", ":"]
assert all(l in lineStyles.keys() for l in LINE_STYLES)


def _get_marker_styles() -> Sequence[str]:
    first_four = (
        ".",
        "x",
        # triangle_up,
        "^",
        # pentagon
        "p",
    )
    assert all(m in lineMarkers.keys() for m in first_four)
    return (*first_four, *(m for m in lineMarkers if m not in first_four))


_MARKER_STYLES: Sequence[str] = _get_marker_styles()


def zip_with_marker_styles(s: Sequence[T]) -> Iterable[Tuple[T, str]]:
    assert len(s) <= len(_MARKER_STYLES), f"Drawing {len(s)} lines, need to add more marker styles"
    return zip_shorten_latter(s, _MARKER_STYLES)


Color = Tuple[float, float, float, float]


def get_colors(n: int) -> Sequence[Color]:
    cmap = get_cmap("rainbow")
    return [cmap(i) for i in _linspace(0.1, 0.9, n)]


def _linspace(lo: float, hi: float, n: int) -> Sequence[float]:
    """
    `n` evenly spaced numbers, from `lo` to `hi`, inclusive.
    """

    assert n >= 0
    if n == 0:
        return ()
    elif n == 1:
        return (lo,)
    else:
        diff = (hi - lo) / (n - 1)
        return [lo + diff * i for i in range(n)]


def zip_with_colors(s: Sequence[T]) -> Iterable[Tuple[T, Color]]:
    return zip_check(s, get_colors(len(s)))


# Be sure to call this *after* all `.plot()` calls, not before!
def set_axes(ax: SubplotBase, zero_x: bool, zero_y: bool, ranges: Optional[XYRanges]) -> None:
    if ranges is None:
        if zero_x:
            ax.set_xlim(left=0)
        if zero_y:
            ax.set_ylim(bottom=0)
    else:
        ax.set_xlim(left=0 if zero_x else ranges.x_min, right=ranges.x_max)
        # * 1.1 to ensure top y value is visible and not off the chart
        # TODO: The upper bound appears to be applying to *all* axes, not just this particular one.
        # ax.set_ylim(bottom=0 if zero_y else ranges.y_min, top=ranges.y_max * 1.1)
        ax.set_ylim(bottom=0)


OUT_SVG_DOC = "If set, write an SVG file to this path instead of displaying the chart in a window."


def show_or_save(out: Optional[Path], width_factor: Optional[float] = None) -> None:
    if out is None:
        plt.show()
    else:
        ensure_dir(out.parent)
        plt.savefig(str(out), bbox_inches="tight", pad_inches=0)
        if width_factor is not None:
            _fix_svg_width(out, out, width_factor)
        # Also write out the command used

        out_txt = change_extension(out, "txt")
        assert out_txt != out
        out_txt.write_text(get_command_line())
        print(f"Saved to {out}")


# Setting individual_figure_size too small leads to individual graphs overlapping each other.
def subplots(
    nplots: int, individual_figure_size: Tuple[float, float] = (4, 4)
) -> Tuple[plt.Figure, Sequence[SubplotBase]]:
    assert nplots > 0
    # TODO: more intelligently determine number of cols/rows, using individual_figure_size
    if nplots <= 4:
        ncols = nplots
        nrows = 1
    elif nplots <= 6:
        ncols = 3
        nrows = 2
        # If nplots == 5, last plot is unused
    elif nplots <= 8:
        ncols = 4
        nrows = 2
    elif nplots == 9:
        ncols = 3
        nrows = 3
    elif nplots <= 12:
        ncols = 4
        nrows = 3
    elif nplots <= 16:
        ncols = 4
        nrows = 4
    elif nplots == 48:
        ncols = 4
        nrows = 12
    else:
        raise Exception(f"TODO: layout for {nplots} plots")

    figsize = (ncols * individual_figure_size[0], nrows * individual_figure_size[1])
    fig, axes = plt.subplots(
        nrows=nrows, ncols=ncols, figsize=figsize, squeeze=False, gridspec_kw=dict(hspace=0.45)
    )
    return fig, tuple(flatten(cast(Sequence[Sequence[SubplotBase]], axes)))[:nplots]


def _fix_svg_width(svg: Path, out_path: Path, width_factor: float) -> None:
    ElementTree.register_namespace("", "http://www.w3.org/2000/svg")
    et = ElementTree.parse(check_cast(str, svg))  # This works with a Path, mypy doesn't realize
    root = et.getroot()
    width_str = remove_str_end(root.attrib["width"], "pt")
    width = float(width_str)
    width *= width_factor
    root.attrib["width"] = f"{width}pt"

    viewBox = root.attrib["viewBox"]
    l, r, w, h = viewBox.split(" ")
    assert w == width_str
    root.attrib["viewBox"] = f"{l} {r} {width} {h}"

    # This seems to break it. ElementTree fails to preserve a lot.
    # et.write(cast(str, out_path))  # This works with a Path
    # Instead, just replace the float and hope it's unique enough.
    assert len(width_str) > 6  # So unlikely to replace by accident
    out_path.write_text(svg.read_text().replace(width_str, str(width)))


@with_slots
@dataclass(frozen=True)
class BasicLine:
    xs: Sequence[float]
    ys: Sequence[float]
    name: Optional[str] = None

    @property
    def n_values(self) -> int:
        return len(self.xs)

    def __post_init__(self) -> None:
        assert len(self.xs) == len(
            self.ys
        ), f"Line has {len(self.xs)} x-values but {len(self.ys)} y-values"


@with_slots
@dataclass(frozen=True)
class BasicLineChart:
    lines: Sequence[BasicLine]
    name: Optional[str] = None
    x_label: Optional[str] = None
    y_label: Optional[str] = None

    @property
    def n_values(self) -> int:
        return self.lines[0].n_values

    def __post_init__(self) -> None:
        for line in self.lines:
            assert line.n_values == self.n_values, (
                f"{option_or(line.name, '<unnamed>')} has {line.n_values} data points, "
                + f"should have {self.n_values}"
            )


@with_slots
@dataclass(frozen=True)
class BasicHistogram:
    values: Sequence[float]
    name: Optional[str] = None
    x_label: Optional[str] = None
    bins: Optional[int] = None


AnyChart = Union[BasicLineChart, BasicHistogram]


def basic_chart(charts: Sequence[AnyChart]) -> None:
    _fig, axes = subplots(len(charts), individual_figure_size=(8, 4))
    for ax, chart in zip(axes, charts):
        map_option(chart.name, ax.set_title)
        map_option(chart.x_label, ax.set_xlabel)

        if isinstance(chart, BasicLineChart):
            map_option(chart.x_label, ax.set_xlabel)
            map_option(chart.y_label, ax.set_ylabel)
            for line, color in zip_with_colors(chart.lines):
                # line.name is Optional[str], this is fine although mypy thinks
                # it isn't.
                # TODO: customizable marker
                ax.plot(line.xs, line.ys, label=cast(str, line.name),
                        marker="*", color=color)
            if any(line.name is not None for line in chart.lines):
                ax.legend()
        else:
            ax.hist(chart.values, bins=option_or(chart.bins, 16))


@with_slots
@dataclass(frozen=True)
class Trace(Generic[T]):
    name: str
    data: Sequence[T]


def chart_lines_from_fields(
    t: Type[T],
    traces: Sequence[Trace[T]],
    y_property_names: Sequence[str],
    x_property_name: Optional[str] = None,
) -> None:
    _assert_is_property_name(t, x_property_name)
    for y in y_property_names:
        _assert_is_property_name(t, y)

    lines: List[BasicLine] = []
    for trace in traces:
        xs = _get_values(trace.data, x_property_name)
        for y_property_name in y_property_names:
            lines.append(
                BasicLine(
                    name=trace.name + " " + y_property_name,
                    xs=xs,
                    ys=_get_values(trace.data, y_property_name),
                )
            )

    chart = BasicLineChart(
        lines=lines,
        x_label=x_property_name,
        y_label=y_property_names[0] if len(y_property_names) == 1 else None,
    )
    basic_chart([chart])


def chart_histograms_from_fields(
    t: Type[T], gcs: Sequence[T], property_names: Sequence[str], bins: Optional[int] = None
) -> None:
    for name in property_names:
        _assert_is_property_name(t, name)

    def get_one_chart(property_name: str) -> BasicHistogram:
        return BasicHistogram(
            values=_get_values(gcs, property_name), x_label=property_name, bins=bins
        )

    basic_chart([get_one_chart(name) for name in property_names])


def chart_heaps(
    t: Type[T],
    heaps: Sequence[Sequence[T]],
    y_property_names: Sequence[str],
    # TODO: USE THIS
    heap_indices: Optional[Sequence[int]] = None,
    xs: Optional[Sequence[float]] = None,
    x_label: Optional[str] = None,
) -> None:
    for y_property_name in y_property_names:
        _assert_is_property_name(t, y_property_name)

    n_values = len(heaps[0])
    for i, hp in enumerate(heaps):
        assert (
            len(hp) == n_values
        ), f"All heaps should have {n_values} values, but heap {i} has {len(hp)}"

    real_heap_indices = option_or(heap_indices, indices(heaps))
    real_xs = option_or(xs, indices(heaps[0]))

    def get_one_chart(y_property_name: str) -> BasicLineChart:
        lines = [
            BasicLine(
                name=f"heap {heap_index}",
                xs=real_xs,
                ys=_get_values(heaps[heap_index], y_property_name),
            )
            for heap_index in real_heap_indices
        ]
        return BasicLineChart(lines=lines, x_label=x_label, y_label=y_property_name)

    basic_chart([get_one_chart(y) for y in y_property_names])


def _assert_is_property_name(t: Type[T], property_name: Optional[str]) -> None:
    assert property_name is None or _is_property_name(
        t, property_name
    ), f"Type '{t.__name__}' has no property '{property_name}'"


def _is_property_name(t: Type[T], property_name: str) -> bool:
    return (
        is_dataclass(t) and property_name in (f.name for f in fields(t)) or property_name in dir(t)
    )


def _get_values(trace: Sequence[object], property_name: Optional[str]) -> Sequence[float]:
    if property_name is None:
        return indices(trace)
    else:
        return [check_cast(float, getattr(obj, property_name)) for obj in trace]
