# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from datetime import datetime
from enum import Enum
from os.path import getmtime
from pathlib import Path
from platform import machine, processor
from shutil import copyfile, copytree
from typing import Mapping, Optional, Sequence

from .bench_file import Architecture, Bitness, CoreclrSpecifier, get_architecture_bitness
from .command import Command, CommandKind, CommandsMapping
from .config import (
    BENCH_DIR_PATH,
    DEPENDENCIES_PATH,
    EXEC_ENV_PATH,
    PERFORMANCE_PATH,
    sigcheck_exists,
    SIGCHECK64_PATH,
    SRC_PATH,
)
from .option import map_option, non_null, optional_to_iter
from .type_utils import argument, with_slots
from .util import (
    assert_dir_exists,
    assert_file_exists,
    ensure_dir,
    ExecArgs,
    exec_and_get_output,
    exec_and_get_output_and_exit_code,
    exec_and_expect_output,
    get_os,
    OS,
    os_is_windows,
    remove_str_start,
    walk_files_recursive,
)


class BuildKind(Enum):
    allow_out_of_date = 0
    forbid_out_of_date = 1


def _get_os_name() -> str:
    # TODO: support BSD, Mac
    return {OS.posix: "Linux", OS.windows: "Windows_NT"}[get_os()]


def is_arm() -> bool:
    return machine().find("ARM") != -1


def get_platform_name() -> str:
    if is_arm():
        # On ARM, the machine() function returns the exact name we need here.
        return machine().lower()
    else:
        p = processor()
        assert any(x in p for x in ("AMD64", "Intel64", "x86_64")), f"Processor {p} is not supported."
        return "x64"


def get_built_tests_dir(coreclr_repository_root: Path, debug: bool) -> Path:
    os_dir = f"{_get_os_name()}.{get_platform_name()}"
    r = "Debug" if debug else "Release"
    return coreclr_repository_root / "bin" / "tests" / f"{os_dir}.{r}"


@with_slots
@dataclass(frozen=True)
class CoreclrPaths:
    exe_path: Path
    # If exe_path comes from core_root, store that here
    core_root: Optional[Path]

    @property
    def corerun(self) -> Path:
        return assert_file_exists(self.exe_path)


@with_slots
@dataclass(frozen=True)
class BuiltWindowsOnly:
    get_host_info_exe: Path
    is_in_job_exe: Path
    make_memory_load: Path
    run_in_job_exe: Path


# coreclr and gcperfsim are built before generating tests.
# Then bench.yaml will contain absolute paths to the build output.
@with_slots
@dataclass(frozen=True)
class Built:
    # Keys are e.g. "GCPerfSim"
    tests: Mapping[str, Path]
    # Key is the *name* of the coreclr -- the key in the benchfile
    # None means we are using self-contained executables
    coreclrs: Mapping[str, Optional[CoreclrPaths]]
    _win: Optional[BuiltWindowsOnly]

    def __post_init__(self) -> None:
        assert "GCPerfSim" in self.tests

    @property
    def win(self) -> BuiltWindowsOnly:
        # Visual Studio tools are not supported on ARM. Hence, we assert we are
        # not working on said architecture.
        assert os_is_windows() and not is_arm()
        return non_null(self._win)


# input may be a directory: getmtime should be updated when a file in that directory is touched
def _is_build_is_out_of_date(
    in_paths: Sequence[Path], out_path: Path, build_kind: BuildKind
) -> Optional[str]:
    if build_kind == BuildKind.forbid_out_of_date:
        return map_option(
            _try_get_needs_rebuild_reason(in_paths, out_path),
            lambda reason: f"{out_path} needs a rebuild: {reason}",
        )
    else:
        return None


