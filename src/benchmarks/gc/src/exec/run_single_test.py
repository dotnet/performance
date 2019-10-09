# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from contextlib import contextmanager
from dataclasses import dataclass
from os import environ
from pathlib import Path
from random import randint
from signal import SIGINT
from shutil import which
from subprocess import PIPE, Popen
from sys import executable as py
from tempfile import TemporaryDirectory
from time import sleep, time
from typing import Iterable, Iterator, Mapping, Optional, Sequence, Tuple

from psutil import process_iter

from ..analysis.core_analysis import get_process_info, process_predicate_from_id
from ..analysis.clr import Clr
from ..analysis.types import ProcessInfo

from ..commonlib.bench_file import (
    Benchmark,
    BenchOptions,
    CollectKind,
    GCPerfSimResult,
    LogOptions,
    SingleTestCombination,
    TestConfigCombined,
    TestConfigContainer,
    TestPaths,
    TestRunStatus,
)
from ..commonlib.get_built import Built, CoreclrPaths
from ..commonlib.collection_util import (
    combine_mappings,
    empty_mapping,
    empty_sequence,
    find,
    is_empty,
)
from ..commonlib.config import GC_PATH, EXEC_ENV_PATH, PERFVIEW_PATH
from ..commonlib.option import map_option, non_null, optional_to_iter, option_or, option_or_3
from ..commonlib.parse_and_serialize import parse_yaml
from ..commonlib.type_utils import with_slots
from ..commonlib.util import (
    args_with_cmd,
    assert_admin,
    check_no_processes,
    decode_stdout,
    ensure_empty_dir,
    ExecArgs,
    exec_and_expect_output,
    exec_and_get_output,
    exec_and_get_result,
    exec_cmd,
    exec_start,
    ExecutableNotFoundException,
    gb_to_mb,
    give_user_permissions,
    hex_no_0x,
    is_admin,
    kill_process,
    mb_to_bytes,
    os_is_windows,
    seconds_to_usec,
    unlink_if_exists,
    USECS_PER_SECOND,
    wait_on_process_with_timeout,
)


_TEST_MIN_SECONDS_DEFAULT = 10.0
_TEST_MAX_SECONDS_DEFAULT = 90.0


@with_slots
@dataclass(frozen=True)
class SingleTest:
    """
    Unlike SingleTestCombination, contains processed information for actually executing the test.
    """

    test: SingleTestCombination
    coreclr: Optional[CoreclrPaths]
    test_exe: Path
    options: BenchOptions
    default_env: Mapping[str, str]

    @property
    def coreclr_name(self) -> str:
        return self.test.coreclr_name

    @property
    def benchmark_name(self) -> str:
        return self.test.benchmark_name

    @property
    def benchmark(self) -> Benchmark:
        return self.test.benchmark.benchmark

    @property
    def config_name(self) -> str:
        return self.test.config_name

    @property
    def config(self) -> TestConfigCombined:
        return TestConfigCombined(self.test.config.config)


# Writes to out_path.etl, out_path.yaml, and out_path as a directory
def run_single_test(built: Built, t: SingleTest, out: TestPaths) -> TestRunStatus:
    check_no_test_processes()
    partial_test_status = _do_run_single_test(built, t, out)
    gcperfsim_result = (
        _parse_gcperfsim_result(partial_test_status.stdout)
        if t.benchmark.executable is None
        or any(t.benchmark.executable.endswith(x) for x in ("GCPerfSim", "GCPerfSim.exe"))
        else None
    )
    test_status = TestRunStatus(
        test=t.test,
        success=partial_test_status.success,
        process_id=partial_test_status.process_id,
        seconds_taken=partial_test_status.seconds_taken,
        trace_file_name=partial_test_status.trace_file_name,
        stdout=partial_test_status.stdout,
        gcperfsim_result=gcperfsim_result,
    )

    out.write_test_status(test_status)
    if not os_is_windows() and is_admin():
        give_user_permissions(out.test_status_path)

    if test_status.success:
        print(f"Took {test_status.seconds_taken} seconds")
        min_seconds = option_or_3(
            t.benchmark.min_seconds, t.options.default_min_seconds, _TEST_MIN_SECONDS_DEFAULT
        )
        if test_status.seconds_taken < min_seconds:
            desc = f"coreclr={t.coreclr_name} config={t.config_name} benchmark={t.benchmark_name}"
            raise Exception(
                f"{desc} took {test_status.seconds_taken} seconds, minimum is {min_seconds}"
                "(you could change the benchmark's min_seconds or options.default_min_seconds)"
            )
    else:
        print("Test failed, continuing...")

    sleep(1)  # Give process time to close
    check_no_test_processes()
    return test_status


