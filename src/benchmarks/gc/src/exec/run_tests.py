# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Mapping, Optional

from ..analysis.core_analysis import get_process_info, process_predicate_from_id
from ..analysis.clr import Clr, get_clr

from ..commonlib.bench_file import (
    BenchFileAndPath,
    get_benchmark,
    get_config,
    get_coreclr,
    get_this_machine,
    iter_tests_to_run,
    out_dir_for_bench_yaml,
    MAX_ITERATIONS_FOR_RUN_DOC,
    parse_bench_file,
    SingleTestCombination,
    TestConfigCombined,
)
from ..commonlib.get_built import BuildKind, Built, get_built, is_arm
from ..commonlib.collection_util import combine_mappings, is_empty
from ..commonlib.command import Command, CommandKind, CommandsMapping
from ..commonlib.host_info import HostInfo, read_this_machines_host_info
from ..commonlib.option import map_option, non_null, option_or
from ..commonlib.type_utils import argument, with_slots
from ..commonlib.util import (
    assert_file_exists,
    ensure_empty_dir,
    ExecArgs,
    exec_and_get_output,
    get_existing_absolute_file_path,
    get_os,
    hex_no_0x,
    mb_to_bytes,
    OS,
    os_is_windows,
)

from .run_single_test import (
    check_env,
    check_no_test_processes,
    get_perfview_run_cmd,
    log_env,
    run_single_test,
    SingleTest,
)


@with_slots
@dataclass(frozen=True)
class CheckRunOutputArgs:
    bench_file_path: Path = argument(
        name_optional=True, doc="Path to a benchfile that has already been run."
    )
    config: Optional[str] = argument(default=None, doc="Only check runs with this config name")
    bench: Optional[str] = argument(default=None, doc="Only check runs with this benchmark name")
    out_dir: Optional[Path] = argument(
        default=None, doc="Use this instead of the default output directory for the benchfile."
    )


def check_run_output(args: CheckRunOutputArgs) -> None:
    host_info = read_this_machines_host_info()
    clr = get_clr()
    for t in iter_tests_to_run(
        parse_bench_file(args.bench_file_path),
        get_this_machine(),
        max_iterations=None,
        out_dir=args.out_dir,
    ):
        if (args.config is None or args.config == t.config_name) and (
            args.bench is None or args.bench == t.benchmark_name
        ):
            test_status = t.out.load_test_status()
            if test_status.success:
                trace_file = t.out.trace_file_path(test_status)
                if trace_file is not None:
                    print(f"checking {trace_file}")
                    _check_test_run(clr, t.config, trace_file, host_info, test_status.process_id)
            else:
                print(f"{t} did not succeed")


@with_slots
@dataclass(frozen=True)
class RunArgs:
    bench_file_path: Path = argument(name_optional=True, doc="Path to the benchfile to run.")
    skip_coreclr_checks: bool = argument(
        default=False, doc="Set to true to avoid validating coreclrs."
    )
    overwrite: bool = argument(
        default=False,
        doc="""
    Set to true to delete the test output directory before running.
    """,
    )
    # Don't run tests where output etls already exist
    skip_where_exists: bool = argument(
        default=False,
        doc="""
    If this is set, the test runner will not run a test if the test output already exists.
    If neither this nor '--overwrite' are specified, it is an error for test output to already exist.
    """,
    )
    out: Optional[Path] = argument(
        default=None, doc="Set to specify a custom test output path instead of the default."
    )
    max_iterations: Optional[int] = argument(default=None, doc=MAX_ITERATIONS_FOR_RUN_DOC)
    use_debug_coreclrs: bool = argument(
        hidden=True,
        default=False,
        doc="""
    Set to use debug builds of coreclrs instead of release builds.
    Only works if you specified coreclrs with 'repo_path' instead of 'core_root'.
    """,
    )
    no_check_runs: bool = argument(
        default=False, doc="If set to true, test runs will not be validated."
    )