def _try_get_needs_rebuild_reason(in_paths: Sequence[Path], out_path: Path) -> Optional[str]:
    try:
        # non_null because some input file should exist
        in_file = non_null(
            _max_updated_file(
                [
                    f
                    for in_path in in_paths
                    for f in optional_to_iter(_get_most_recently_updated_file(in_path))
                ]
            )
        )
        out_file = _get_most_recently_updated_file(out_path)
        if out_file is None:
            return "No output files exist"
        elif in_file.mtime > out_file.mtime:
            return (
                f"{in_file.path} was updated on {in_file.mtime}, "
                + f"but {out_file.path} hasn't been updated since {out_file.mtime}"
            )
        else:
            return None
    except FileNotFoundError as err:
        return f"{err.filename} does not exist"


@with_slots
@dataclass(frozen=True)
class UpdatedFile:
    path: Path
    mtime: datetime


def _max_updated_file(updated_files: Sequence[UpdatedFile]) -> Optional[UpdatedFile]:
    return max(updated_files, key=lambda f: f.mtime)


def _get_most_recently_updated_file(path: Path) -> Optional[UpdatedFile]:
    if path.exists():
        here = UpdatedFile(path=path, mtime=datetime.fromtimestamp(getmtime(path)))
        if path.is_dir():
            return _max_updated_file(
                (
                    here,
                    *(
                        f
                        for child in path.iterdir()
                        for f in optional_to_iter(_get_most_recently_updated_file(child))
                    ),
                )
            )
        else:
            return here
    else:
        return None


_TEST_PATH = SRC_PATH / "exec"
_ARTIFACTS_BIN_PATH: Path = PERFORMANCE_PATH / "artifacts" / "bin"


def _get_test_path(name: str) -> Path:
    return _TEST_PATH / name


def _get_cs_files(path: Path) -> Sequence[Path]:
    return [
        p
        for p in walk_files_recursive(path, filter_dir=lambda d: d.name != "obj")
        if p.name.endswith(".cs")
    ]


def get_built_gcperf() -> Sequence[Path]:
    """Returns all DLLs needed to use GCPerf"""
    analysis_path = SRC_PATH / "analysis"
    managed_lib_path = analysis_path / "managed-lib"
    # This isn't run as part of a performance test, so use debug version
    gcperf_publish_path = _ARTIFACTS_BIN_PATH / "GCPerf" / "Debug" / "netstandard2.0" / "publish"
    gcperf_dll_path = gcperf_publish_path / "GCPerf.dll"
    traceevent_dll_path = gcperf_publish_path / "Microsoft.Diagnostics.Tracing.TraceEvent.dll"

    msg = _is_build_is_out_of_date(
        (_DEPENDENCIES_DLLS_PATH, *_get_cs_files(managed_lib_path)),
        gcperf_publish_path,
        BuildKind.forbid_out_of_date,
    )
    assert (
        msg is None
    ), f"You probably need to go to {managed_lib_path} and run `dotnet publish`.\n{msg}"
    return [assert_file_exists(p) for p in (gcperf_dll_path, traceevent_dll_path)]


def _get_latest_testbin_path(test_name: str) -> Path:
    base_bin_path = _ARTIFACTS_BIN_PATH / test_name / "release"
    bin_build_dirs = [str(f.absolute()).split('\\')[-1]
                  for f in base_bin_path.iterdir() if f.is_dir()
                  and 'netcoreapp' in str(f)]

    bin_versions = list(map(lambda d: float(d.split('netcoreapp')[-1]),
                            bin_build_dirs))
    return base_bin_path / f"netcoreapp{bin_versions[-1]}" / f"{test_name}.dll"


def _get_built_test(name: str, build_kind: BuildKind) -> Path:
    # Apparently, built files go to the root of the performance repo instead of next to the source.
    test_dir = _get_test_path(name)
    out_path = _get_latest_testbin_path(name)
    test_cs = test_dir / f"{name}.cs"
    assert_file_exists(test_cs)
    msg = _is_build_is_out_of_date(_get_cs_files(test_dir), out_path, build_kind)
    if msg is None:
        return assert_file_exists(out_path)
    else:
        raise Exception(
            f"You probably need to go to {test_dir} and run `dotnet build -c release`.\n{msg}"
        )