def _parse_gcperfsim_result(stdout: str) -> GCPerfSimResult:
    # Everything before the marker is ignored
    marker = "=== STATS ==="
    try:
        idx = stdout.index(marker)
    except ValueError:
        print(f"STDOUT: '{stdout}'")
        raise Exception(f"GCPerfSim stdout does not include '{marker}'") from None
    yaml = stdout[idx + len(marker) :]
    return parse_yaml(GCPerfSimResult, yaml)


@with_slots
@dataclass(frozen=True)
class _PartialTestRunStatus:
    """Compared to TestRunStatus, this is missing perfview_result, we parse that at the end."""

    success: bool
    process_id: int
    seconds_taken: float
    trace_file_name: Optional[str]
    stdout: str


def _do_run_single_test(built: Built, t: SingleTest, out: TestPaths) -> _PartialTestRunStatus:
    if t.options.collect == CollectKind.none:
        return _run_single_test_no_collect(built, t, out)
    elif t.options.always_use_dotnet_trace or not os_is_windows():
        return _run_single_test_dotnet_trace(built, t, out)
    else:
        return _run_single_test_windows_perfview(built, t, out)


# Use this instead of TemporaryDirectory if you want to analyze the output
@contextmanager
def NonTemporaryDirectory(name: str) -> Iterator[Path]:
    yield GC_PATH / "temp" / (name + str(randint(0, 99)))


def run_single_test_temporary(clr: Clr, built: Built, t: SingleTest) -> ProcessInfo:
    with TemporaryDirectory(t.coreclr_name) as td:
        temp = Path(td)
        paths = TestPaths(temp / "temp")
        test_status = run_single_test(built, t, paths)
        # TODO: configurable process_predicate
        trace_file = non_null(paths.trace_file_path(test_status))
        return get_process_info(
            clr, trace_file, str(trace_file), process_predicate_from_id(test_status.process_id)
        )


def check_env() -> Mapping[str, str]:
    e = environ
    for k in e.keys():
        if any(k.lower().startswith(start) for start in ("complus", "core_root")):
            raise Exception(f"Environment variable '{k}' should not be set")
    return e


TEST_PROCESS_NAMES: Sequence[str] = ("corerun", "dotnet", "make_memory_load", "perfview")


def check_no_test_processes() -> None:
    check_no_processes(TEST_PROCESS_NAMES)


def kill_test_processes() -> None:
    for proc in process_iter():
        if any(name in proc.name().lower() for name in TEST_PROCESS_NAMES):
            print(f"Killing {proc.name()}")
            for _ in range(10):
                proc.kill()
                # Sometimes it's still alive after being killed ...
                sleep(1)
                if not proc.is_running():
                    break
            assert not proc.is_running()
    check_no_test_processes()


_PERFVIEW_ALWAYS_ARGS: Sequence[str] = (
    "-NoV2Rundown",
    "-NoNGENRundown",
    "-NoRundown",
    "-AcceptEULA",
    "-NoGUI",
    "-Merge:true",
    "-SessionName:CoreGCBench",  # TODO:necessary?
    "-zip:false",
)


def get_perfview_run_cmd(
    built: Built,
    t: SingleTest,
    log_file: Path,
    trace_file: Path,
    perfview: bool = True,
    ignore_container: bool = False,
) -> Sequence[str]:
    test_args = _get_windows_test_cmd(built, t, ignore_container).command
    if perfview:
        # Since this is a perf test, we don't want to waste time logging the output.
        # We will log errors though.
        return (
            str(PERFVIEW_PATH),
            *_get_perfview_collect_or_run_common_args(t, log_file, trace_file),
            "run",
            *test_args,
        )
    else:
        return test_args


@with_slots
@dataclass(frozen=True)
class CommandAndIsRunInJob:
    command: Sequence[str]
    is_run_in_job: bool


