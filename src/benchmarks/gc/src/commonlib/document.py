# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from collections.abc import Sequence as ABCSequence
from dataclasses import dataclass
from enum import Enum
from io import StringIO
from math import ceil, floor
from os import get_terminal_size, terminal_size
from pathlib import Path
from typing import Callable, cast, Iterable, Mapping, Optional, List, Sequence, Tuple, Union

from psutil import Process
from termcolor import colored
from xlsxwriter import Workbook
from yattag import Doc, indent as indent_xml
from yattag.simpledoc import SimpleDoc

from .collection_util import (
    empty_sequence,
    index_of_max,
    is_empty,
    try_index,
    with_is_last,
    zip_check,
    zip_check_3,
)
from .option import map_option, optional_to_iter, option_or
from .type_utils import argument, check_cast, with_slots
from .util import float_to_str, get_command_line, os_is_windows

Tag = SimpleDoc.Tag

# Parameters: text, bold, color
_ColorType = Callable[[str, bool, Optional[str]], str]


class Align(Enum):
    left = 0
    right = 1
    center = 2


CellValueSingle = Union[str, float, int, None]
CellValue = Union[CellValueSingle, Sequence[CellValueSingle]]


@with_slots
@dataclass(frozen=True)
class Cell:
    value: CellValue = None
    align: Align = Align.right
    color: Optional[str] = None
    bold: bool = False


@with_slots
@dataclass(frozen=True)
class HeaderGroup:
    text: str
    size_cells: int

    def __post_init__(self) -> None:
        assert self.size_cells > 0


Row = Sequence[Cell]


@with_slots
@dataclass(frozen=True)
class Table:
    name: Optional[str] = None
    text: Optional[str] = None
    headers: Optional[Sequence[str]] = None
    rows: Sequence[Row] = ()
    header_groups: Optional[Sequence[HeaderGroup]] = None

    @property
    def n_columns(self) -> int:
        if self.headers is not None:
            return len(self.headers)
        elif not is_empty(self.rows):
            return len(self.rows[0])
        else:
            return 0

    def __post_init__(self) -> None:
        n_columns = self.n_columns
        assert self.headers is None or len(self.headers) == n_columns
        assert (
            self.header_groups is None or sum(x.size_cells for x in self.header_groups) == n_columns
        )
        for row in self.rows:
            assert (
                len(row) == n_columns
            ), f"Row has {len(row)} entries but table has {n_columns} column headers"


@with_slots
@dataclass(frozen=True)
class Section:
    name: Optional[str] = None
    text: Optional[str] = None
    tables: Sequence[Table] = ()

    @property
    def n_columns(self) -> int:
        return 0 if is_empty(self.tables) else self.tables[0].n_columns

    def __post_init__(self) -> None:
        for table in self.tables:
            assert table.n_columns == self.n_columns


@with_slots
@dataclass(frozen=True)
class Document:
    comment: Optional[str] = None
    sections: Sequence[Section] = empty_sequence()


def single_table_document(table: Table) -> Document:
    return Document(sections=(Section(tables=(table,)),))


def _pad(width: int, s: str, align: Align) -> str:
    assert len(s) <= width, (
        f"Line '{s}' (len {len(s)}) is bigger than allowed width {width}.\n"
        + "(This is a bug in document.py)"
    )
    pad = width - len(s)
    switch: Mapping[Align, Callable[[], str]] = {
        Align.left: lambda: s + " " * pad,
        Align.right: lambda: " " * pad + s,
        Align.center: lambda: " " * floor(pad / 2) + s + " " * ceil(pad / 2),
    }
    return switch[align]()


def _shell_supports_color() -> bool:
    if os_is_windows():
        py = _get_py_process()
        parent = py.parent()
        if parent.name() in ("Code.exe", "jupyter-notebook.exe"):
            return True
        else:
            assert parent.name() in ("powershell.exe", "cmd.exe")
            shell = parent.parent()
            return {"ConEmuC64.exe": True, "explorer.exe": False}[shell.name()]
    else:
        return True


def _get_py_process() -> Process:
    p = Process()
    assert p.name() == "python.exe"
    par = p.parent()
    # In jupyter notebook in code, "python.exe" has "python.exe" as a parent, then "Code.exe"
    return par if par.name() in ("py.exe", "python.exe") else p


