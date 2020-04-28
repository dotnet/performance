# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from operator import floordiv
from pathlib import Path
from re import search
from textwrap import indent
from typing import Iterable, List, Mapping, Optional, Sequence, Tuple

from .bench_file import (
    change_path_machine,
    get_this_machine,
    Machine,
    MACHINE_DOC,
    parse_machines_arg,
)
from .get_built import get_built, Built
from .collection_util import empty_mapping, is_empty
from .command import Command, CommandKind, CommandsMapping
from .config import HOST_INFO_PATH
from .option import map_option, map_option_2
from .parse_and_serialize import load_yaml, parse_yaml, to_yaml, write_yaml_file
from .type_utils import argument, check_cast, with_slots
from .util import (
    ensure_dir,
    ExecArgs,
    exec_and_get_output,
    get_hostname,
    get_os,
    kb_to_bytes,
    kb_to_mb,
    mhz_to_ghz,
    OS,
    remove_str_end,
    try_remove_str_start,
)


@with_slots
@dataclass(frozen=True)
class CacheInfoForLevel:
    # Values are None if unknown
    n_caches: Optional[int] = None
    total_bytes: Optional[int] = None

    @property
    def average_bytes(self) -> Optional[int]:
        return map_option_2(self.total_bytes, self.n_caches, floordiv)


@with_slots
@dataclass(frozen=True)
class CacheInfo:
    l1: CacheInfoForLevel
    l2: CacheInfoForLevel
    l3: CacheInfoForLevel


@with_slots
@dataclass(frozen=True)
class Range:
    # Both inclusive
    lo: int
    hi: int

    def with_hi(self, new_hi: int) -> "Range":
        return Range(self.lo, new_hi)


@with_slots
@dataclass(frozen=True)
class NumaNodeInfo:
    numa_node_number: int
    ranges: Sequence[Range]
    # None on non-Windows
    cpu_group_number: Optional[int] = None


@with_slots
@dataclass(frozen=True)
class HostInfo:
    # All values are None if unknown
    hostname: str
    n_physical_processors: int
    n_logical_processors: int
    numa_nodes: Sequence[NumaNodeInfo]
    cache_info: CacheInfo
    clock_ghz: Optional[float] = None
    total_physical_memory_mb: Optional[int] = None


@with_slots
@dataclass(frozen=True)
class _NumaNodesAndCacheInfo:
    numa_nodes: Sequence[NumaNodeInfo]
    n_physical_processors: int
    n_logical_processors: int
    caches: CacheInfo


def _get_total_physical_memory_mb_windows() -> int:
    output = exec_and_get_output(ExecArgs(("systeminfo",), quiet_print=True))
    for line in output.splitlines():
        tot = try_remove_str_start(line, "Total Physical Memory:")
        if tot is not None:
            mem = remove_str_end(tot, "MB")
            return int(mem.replace(",", ""))
    raise Exception("Didn't find total physical memory")


def _get_host_info(built: Built) -> HostInfo:
    return {OS.posix: lambda _: _get_host_info_posix(), OS.windows: _get_host_info_windows}[
        get_os()
    ](built)


_UNKNOWN_MSG: str = "unknown"

def _get_host_info_posix() -> HostInfo:
    # lscpu output is a bunch of lines all of the form key: value. Make a dict from that.
    dct = _parse_keys_values_lines(exec_and_get_output(ExecArgs(("lscpu",), quiet_print=True)))

    def get_opt(name: str) -> Optional[str]:
        return dct.get(name, None)

    def get_int(name: str) -> int:
        return int(dct[name])

    def get_opt_float(name: str) -> Optional[float]:
        return map_option(get_opt(name), float)

    def get_opt_kb(name: str) -> Optional[int]:
        opt = get_opt(name)
        if opt is not None and _UNKNOWN_MSG in opt.lower():
            return None
        return map_option(opt, lambda s: int(remove_str_end(s, "K")))

    # Note: "CPU MHz" is the *current* cpu rate which varies. Going with max here.
    # TODO: Max is probably wrong, we want a typical value.
    clock_ghz = map_option(get_opt_float("CPU max MHz"), mhz_to_ghz)

    sockets = get_int("Socket(s)")
    cores = get_int("Core(s) per socket")
    threads = get_int("Thread(s) per core")

    n_physical_processors = sockets * cores
    n_logical_processors = n_physical_processors * threads

    l1d_cache_kb = get_opt_kb("L1d cache")
    l1i_cache_kb = get_opt_kb("L1i cache")
    l2_cache_kb = get_opt_kb("L2 cache")
    l3_cache_kb = get_opt_kb("L3 cache")

    x = _parse_keys_values_lines((Path("/proc") / "meminfo").read_text())

    total_physical_memory_mb = round(kb_to_mb(float(remove_str_end(x["MemTotal"], " kB"))))

    numa_nodes = _get_numa_nodes_posix()

    return HostInfo(
        hostname=get_hostname(),
        n_physical_processors=n_physical_processors,
        n_logical_processors=n_logical_processors,
        numa_nodes=numa_nodes,
        cache_info=CacheInfo(
            # TODO: figure out how to determine number of caches on posix
            l1=CacheInfoForLevel(
                n_caches=None,
                total_bytes=map_option_2(
                    l1d_cache_kb, l1i_cache_kb, lambda a, b: kb_to_bytes(a + b)
                ),
            ),
            l2=CacheInfoForLevel(n_caches=None, total_bytes=map_option(l2_cache_kb, kb_to_bytes)),
            l3=CacheInfoForLevel(n_caches=None, total_bytes=map_option(l3_cache_kb, kb_to_bytes)),
        ),
        clock_ghz=clock_ghz,
        total_physical_memory_mb=total_physical_memory_mb,
    )


