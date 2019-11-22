# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from pathlib import Path
from typing import cast, Iterable, Mapping, Optional, Sequence, Type

from ..commonlib.bench_file import (
    BenchFile,
    BenchOptions,
    Benchmark,
    BenchmarkAndName,
    Config,
    ConfigsVaryBy,
    CoreclrSpecifier,
    GCPerfSimArgs,
    TestKind,
)
from ..commonlib.get_built import Built, get_built, get_built_tests_dir, get_current_git_commit_hash
from ..commonlib.collection_util import find_only_matching, make_mapping, map_mapping_values
from ..commonlib.command import Command, CommandKind, CommandsMapping
from ..commonlib.host_info import HostInfo, read_this_machines_host_info
from ..commonlib.option import optional_to_iter
from ..commonlib.parse_and_serialize import load_yaml, write_yaml_file
from ..commonlib.type_utils import argument, with_slots
from ..commonlib.util import (
    assert_dir_exists,
    assert_file_exists,
    remove_str_end,
    try_parse_single_tag_from_xml_document,
    walk_files_recursive,
)


def to_benchmark(
    args: GCPerfSimArgs,
    min_seconds: Optional[int] = None,
    max_seconds: Optional[int] = None,
    only_configs: Optional[Sequence[str]] = None,
) -> Benchmark:
    return Benchmark(
        executable=None,
        arguments=args.to_str(),
        min_seconds=min_seconds,
        max_seconds=max_seconds,
        only_configs=only_configs,
    )


@with_slots
@dataclass(frozen=True)
class _CoreclrRepositorySpecifier:
    """Like CoreclrSpecifier, but always specifies the repository root and not just CORE_ROOT."""

    path: Path
    commit_hash: Optional[str] = None

    def to_coreclr_specifier(self) -> CoreclrSpecifier:
        return CoreclrSpecifier(repo_path=self.path, commit_hash=self.commit_hash)


def _to_coreclr_specifiers(
    coreclrs: Mapping[str, _CoreclrRepositorySpecifier]
) -> Mapping[str, CoreclrSpecifier]:
    return map_mapping_values(lambda c: c.to_coreclr_specifier(), coreclrs)


@with_slots
@dataclass(frozen=True)
class _ArgsForGenerate:
    built: Built
    coreclrs: Mapping[str, _CoreclrRepositorySpecifier]
    host_info: HostInfo

    @property
    def coreclr_specifiers(self) -> Mapping[str, CoreclrSpecifier]:
        return _to_coreclr_specifiers(self.coreclrs)


@with_slots
@dataclass(frozen=True)
class _CommandLineArgsForGenerate:
    path: Path = argument(name_optional=True, doc="Path to write the output benchfile to.")
    coreclrs: Path = argument(doc="Path to 'coreclrs.yaml'")
    overwrite: bool = argument(
        default=False, doc="If true, allow the output path to already exist."
    )


def _parse_coreclrs(path: Path) -> Mapping[str, _CoreclrRepositorySpecifier]:
    # https://github.com/python/mypy/issues/4717
    t = cast(
        Type[Mapping[str, _CoreclrRepositorySpecifier]], Mapping[str, _CoreclrRepositorySpecifier]
    )
    coreclrs: Mapping[str, _CoreclrRepositorySpecifier] = load_yaml(t, path)

    def ensure_has_commit_hash(c: _CoreclrRepositorySpecifier) -> _CoreclrRepositorySpecifier:
        path = assert_dir_exists(c.path)
        commit_hash = c.commit_hash
        if commit_hash is None:
            commit_hash = get_current_git_commit_hash(c.path)
        return _CoreclrRepositorySpecifier(path, commit_hash)

    return {k: ensure_has_commit_hash(v) for k, v in coreclrs.items()}