def get_current_git_commit_hash(git_repo_path: Path) -> str:
    return exec_and_get_output(
        ExecArgs(("git", "rev-parse", "HEAD"), cwd=git_repo_path, quiet_print=True)
    )


def first_line(s: str) -> str:
    return s.split("\n")[0]


def _check_sig(core_root_path: Path, spec: CoreclrSpecifier) -> None:
    # TODO: similar check on other OS
    if os_is_windows() and sigcheck_exists():
        for name in _CORECLR_IMPORTANT_DLL_NAMES_FOR_SIGCHECK:
            dll_path = core_root_path / f"{name}.dll"
            out = get_sigcheck_output(dll_path)
            _check_bitness(dll_path, out, spec.get_architecture())
            if spec.commit_hash is not None:
                _check_commit_hash(
                    dll_path=dll_path, expected_commit_hash=spec.commit_hash, out=out
                )


def _check_commit_hash(dll_path: Path, expected_commit_hash: str, out: str) -> None:
    srccode = "@SrcCode: "
    commit = "@Commit: "
    if srccode in out:
        src = first_line(out[out.index(srccode) + len(srccode) :])
        actual_commit_hash = remove_str_start(src, "https://github.com/dotnet/coreclr/tree/")
    elif commit in out:
        actual_commit_hash = first_line(out[out.index(commit) + len(commit) :])
    else:
        raise Exception(f"Sigcheck result not parseable:\n{out}")
    assert (
        actual_commit_hash == expected_commit_hash
    ), f"{dll_path} was built with {actual_commit_hash}, expected {expected_commit_hash}"


class RebuildKind(Enum):
    debug = 0
    release = 1
    both = 2


class _DebugKind(Enum):
    debug = 0
    release = 1


def _to_debug_kinds(rebuild_kind: RebuildKind) -> Sequence[_DebugKind]:
    return {
        RebuildKind.debug: [_DebugKind.debug],
        RebuildKind.release: [_DebugKind.release],
        RebuildKind.both: [_DebugKind.debug, _DebugKind.release],
    }[rebuild_kind]


@with_slots
@dataclass(frozen=True)
class _CopyBuildArgs:
    runtime: Path = argument(
        name_optional=True, doc="Path to a checkout of the 'dotnet/runtime' repository"
    )
    kind: _DebugKind = argument(
        default=_DebugKind.release, doc="Whether to copy the debug or release build"
    )
    name: Optional[str] = argument(
        default=None, doc="Name of the output directory. Defaults to the commit hash."
    )
    overwrite: bool = argument(
        default=False, doc="If true, the output directory will be copied over if it exists."
    )


_BUILDS_PATH = BENCH_DIR_PATH / "builds"


def _copy_build(args: _CopyBuildArgs) -> None:
    core_root = _get_core_root(args.runtime, args.kind)
    name = _get_default_build_name(args.runtime, args.kind) if args.name is None else args.name
    cp_dir(core_root, _BUILDS_PATH / name, args.overwrite)


def _get_default_build_name(runtime_repository: Path, kind: _DebugKind) -> str:
    commit_hash = get_current_git_commit_hash(runtime_repository)
    kind_str = {_DebugKind.debug: "_debug", _DebugKind.release: "_release"}[kind]
    return commit_hash + kind_str


@with_slots
@dataclass(frozen=True)
class RebuildCoreclrArgs:
    runtime_repo_paths: Sequence[Path] = argument(
        name_optional=True, doc="Path(s) to a checkout(s) of the 'dotnet/runtime' repository"
    )
    kind: RebuildKind = argument(doc="Whether to rebuild a debug or release build, or both.")
    just_copy: bool = argument(
        default=False,
        doc="If set, copy files from build directories to Core_Root but do not do a rebuild.",
    )


