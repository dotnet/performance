# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from __future__ import annotations  # Allow subscripting Popen
from dataclasses import dataclass, replace
from datetime import timedelta
from difflib import get_close_matches
from enum import Enum
from functools import reduce
from inspect import getfile
from math import ceil, floor, inf, isclose, isnan
from operator import mul
import os
from os import kill, name as os_name
from os.path import splitext
from pathlib import Path
from platform import node
from signal import signal, SIGINT
from subprocess import DEVNULL, PIPE, Popen, run
from stat import S_IREAD, S_IWRITE, S_IRUSR, S_IWUSR, S_IRGRP, S_IWGRP, S_IROTH, S_IWOTH
from statistics import median, StatisticsError
from sys import argv
from threading import Event, Thread
from time import sleep, time
from typing import Any, Callable, cast, Dict, Iterable, List, Mapping, Optional, Sequence, Union
from xml.etree.ElementTree import Element, parse as parse_xml

from psutil import process_iter
from result import Err, Ok, Result

from .collection_util import add, find, identity, is_empty, min_max_float
from .option import option_or
from .type_utils import check_cast, T, U, V, with_slots


def remove_str_start(s: str, start: str) -> str:
    assert s.startswith(start), f"Expected {s} to start with {repr(start)}"
    return s[len(start) :]


def remove_str_end(s: str, end: str) -> str:
    assert s.endswith(end), f"Expected {s} to end with {end}"
    return s[: -len(end)]


def remove_str_start_end(s: str, start: str, end: str) -> str:
    return remove_str_end(remove_str_start(s, start), end)


def try_remove_str_start(s: str, start: str) -> Optional[str]:
    return remove_str_start(s, start) if s.startswith(start) else None


def try_remove_str_end(s: str, end: str) -> Optional[str]:
    return remove_str_end(s, end) if s.endswith(end) else None


def remove_char(s: str, char: str) -> str:
    return s.translate(str.maketrans("", "", char))


def ensure_empty_dir(dir_path: Path) -> None:
    ensure_dir(dir_path)
    clear_dir(dir_path)


def unlink_if_exists(path: Path) -> None:
    if path.exists():
        path.unlink()


def clear_dir(dir_path: Path) -> None:
    tries = 1
    while tries > 0:
        try:
            # shutil.rmtree fails: github.com/hashdist/hashdist/issues/113#issuecomment-25374977
            # TODO: avoid str(path)
            for sub in dir_path.iterdir():
                if not sub.is_dir():
                    sub.unlink()
            for sub in dir_path.iterdir():
                assert sub.is_dir()
                clear_dir(sub)
                sub.rmdir()
        except OSError as e:
            tries -= 1
            if tries <= 0 or "The directory is not empty" not in e.strerror:
                raise
            sleep(1)
        else:
            break


def ensure_dir(dir_path: Path) -> None:
    if not dir_path.exists():
        assert dir_path.parent != dir_path
        ensure_dir(dir_path.parent)
        dir_path.mkdir()


def get_factor_diff(old: float, new: float) -> float:
    if old == 0:
        return 0 if new == 0 else inf if new > 0 else -inf
    else:
        return (new - old) / old


def get_max_factor_diff(values: Iterable[float]) -> Optional[float]:
    mm = min_max_float(values)
    return None if mm is None else get_factor_diff(*mm.to_pair())


def product(values: Sequence[float]) -> float:
    return reduce(mul, values)


def geometric_mean(values: Sequence[float]) -> float:
    # Geometric mean only works for positive values
    assert all(v > 0 for v in values)
    # 'pow' returns 'Any', this has caused me problems in the past
    return check_cast(float, pow(product(values), 1.0 / len(values)))


def assert_is_percent(p: float) -> float:
    return 0 <= p <= 100


def get_percent(f: float) -> float:
    return f * 100


def percent_to_fraction(p: float) -> float:
    return p / 100


def float_to_str(f: float) -> str:
    if f == 0:
        return "0"
    elif isnan(f):
        return "NaN"
    else:

        def get_fmt() -> str:
            a = abs(f)
            if 0.001 <= a < 10000:
                if a < 0.01:
                    return "%.5f"
                elif a < 0.1:
                    return "%.4f"
                elif a < 1:
                    return "%.3f"
                elif a < 10:
                    return "%.2f"
                elif a < 100:
                    return "%.1f"
                else:
                    return "%.0f"
            else:
                return "%.2e"

        res = get_fmt() % f
        assert isclose(float(res), f, rel_tol=5e-3)
        return res


