# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from pathlib import Path
from typing import cast, Dict, Iterable, List, Optional, Sequence, Tuple
from xml.etree import ElementTree

from ..commonlib.collection_util import empty_sequence, get_diff, is_empty, try_index
from ..commonlib.command import Command, CommandKind, CommandsMapping
from ..commonlib.document import (
    Cell,
    Document,
    handle_doc,
    OutputOptions,
    Section,
    single_table_document,
    Table,
)
from ..commonlib.option import non_null, option_or
from ..commonlib.type_utils import argument, with_slots
from ..commonlib.util import (
    get_percent,
    mb_to_bytes,
    remove_char,
    remove_str_end,
    remove_str_start_end,
    show_size_bytes,
)


@with_slots
@dataclass(frozen=True)
class AddressRange:
    lo: int
    hi: int

    def __str__(self) -> str:
        return f"{show_size_bytes(self.size)} {hex(self.lo)}-{hex(self.hi)}"

    @property
    def size(self) -> int:
        return self.hi - self.lo

    def __post_init__(self) -> None:
        assert self.hi > self.lo


@with_slots
@dataclass(frozen=True)
class Permissions:
    r: bool
    w: bool
    x: bool
    p: bool

    def __str__(self) -> str:
        return "".join(
            (
                ("r" if self.r else "-"),
                ("w" if self.w else "-"),
                ("x" if self.x else "-"),
                ("p" if self.p else "s"),
            )
        )

    @property
    def is_pure_p(self) -> bool:
        return self == Permissions(r=False, w=False, x=False, p=True)

    @property
    def rw(self) -> bool:
        return self.r and self.w

    @property
    def r_only(self) -> bool:
        return self.r and not self.w


@with_slots
@dataclass(frozen=True)
class MMapEntry:
    address_range: AddressRange
    permissions: Permissions
    offset: int
    dev: Tuple[int, int]
    inode: int
    pathname: Optional[str]

    def __str__(self) -> str:
        return (
            f"{self.address_range} {self.permissions} {self.offset} "
            + f"{self.dev} {self.inode} {self.pathname}"
        )

    @property
    def size(self) -> int:
        return self.address_range.size


def treat_maps_same(a: MMapEntry, b: MMapEntry) -> bool:
    return (
        a.size == b.size
        and a.permissions == b.permissions
        and a.offset == b.offset
        and a.dev == b.dev
        and a.inode == b.inode
        and a.pathname == b.pathname
    )


def parse_address(a: str) -> AddressRange:
    parts = a.split("-")
    assert len(parts) == 2
    lo, hi = parts
    return AddressRange(parse_hex(lo), parse_hex(hi))


def parse_permissions(perms: str) -> Permissions:
    assert len(perms) == 4
    r, w, x, p = perms

    def get(actual: str, expect_true: str, expect_false: str) -> bool:
        return {expect_true: True, expect_false: False}[actual]

    return Permissions(
        r=get(r, "r", "-"), w=get(w, "w", "-"), x=get(x, "x", "-"), p=get(p, "p", "s")
    )


def parse_offset(offset: str) -> int:
    return parse_hex(offset)


def parse_hex(s: str) -> int:
    return int(s, 16)


def parse_dev(dev: str) -> Tuple[int, int]:
    parts = dev.split(":")
    assert len(parts) == 2
    l, r = parts
    return parse_hex(l), parse_hex(r)


def parse_map(line: str) -> MMapEntry:
    # ex: 55555575b000-55555575c000 rw-p 00007000 103:05 4333431 /path/to/corerun
    # ex: 7fff44000000-7fff44021000 rw-p 00000000 00:00 0
    parts = [part for part in line.split(" ") if part != ""]
    assert len(parts) == 5 or len(parts) == 6
    address, permissions, offset, dev, inode = parts[:5]
    pathname = None if len(parts) == 5 else parts[5]
    return MMapEntry(
        address_range=parse_address(address),
        permissions=parse_permissions(permissions),
        offset=parse_offset(offset),
        dev=parse_dev(dev),
        inode=parse_hex(inode),
        pathname=pathname,
    )