_CORECLR_IMPORTANT_DLL_NAMES_FOR_SIGCHECK: Sequence[str] = ("coreclr", "clrjit")
_CORECLR_IMPORTANT_DLL_NAMES: Sequence[str] = (
    *_CORECLR_IMPORTANT_DLL_NAMES_FOR_SIGCHECK,
    "System.Private.CoreLib",
)

_CORECLR_IMPORTANT_SO_NAMES: Sequence[str] = (
    "libcoreclr.so",
    "libclrjit.so",
    "System.Private.CoreLib.dll",
)


def rebuild_coreclr(args: RebuildCoreclrArgs) -> None:
    for debug_kind in _to_debug_kinds(args.kind):
        for runtime_repository in args.runtime_repo_paths:
            _do_rebuild_coreclr(runtime_repository, args.just_copy, debug_kind)


def _get_debug_or_release(debug_kind: _DebugKind) -> str:
    return {_DebugKind.debug: "debug", _DebugKind.release: "release"}[debug_kind]


def _get_debug_or_release_dir_name(debug_kind: _DebugKind) -> str:
    return (
        f"{_get_os_name()}.{get_platform_name()}.{_get_debug_or_release(debug_kind).capitalize()}"
    )


def _get_coreclr_from_runtime(runtime_repository: Path) -> Path:
    return runtime_repository / "src" / "coreclr"


def _get_core_root(runtime_repository: Path, debug_kind: _DebugKind) -> Path:
    debug_release = _get_debug_or_release_dir_name(debug_kind)
    return (
        runtime_repository
        / "artifacts"
        / "tests"
        / "coreclr"
        / debug_release
        / "Tests"
        / "Core_Root"
    )


def _do_rebuild_coreclr(runtime_repository: Path, just_copy: bool, debug_kind: _DebugKind) -> None:
    coreclr = _get_coreclr_from_runtime(runtime_repository)
    plat = get_platform_name()
    assert_dir_exists(coreclr)
    debug_release = _get_debug_or_release_dir_name(debug_kind)
    if not just_copy:
        output = exec_and_get_output(
            ExecArgs(
                cmd=(
                    str(coreclr / f"build.{get_build_ext()}"),
                    plat,
                    _get_debug_or_release(debug_kind),
                    # build.sh does not support --skiptests
                    *optional_to_iter("skiptests" if os_is_windows() else None),
                    "skipmscorlib",
                    "skipbuildpackages",
                    "-skiprestore",
                ),
                cwd=coreclr,
            )
        )
        if "failed" in output.lower():
            print(output)
            raise Exception("build failed")

    product_dir = runtime_repository / "artifacts" / "bin" / "coreclr" / debug_release
    core_root = _get_core_root(runtime_repository, debug_kind)

    if os_is_windows():
        for name in _CORECLR_IMPORTANT_DLL_NAMES:
            dll = f"{name}.dll"
            pdb = f"{name}.pdb"

            cp(from_path=product_dir / dll, to_path=core_root / dll)
            cp(from_path=product_dir / "PDB" / pdb, to_path=core_root / pdb)
    else:
        for name in _CORECLR_IMPORTANT_SO_NAMES:
            assert_file_exists(core_root / name)
            cp(from_path=product_dir / name, to_path=core_root / name)


def cp_dir(from_dir: Path, to_dir: Path, overwrite: bool) -> None:
    if to_dir.exists():
        if overwrite:
            to_dir.unlink()
        else:
            raise Exception(f"{to_dir} already exists. (Maybe you want to '--overwrite'?)")
    print(f"Copy directory {from_dir} to {to_dir}")
    copytree(from_dir, to_dir)


def cp(from_path: Path, to_path: Path) -> None:
    print(f"Copy {from_path} to {to_path}")
    _cp(from_path, to_path)


def _cp(from_path: Path, to_path: Path) -> None:
    # Warn: does not preserve metadata, so don't use this to copy executables
    assert_file_exists(from_path)
    copyfile(from_path, to_path)
    assert_file_exists(to_path)