def _get_windows_test_cmd(
    built: Built, t: SingleTest, ignore_container: bool
) -> CommandAndIsRunInJob:
    test_args: Sequence[str] = _benchmark_command(t)
    if (t.config.container is not None or t.config.affinitize) and not ignore_container:
        c = t.config.container
        assert c is None or c.image_name is None, "TODO"
        return CommandAndIsRunInJob(
            command=(
                str(built.win.run_in_job_exe),
                *(
                    empty_sequence()
                    if c is None or c.memory_mb is None
                    else ["--memory-mb", str(c.memory_mb)]
                ),
                *(empty_sequence() if not t.config.affinitize else ["--affinitize"]),
                *(
                    empty_sequence()
                    if c is None or c.cpu_rate_hard_cap is None
                    else ["--cpu-rate-hard-cap", str(c.cpu_rate_hard_cap)]
                ),
                "--",
                *test_args,
            ),
            is_run_in_job=True,
        )
    else:
        return CommandAndIsRunInJob(command=test_args, is_run_in_job=False)


def _get_perfview_start_or_stop_cmd(
    t: SingleTest, log_file: Path, trace_file: Path, is_start: bool
) -> Sequence[str]:
    return (
        str(PERFVIEW_PATH),
        "start" if is_start else "stop",
        *_get_perfview_collect_or_run_common_args(t, log_file, trace_file),
    )


_DEFAULT_MAX_TRACE_SIZE_GB = 1


def _get_perfview_collect_or_run_common_args(
    t: SingleTest, log_file: Path, trace_file: Path
) -> Sequence[str]:
    collect_args: Sequence[str] = {
        CollectKind.gc: ["-GCCollectOnly"],
        CollectKind.verbose: ["-GCCollectOnly", "-ClrEventLevel:Verbose"],
        # Need default kernel events to get the process name
        CollectKind.cpu_samples: [
            "-OnlyProviders:ClrPrivate:1:5,Clr:1:5",
            "-ClrEvents:GC+Stack",
            "-ClrEventLevel:Verbose",
            "-KernelEvents:Default",
        ],
        CollectKind.cswitch: [
            # Use verbose events (4 instead of 5)
            "-OnlyProviders:ClrPrivate:1:5,Clr:1:5",
            "-ClrEvents:GC+Stack",
            f"-KernelEvents:Default,ContextSwitch",
        ],
    }[t.options.get_collect]
    max_trace_size_mb = round(
        gb_to_mb(option_or(t.options.max_trace_size_gb, _DEFAULT_MAX_TRACE_SIZE_GB))
    )
    return (
        *_PERFVIEW_ALWAYS_ARGS,
        *collect_args,
        # This option prevents perfview from opening a console
        # and requiring the user to press enter
        f"-LogFile:{log_file}",
        f"-CircularMB:{max_trace_size_mb}",
        f"-BufferSizeMB:{max_trace_size_mb}",
        f"-DataFile:{trace_file}",
    )


def log_env(log: Optional[LogOptions], path: Path) -> Mapping[str, str]:
    if log is None:
        return empty_mapping()
    else:
        return {
            "COMPlus_GCLogEnabled": "1",
            "COMPlus_GCLogFile": str(path),
            "COMPlus_GCLogFileSize": hex_no_0x(option_or(log.file_size_mb, 0x30)),
            "COMPlus_SOEnableDefaultRWValidation": "0",  # TODO: is this needed?
            # This env var no longer exists, must recompile to change level
            # "COMPlus_GCprnLvl": hex_no_0x(log.level),
        }