class SpecialOutputWidth(Enum):
    inf = 0


OutputWidth = Union[int, SpecialOutputWidth]


def print_document(
    doc: Document,
    max_width: Optional[OutputWidth] = None,
    table_indent: Optional[int] = None,
    color: Optional[bool] = False,
) -> None:
    # Add an additional newline in front
    # Because jupyter notebook likes removing leading spaces (messing up indent),
    # but wont' remove a leading newline.
    text = "\n" + _render_to_text(
        doc=doc,
        color=yes_color if option_or(color, _shell_supports_color()) else no_color,
        # Need a cast due to https://github.com/python/mypy/issues/6751
        max_width=cast(OutputWidth, option_or(max_width, _get_terminal_width())),
        op_table_indent=option_or(table_indent, 2),
    )
    # Avoid https://bugs.python.org/issue37871
    for line in text.split("\n"):
        print(line)


def _get_terminal_width() -> OutputWidth:
    try:
        term_size: Optional[terminal_size] = get_terminal_size()
    except OSError:
        term_size = None

    if term_size is None:
        return SpecialOutputWidth.inf
    else:
        # PowerShell wraps to a blank line if you fill in exactly the terminal width.
        # So reduce by 1.
        res = term_size.columns - 1
        assert 20 < res < 2000
        return res


def yes_color(text: str, bold: bool, color: Optional[str]) -> str:
    return colored(text, color, attrs=("bold",) if bold else ())


def no_color(text: str, _bold: bool, _color: Optional[str]) -> str:
    return text


def _render_to_plaintext(
    doc: Document, max_width: Optional[OutputWidth], table_indent: Optional[int]
) -> str:
    return _render_to_text(
        doc,
        no_color,
        # Need a cast due to https://github.com/python/mypy/issues/6751
        max_width=cast(OutputWidth, option_or(max_width, SpecialOutputWidth.inf)),
        op_table_indent=table_indent,
    )


def _render_to_excel(doc: Document, file_name: Path) -> None:
    """WARN: This is untested."""
    workbook = Workbook(str(file_name))
    worksheet = workbook.add_worksheet()

    row_index = 0

    def next_row() -> int:
        nonlocal row_index
        res = row_index
        row_index += 1
        return res

    if doc.comment is not None:
        raise Exception("TODO: render doc comment to excel")

    for section in doc.sections:
        next_row()
        if section.name is not None:
            worksheet.write(next_row(), 0, section.name)
        if section.text is not None:
            raise Exception("TODO: render section text to excel")
        next_row()

        for table in section.tables:
            if table.name is not None:
                worksheet.write(next_row(), 0, table.name)
            if table.text is not None:
                raise Exception("TODO: render table text to excel")
            assert table.header_groups is None
            if table.headers is not None:
                for i, header in enumerate(table.headers):
                    worksheet.write(row_index, i, header)
            next_row()
            for row in table.rows:
                for i, value in enumerate(row):
                    if value.color is not None or value.bold is not None:
                        raise Exception("TODO: render to excel with bold or colored cells")
                    worksheet.write(row_index, i, "" if value.value is None else value.value)

    workbook.close()


def _render_to_text(
    doc: Document, color: _ColorType, max_width: OutputWidth, op_table_indent: Optional[int]
) -> str:
    table_indent = option_or(op_table_indent, 2)

    out = StringIO()

    def write(s: str) -> None:
        out.write(s)

    def nl(n: int = 1) -> None:
        write("\n" * n)

    if doc.comment is not None:
        write(doc.comment)
        nl(2)

    for section in doc.sections:
        cell_sizes = _get_cell_sizes_for_section(
            section,
            max_table_width=max_width
            if isinstance(max_width, SpecialOutputWidth)
            else max_width - table_indent,
        )
        if section.name is not None:
            _write_in_box(write, section.name, total_width=_sum_cell_sizes(cell_sizes))
            nl(2)
        if section.text is not None:
            write(section.text)
            nl(2)
        for table in section.tables:
            _render_table_to_text(table, write, color, cell_sizes, table_indent)
            nl(2)
        nl()

    return out.getvalue()


Write = Callable[[str], None]