def _get_numa_nodes_posix() -> Sequence[NumaNodeInfo]:
    return tuple(
        _parse_numa_nodes_posix(
            exec_and_get_output(ExecArgs(("numactl", "--hardware"), quiet_print=True))
        )
    )


def _parse_numa_nodes_posix(s: str) -> Iterable[NumaNodeInfo]:
    for line in s.splitlines():
        res = search(r"^node (\d+) cpus: ", line)
        if res is not None:
            node_number = int(res.group(1))
            yield NumaNodeInfo(
                numa_node_number=node_number,
                cpu_group_number=None,
                ranges=_ranges_from_numbers([int(x) for x in line[res.span()[1] :].split()]),
            )


def _ranges_from_numbers(ns: Iterable[int]) -> Sequence[Range]:
    ranges: List[Range] = []
    for n in ns:
        if is_empty(ranges) or n != ranges[-1].hi + 1:
            ranges.append(Range(n, n))
        else:
            ranges.append(ranges.pop().with_hi(n))
    return ranges


def _parse_keys_values_lines(s: str) -> Mapping[str, str]:
    return {k: v for line in s.split("\n") if line != "" for k, v in (_split_line(line),)}


def _split_line(line: str) -> Tuple[str, str]:
    parts = line.split(":")
    assert len(parts) == 2
    l, r = parts
    return l.strip(), r.strip()


def _get_host_info_windows(built: Built) -> HostInfo:
    total_physical_memory_mb = _get_total_physical_memory_mb_windows()
    info_from_c = parse_yaml(
        _NumaNodesAndCacheInfo,
        exec_and_get_output(ExecArgs((str(built.win.get_host_info_exe),), quiet_print=True)),
    )

    return HostInfo(
        hostname=get_hostname(),
        clock_ghz=_get_clock_ghz_windows(),
        total_physical_memory_mb=total_physical_memory_mb,
        n_physical_processors=info_from_c.n_physical_processors,
        n_logical_processors=info_from_c.n_logical_processors,
        numa_nodes=info_from_c.numa_nodes,
        cache_info=info_from_c.caches,
    )


def _get_clock_ghz_windows() -> float:
    # Import lazily as this is only available on Windows
    # pylint:disable=import-outside-toplevel
    from winreg import ConnectRegistry, HKEY_LOCAL_MACHINE, OpenKey, QueryValueEx

    registry = ConnectRegistry(None, HKEY_LOCAL_MACHINE)
    key = OpenKey(registry, "HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0")
    mhz, _ = QueryValueEx(key, "~MHz")
    ghz = mhz_to_ghz(check_cast(float, mhz))
    assert 0 < ghz < 10
    return ghz


def read_host_info_for_machine(machine: Machine) -> HostInfo:
    return _read_host_info_at_path(change_path_machine(HOST_INFO_PATH, machine))


def _read_host_info_at_path(path: Path) -> HostInfo:
    try:
        return load_yaml(HostInfo, path)
    except FileNotFoundError:
        raise Exception(
            f"Host info not found at {path}. Did you forget to run 'write-host-info'?"
        ) from None


def read_this_machines_host_info() -> HostInfo:
    return read_host_info_for_machine(get_this_machine())


def write_host_info() -> None:
    built = get_built(coreclrs=empty_mapping())
    ensure_dir(HOST_INFO_PATH.parent)
    write_yaml_file(HOST_INFO_PATH, _get_host_info(built), overwrite=True)
    print(f"Wrote to {HOST_INFO_PATH}")


def print_host_info() -> None:
    print(to_yaml(read_this_machines_host_info()))


@with_slots
@dataclass(frozen=True)
class PrintAllHostInfosArgs:
    machines: Sequence[str] = argument(doc=MACHINE_DOC)


def print_all_host_infos(args: PrintAllHostInfosArgs) -> None:
    for machine in parse_machines_arg(args.machines):
        print(machine)
        hi = read_host_info_for_machine(machine)
        print(indent(to_yaml(hi), "  "))


HOST_INFO_COMMANDS: CommandsMapping = {
    "print-host-info": Command(
        hidden=True,
        kind=CommandKind.infra,
        fn=print_host_info,
        doc="""Print info about this machine generated from write-host-info""",
    ),
    # Hidden because 'setup' does this already.
    # Though it's useful to run again if the code for getting host-info is modified.
    "write-host-info": Command(
        hidden=True,
        kind=CommandKind.infra,
        fn=write_host_info,
        doc=f"Write host info to {HOST_INFO_PATH}.",
    ),
    "print-all-host-infos": Command(
        kind=CommandKind.infra,
        fn=print_all_host_infos,
        doc="Fetch and print host info for all machines.",
        priority=1,
    ),
}