def _run_single_test_windows_perfview(
    built: Built, t: SingleTest, out: TestPaths
) -> _PartialTestRunStatus:
    assert_admin()
    ensure_empty_dir(out.out_path_base)

    # Start with the memory load
    mem_load_pct = t.config.memory_load_percent
    mem_load_process = None
    if mem_load_pct is not None:
        print("setting up memory load...")
        mem_load_process = Popen(
            args=(str(built.win.make_memory_load), "-percent", str(mem_load_pct)), stderr=PIPE
        )
        assert mem_load_process.stderr is not None
        # Wait on it to start up
        line = decode_stdout(mem_load_process.stderr.readline())
        assert line == "make_memory_load finished starting up"
        print("done")

    log_file = out.add_ext("perfview-log.txt")
    trace_file = out.add_ext("etl")
    timeout_seconds = _get_timeout(t)

    exec_and_expect_output(
        ExecArgs(_get_perfview_start_or_stop_cmd(t, log_file, trace_file, is_start=True)),
        expected_output="",
        err="PerfView start failed",
    )

    start_time_seconds = time()
    test_cmd = _get_windows_test_cmd(built, t, ignore_container=False)
    run_process = exec_start(_get_exec_args(test_cmd.command, t, out), pipe_stdout=True)

    run_result = wait_on_process_with_timeout(
        run_process, start_time_seconds=start_time_seconds, timeout_seconds=timeout_seconds
    )

    exec_and_expect_output(
        ExecArgs(_get_perfview_start_or_stop_cmd(t, log_file, trace_file, is_start=False)),
        expected_output="",
        err="PerfView stop failed",
    )

    if run_result.time_taken is None:
        kill_test_processes()
    elif mem_load_process is not None:
        # Releasing all the memory can take a while, so give it plenty of time
        kill_process(mem_load_process, time_allowed_seconds=60)
        # WIll have a return code of 2 because we interrupted it.
        assert mem_load_process.returncode == 2

    _rename_gcperfsim_out(out)

    success = (
        run_process.returncode == 0
        and run_result.time_taken is not None
        and not _perfview_has_warnings(log_file.read_text(encoding="utf-8"))
    )

    # If running in job, run_process.pid is run-in-job's PID, not the test process' PID.
    # So it prints 'PID: 123' before exiting.
    stdout, process_id = (
        _strip_pid(run_result.stdout)
        if test_cmd.is_run_in_job
        else (run_result.stdout, run_process.pid)
    )

    return _PartialTestRunStatus(
        success=success,
        process_id=process_id,
        seconds_taken=timeout_seconds
        if run_result.time_taken is None
        else run_result.time_taken.total_seconds(),
        trace_file_name=trace_file.name,
        stdout=stdout,
    )


def _strip_pid(stdout: str) -> Tuple[str, int]:
    pid_str = "PID: "
    idx = stdout.rindex(pid_str)
    return stdout[:idx].rstrip(), int(stdout[idx + len(pid_str) :])


def _rename_only_file_in_dir(dir_path: Path, expected_suffix: str, target: Path) -> None:
    files_in_dir = tuple(dir_path.iterdir())
    if not is_empty(files_in_dir):
        assert len(files_in_dir) == 1
        file = files_in_dir[0]
        # assert file.name.endswith(expected_suffix)
        if file.name.endswith(expected_suffix):
            file.rename(target)
        else:
            file.unlink()
    dir_path.rmdir()


_ALLOWED_WARNINGS: Sequence[str] = (
    # Some coreclr benchmarks return 100 for some reason
    "warning: command exited with non-success error code 0x64",
    "warning: newly-allocated dummy ended up in gen 1",
    "warning no _nt_symbol_path set ...",
)
for w in _ALLOWED_WARNINGS:
    assert w.islower()


def _perfview_has_warnings(text: str) -> bool:
    text_lower = text.lower()

    if any(s in text_lower for s in ("process is terminating", "unhandled exception")):
        return True

    def is_allowed_warning(idx: int) -> bool:
        rest = text_lower[idx:]
        return any(rest.startswith(w) for w in _ALLOWED_WARNINGS)

    warning_index = find(
        lambda idx: not is_allowed_warning(idx), _substring_locations(text_lower, "warning")
    )
    if warning_index is not None:
        print("Failing due to warning: ", text[warning_index:].split("\n")[0])
        return True
    else:
        return False


def _get_timeout(t: SingleTest) -> float:
    return option_or_3(
        t.benchmark.max_seconds, t.options.default_max_seconds, _TEST_MAX_SECONDS_DEFAULT
    )


def _run_single_test_no_collect(
    _built: Built, t: SingleTest, out: TestPaths
) -> _PartialTestRunStatus:
    if t.options.log is not None:
        raise Exception("TODO")
    if t.config.memory_load_percent is not None:
        # The script only works on windows right now
        raise Exception("TODO")

    out.out_path_base.mkdir()  # GCPerfSim will write here

    test_process_args: Sequence[str] = _benchmark_command(t)
    args = _get_exec_args(test_process_args, t, out)

    container = t.config.container
    timeout_seconds = _get_timeout(t)

    if container is not None or t.config.affinitize:
        raise Exception("TODO")

    start_time_seconds = time()
    process = exec_start(args, pipe_stdout=True)
    wait_result = wait_on_process_with_timeout(process, start_time_seconds, timeout_seconds)

    gcperfsim_out = _rename_gcperfsim_out(out)
    if container is not None:
        give_user_permissions(gcperfsim_out)

    # TODO: handle failure
    return _PartialTestRunStatus(
        success=wait_result.time_taken is not None,
        process_id=process.pid,
        seconds_taken=timeout_seconds
        if wait_result.time_taken is None
        else wait_result.time_taken.total_seconds(),
        trace_file_name=None,
        stdout=wait_result.stdout,
    )