def run(args: RunArgs) -> None:
    bench_file_path = assert_file_exists(args.bench_file_path)
    bench = parse_bench_file(bench_file_path)
    out_dir = (
        out_dir_for_bench_yaml(bench_file_path, machine=get_this_machine())
        if args.out is None
        else args.out
    )
    built = get_built(
        bench.content.coreclrs,
        build_kind=BuildKind.forbid_out_of_date,
        use_debug_coreclrs=args.use_debug_coreclrs,
        skip_coreclr_checks=args.skip_coreclr_checks,
    )

    _assert_not_in_job(built)

    assert not (args.overwrite and args.skip_where_exists)

    assert (
        args.overwrite or args.skip_where_exists or not out_dir.exists()
    ), f"{out_dir} already exists (maybe you want to  `--overwrite` or `--skip-where-exists`?)"
    if not args.skip_where_exists:
        if args.overwrite:
            # A previous 'corerun' may be blocking us from emptying the directory
            check_no_test_processes()
        ensure_empty_dir(out_dir)

    _run_all_benchmarks(
        built, bench, args.skip_where_exists, args.max_iterations, out_dir, args.no_check_runs
    )


def _run_all_benchmarks(
    built: Built,
    bench: BenchFileAndPath,
    skip_where_exists: bool,
    max_iterations: Optional[int],
    out_dir: Path,
    no_check_runs: bool,
) -> None:
    host_info = read_this_machines_host_info()
    clr = None if no_check_runs else get_clr()
    default_env = check_env()
    for t in iter_tests_to_run(bench, get_this_machine(), max_iterations, out_dir):
        now = datetime.now().strftime("%H:%M:%S")
        print(f"{now} Running {t.out.out_path_base.name}")
        if not (skip_where_exists and t.out.exists()):
            test_status = run_single_test(
                built,
                SingleTest(
                    test=t.test,
                    coreclr=built.coreclrs[t.coreclr_name],
                    test_exe=_get_path(built, t.bench_file.paths, t.benchmark.get_executable),
                    options=t.bench_file.options,
                    default_env=default_env,
                ),
                t.out,
            )
            if clr is not None and test_status.success:
                trace_file = t.out.trace_file_path(test_status)
                if trace_file is not None:
                    _check_test_run(clr, t.config, trace_file, host_info, test_status.process_id)


def _assert_not_in_job(built: Built) -> None:
    # TOOD: on ARM IsProcessInJob seems to always return true
    if os_is_windows() and not is_arm():
        res = exec_and_get_output(ExecArgs(cmd=(str(built.win.is_in_job_exe),), quiet_print=True))
        assert not _str_to_bool(res), (
            "Should not run_tests within a job object\n"
            + "(ConEmu opens job objects, powershell/cmd do not)"
        )


def _str_to_bool(s: str) -> bool:
    if s == "true":
        return True
    elif s == "false":
        return False
    else:
        raise Exception(f"Unexpected output: {s}")


@with_slots
@dataclass(frozen=True)
class HowToRunTestArgs:
    bench_file: Path = argument(name_optional=True, doc="Path to a benchfile.")
    coreclr: Optional[str] = argument(
        default=None, doc="Coreclr name for the test. May omit if there is only one coreclr."
    )
    config: Optional[str] = argument(
        default=None, doc="Config name for the test. May omit if there is only one config."
    )
    benchmark: Optional[str] = argument(
        default=None, doc="Benchmark name for the test. May omit if there is only one benchmark."
    )
    skip_coreclr_checks: bool = argument(
        default=False, doc="Set to true to not validate the coreclr used."
    )
    debug: bool = argument(default=False, doc="Set to true to use debug builds from the coreclr.")
    perfview: bool = argument(
        default=False, doc="If set, the output command will include the PerfView command."
    )