def float_to_str_smaller(f: float) -> str:
    if f == 0:
        return "0"
    elif isnan(f):
        return "NaN"
    else:

        def get_fmt() -> str:
            a = abs(f)
            if 0.01 <= a < 1000:
                if a < 0.1:
                    return "%.3f"
                elif a < 1:
                    return "%.2f"
                elif a < 10:
                    return "%.1f"
                else:
                    return "%.0f"
            else:
                return "%.1e"

        res = get_fmt() % f
        assert isclose(float(res), f, rel_tol=5e-2)
        return res


def _assert_exists(path: Path) -> Path:
    assert path.exists(), f"Could not find {path}"
    return path


def assert_file_exists(path: Path) -> Path:
    _assert_exists(path)
    assert path.is_file(), f"{path} is not a file"
    return path


def assert_dir_exists(path: Path) -> Path:
    _assert_exists(path)
    assert path.is_dir(), f"{path} is not a directory"
    return path


def make_absolute_path(path: Path) -> Path:
    if path.is_absolute():
        return path
    else:
        return Path.cwd() / path


def get_existing_absolute_path(path: object, message: Optional[Callable[[], str]] = None) -> Path:
    assert isinstance(path, str)
    p = Path(path)
    assert p.is_absolute(), f"Path {path} should be absolute" if message is None else message()
    return _assert_exists(p)


def get_existing_absolute_file_path(
    path: object, message: Optional[Callable[[], str]] = None
) -> Path:
    p = get_existing_absolute_path(path, message)
    assert p.is_file(), f"Path {p} exists, but is not a file"
    return p


def stdev_frac(stdv: float, avg: float) -> float:
    if avg == 0.0:
        return 0.0 if stdv == 0.0 else 1.0
    else:
        return stdv / avg


def os_is_windows() -> bool:
    return {OS.posix: False, OS.windows: True}[get_os()]


class OS(Enum):
    posix = 0
    windows = 1


def get_os() -> OS:
    return {"nt": OS.windows, "posix": OS.posix}[os_name]


@with_slots
@dataclass(frozen=True)
class ExecArgs:
    cmd: Sequence[str]
    cwd: Optional[Path] = None
    env: Optional[Mapping[str, str]] = None
    # Don't print the command before running
    quiet_print: bool = False
    # Ignore print to stdout
    quiet_stdout: bool = False
    # Ignore print to stderr
    quiet_stderr: bool = False

    def print(self) -> None:
        if not self.quiet_print:
            print(self)

    def __str__(self) -> str:
        s = " ".join(self.cmd)
        if self.cwd is not None:
            s += f" (cwd {self.cwd})"
        # printing env is too verbose
        return s


def args_with_cmd(a: ExecArgs, cmd: Sequence[str]) -> ExecArgs:
    # Note: replace is not type-safe, so putting this near the definition of cmd
    return replace(a, cmd=cmd)


AnyPopen = Union["Popen[str]", "Popen[bytes]"]


def is_process_alive(process: AnyPopen) -> bool:
    return process.poll() is None


class ExecError(Exception):
    pass


def _call_and_allow_interrupts(args: ExecArgs) -> timedelta:
    start_time_seconds = time()
    process = Popen(
        args.cmd,
        cwd=args.cwd,
        env=args.env,
        stdout=DEVNULL if args.quiet_stdout else None,
        stderr=DEVNULL if args.quiet_stderr else None,
    )

    def handler(sig: int, _: Any) -> None:  # TODO: `_: FrameType`
        process.send_signal(sig)
        raise KeyboardInterrupt

    signal(SIGINT, handler)

    exit_code = process.wait()
    if exit_code != 0:
        quiet_warning = " (Try running without 'quiet_stderr')" if args.quiet_stderr else ""
        raise ExecError(f"Process {args.cmd} failed with exit code {exit_code}{quiet_warning}")
    return timedelta(seconds=time() - start_time_seconds)