def print_maps(mmaps: Sequence[MMapEntry]) -> None:
    for mmap_entry in sorted(mmaps, key=lambda m: m.size, reverse=True):
        print(str(mmap_entry))

    sum_sizes = sum(m.size for m in mmaps)
    print(f"Total size: {show_size_bytes(sum_sizes)}")


def load_maps(p: Path) -> Sequence[MMapEntry]:
    return [parse_map(line) for line in p.read_text(encoding="utf-8").splitlines() if line != ""]


def normalize(a: Sequence[MMapEntry]) -> Sequence[MMapEntry]:
    used = [m for m in a if not m.permissions.is_pure_p]
    s = sorted(used, key=lambda m: (option_or(m.pathname, ""), m.size), reverse=True)
    return s


def show_sum_sizes(maps: Iterable[MMapEntry]) -> str:
    return show_size_bytes(sum(m.size for m in maps))


def diff_maps(a: Sequence[MMapEntry], b: Sequence[MMapEntry]) -> None:
    print(f"len(a): {len(a)}, len(b): {len(b)}")
    diff = get_diff(a, b, treat_maps_same)

    size_corresponding = show_sum_sizes(a for a, _ in diff.corresponding)
    print(f"Number of corresponding entires: {len(diff.corresponding)}, size {size_corresponding}")
    # print(f"??? {len(a)} {len(a_unique)}")
    # print(f"??? {len(b)} {len(b_unique)}")

    print(f"Unique to A: {show_sum_sizes(diff.unique_to_a)}")
    for ax in diff.unique_to_a:
        print(str(ax))

    print(f"\nUnique to B: {show_sum_sizes(diff.unique_to_b)}")
    for bx in diff.unique_to_b:
        print(str(bx))


def parse_maps() -> None:
    a = load_maps(Path("mmaps") / "dynCompile_mmap_after.txt")
    b = load_maps(Path("mmaps") / "nada_mmap_after.txt")

    diff_maps(normalize(a), normalize(b))

    # not_pure_p = [m for m in maps if not m.permissions.is_pure_p]

    # rw = [m for m in maps if m.permissions.rw]

    # named_maps = [m for m in maps if m.pathname is not None]
    # no_p_maps = [m for m in maps if not m.permissions.p]

    # print_maps([m for m in not_pure_p if not m.permissions.p])


def parse_valgrind_err() -> None:
    path = Path("hello-world-err.xml")
    et = ElementTree.parse(cast(str, path))

    root = et.getroot()
    print(root)

    error_nodes = [child for child in root if child.tag == "error"]

    kinds: Dict[str, int] = {}
    lost_by_kind: Dict[str, int] = {}

    for e in error_nodes:
        kind = non_null(non_null(e.find("kind")).text)
        kinds[kind] = kinds.get(kind, 0) + 1

        unique = non_null(non_null(e.find("unique")).text)

        # DefinitelyLost didn't account to much
        # if kind == "Leak_DefinitelyLost":
        #    print(e.find("unique").text)

        xwhat = e.find("xwhat")
        if xwhat is None:
            print(f"No xwhat for {unique}")
        else:
            leaked_bytes = int(
                non_null(non_null(non_null(e.find("xwhat")).find("leakedbytes")).text)
            )
            lost_by_kind[kind] = lost_by_kind.get(kind, 0) + leaked_bytes

    print("count by kind: ", kinds)
    print("lost bytes by kind:", lost_by_kind)


@with_slots
@dataclass(frozen=True)
class MassifPart:
    pct: float
    bytes: int
    text: str
    children: List["MassifPart"]


def split_no_empty(s: str, splitter: str, maxsplit: Optional[int] = None) -> Sequence[str]:
    if maxsplit is None:
        return [x for x in s.split(splitter) if x != ""]
    else:
        real_maxsplit = maxsplit
        while True:
            f = s.split(splitter, maxsplit=real_maxsplit)
            res = [x for x in f if x != ""]
            if len(res) == maxsplit + 1:
                return res
            else:
                if len(f) == real_maxsplit + 1:
                    real_maxsplit += 1
                else:
                    # Asking for more maxsplit isn't helping
                    return res