def how_to_run_test(args: HowToRunTestArgs) -> None:
    bench = parse_bench_file(args.bench_file).content
    coreclr = get_coreclr(bench, args.coreclr)
    built = get_built(
        {coreclr.name: coreclr.coreclr},
        build_kind=BuildKind.forbid_out_of_date,
        use_debug_coreclrs=args.debug,
        skip_coreclr_checks=args.skip_coreclr_checks,
    )

    coreclr_paths = built.coreclrs[coreclr.name]
    config_and_name = get_config(bench, args.config)
    config = config_and_name.config
    benchmark_and_name = get_benchmark(bench, args.benchmark)
    benchmark = benchmark_and_name.benchmark

    env = combine_mappings(
        config.env(map_option(coreclr_paths, lambda c: c.core_root)),
        log_env(bench.options.log, Path.cwd() / "log"),
    )
    container = config.container
    if container:
        if container.image_name is not None:
            print(f"Warning: Expected to run in {container.image_name}")
        if container.memory_mb:
            # In GCHeap::Initialize, we will only use 75/100 of the container's memory.
            # But if COMPlus_GCHeapHardLimit is set, we use it all.
            actual_available_memory = mb_to_bytes(container.memory_mb * 75 / 100)
            env = combine_mappings(
                env, {"COMPlus_GCHeapHardLimit": hex_no_0x(actual_available_memory)}
            )

    env_str = _env_to_shell_commands(env)
    t = SingleTest(
        test=SingleTestCombination(
            machine=get_this_machine(),
            coreclr=coreclr,
            config=config_and_name.as_partial,
            benchmark=benchmark_and_name,
        ),
        coreclr=coreclr_paths,
        test_exe=_get_path(built, bench.paths, benchmark.get_executable),
        options=bench.options,
        default_env={},
    )
    cmd = get_perfview_run_cmd(
        built, t, Path("log.txt"), Path("trace.etl"), perfview=args.perfview, ignore_container=True
    )
    print(f"{env_str}{' '.join(cmd)}")


def _quote_if_necessary(s: str) -> str:
    try:
        int(s)
        return s
    except ValueError:
        return f'"{s}"'


def _env_to_shell_commands(env: Mapping[str, str]) -> str:
    prefix = {OS.windows: "$env:", OS.posix: "export "}[get_os()]
    return "".join(f"{prefix}{k}={_quote_if_necessary(v)}\n" for k, v in env.items())


def _check_test_run(
    clr: Clr, config: TestConfigCombined, trace_path: Path, host_info: HostInfo, process_id: int
) -> None:
    proc = get_process_info(clr, trace_path, str(trace_path), process_predicate_from_id(process_id))

    assert not is_empty(proc.gcs), f"{trace_path} has no GCs"

    for gc in proc.gcs:
        ghh = non_null(gc.GlobalHeapHistory)
        num_heaps = ghh.NumHeaps
        if config.complus_gcheapcount is not None or not config.complus_gcserver:
            expect_heaps = (
                option_or(config.complus_gcheapcount, host_info.n_logical_processors)
                if config.complus_gcserver
                else 1
            )
            assert (
                num_heaps == expect_heaps
            ), f"{trace_path}: Expected {expect_heaps} heaps, but GlobalHeapHistory has {num_heaps}"


def _get_path(built: Built, paths: Optional[Mapping[str, Path]], key: str) -> Path:
    path = None if paths is None else paths.get(key)
    if path is not None:
        return path
    elif key in built.tests:
        return built.tests[key]
    else:
        return get_existing_absolute_file_path(
            key, lambda: f"{key} should be absolute, a key in 'paths:', or \"GCPerfSim\""
        )


RUN_TESTS_COMMANDS: CommandsMapping = {
    "check-run-output": Command(
        hidden=True,
        kind=CommandKind.run,
        fn=check_run_output,
        doc="Check that trace files look like they came from the given benchfile and machine.",
    ),
    "how-to-run-test": Command(
        kind=CommandKind.run,
        fn=how_to_run_test,
        doc="Prints the command line used to run a single test from a benchfile.",
        priority=1,
    ),
    "run": Command(
        kind=CommandKind.run,
        fn=run,
        doc="""
    Run tests specified by a benchfile.
    This command always runs on the current machine;
    use the `remote-do` command to run on remote machines.
    
    On Windows this command must be run as administrator.
    """,
    ),
}