def _run_single_test_dotnet_trace(
    _built: Built, t: SingleTest, out: TestPaths
) -> _PartialTestRunStatus:
    if t.options.log is not None:
        raise Exception("TODO")
    if t.config.affinitize:
        raise Exception("TODO")
    if t.config.memory_load_percent is not None:
        # The script only works on windows right now
        raise Exception("TODO")

    test_process_args: Sequence[str] = _benchmark_command(t)

    trace_out_dir = out.out_path_base.parent / "trace"
    trace_out_dir.mkdir()

    out.out_path_base.mkdir()  # GCPerfSim will write here
    args = _get_exec_args(test_process_args, t, out)

    container = t.config.container

    start_time_seconds = time()
    if container is not None:
        process_to_wait_on, pid_to_trace = _launch_process_in_cgroup(container, args)
    else:
        process_to_wait_on = exec_start(args, pipe_stdout=True)
        pid_to_trace = process_to_wait_on.pid

    trace_file = out.add_ext("nettrace")

    dotnet_trace = exec_start(
        _get_dotnet_trace_args(t.options, pid_to_trace, trace_file), pipe_stdout=False
    )

    timeout_seconds = _get_timeout(t)
    wait_result = wait_on_process_with_timeout(
        process_to_wait_on, start_time_seconds=start_time_seconds, timeout_seconds=timeout_seconds
    )

    # Shouldn't take more than 10 seconds to shut down
    trace_stdout, trace_stderr = dotnet_trace.communicate("\n", timeout=10)
    assert trace_stdout is None and trace_stderr is None

    if container is not None:
        _delete_cgroup()

    # WARN: If the test is too short and no events fire, the output file will not be written to.
    _rename_only_file_in_dir(trace_out_dir, expected_suffix=".nettrace", target=trace_file)

    gcperfsim_out = _rename_gcperfsim_out(out)

    if container is not None:
        for file in (trace_file, gcperfsim_out):
            give_user_permissions(file)

    # TODO: handle failure
    return _PartialTestRunStatus(
        success=wait_result.time_taken is not None,
        process_id=pid_to_trace,
        seconds_taken=timeout_seconds
        if wait_result.time_taken is None
        else wait_result.time_taken.total_seconds(),
        trace_file_name=trace_file.name,
        stdout=wait_result.stdout,
    )


def _get_dotnet_trace_path(options: BenchOptions) -> Path:
    if options.dotnet_trace_path is not None:
        return Path(options.dotnet_trace_path)
    else:
        res = which("dotnet-trace")
        if res is None:
            raise Exception("Can't find 'dotnet-trace' installed.")
        else:
            return Path(res)


def _get_dotnet_trace_args(options: BenchOptions, pid_to_trace: int, trace_file: Path) -> ExecArgs:
    # if collect == CollectKind.gc:
    #    # 1 means GC, 4 means informational
    #    providers = "Microsoft-Windows-DotNETRuntime:1:4"
    # elif collect == CollectKind.verbose:
    #    # 5 means verbose
    #    providers = "Microsoft-Windows-DotNETRuntime:1:5"
    # else:
    #    raise Exception(f"TODO: handle collect kind {collect} on linux")

    profile = {
        CollectKind.gc: "gc-collect",
        CollectKind.verbose: "gc-verbose",
        CollectKind.cpu_samples: "cpu-sampling",
    }[options.get_collect]

    args: Sequence[str] = (
        str(_get_dotnet_trace_path(options)),
        "collect",
        "--process-id",
        str(pid_to_trace),
        "--output",
        str(trace_file),
        *(
            empty_sequence()
            if options.dotnet_trace_buffersize_mb is None
            else ["--buffersize", str(options.dotnet_trace_buffersize_mb)]
        ),
        # TODO: use args.collect option to determine providers
        # "--providers",
        # 1 = GC, 4 = informational (5 = verbose)
        # providers,
        "--profile",
        profile,
    )
    env: Mapping[str, str] = empty_mapping() if options.dotnet_path is None else {
        "DOTNET_ROOT": str(options.dotnet_path.parent)
    }
    return ExecArgs(args, env=env)