def exec_cmd(args: ExecArgs) -> timedelta:
    args.print()
    return _call_and_allow_interrupts(args)


@with_slots
@dataclass(frozen=True)
class BenchmarkRunErrorInfo:
    name: str
    iteration_num: int
    message: str
    trace: List[str]

    def print(self) -> None:
        print(
            f"- Benchmark: '{self.name}' -\n"
            f"Iteration: {self.iteration_num}\n"
            f"Error Message: {self.message}\n"
            f"\nStack Trace:\n{self.__rebuild_trace()}\n"
        )

    def __rebuild_trace(self) -> str:
        return "".join(self.trace)


@with_slots
@dataclass(frozen=True)
class ConfigRunErrorInfo:
    name: str
    benchmarks_run: BenchmarkErrorList

    def print(self) -> None:
        print(f"=== Configuration '{self.name}' ===\n")
        for bench in self.benchmarks_run:
            bench.print()

    def add_benchmark(self, new_bench: BenchmarkRunErrorInfo) -> None:
        self.benchmarks_run.append(new_bench)


@with_slots
@dataclass(frozen=True)
class CoreRunErrorInfo:
    name: str
    configs_run: ConfigurationErrorMap

    def print(self) -> None:
        print(f"===== Core '{self.name}' =====\n")
        for config in self.configs_run.values():
            config.print()

    def add_config(self, new_config: ConfigRunErrorInfo) -> None:
        add(self.configs_run, new_config.name, new_config)


@with_slots
@dataclass(frozen=True)
class ExecutableRunErrorInfo:
    name: str
    coreclrs_run: CoreErrorMap

    def print(self) -> None:
        print(f"======= Executable '{self.name}' =======\n")
        for coreclr in self.coreclrs_run.values():
            coreclr.print()

    def add_coreclr(self, new_coreclr: CoreRunErrorInfo) -> None:
        add(self.coreclrs_run, new_coreclr.name, new_coreclr)


RunErrorMap = Dict[str, ExecutableRunErrorInfo]
CoreErrorMap = Dict[str, CoreRunErrorInfo]
ConfigurationErrorMap = Dict[str, ConfigRunErrorInfo]
BenchmarkErrorList = List[BenchmarkRunErrorInfo]


def add_new_error(
    run_errors: RunErrorMap,
    exec_name: str,
    core_name: str,
    config_name: str,
    bench_name: str,
    iteration_num: int,
    message: str,
    trace: List[str],
) -> None:
    if exec_name not in run_errors:
        bench_list = [BenchmarkRunErrorInfo(bench_name, iteration_num, message, trace)]
        config_dict = {config_name: ConfigRunErrorInfo(config_name, bench_list)}
        coreclr_dict = {core_name: CoreRunErrorInfo(core_name, config_dict)}
        add(run_errors, exec_name, ExecutableRunErrorInfo(exec_name, coreclr_dict))

    else:
        exec_info = run_errors[exec_name]

        if core_name not in exec_info.coreclrs_run:
            bench_list = [BenchmarkRunErrorInfo(bench_name, iteration_num, message, trace)]
            config_dict = {config_name: ConfigRunErrorInfo(config_name, bench_list)}
            exec_info.add_coreclr(CoreRunErrorInfo(core_name, config_dict))

        else:
            core_info = exec_info.coreclrs_run[core_name]

            if config_name not in core_info.configs_run:
                bench_list = [BenchmarkRunErrorInfo(bench_name, iteration_num, message, trace)]
                core_info.add_config(ConfigRunErrorInfo(config_name, bench_list))

            else:
                config_info = core_info.configs_run[config_name]
                config_info.add_benchmark(
                    BenchmarkRunErrorInfo(bench_name, iteration_num, message, trace)
                )


@with_slots
@dataclass(frozen=True)
class WaitOnProcessResult:
    stdout: str
    # None if timed out
    time_taken: Optional[timedelta]


def exec_start(args: ExecArgs, pipe_stdout: bool, pipe_stdin: bool = False) -> Popen[str]:
    args.print()
    assert not (args.quiet_stdout and pipe_stdout)
    return Popen(
        args.cmd,
        env=args.env,
        cwd=None if args.cwd is None else str(args.cwd),
        stdin=PIPE if pipe_stdin else None,
        stdout=DEVNULL if args.quiet_stdout else PIPE if pipe_stdout else None,
        text=True,
    )