# Show int as "1,234" instead of the default "1234"
def show_int(i: int) -> str:
    return f"{i:,}"


def parse_int_with_commas(s: str) -> int:
    return int(remove_char(s, ","))


def _parse_part(line: str) -> MassifPart:
    pct_str, bytes_str, rest = split_no_empty(line, " ", maxsplit=2)
    pct = float(remove_str_end(pct_str, "%"))
    n_bytes = parse_int_with_commas(remove_str_start_end(bytes_str, "(", "B)"))
    return MassifPart(pct=pct, bytes=n_bytes, text=rest, children=[])


def _take_bars_and_arrow(line: str) -> Optional[Tuple[int, str]]:
    """
    Given '| | ->abc', returns (2, "abc")
    Given '| |', returns None
    """
    bars = 0
    for i, ch in enumerate(line):
        if ch == "|":
            bars += 1
        elif ch == "-":
            assert line[i + 1] == ">"
            return bars, line[i + 2 :]
        else:
            assert ch == " "
    # Made it to end without seeing '-'
    return None


def _indent_str(indent: int) -> str:
    return "|  " * indent


def _show_pct(pct: float) -> str:
    return ("%2.2f" % pct) + "%"


def _print_massif(root: MassifPart, indent: int = 0) -> None:
    print(f"{_indent_str(indent)}{_show_pct(root.pct)} ({show_int(root.bytes)}B) {root.text}")
    for child in root.children:
        _print_massif(child, indent=indent + 1)


def _parse_massif(text: str) -> MassifPart:
    peak = text.index(" (peak)")
    preceding_comma = text.rindex(",", 0, peak)

    peak_n = int(text[preceding_comma + 1 : peak])
    # Find next time this number appears at the beginning of a line. Indent is always one.
    line_before = text.index(f"\n {peak_n} ", peak)
    section = text[line_before + 1 : text.index("------", line_before)]

    # Skip first line
    lines = section.splitlines()[1:]

    nodes: List[MassifPart] = [_parse_part(lines[0])]
    cur_bars = 1

    for line in lines[1:]:
        br = _take_bars_and_arrow(line)
        if br is not None:
            bars, rest = br
            node = _parse_part(rest)
            if bars >= cur_bars:
                assert bars in (cur_bars, cur_bars + 1)
            else:
                assert bars == cur_bars - 1
                while len(nodes) > bars + 1:
                    nodes.pop()
            cur_bars = bars
            # Add a new child to end of nodes
            nodes[-1].children.append(node)
            nodes.append(node)

    print(len(nodes))
    assert len(nodes) == 2
    nodes.pop()
    return nodes.pop()


@with_slots
@dataclass(frozen=True)
class _AnalyzeMassifArgs:
    path: Path = argument(name_optional=True, doc="Path to massif output text")


def _analyze_massif(args: _AnalyzeMassifArgs) -> None:
    root = _parse_massif(args.path.read_text(encoding="utf-8"))
    _print_massif(root)


@with_slots
@dataclass(frozen=True)
class _DumpHeapObject:
    address: int
    method_table: int
    size: int
    free: bool


@with_slots
@dataclass(frozen=True)
class _CountAndSize:
    count: int
    size_bytes: int


def add(a: _CountAndSize, b: _CountAndSize) -> _CountAndSize:
    return _CountAndSize(a.count + b.count, a.size_bytes + b.size_bytes)


EMPTY_COUNT_AND_SIZE = _CountAndSize(count=0, size_bytes=0)


@with_slots
@dataclass(frozen=True)
class _DumpHeapStatistic:
    method_table: int
    class_name: str
    count: int
    total_size: int

    def __str__(self) -> str:
        return f"{self.count} {self.total_size} {self.class_name}"
        # return f"{hex(self.method_table)}"

    @property
    def count_and_size(self) -> _CountAndSize:
        return _CountAndSize(count=self.count, size_bytes=self.total_size)


