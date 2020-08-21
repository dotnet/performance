# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from sys import exc_info
from traceback import format_tb
from typing import Mapping, Optional

from ..commonlib.bench_file import (
    BenchFileAndPath,
    get_benchmark,
    get_config,
    get_coreclr,
    get_test_executable,
    get_this_machine,
    iter_tests_to_run,
    out_dir_for_bench_yaml,
    MAX_ITERATIONS_FOR_RUN_DOC,
    parse_bench_file,
    SingleTestCombination,
)
from ..commonlib.get_built import BuildKind, Built, get_built, is_arm
from ..commonlib.collection_util import combine_mappings, is_empty
from ..commonlib.command import Command, CommandKind, CommandsMapping
from ..commonlib.option import map_option
from ..commonlib.type_utils import argument, with_slots
from ..commonlib.util import (
    add_new_error,
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
    RunErrorMap,
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


def run(args: RunArgs) -> None:
    # We need to record exceptions, in order to display them nicely at the end
    # of any given test run. Due to the inner gears of this tool's engine, any
    # function directly related to a command can have ONLY ONE parameter.
    #
    # Therefore, we are using this function as a wrapper for the 'run' command
    # and leave all the heavy lifting to run_test(). This is in order to
    # provide a safe mechanism for suite-runs to be able to display any
    # encountered errors once they are done running. To elaborate on this:
    #
    # Suite runs will display errors once all tests are done, as opposed to
    # after each test finishes. This is to help the user see all this
    # information at once, instead of having them scroll endlessly (each test
    # yields a ton of output to the terminal). This is the reason why both,
    # run_test() and suite_run() should have access to the errors that might
    # have happened while these benchmarks were running.
    run_test(args)


def run_test(
    args: RunArgs, run_errors: Optional[RunErrorMap] = None, is_suite: bool = False
) -> None:
    # Receiving an already initialized List as default value is very prone
    # to unexpected side-effects. None is the convention for these cases.
    if run_errors is None:
        run_errors = {}

    bench_file_path = assert_file_exists(args.bench_file_path)
    bench = parse_bench_file(bench_file_path)
    out_dir = (
        out_dir_for_bench_yaml(bench_file_path, machine=get_this_machine())
        if args.out is None
        else args.out
    )
    built = get_built(
        coreclrs=bench.content.coreclrs,
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
        built, bench, args.skip_where_exists, args.max_iterations, out_dir, run_errors
    )

    if not is_suite and not is_empty(run_errors):
        print(
            f"\n========= *WARNING*: Test '{bench_file_path}' encountered errors. =========\n"
            "\n*** Here is a summary of the problems found: ***\n"
        )
        for executable in run_errors.values():
            executable.print()


# pylint: disable=broad-except
def _run_all_benchmarks(
    built: Built,
    bench: BenchFileAndPath,
    skip_where_exists: bool,
    max_iterations: Optional[int],
    out_dir: Path,
    run_errors: RunErrorMap,
) -> None:
    default_env = check_env()
    for t in iter_tests_to_run(bench, get_this_machine(), max_iterations, out_dir):
        now = datetime.now().strftime("%H:%M:%S")

        try:
            print(f"{now} Running {t.out.out_path_base.name}")
            if not (skip_where_exists and t.out.exists()):
                run_single_test(
                    built,
                    SingleTest(
                        test=t.test,
                        coreclr=built.coreclrs[t.coreclr_name],
                        test_exe=t.test.executable.executable_path,
                        options=t.bench_file.options,
                        default_env=default_env,
                    ),
                    t.out,
                )
        except Exception:
            _, exception_message, exception_trace = exc_info()
            add_new_error(
                run_errors=run_errors,
                exec_name=t.executable_name,
                core_name=t.coreclr_name,
                config_name=t.config_name,
                bench_name=t.benchmark_name,
                iteration_num=t.iteration,
                message=str(exception_message),
                trace=format_tb(exception_trace),
            )
            continue


def _assert_not_in_job(built: Built) -> None:
    # TODO: on ARM IsProcessInJob seems to always return true
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
    executable: Optional[str] = argument(
        default=None, doc="Executable name for the test. May omit if there is only one executable."
    )
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
    executable = get_test_executable(bench, args.executable)
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
        config.with_coreclr(coreclr.name).env(map_option(coreclr_paths, lambda c: c.core_root)),
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
            executable=executable,
            coreclr=coreclr,
            config=config_and_name.as_partial,
            benchmark=benchmark_and_name,
        ),
        coreclr=coreclr_paths,
        test_exe=executable.executable_path,
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