def wait_on_process_with_timeout(
    process: Popen[str], start_time_seconds: float, timeout_seconds: float
) -> WaitOnProcessResult:
    assert is_process_alive(process)

    done = Event()

    killed = False

    def process_kill_function() -> None:
        nonlocal killed
        is_done = done.wait(timeout=timeout_seconds)
        if not is_done and is_process_alive(process):
            print(f"Process timed out after {timeout_seconds} seconds! Sending SIGINT")
            # process.send_signal(SIGINT)  # This causes ValueError: Unsupported signal: 2
            kill_process(process, time_allowed_seconds=1)
            killed = True

    process_killer = Thread(target=process_kill_function)
    process_killer.start()
    stdout, stderr = process.communicate()
    assert stderr is None
    returncode = process.wait()
    end_time_seconds = time()
    # If the process exited normally early, process_kill_function can exit.
    # (If it was killed, this will have no effect)
    done.set()
    process_killer.join()

    assert returncode == process.returncode
    assert killed or process.returncode == 0, f"Process failed with code {process.returncode}"

    return WaitOnProcessResult(
        stdout=stdout,
        time_taken=None if killed else timedelta(seconds=(end_time_seconds - start_time_seconds)),
    )


def kill_process(process: AnyPopen, time_allowed_seconds: float) -> None:
    assert is_process_alive(process)
    kill(process.pid, SIGINT)
    start_time_seconds = time()
    while is_process_alive(process):
        sleep(1)
        if (time() - start_time_seconds) > time_allowed_seconds:
            print(
                f"Process '{check_cast(str, process.args)}' refused to shut down normally. "
                + "Trying again without asking nicely."
            )
            process.kill()
            break
    assert not is_process_alive(process)


class ExecutableNotFoundException(Exception):
    def __init__(self, path: Path):
        self.path = path
        super().__init__(f"Cannot find {path}")


@with_slots
@dataclass(frozen=True)
class OutputAndExitCode:
    stdout: str
    exit_code: int


def exec_and_get_output_and_exit_code(args: ExecArgs) -> OutputAndExitCode:
    args.print()
    # These arguments don't apply here, should have their default values
    assert args.quiet_stdout is False and args.quiet_stderr is False

    try:
        r = run(args.cmd, stdout=PIPE, cwd=args.cwd, env=args.env, check=False)
    except FileNotFoundError:
        raise ExecutableNotFoundException(Path(args.cmd[0])) from None
    except NotADirectoryError:
        raise Exception(f"Invalid cwd: {args.cwd}") from None

    return OutputAndExitCode(decode_stdout(r.stdout), r.returncode)


def exec_and_get_output(args: ExecArgs, expect_exit_code: Optional[int] = None) -> str:
    expected_exit_code = option_or(expect_exit_code, 0)
    res = exec_and_get_output_and_exit_code(args)
    assert (
        res.exit_code == expected_exit_code
    ), f"Returned with code {res.exit_code}, expected {expected_exit_code}"
    return res.stdout


@with_slots
@dataclass(frozen=True)
class ProcessResult:
    exit_code: int
    stdout: str
    stderr: str


def exec_and_get_result(args: ExecArgs) -> ProcessResult:
    args.print()
    # These arguments don't apply here, should have their default values
    assert args.quiet_stdout is False and args.quiet_stderr is False
    try:
        r = run(args.cmd, stdout=PIPE, stderr=PIPE, cwd=args.cwd, env=args.env, check=False)
    except FileNotFoundError:
        raise Exception(f"Cannot find {args.cmd[0]}") from None

    return ProcessResult(
        exit_code=r.returncode, stdout=decode_stdout(r.stdout), stderr=decode_stdout(r.stderr)
    )


def decode_stdout(stdout: bytes) -> str:
    # Microsoft trademark confuses python
    stdout = stdout.replace(b"\xae", b"")
    return stdout.decode("utf-8").strip().replace("\r", "")


def exec_and_expect_output(args: ExecArgs, expected_output: str, err: str) -> None:
    output = exec_and_get_output(args)
    if output != expected_output:
        print("actual:", repr(output))
        print("expect:", repr(expected_output))
        raise Exception(err)