_WRITE_PID_AND_EXEC_PATH = EXEC_ENV_PATH / "write_pid_and_exec.py"
_PID_FILE_PATH = EXEC_ENV_PATH / "__pid.txt"

_ALL_CGROUP_CATEGORIES: Sequence[str] = ("memory", "cpu")

_CGROUP_NAME = "gc-test-cgroup"


def _get_cgroup_categories_and_name(container: TestConfigContainer) -> str:
    categories: Sequence[str] = (
        (
            *optional_to_iter(None if container.memory_mb is None else "memory"),
            *optional_to_iter(None if container.cpu_rate_hard_cap is None else "cpu"),
        )
    )
    assert all(c in _ALL_CGROUP_CATEGORIES for c in categories)

    return f"{','.join(categories)}:{_CGROUP_NAME}"


def _create_cgroup(container: TestConfigContainer) -> None:
    memory_mb = container.memory_mb
    cpu_cap = container.cpu_rate_hard_cap

    assert memory_mb is not None or cpu_cap is not None

    if _cgroup_exists():
        # cgroup_exists() may be a false positive, so allow failure
        _delete_cgroup()

    assert not _cgroup_exists()

    exec_cmd(ExecArgs(("cgcreate", "-g", _get_cgroup_categories_and_name(container))))
    if memory_mb is not None:
        exec_cmd(
            ExecArgs(
                ("cgset", "-r", f"memory.limit_in_bytes={mb_to_bytes(memory_mb)}", _CGROUP_NAME)
            )
        )

    if cpu_cap is not None:
        quota = round(seconds_to_usec(cpu_cap))
        exec_cmd(
            ExecArgs(
                (
                    "cgset",
                    "-r",
                    f"cpu.cfs_period_us={USECS_PER_SECOND}",
                    f"cpu.cfs_quota_us={quota}",
                    _CGROUP_NAME,
                )
            )
        )


def _ls_cgroup() -> str:
    try:
        return exec_and_get_output(ExecArgs(("lscgroup",), quiet_print=True))
    except ExecutableNotFoundException:
        raise Exception("cgroup-tools is not installed.")


# WARN: this sometimes has a false positive
def _cgroup_exists() -> bool:
    return _CGROUP_NAME in _ls_cgroup()


def _delete_cgroup() -> None:
    assert _cgroup_exists()

    result = exec_and_get_result(
        ExecArgs(("cgdelete", "-g", f"{','.join(_ALL_CGROUP_CATEGORIES)}:{_CGROUP_NAME}"))
    )
    assert result.exit_code
    if result.exit_code == 96:
        # Sometimes 'lscgroup' tells us the group exists, but then it doesn't. Just allow this.
        assert (
            result.stderr
            == f"cgdelete: cannot remove group '{_CGROUP_NAME}': No such file or directory"
        )
        assert result.stdout == ""
    else:
        assert result.exit_code == 0 and result.stdout == "" and result.stderr == ""

    assert not _cgroup_exists()


# Returns: The 'cgexec' process to wait on, and the PID of the *inner* process.
def _launch_process_in_cgroup(
    container: TestConfigContainer, args: ExecArgs
) -> Tuple["Popen[str]", int]:
    assert_admin()

    _create_cgroup(container)

    unlink_if_exists(_PID_FILE_PATH)

    # TODO: Would be nice to have a better way to get the PID of the inner process.
    cgexec_args = args_with_cmd(
        args,
        (
            "cgexec",
            "-g",
            _get_cgroup_categories_and_name(container),
            py,
            str(_WRITE_PID_AND_EXEC_PATH),
            *args.cmd,
        ),
    )
    cgexec_process = exec_start(cgexec_args, pipe_stdout=True)
    # TODO: lower timeout_seconds
    pid = int(
        _read_file_when_it_exists(_PID_FILE_PATH, timeout_seconds=0.5, time_between_tries=0.1)
    )

    _PID_FILE_PATH.unlink()
    return cgexec_process, pid