def _generate_benchyaml_for_coreclr_unit_tests(args: _CommandLineArgsForGenerate) -> None:
    assert (
        not args.path.exists() or args.overwrite
    ), f"{args.path} already exists, did you mean to '--overwrite'?"
    coreclrs = _parse_coreclrs(args.coreclrs)
    built = get_built(_to_coreclr_specifiers(coreclrs))
    content = _generate_benchyaml_for_coreclr_unit_tests_worker(
        _ArgsForGenerate(built, coreclrs, read_this_machines_host_info())
    )
    write_yaml_file(args.path, content, overwrite=args.overwrite)


def _generate_benchyaml_for_coreclr_unit_tests_worker(args: _ArgsForGenerate) -> BenchFile:
    return BenchFile(
        comment=None,
        configs_vary_by=None,
        coreclrs=args.coreclr_specifiers,
        # Have a very generous time range
        options=BenchOptions(default_min_seconds=0, default_max_seconds=600),
        common_config=Config(complus_gcconcurrent=True),
        configs={
            "server": Config(complus_gcserver=True),
            "workstation": Config(complus_gcserver=False),
        },
        benchmarks=to_benchmarks_dict(_find_coreclr_unit_tests(args)),
    )


# both lo and hi are inclusive
def _float_range_with_n_elements(lo: float, hi: float, n_elements: int) -> Sequence[float]:
    assert n_elements > 1
    assert lo < hi, f"Expected {lo} < {hi}"
    step = (hi - lo) / (n_elements - 1)
    return [lo + i * step for i in range(n_elements)]


def _float_range_around(v: float) -> Sequence[float]:
    N = 4
    # 0.25, 0.5, 0.75, 1, 1. 25, 1.5, 1.75, 2
    return _float_range_with_n_elements(v / N, v * 2, N * 2)
    # return [
    #    *_float_range_with_n_elements(v / N, v, N),
    #    *_float_range_with_n_elements(v * (N + 1) / N, v * 2, N),
    # ]


def _survive_benchmarks() -> Mapping[str, Benchmark]:
    return map_mapping_values(
        to_benchmark,
        {
            "nosurvive": GCPerfSimArgs(tc=8, tagb=500, tlgb=0, lohar=0, sohsi=0, sohpi=0, lohpi=0),
            # tagb is arbitrary
            "hisurvive": GCPerfSimArgs(
                tc=8, tagb=0, tlgb=0.5, totalMins=1, testKind=TestKind.highSurvival
            ),
        },
    )


def _normal_bench_file(
    args: _ArgsForGenerate,
    common_config: Config,
    configs: Mapping[str, Config],
    benchmarks: Mapping[str, Benchmark],
    comment: Optional[str] = None,
    configs_vary_by: Optional[ConfigsVaryBy] = None,
) -> BenchFile:
    return BenchFile(
        comment=comment,
        configs_vary_by=configs_vary_by,
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args),
        common_config=common_config,
        configs=configs,
        benchmarks=benchmarks,
    )


def _survive_bench_file(
    args: _ArgsForGenerate, common_config: Config, configs: Mapping[str, Config]
) -> BenchFile:
    return _normal_bench_file(args, common_config, configs, _survive_benchmarks())


def _gcsmall_benchyaml(
    args: _ArgsForGenerate, common_config: Config, configs: Mapping[str, Config]
) -> BenchFile:
    benchmark_name = "GCSmall"
    gcsmall = _find_test(args, benchmark_name)
    paths = {benchmark_name: Path(gcsmall.benchmark.get_executable)}
    return BenchFile(
        comment=None,
        configs_vary_by=None,
        paths=paths,
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args),
        common_config=common_config,
        configs=configs,
        benchmarks={benchmark_name: Benchmark(benchmark_name)},
    )


GENERATE_COMMANDS: CommandsMapping = {
    "generate-coreclr-unit-tests": Command(
        hidden=True,
        kind=CommandKind.run,
        fn=_generate_benchyaml_for_coreclr_unit_tests,
        doc="Generates a benchfile for all unit tests from coreclr",
    )
}