def _check_bitness(path: Path, sigcheck_output: str, architecture: Architecture) -> None:
    is_64bit = "64-bit" in sigcheck_output
    if not is_64bit:
        assert "32-bit" in sigcheck_output
    actual = Bitness.bit64 if is_64bit else Bitness.bit32

    expected = get_architecture_bitness(architecture)

    assert actual == expected, f"{path}: built files are {actual} but expected {expected}"


def get_sigcheck_output(exe_to_test_path: Path) -> str:
    assert_file_exists(exe_to_test_path)
    cmd: Sequence[str] = (str(SIGCHECK64_PATH), str(exe_to_test_path))
    # For some reason it always exits with a 1, even if it worked.
    try:
        res = exec_and_get_output(ExecArgs(cmd=cmd, quiet_print=True), expect_exit_code=1)
    except UnicodeDecodeError:
        raise Exception(
            f"Running {SIGCHECK64_PATH} failed\n"
            "You may need to run it once to accept the license"
        )
    if res.endswith("n/a"):
        raise Exception(f"sigcheck {exe_to_test_path} failed -- not an exe?")
    return res


def get_build_ext() -> str:
    return {OS.posix: "sh", OS.windows: "cmd"}[get_os()]


def _get_built_coreclr(
    spec: CoreclrSpecifier, build_kind: BuildKind, debug: bool, skip_coreclr_checks: bool
) -> Optional[CoreclrPaths]:
    if spec.self_contained:
        return None
    elif spec.exact_path:
        return CoreclrPaths(exe_path=spec.exact_path, core_root=None)
    else:
        repo_path = spec.repo_path
        # Can't build if CORE_ROOT was specified
        if repo_path is not None:
            check_is_repository = exec_and_get_output_and_exit_code(
                ExecArgs(("git", "-C", str(repo_path), "rev-parse"))
            )
            if check_is_repository.exit_code != 0:
                raise Exception(
                    f"{repo_path} was specified as repo_path, but is not a git repository"
                )

            if not repo_path.exists():
                raise Exception(f"Bad coreclr path? {spec.repo_path}")

            if build_kind == BuildKind.forbid_out_of_date:
                if not skip_coreclr_checks:
                    exec_and_expect_output(
                        ExecArgs(("git", "diff"), cwd=spec.repo_path, quiet_print=True),
                        "",
                        err=f"git repo {repo_path} has changes"
                        + " (maybe you want to '--skip-coreclr-checks'?)",
                    )

                if spec.commit_hash is not None:
                    #  Allow a short commit hash prefix. But not too short
                    assert len(spec.commit_hash) >= 6
                    if not skip_coreclr_checks:
                        actual_commit_hash = get_current_git_commit_hash(repo_path)
                        # Could do_coreclr_build instead of fail, but that takes a long time.
                        assert actual_commit_hash.startswith(
                            spec.commit_hash
                        ), f"{repo_path}: commit is {actual_commit_hash}, wanted {spec.commit_hash}"
                else:
                    print(f"commit_hash is None, running with whatever's in {repo_path}")

        assert not (
            debug and spec.repo_path is None
        ), "Can't use debug builds if coreclr is specified by 'core_root' instead of 'repo_path'."

        core_root_path = (
            spec.core_root
            if spec.core_root is not None
            else get_built_tests_dir(non_null(spec.repo_path), debug) / "Tests" / "Core_Root"
        )

        coreclr = CoreclrPaths(
            exe_path=get_corerun_path_from_core_root(core_root_path), core_root=core_root_path
        )
        if not skip_coreclr_checks:
            _check_sig(core_root_path, spec)
        return coreclr


def get_corerun_path_from_core_root(core_root: Path) -> Path:
    name = "CoreRun.exe" if os_is_windows() else "corerun"
    return core_root / name