def _read_file_when_it_exists(path: Path, timeout_seconds: float, time_between_tries: float) -> str:
    start = time()
    while True:
        try:
            return path.read_text(encoding="utf-8")
        except FileNotFoundError:
            sleep(time_between_tries)
            time_taken = time() - start
            if time_taken > timeout_seconds:
                raise Exception(f"{path} still doesn't exist after {timeout_seconds} seconds")


def _rename_gcperfsim_out(out: TestPaths) -> Path:
    # Move the output file to avoid nested directoriess
    target = out.add_ext("output.txt")
    _rename_only_file_in_dir(out.out_path_base, expected_suffix="-output.txt", target=target)
    return target


def _get_exec_args(cmd: Sequence[str], t: SingleTest, out: TestPaths) -> ExecArgs:
    env = combine_mappings(
        t.default_env,
        t.config.env(map_option(t.coreclr, lambda c: c.core_root)),
        log_env(t.options.log, out.out_path_base),
    )
    return ExecArgs(
        cmd,
        env=env,
        # GCPerfSim will write a file `{pid}-output.txt` inside this directory
        cwd=out.out_path_base,
    )


# TODO: This isn't working yet. Uses lttng instead of eventpipe
def _run_single_test_linux_perfcollect(t: SingleTest, out: TestPaths) -> TestRunStatus:

    # TODO: Could launch sudo just for the part it needs?
    # TODO: running in sudo causes output files to only be readable by super user...
    #  A future perfcollect may fix this.
    assert_admin()

    ensure_empty_dir(out.out_path_base)

    cwd = non_null(t.coreclr).corerun.parent  # TODO: handle self-contained executables
    env = combine_mappings(
        t.config.env(map_option(t.coreclr, lambda c: c.core_root)),
        {"COMPlus_PerfMapEnabled": "1", "COMPlus_EnableEventLog": "1"},
    )
    cmd: Sequence[str] = _benchmark_command(t)
    print(f"cd {cwd}")
    print(" ".join(cmd))
    test_process = Popen(cmd, cwd=cwd, env=env)

    test_process_pid = test_process.pid
    # launch

    print("PID", test_process_pid)

    # Now launch that thing with the stuff
    perfcollect_cmd = (
        str(_PERFCOLLECT),
        "collect",
        str(out.out_path_base),
        "-gccollectonly",  # TODO: if I pass this, only event I get is EventID(200) ?
        "-pid",
        str(test_process_pid),
    )
    print(" ".join(perfcollect_cmd))
    # TODO: not sure cwd needs to be set...
    perfcollect_process = Popen(perfcollect_cmd, cwd=_PERFCOLLECT.parent)

    print("waiting on test...")

    test_process.wait()

    assert test_process.returncode == 0

    print("sending signal...")

    perfcollect_process.send_signal(SIGINT)

    print("waiting on perfcollect...")

    perfcollect_process.wait()
    assert perfcollect_process.returncode == 0

    print("Closed")

    raise Exception("TODO:finish")


_GC_KEYWORDS: Sequence[str] = (
    "GC",
    # Above isn't enough -- want GlobalHeapHistory ...
    # "GCHandle",
    # "GCHeapDump",
    # "GCSampledObjectAllocationHigh",
    # "GCHeapSurvivalAndMovement",
    # "GCHeapCollect",
    # "GCSampledObjectAllocationLow",
)
_GC_EVENTS_ID = 1
_GC_LEVEL = 5  # TODO: where is this documented?
_GC_PROVIDER: str = f"Microsoft-Windows-DotNETRuntime:0xFFFFFFFFFFFFFFFF:{_GC_LEVEL}"


# This is a bash script, no need to build
_PERFCOLLECT = Path("/home/anhans/work/corefx-tools/src/performance/perfcollect/perfcollect")


def _benchmark_command(t: SingleTest) -> Sequence[str]:
    return (
        *optional_to_iter(map_option(t.coreclr, lambda c: str(c.corerun))),
        str(t.test_exe),
        *t.benchmark.arguments_list,
    )


# TODO:MOVE
def _substring_locations(s: str, substr: str) -> Iterable[int]:
    idx = 0
    while True:
        idx = s.find(substr, idx)
        if idx == -1:
            break
        yield idx
        idx = idx + 1