def _empty_dump_heap_statistic(method_table: int, class_name: str) -> _DumpHeapStatistic:
    return _DumpHeapStatistic(
        method_table=method_table, class_name=class_name, count=0, total_size=0
    )


@with_slots
@dataclass(frozen=True)
class _DumpHeapFragmentedBlock:
    addr: int
    size_mb: float
    followed_by: int
    followed_by_class: str

    @property
    def size_bytes(self) -> int:
        return mb_to_bytes(self.size_mb)


def parse_object(line: str) -> _DumpHeapObject:
    parts = split_no_empty(line, " ")
    assert len(parts) in (3, 4)
    address_str, method_table_str, size_str = parts[:3]
    free_str = None if len(parts) == 3 else parts[3]
    assert free_str in (None, "Free")
    free = free_str is not None
    return _DumpHeapObject(
        address=parse_hex(address_str),
        method_table=parse_hex(method_table_str),
        size=int(size_str),
        free=free,
    )


def parse_statistic(line: str) -> _DumpHeapStatistic:
    try:
        method_table_str, count_str, total_size_str, class_name = split_no_empty(
            line, " ", maxsplit=3
        )
    except ValueError:
        raise Exception(f"Bad line: {line}")
    return _DumpHeapStatistic(
        method_table=parse_hex(method_table_str),
        count=int(count_str),
        total_size=int(total_size_str),
        class_name=class_name,
    )


@with_slots
@dataclass(frozen=True)
class _ParsedDumpHeap:
    objects: Optional[Sequence[_DumpHeapObject]]
    statistics: Sequence[_DumpHeapStatistic]
    fragmented_blocks: Sequence[_DumpHeapFragmentedBlock]

    @property
    def total_objects_size(self) -> int:
        """Includes free objects."""
        return sum(s.total_size for s in self.statistics)

    @property
    def total_fragmented_blocks_size(self) -> int:
        return mb_to_bytes(sum(f.size_mb for f in self.fragmented_blocks))


def try_split_lines(
    lines: Sequence[str], line: str
) -> Tuple[Sequence[str], Optional[Sequence[str]]]:
    index = try_index(lines, line)
    if index is None:
        return lines, None
    else:
        return lines[:index], lines[index + 1 :]


def split_lines(lines: Sequence[str], line: str) -> Tuple[Sequence[str], Sequence[str]]:
    index = lines.index(line)
    return lines[:index], lines[index + 1 :]


def _parse_dump_heap(path: Path) -> _ParsedDumpHeap:
    text = path.read_text("utf-8")
    if text == "":
        raise Exception(f"File {path} is empty")

    non_empty_lines = [line for line in text.splitlines() if not is_empty(line)]

    before_statistics, after_statistics = split_lines(non_empty_lines, "Statistics:")

    before_fragmented, after_fragmented = try_split_lines(
        after_statistics, "Fragmented blocks larger than 0.5 MB:"
    )
    objects = _parse_dump_heap_objects(before_statistics)
    statistics = _parse_dump_heap_statistics(before_fragmented, path)

    fragmented_blocks = (
        empty_sequence() if after_fragmented is None else _parse_fragmented_blocks(after_fragmented)
    )

    n_objects = sum(s.count for s in statistics)

    # Verify that statistics are correct
    if objects is not None:
        assert n_objects == len(objects)
        # Method table -> count and total size
        d: Dict[int, _CountAndSize] = {}
        for obj in objects:
            d[obj.method_table] = add(
                d.get(obj.method_table, EMPTY_COUNT_AND_SIZE),
                _CountAndSize(count=1, size_bytes=obj.size),
            )

        for s in statistics:
            assert s.count_and_size == d[s.method_table]

    return _ParsedDumpHeap(
        objects=objects, statistics=statistics, fragmented_blocks=fragmented_blocks
    )


def _parse_dump_heap_objects(lines: Sequence[str]) -> Optional[Sequence[_DumpHeapObject]]:
    # `dumpheap -stat` does not have individual objects
    if is_empty(lines):
        return None
    else:
        assert lines[0] == "         Address               MT     Size"
        return [parse_object(line) for line in lines[1:]]