_VERTICAL_BAR = "│"
_HORIZONTAL_BAR = "─"
_HORIZONTAL_AND_VERTICAL_BAR = "┼"
_TOP_LEFT_CORNER = "┌"
_TOP_RIGHT_CORNER = "┐"
_BOTTOM_LEFT_CORNER = "└"
_BOTTOM_RIGHT_CORNER = "┘"


def _write_in_box(write: Write, s: str, total_width: int) -> None:
    width = _max_line_len(s) + 4
    pad = " " * ((total_width - width) // 2) if width < total_width else ""

    write(f"{pad}{_TOP_LEFT_CORNER}{_HORIZONTAL_BAR * (width - 2)}{_TOP_RIGHT_CORNER}\n")
    for line in _lines(s):
        write(f"{pad}{_VERTICAL_BAR} {_pad(width - 4, line, Align.left)} {_VERTICAL_BAR}\n")
    write(f"{pad}{_BOTTOM_LEFT_CORNER}{_HORIZONTAL_BAR * (width - 2)}{_BOTTOM_RIGHT_CORNER}\n")


_MIN_CELL_SIZE = 3
_SPACE_BETWEEN_COLUMNS = 3
assert _SPACE_BETWEEN_COLUMNS % 2 == 1
_HALF_SPACE_BETWEEN_COLUMNS = 1
assert _HALF_SPACE_BETWEEN_COLUMNS * 2 + 1 == _SPACE_BETWEEN_COLUMNS
_HALF_SPACE_BETWEEN_COLUMNS_STR = " " * _HALF_SPACE_BETWEEN_COLUMNS
_BETWEEN_COLUMNS_STR = (
    f"{_HALF_SPACE_BETWEEN_COLUMNS_STR}{_VERTICAL_BAR}{_HALF_SPACE_BETWEEN_COLUMNS_STR}"
)
_HALF_HORIZ = _HORIZONTAL_BAR * _HALF_SPACE_BETWEEN_COLUMNS
_HORIZ_BETWEEN_COLUMNS_STR = f"{_HALF_HORIZ}{_HORIZONTAL_AND_VERTICAL_BAR}{_HALF_HORIZ}"


# WARN: Cell size is the *interior* size of each cell, does not include _SPACE_BETWEEN_COLUMNS
# Each table in a section must have the same number of columns, so this is for the whole section.
def _get_cell_sizes_for_section(section: Section, max_table_width: OutputWidth) -> Sequence[int]:
    assert isinstance(max_table_width, SpecialOutputWidth) or max_table_width > _sum_cell_sizes(
        _MIN_CELL_SIZE for _ in range(section.n_columns)
    ), f"Can't squeeze a {section.n_columns}-column table with a width of only {max_table_width}"

    cell_sizes = _get_cell_sizes_ignoring_max_width(section)

    while (
        not isinstance(max_table_width, SpecialOutputWidth)
        and _sum_cell_sizes(cell_sizes) > max_table_width
    ):
        # Find the largest cell size and reduce it
        # TODO: We should actually be trying to minimize the number of line breaks
        # we'll have to insert to fit text into the smaller width.
        i = index_of_max(cell_sizes)
        assert cell_sizes[i] > _SPACE_BETWEEN_COLUMNS
        cell_sizes[i] -= 1

    return cell_sizes


def _get_cell_sizes_ignoring_max_width(section: Section) -> List[int]:
    cell_sizes = [0 for _ in range(section.n_columns)]
    for table in section.tables:
        for i in range(table.n_columns):
            cell_sizes[i] = max(
                cell_sizes[i],
                0 if table.headers is None else len(table.headers[i]),
                *(_max_line_len(_to_str(r[i].value)) for r in table.rows),
            )

        # header_groups will expand the last column so the header will fit
        if table.header_groups is not None:
            i = 0
            for hg in table.header_groups:
                end = i + hg.size_cells
                cur = _sum_cell_sizes([cell_sizes[j] for j in range(i, end)])
                diff = _max_line_len(hg.text) - cur
                if diff > 0:
                    cell_sizes[end - 1] += diff
                i = end

    return cell_sizes


def _sum_cell_sizes(cell_sizes: Iterable[int]) -> int:
    total, count = _sum_and_count(cell_sizes)
    return total + (_SPACE_BETWEEN_COLUMNS * (count - 1))


def _sum_and_count(i: Iterable[int]) -> Tuple[int, int]:
    total = 0
    count = 0
    for x in i:
        total += x
        count += 1
    return total, count


def _render_table_to_text(
    table: Table, write: Write, color: _ColorType, cell_sizes: Sequence[int], indent: int
) -> None:
    if table.name is not None:
        write(table.name + "\n\n")

    if table.text is not None:
        write(table.text + "\n\n")

    if table.header_groups is not None:
        _write_header_groups(table.header_groups, write, cell_sizes, indent)

    if table.headers is not None:
        _write_cells([Cell(h) for h in table.headers], write, no_color, cell_sizes, indent)
        _write_between_rows(write, cell_sizes, indent)

    for is_last, row in with_is_last(table.rows):
        _write_cells(row, write, color, cell_sizes, indent)
        if not is_last:
            _write_between_rows(write, cell_sizes, indent)


def _write_header_groups(
    header_groups: Sequence[HeaderGroup], write: Write, cell_sizes: Sequence[int], indent: int
) -> None:
    group_sizes = _get_header_group_sizes(cell_sizes, [group.size_cells for group in header_groups])
    _write_cells(
        [Cell(group.text) for group in header_groups], write, no_color, group_sizes, indent
    )


def _get_header_group_sizes(
    cell_sizes: Sequence[int], group_sizes_in_cells: Sequence[int]
) -> Sequence[int]:
    cell_i = 0

    def group_size_columns(group_size_cells: int) -> int:
        nonlocal cell_i
        old_cell_i = cell_i
        cell_i = cell_i + group_size_cells
        return _sum_cell_sizes(cell_sizes[old_cell_i:cell_i])

    group_cell_sizes = [
        group_size_columns(group_size_cells) for group_size_cells in group_sizes_in_cells
    ]
    assert cell_i == len(cell_sizes)
    assert _sum_cell_sizes(cell_sizes) == _sum_cell_sizes(group_cell_sizes)
    return group_cell_sizes


def _write_cells(
    cells: Sequence[Cell], write: Write, color: _ColorType, cell_sizes: Sequence[int], indent: int
) -> None:
    cell_lines = [
        _split_text_to_lines(_to_str(cell.value), cell_size)
        for cell_size, cell in zip_check(cell_sizes, cells)
    ]
    n_lines = max(len(lines) for lines in cell_lines)
    for line_index in range(n_lines):
        _write_indent(write, indent)
        for is_last, (cell_size, cell, lines) in with_is_last(
            zip_check_3(cell_sizes, cells, cell_lines)
        ):
            line_text = lines[line_index] if line_index < len(lines) else ""
            assert len(line_text) <= cell_size
            write(color(_pad(cell_size, line_text, cell.align), cell.bold, cell.color))
            if not is_last:
                write(_BETWEEN_COLUMNS_STR)
        write("\n")


def _write_between_rows(write: Write, cell_sizes: Sequence[int], indent: int) -> None:
    _write_indent(write, indent)
    for is_last, i in with_is_last(range(len(cell_sizes))):
        write(_HORIZONTAL_BAR * cell_sizes[i])
        if not is_last:
            write(_HORIZ_BETWEEN_COLUMNS_STR)
    write("\n")


def _write_indent(write: Write, indent: int) -> None:
    write(" " * indent)


def _render_to_html(document: Document) -> str:
    doc = Doc()
    tag = doc.tag
    text = doc.text
    line = doc.line
    with tag("html"):
        with tag("head"):
            with tag("style"):
                text(
                    """
                table {
                    border-collapse: collapse;
                }
                tr, td, th {
                    border: solid;
                }
                tr {
                    border-width: 1px 0;
                }
                td, th {
                    border-width: 0 1px;
                }
                div {
                    margin: 1em;
                }
                """
                )
        with tag("body"):
            if document.comment is not None:
                text(document.comment)

            for section in document.sections:
                with tag("div"):
                    if section.name is not None:
                        line("h1", section.name)
                    if section.text is not None:
                        text(section.text)
                    for table in section.tables:
                        _render_table_to_html(doc, table)
    return indent_xml(doc.getvalue(), indent_text=True)


def _render_table_to_html(doc: Doc, table: Table) -> None:
    tag = doc.tag
    line = doc.line
    text = doc.text
    if table.name is not None:
        line("h2", table.name)
    if table.text is not None:
        raise Exception("TODO")
    with tag("table", style="width: 100%"):  # TODO: use css
        if table.header_groups is not None:
            with tag("tr"):
                for hg in table.header_groups:
                    line("th", hg.text, colspan=hg.size_cells)

        if table.headers is not None:
            with tag("tr"):
                for header in table.headers:
                    line("th", header)

        for row in table.rows:
            with tag("tr"):
                for cell in row:
                    cell_text = _to_str(cell.value)
                    with tag("td"):
                        style = ";".join(
                            (
                                *optional_to_iter(map_option(cell.color, lambda c: f"color:{c}")),
                                *optional_to_iter("bold" if cell.bold else None),
                            )
                        )
                        if is_empty(style):
                            text(cell_text)
                        else:
                            with tag("span", style=style):
                                text(cell_text)


def _to_str(v: CellValue) -> str:
    if isinstance(v, ABCSequence) and not isinstance(v, str):
        return "\n".join(_to_str_single(x) for x in v)
    else:
        return _to_str_single(v)


def _to_str_single(u: CellValueSingle) -> str:
    if u is None:
        return ""
    elif isinstance(u, str):
        return u
    elif isinstance(u, int):
        return str(u)
    else:
        return float_to_str(u)


def _split_text_to_lines(s: str, width: int) -> Sequence[str]:
    return tuple(_iter_lines_of_split_text(s, width))


def _iter_lines_of_split_text(s: str, width: int) -> Iterable[str]:
    assert width > 0
    while not is_empty(s):
        first_line = s[:width]
        nl = try_index(first_line, "\n")
        if nl is None:
            yield first_line
            s = s[width:]
        else:
            yield first_line[:nl]
            s = s[nl + 1 :]


def _max_line_len(s: str) -> int:
    return max(len(line) for line in _lines(s))


def _lines(s: str) -> Sequence[str]:
    return s.split("\n")


@with_slots
@dataclass(frozen=True)
class OutputOptions:
    width: Optional[OutputWidth] = None  # Applies only to text output
    table_indent: Optional[int] = None
    html: Optional[Path] = None
    txt: Optional[Path] = None
    excel: Optional[Path] = None

    def __post_init__(self) -> None:
        check_cast(Optional[Path], self.html)
        check_cast(Optional[Path], self.txt)
        check_cast(Optional[Path], self.excel)

    def any_file_output(self) -> bool:
        return self.html is not None or self.txt is not None or self.excel is not None


EMPTY_OUTPUT_OPTIONS = OutputOptions()


@with_slots
@dataclass(frozen=True)
class DocOutputArgs:
    output_width: Optional[OutputWidth] = argument(
        default=None,
        doc="""
        Maximum width (in columns) of console or text file output.
        Default is the current terminal size.
        """,
    )
    table_indent: Optional[int] = argument(default=None, doc="Indent tables by this many spaces.")
    txt: Optional[Path] = argument(default=None, doc="Output to a '.txt' file")
    html: Optional[Path] = argument(default=None, doc="Output to a '.html' file")
    # Hidden because render_to_excel is incomplete
    xlsx: Optional[Path] = argument(default=None, hidden=True, doc="Output to a '.xlsx' file")


def output_options_from_args(args: DocOutputArgs) -> OutputOptions:
    return OutputOptions(
        width=args.output_width,
        table_indent=args.table_indent,
        html=args.html,
        txt=args.txt,
        excel=args.xlsx,
    )


def handle_doc(doc: Document, output: OutputOptions = EMPTY_OUTPUT_OPTIONS) -> None:
    if output.html:
        output.html.write_text(_render_to_html(doc))
    if output.txt:
        doc_txt = _render_to_plaintext(
            doc, max_width=output.width, table_indent=output.table_indent
        )
        txt = f"{get_command_line()}\n\n{doc_txt}"
        output.txt.write_text(txt, encoding="utf-8")
    if output.excel:
        _render_to_excel(doc, output.excel)
    if not output.any_file_output():
        print_document(doc, max_width=output.width, table_indent=output.table_indent)