_BYTES_PER_KB: int = 2 ** 10
_BYTES_PER_MB: int = 2 ** 20
_BYTES_PER_GB: int = 2 ** 30


def bytes_to_kb(n_bytes: Union[int, float]) -> float:
    return n_bytes / _BYTES_PER_KB


def bytes_to_mb(n_bytes: Union[int, float]) -> float:
    return n_bytes / _BYTES_PER_MB


def bytes_to_gb(n_bytes: Union[int, float]) -> float:
    return n_bytes / _BYTES_PER_GB


def kb_to_bytes(kb: float) -> int:
    return round(kb * _BYTES_PER_KB)


def mb_to_bytes(mb: float) -> int:
    return round(mb * _BYTES_PER_MB)


def gb_to_bytes(gb: float) -> int:
    return round(gb * _BYTES_PER_GB)


def kb_to_mb(kb: float) -> float:
    return bytes_to_mb(kb_to_bytes(kb))


def mb_to_gb(mb: float) -> float:
    return bytes_to_gb(mb_to_bytes(mb))


def gb_to_mb(gb: float) -> float:
    return bytes_to_mb(gb_to_bytes(gb))


MSECS_PER_SECOND = 1000
USECS_PER_SECOND = 1_000_000


def show_size_bytes(n_bytes: float) -> str:
    return show_in_units(
        n_bytes,
        (Unit(_BYTES_PER_GB, "GB"), Unit(_BYTES_PER_MB, "MB"), Unit(_BYTES_PER_KB, "KB")),
        Unit(1, "bytes"),
    )


@with_slots
@dataclass(frozen=True)
class Unit:
    amount: float
    name: str


def show_in_units(amount: float, units: Sequence[Unit], base_unit: Unit) -> str:
    # Find a unit where this is >= 1 of it
    unit = option_or(find(lambda u: abs(amount) >= u.amount, units), base_unit)
    amount_in_units = (
        str(amount) if unit.amount == 1 and amount % 1 == 0 else "%.2f" % (amount / unit.amount)
    )
    return amount_in_units + f" {unit.name}"


def seconds_to_msec(seconds: float) -> float:
    return seconds * MSECS_PER_SECOND


def seconds_to_usec(seconds: float) -> float:
    return seconds * USECS_PER_SECOND


def msec_to_seconds(msec: float) -> float:
    return msec / MSECS_PER_SECOND


def mhz_to_ghz(mhz: float) -> float:
    return mhz / 1000


# Python's os.walk won't work, because it takes Strings and not paths.
# Unfortunately `Path(str(path))` isn't the identity if path is `//machine/`. (Python bug?)
def walk_files_recursive(path: Path, filter_dir: Callable[[Path], bool]) -> Iterable[Path]:
    for sub in path.iterdir():
        if sub.is_dir():
            if filter_dir(sub):
                for x in walk_files_recursive(sub, filter_dir):
                    yield x
        else:
            yield sub


def get_hostname() -> str:
    return node()


# TODO:MOVE
def assert_admin() -> None:
    if not is_admin():
        raise Exception(
            "PerfView requires you to be an administrator"
            if os_is_windows()
            else "cgcreate requires you to be a super user"
        )


def is_admin() -> bool:
    if os_is_windows():
        # Do this import lazily as it is only available on Windows
        from win32com.shell.shell import IsUserAnAdmin  # pylint:disable=import-outside-toplevel

        return IsUserAnAdmin()
    else:
        # Importing it this way since geteuid doesn't exist in windows and mypy complains there
        geteuid = cast(Callable[[], int], getattr(os, "geteuid"))
        return geteuid() == 0


def get_extension(path: Path) -> str:
    return splitext(path.name)[1]


def add_extension(p: Path, ext: str) -> Path:
    return p.parent / f"{p.name}.{ext}"


def remove_extension(p: Path) -> Path:
    return p.parent / splitext(p.name)[0]


def change_extension(p: Path, ext: str) -> Path:
    return add_extension(remove_extension(p), ext)


def get_or_did_you_mean(mapping: Mapping[str, V], key: str, name: str) -> V:
    try:
        return mapping[key]
    except KeyError:
        raise Exception(did_you_mean(tuple(mapping.keys()), key, name)) from None