def get_built(
    coreclrs: Mapping[str, CoreclrSpecifier],
    build_kind: BuildKind = BuildKind.forbid_out_of_date,
    use_debug_coreclrs: bool = False,
    skip_coreclr_checks: bool = False,
) -> Built:
    test_names: Sequence[str] = ("GCPerfSim",)
    tests = {name: _get_built_test(name, build_kind) for name in test_names}

    built_coreclrs = {
        name: _get_built_coreclr(
            spec, build_kind, use_debug_coreclrs, skip_coreclr_checks=skip_coreclr_checks
        )
        for name, spec in coreclrs.items()
    }

    # Note: not building gcperf here because we already did that when setting
    # up CLR imports. Also, these scripts are not built on ARM because Visual
    # Studio tools are not supported there. Therefore, we just skip in this case.

    win = (
        BuiltWindowsOnly(
            get_host_info_exe=_get_built_c_script("get_host_info"),
            is_in_job_exe=_get_built_c_script("is_in_job"),
            make_memory_load=_get_built_c_script("make_memory_load"),
            run_in_job_exe=_get_built_c_script("run_in_job"),
        )
        if os_is_windows() and not is_arm()
        else None
    )

    return Built(tests=tests, coreclrs=built_coreclrs, _win=win)


_EXEC_ENV_BUILD_CMD_PATH = EXEC_ENV_PATH / "build.cmd"
_EXEC_ENV_BUILD_DEBUG_PATH = EXEC_ENV_PATH / "out" / "Debug"
assert_file_exists(_EXEC_ENV_BUILD_CMD_PATH)


def _get_built_c_script(name: str) -> Path:
    out_path = _EXEC_ENV_BUILD_DEBUG_PATH / f"{name}.exe"
    assert out_path.exists(), (
        f"Could not find {out_path}\nMaybe you need to run {_EXEC_ENV_BUILD_CMD_PATH}"
        + " (using a Visual Studio Developer Command Prompt)?"
    )
    return out_path


@with_slots
@dataclass(frozen=True)
class UpdatePerfviewDllsArgs:
    perfview_repo: Path = argument(name_optional=True, doc="PerfView repository path.")


_DEPENDENCIES_DLLS_PATH = DEPENDENCIES_PATH / "dlls"


def update_perfview_dlls(args: UpdatePerfviewDllsArgs) -> None:
    print("WARN: This does not build perfview, just copies build output from there to here")

    assert_dir_exists(args.perfview_repo)
    trace_event = assert_dir_exists(
        args.perfview_repo / "src" / "TraceEvent" / "bin" / "Debug" / "netstandard2.0"
    )
    dll_names: Sequence[str] = (
        "Microsoft.Diagnostics.FastSerialization.dll",
        "Microsoft.Diagnostics.Tracing.TraceEvent.dll",
        "TraceReloggerLib.dll",
    )
    ensure_dir(_DEPENDENCIES_DLLS_PATH)
    for dll_name in dll_names:
        cp(from_path=trace_event / dll_name, to_path=_DEPENDENCIES_DLLS_PATH / dll_name)


BUILD_COMMANDS: CommandsMapping = {
    "copy-build": Command(
        kind=CommandKind.infra,
        fn=_copy_build,
        doc="""
    Copy a build from a 'dotnet/runtime' repository checkout to 'bench/builds'.
    Output directory name uses the commit hash.
    """,
    ),
    "rebuild-coreclr": Command(
        kind=CommandKind.infra,
        fn=rebuild_coreclr,
        doc="""
    Quickly rebuild a coreclr that has only seen small changes to GC code.
    WARN: Not guaranteed to produce a correct rebuild if other code has been modified!
    """,
    ),
    # TODO: kill this command, use nuget
    "update-perfview-dlls": Command(
        hidden=True,
        kind=CommandKind.infra,
        fn=update_perfview_dlls,
        doc="""
    Copies PerfView's build output to dependencies.
    Does *not* build perfview.
    """,
    ),
}