def _parse_dump_heap_statistics(lines: Sequence[str], path: Path) -> Sequence[_DumpHeapStatistic]:
    assert lines[0] == "              MT    Count    TotalSize Class Name"

    try:
        total_str, n_objects_str, objects_str = lines[-1].split(" ")
    except ValueError:
        raise Exception(f"Unexpected last line of {path}: {lines[-1]}")
    assert total_str == "Total" and objects_str == "objects"

    statistics = [parse_statistic(line) for line in lines[1:-1]]

    n_objects = int(n_objects_str)
    assert n_objects == sum(s.count for s in statistics)
    return statistics


def _parse_fragmented_blocks(lines: Sequence[str]) -> Sequence[_DumpHeapFragmentedBlock]:
    assert lines[0] == "            Addr     Size      Followed by"
    return [_parse_fragmented_block(line) for line in lines[1:]]


def _parse_fragmented_block(line: str) -> _DumpHeapFragmentedBlock:
    try:
        addr_str, size_str, followed_by_str, followed_by_class = split_no_empty(
            line, " ", maxsplit=3
        )
    except:
        raise Exception(f"Unexpected fragmented block line {line}")
    return _DumpHeapFragmentedBlock(
        addr=parse_hex(addr_str),
        size_mb=float(remove_str_end(size_str, "MB")),
        followed_by=parse_hex(followed_by_str),
        followed_by_class=followed_by_class,
    )


@with_slots
@dataclass(frozen=True)
class _DiffDumpHeapArgs:
    paths: Tuple[Path, Path] = argument(name_optional=True, doc="Paths of heap dumps to diff")
    txt: Optional[Path] = argument(default=None, doc="Output file")


@with_slots
@dataclass(frozen=True)
class _DumpHeapStatisticDiff:
    a: _DumpHeapStatistic
    b: _DumpHeapStatistic

    def __post_init__(self) -> None:
        # Method table may differ if we are comparing different machines
        assert self.a.class_name == self.b.class_name

    @property
    def method_table(self) -> int:
        return self.a.method_table

    @property
    def class_name(self) -> str:
        return self.a.class_name

    @property
    def count_diff(self) -> int:
        return self.b.count - self.a.count

    @property
    def size_diff(self) -> int:
        return self.b.total_size - self.a.total_size

    @property
    def abs_count_diff(self) -> int:
        return abs(self.count_diff)

    @property
    def abs_size_diff(self) -> int:
        return abs(self.size_diff)

    @property
    def is_same(self) -> bool:
        return self.a.count == self.b.count and self.a.total_size == self.b.total_size


@with_slots
@dataclass(frozen=True)
class _DumpHeapStatisticsDiff:
    diff_of_total_size_bytes: int
    n_identical_statistics: int
    different_statistics: Sequence[_DumpHeapStatisticDiff]


def _get_dump_heap_statistics_diff(
    a: _ParsedDumpHeap, b: _ParsedDumpHeap
) -> _DumpHeapStatisticsDiff:
    # Group by class name
    # Not using method table, because those may differ between different machines.
    b_by_mt: Dict[str, _DumpHeapStatistic] = {x.class_name: x for x in b.statistics}
    diffs: List[_DumpHeapStatisticDiff] = []
    identical = 0
    for ax in a.statistics:
        bx = b_by_mt.pop(ax.class_name, _empty_dump_heap_statistic(ax.method_table, ax.class_name))
        df = _DumpHeapStatisticDiff(a=ax, b=bx)
        if not df.is_same:
            diffs.append(df)
        else:
            identical += 1
    for bx in b_by_mt.values():
        diffs.append(
            _DumpHeapStatisticDiff(
                a=_empty_dump_heap_statistic(bx.method_table, bx.class_name), b=bx
            )
        )

    return _DumpHeapStatisticsDiff(
        diff_of_total_size_bytes=b.total_objects_size - a.total_objects_size,
        n_identical_statistics=identical,
        different_statistics=sorted(diffs, key=lambda d: d.abs_size_diff, reverse=True),
    )