def did_you_mean(
    choices: Iterable[str], choice: str, name: str, show_choice: Callable[[str], str] = identity
) -> str:
    assert choice not in choices
    # Mypy has the return type of get_close_matches wrong?
    close = check_cast(Sequence[str], get_close_matches(choice, choices))  # type: ignore
    if is_empty(close):
        choices = tuple(choices)
        if len(choices) < 20:
            return f"Bad {name} {show_choice(choice)}. Available: {tuple(choices)}"
        else:
            return f"Bad {name} {show_choice(choice)}."
    elif len(close) == 1:
        return f"Bad {name} {show_choice(choice)}. Did you mean {show_choice(close[0])}?"
    else:
        close_str = "\n".join(tuple(show_choice(c) for c in close))
        return f"Bad {name} {show_choice(choice)}. Did you mean one of:\n{close_str}"


def hex_no_0x(i: int) -> str:
    return remove_str_start(hex(i), "0x")


def try_parse_single_tag_from_xml_document(path: Path, tag_name: str) -> Optional[str]:
    assert tag_name.startswith("{"), "Should start with schema"

    root = parse_xml(str(path)).getroot()
    tags = tuple(_iter_tag_recursive(root, tag_name))
    if is_empty(tags):
        return None
    else:
        assert len(tags) == 1  # Should only be specified once
        tag = tags[0]
        return tag.text


def _iter_tag_recursive(e: Element, tag_name: str) -> Iterable[Element]:
    for child in e:
        if child.tag == tag_name:
            yield child
        else:
            yield from _iter_tag_recursive(child, tag_name)


# Note: WeakKeyDictionary does not seem to work on CLR types. So using this hack instead.
def lazy_property(obj: T, f: Callable[[T], U], name: Optional[str] = None) -> U:
    if name is None:
        # Mypy expects f to be a "FunctionType", but I don't know how  to import that
        name = f"{getfile(cast(Any, f))}/{f.__name__}"
    res: Optional[U] = getattr(obj, name, None)
    if res is None:
        res = f(obj)
        assert res is not None
        setattr(obj, name, res)
    return res


def opt_max(i: Iterable[float]) -> Optional[float]:
    try:
        return max(i)
    except ValueError:
        return None


def opt_median(i: Iterable[float]) -> Optional[float]:
    try:
        return median(i)
    except StatisticsError:
        return None


# numpy has problems on ARM, so using this instead.
def get_percentile(values: Sequence[float], percent: float) -> float:
    assert not is_empty(values)
    assert 0.0 <= percent <= 100.0
    sorted_values = sorted(values)
    fraction = percent / 100.0
    index_and_fraction = (len(values) - 1) * fraction
    prev_index = floor(index_and_fraction)
    next_index = ceil(index_and_fraction)
    # The closer we are to 'next_index', the more 'next' should matter
    next_factor = index_and_fraction - prev_index
    prev_factor = 1.0 - next_factor
    return sorted_values[prev_index] * prev_factor + sorted_values[next_index] * next_factor


def get_95th_percentile(values: Sequence[float]) -> Result[str, float]:
    return Err("<no values>") if is_empty(values) else Ok(get_percentile(values, 95))


def update_file(path: Path, text: str) -> None:
    if (not path.exists()) or path.read_text(encoding="utf-8") != text:
        print(f"Updating {path}")
        path.write_text(text, encoding="utf-8")


# When we run a test with 'sudo', we need to make sure other users can access the file
def give_user_permissions(file: Path) -> None:
    flags = S_IREAD | S_IWRITE | S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH
    file.chmod(flags)


def check_no_processes(names: Sequence[str]) -> None:
    assert all(name.islower() for name in names)
    for proc in process_iter():
        for name in names:
            suggestion = {
                OS.posix: f"pkill -f {name}",
                OS.windows: f'Get-Process | Where-Object {{$_.Name -like "{name}"}} | Stop-Process',
            }[get_os()]
            assert name not in proc.name().lower(), (
                f"'{name}' is already running\n" + f"Try: `{suggestion}`"
            )


def get_command_line() -> str:
    return f"> py {' '.join(argv)}"