def _default_options(_: _ArgsForGenerate, min_seconds: Optional[int] = None) -> BenchOptions:
    return BenchOptions(default_iteration_count=3, default_min_seconds=min_seconds)


_DISABLED_TESTS: Sequence[str] = (
    # These have no 'Main' method; they are just utilities used by other tests
    "GCUtil_HeapExpansion",
    "GCUtil_Pinning",
    # No 'Main'
    "doublink",
    # This test takes way too long due to calling `Thread.Sleep` in a loop
    "462651",
    # This test does 5_000_000 * 128 * 1024 * 2 writes; takes way too long
    "536168",
    # This test has no 'Main' and doesn't seem to serve any purpose
    "Managed",
    "ManagedTest",
    # TODO: this test timed out after 10 minutes. Find out why it's so slow
    "lohfragmentation",
)


def _find_coreclr_unit_tests(args: _ArgsForGenerate) -> Sequence[BenchmarkAndName]:
    coreclr = args.coreclrs["master"]
    gc_tests_src_dir = coreclr.path / "tests" / "src" / "GC"
    built_gc_tests_dir = get_built_tests_dir(coreclr.path, debug=True) / "GC"

    def get_bench(csproj_path: Path) -> Optional[BenchmarkAndName]:
        # Tests ending in e.g. "_1" will share the same executable but have different arguments
        base_name = remove_str_end(csproj_path.name, ".csproj")

        if base_name in _DISABLED_TESTS:
            return None

        # GCSimulator tests all share the same exe apparently, just different command line args
        exe_name = (
            _strip_trailing_underscore_number(base_name)
            if base_name.startswith("GCSimulator")
            else base_name
        )

        rel = csproj_path.parent.relative_to(gc_tests_src_dir)
        output_dir = built_gc_tests_dir / rel / exe_name
        output_exe = output_dir / f"{exe_name}.exe"
        # Some are dlls (have <CLRTestKind>SharedLibrary</CLRTestKind>)
        output_dll = output_dir / f"{exe_name}.dll"

        # Some tests have the same file name but different directories, so must do this
        bench_name = remove_str_end(
            str(csproj_path.relative_to(gc_tests_src_dir)).replace("\\", "_").replace("/", "_"),
            ".csproj",
        )

        # TODO: for some reason the default build script is not building some of the tests!
        if not output_exe.exists() and not output_dll.exists():
            raise Exception(f"Need to build {csproj_path} to {output_dir}")
            # exec_cmd(ExecArgs((msbuild, str(csproj_path), "/p:Configuration=Release")))

        output = output_dll if output_dll.exists() else assert_file_exists(output_exe)

        arguments = try_parse_single_tag_from_xml_document(
            csproj_path,
            "{http://schemas.microsoft.com/developer/msbuild/2003}CLRTestExecutionArguments",
        )
        return BenchmarkAndName(
            name=bench_name, benchmark=Benchmark(executable=str(output), arguments=arguments)
        )

    return [
        b
        for f in walk_files_recursive(gc_tests_src_dir, filter_dir=lambda _: True)
        if f.name.endswith(".csproj")
        for b in optional_to_iter(get_bench(f))
    ]


# "a_b_c_123" -> "a_b_c"
# "a_b_c" -> "a_b_c"
def _strip_trailing_underscore_number(s: str) -> str:
    lr = s.rsplit("_", 1)
    if len(lr) == 1:
        return s
    else:
        assert len(lr) == 2
        l, r = lr
        return l if r.isdigit() else s


def _find_test(args: _ArgsForGenerate, name: str) -> BenchmarkAndName:
    return find_only_matching(lambda test: test.name, name, _find_coreclr_unit_tests(args))


def to_benchmarks_dict(bns: Iterable[BenchmarkAndName]) -> Mapping[str, Benchmark]:
    return make_mapping((bn.name, bn.benchmark) for bn in sorted(bns, key=lambda b: b.name))