@with_slots
@dataclass(frozen=True)
class _AnalyzeDumpHeapArgs:
    path: Path = argument(
        name_optional=True, doc="Path to file containing the result of `dumpheap -stat` from SOS"
    )
    min_statistic_size_pct: float = argument(
        default=0.5, doc="Don't show statistics if the size isn't at least this much"
    )


def _analyze_dump_heap(args: _AnalyzeDumpHeapArgs) -> None:
    dump = _parse_dump_heap(args.path)

    def get_size_pct(s: _DumpHeapStatistic) -> float:
        return get_percent(s.total_size / dump.total_objects_size)

    stats = sorted(
        [s for s in dump.statistics if get_size_pct(s) > args.min_statistic_size_pct],
        key=lambda s: s.total_size,
        reverse=True,
    )

    total_frag = show_size_bytes(dump.total_fragmented_blocks_size)
    fragmented_blocks_table = Table(
        text=f"Fragmented blocks larger than 0.5MB (total: {total_frag})",
        headers=("size", "size %", "followed by"),
        rows=[
            (
                Cell(show_size_bytes(f.size_bytes)),
                Cell(get_percent(f.size_bytes / dump.total_fragmented_blocks_size)),
                Cell(f.followed_by_class),
            )
            for f in sorted(dump.fragmented_blocks, key=lambda f: f.size_mb, reverse=True)
        ],
    )

    stats_table = Table(
        text=f"Total size: {show_size_bytes(dump.total_objects_size)}\n"
        + f"Showing statistics with at least {args.min_statistic_size_pct}% of size",
        headers=("class", "size", "size %", "count", "avg size"),
        rows=[
            (
                Cell(s.class_name),
                Cell(show_size_bytes(s.total_size)),
                Cell(get_percent(s.total_size / dump.total_objects_size)),
                Cell(s.count),
                Cell(show_size_bytes(s.total_size / s.count)),
            )
            for s in stats
        ],
    )

    doc = Document(
        sections=(Section(tables=(fragmented_blocks_table,)), Section(tables=(stats_table,)))
    )
    handle_doc(doc, OutputOptions())


def _diff_dump_heap(args: _DiffDumpHeapArgs) -> None:
    p0, p1 = args.paths
    a = _parse_dump_heap(p0)
    b = _parse_dump_heap(p1)

    diff = _get_dump_heap_statistics_diff(a, b)

    rows = [
        (
            Cell(d.class_name),
            Cell(d.count_diff),
            Cell(d.size_diff),
            Cell(d.a.count),
            Cell(d.b.count),
            Cell(d.a.total_size),
            Cell(d.b.total_size),
        )
        for d in diff.different_statistics
    ]

    doc = single_table_document(
        Table(
            text=f"a: {p0}\nb: {p1}\nIgnoring {diff.n_identical_statistics} identical entries.\n"
            + f"Size difference: {show_size_bytes(diff.diff_of_total_size_bytes)} "
            + f"(a: {show_size_bytes(a.total_objects_size)}, "
            + f"b: {show_size_bytes(b.total_objects_size)})",
            headers=(
                "class name",
                "count difference",
                "size difference",
                "a count",
                "b count",
                "a total size",
                "b total size",
            ),
            rows=rows,
        )
    )
    handle_doc(doc, OutputOptions(txt=args.txt))


MEM_UTILS_COMMANDS: CommandsMapping = {
    "analyze-dump-heap": Command(
        hidden=True,
        kind=CommandKind.misc,
        fn=_analyze_dump_heap,
        doc="""
        Prints out the result of `dumpheap -stat` in a slightly prettier format.
        """,
    ),
    "diff-dump-heap": Command(
        hidden=True,
        kind=CommandKind.misc,
        fn=_diff_dump_heap,
        doc="""
        Diffs two text files containing the output of `dumpheap` or `dumpheap -stat` from SOS.
        """,
    ),
    "analyze-massif": Command(
        hidden=True,
        kind=CommandKind.misc,
        fn=_analyze_massif,
        doc="""
        Analyzes the output of 'massif'
        """,
    ),
}
