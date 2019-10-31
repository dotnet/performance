# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from dataclasses import dataclass
from pathlib import Path
from typing import Callable, cast, Dict, Iterable, Mapping, Optional, Sequence, Type

from ..analysis.run_metrics import get_final_youngest_desired_bytes_for_process
from ..analysis.clr import get_clr

from ..commonlib.bench_file import (
    BenchFile,
    BenchOptions,
    Benchmark,
    BenchmarkAndName,
    Config,
    ConfigsVaryBy,
    CoreclrAndName,
    CoreclrSpecifier,
    GCPerfSimArgs,
    get_this_machine,
    PartialConfigAndName,
    SingleTestCombination,
    TestConfigContainer,
    TestKind,
)
from ..commonlib.get_built import Built, get_built, get_built_tests_dir, get_current_git_commit_hash
from ..commonlib.collection_util import (
    combine_mappings,
    find_only_matching,
    make_mapping,
    map_mapping_values,
    unique,
)
from ..commonlib.command import Command, CommandFunction, CommandKind, CommandsMapping
from ..commonlib.host_info import HostInfo, read_this_machines_host_info
from ..commonlib.option import non_null, optional_to_iter
from ..commonlib.parse_and_serialize import load_yaml, write_yaml_file
from ..commonlib.type_utils import argument, with_slots
from ..commonlib.util import (
    assert_dir_exists,
    assert_file_exists,
    bytes_to_mb,
    mb_to_bytes,
    mb_to_gb,
    remove_str_end,
    try_parse_single_tag_from_xml_document,
    walk_files_recursive,
)

from .run_single_test import check_env, run_single_test_temporary, SingleTest


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


def _generate_helper(
    args: _CommandLineArgsForGenerate, generator: Callable[[_ArgsForGenerate], BenchFile]
) -> None:
    assert (
        not args.path.exists() or args.overwrite
    ), f"{args.path} already exists, did you mean to '--overwrite'?"
    coreclrs = _parse_coreclrs(args.coreclrs)
    built = get_built(_to_coreclr_specifiers(coreclrs))
    content = generator(_ArgsForGenerate(built, coreclrs, read_this_machines_host_info()))
    write_yaml_file(args.path, content, overwrite=args.overwrite)


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


def _no_extra_args(
    cb: Callable[[_ArgsForGenerate], BenchFile]
) -> Callable[[_CommandLineArgsForGenerate], None]:
    def f(args: _CommandLineArgsForGenerate) -> None:
        _generate_helper(args, cb)

    return f


def _generate_benchyaml_for_coreclr_unit_tests(args: _ArgsForGenerate) -> BenchFile:
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


def _args_with_defaults(
    tc: int, tlgb: float = 0.5, lohar: int = 0, sohsi: int = 0, sohpi: int = 0, lohpi: int = 0
) -> GCPerfSimArgs:
    return GCPerfSimArgs(
        tc=tc, tagb=500, tlgb=tlgb, lohar=lohar, sohsi=sohsi, sohpi=sohpi, lohpi=lohpi
    )


def _gcperfsim_benchmarks() -> Mapping[str, Benchmark]:
    perfsim_configs = {
        "TC4SOHOnlySohsi0": _args_with_defaults(tc=4),
        "TC4SOHOnlySohsi15": _args_with_defaults(tc=4, sohsi=15),
        "TC4SOHOnlySohsi30": _args_with_defaults(tc=4, sohsi=30),
        "TC8SOHOnly": _args_with_defaults(tc=8, lohar=5, sohsi=30),
        "TC4LOH5": _args_with_defaults(tc=4, lohar=5, sohsi=30),
        "TC8LOH10": _args_with_defaults(tc=8, lohar=5, sohsi=30),
        "TC8LOH20": _args_with_defaults(tc=8, lohar=20, sohsi=30),
        "TC8LOH10P10": _args_with_defaults(tc=8, lohar=10, sohsi=30, sohpi=10, lohpi=100),
        "TC8LOH10P50": _args_with_defaults(tc=8, lohar=10, sohsi=30, sohpi=50),
        "TC8LOH10P100": _args_with_defaults(tc=8, lohar=10, sohsi=30, sohpi=100, lohpi=100),
    }
    return {k: to_benchmark(v) for k, v in perfsim_configs.items()}


def _generate_benchyaml_for_gcperfsim(args: _ArgsForGenerate) -> BenchFile:
    return BenchFile(
        comment=None,
        configs_vary_by=None,
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args),
        benchmarks=_gcperfsim_benchmarks(),
    )


# Sequence of evenly spaced ints in range [lo, hi] with exactly n_elements
def _int_range_with_n_elements(lo: int, hi: int, n_elements: int) -> Sequence[int]:
    assert hi > lo and 2 <= n_elements <= ((hi - lo) + 1)
    return [round(x) for x in _float_range_with_n_elements(lo, hi, n_elements)]


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


def range_inclusive(lo: int, hi: int, step: int = 1) -> Iterable[int]:
    assert lo < hi and (hi - lo) % step == 0
    return range(lo, hi + 1, step)


@with_slots
@dataclass(frozen=True)
class _NheapsArgs(_CommandLineArgsForGenerate):
    # Not really optional, but non-optional fields can't follow optional fields from superclass
    live_gb: Optional[int] = argument(default=None, doc="-tlgb value")
    tc: Optional[int] = argument(default=None, doc="-tc value")


def _survive_benchmarks() -> Mapping[str, Benchmark]:
    return map_mapping_values(
        to_benchmark,
        {
            "nosurvive": _args_with_defaults(tc=8, tlgb=0, sohsi=0),
            # tagb is arbitrary
            "hisurvive": GCPerfSimArgs(
                tc=8, tagb=0, tlgb=0.5, totalMins=1, testKind=TestKind.highSurvival
            ),
        },
    )


_TLGB_HIGH_SURVIVE = 400


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


def _generate_gen0size_nosurvive(args: _ArgsForGenerate) -> BenchFile:
    common_config = Config(complus_gcserver=True, complus_gcconcurrent=False)

    def get_config(gen0size_bytes: int, heap_count: Optional[int]) -> Config:
        return Config(complus_gcgen0size=gen0size_bytes, complus_gcheapcount=heap_count)

    min_seconds = 10  # With no survival we don't need a very long test
    benchmarks: Mapping[str, Benchmark] = {
        f"nosurvive_{n_threads}threads": to_benchmark(
            GCPerfSimArgs(tc=n_threads, tlgb=0, sohsi=0, tagb=300), min_seconds=min_seconds
        )
        for n_threads in (4, args.host_info.n_logical_processors)
    }

    defaults = {
        coreclr: _measure_default_gen0_min_bytes_for_coreclr(args.built, coreclr)
        for coreclr in args.coreclrs.keys()
    }
    sizes = _float_range_with_n_elements(
        min(defaults.values()) / 2, max(defaults.values()) * 2, n_elements=16
    )
    configs = {
        f"{bytes_to_mb(gen0size_bytes)}mb_gen0size": get_config(round(gen0size_bytes), heap_count)
        for gen0size_bytes in sizes
        for heap_count in (None,)  # (4, 8)
    }

    return _normal_bench_file(
        args,
        common_config,
        configs,
        benchmarks,
        configs_vary_by=ConfigsVaryBy(name="gen0size", default_values=defaults),
    )


def _measure_default_gen0_min_bytes_for_coreclr(built: Built, coreclr_name: str) -> int:
    # Get this lazily to ensure we only load DLLs after they have been built
    # NOTE: FinalYoungestDesired should approach the min gen0 size when there is no survival.
    # (With high survival it approaches the max gen0 size.)
    # Allocate 50GB with no survival, should be enough to get a good measure without taking forever
    benchmark = to_benchmark(GCPerfSimArgs(tc=1, tlgb=0, sohsi=0, tagb=100), min_seconds=10)

    coreclr = non_null(built.coreclrs[coreclr_name])

    proc = run_single_test_temporary(
        get_clr(),
        built,
        SingleTest(
            test=SingleTestCombination(
                machine=get_this_machine(),
                coreclr=CoreclrAndName(coreclr_name, CoreclrSpecifier(core_root=coreclr.core_root)),
                config=PartialConfigAndName(
                    "a", Config(complus_gcserver=True, complus_gcconcurrent=False)
                ),
                benchmark=BenchmarkAndName("nosurvive", benchmark),
            ),
            coreclr=coreclr,
            test_exe=built.gcperfsim_dll,
            options=BenchOptions(default_iteration_count=1),
            default_env=check_env(),
        ),
    )

    return get_final_youngest_desired_bytes_for_process(proc)


def _nheaps_configs_vary_by(args: _ArgsForGenerate) -> ConfigsVaryBy:
    df = args.host_info.n_physical_processors
    return ConfigsVaryBy(
        name="nheaps (workstation=-1)", default_values={k: df for k in args.coreclrs.keys()}
    )


def _generate_benchyaml_for_container_nheaps(args: _ArgsForGenerate) -> BenchFile:
    common_config = Config(complus_gcserver=True, complus_gcconcurrent=False)
    configs = {
        str(threads): Config(
            complus_threadpool_forcemaxworkerthreads=threads,
            container=TestConfigContainer(memory_mb=_TLGB_HIGH_SURVIVE * 2),
        )
        for threads in range(1, 8 + 1)
    }
    return BenchFile(
        comment=None,
        configs_vary_by=_nheaps_configs_vary_by(args),
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args),
        common_config=common_config,
        configs=configs,
        benchmarks=_survive_benchmarks(),
    )


def _get_container_memory_test() -> Benchmark:
    return to_benchmark(GCPerfSimArgs(tc=4, tagb=100, tlgb=1, sohsi=0), min_seconds=8)


def _measure_unconstrained_memory_usage_mb(built: Built, coreclr_name: str, n_heaps: int) -> float:
    coreclr = non_null(built.coreclrs[coreclr_name])
    proc = run_single_test_temporary(
        get_clr(),
        built,
        SingleTest(
            test=SingleTestCombination(
                machine=get_this_machine(),
                coreclr=CoreclrAndName(coreclr_name, CoreclrSpecifier(core_root=coreclr.core_root)),
                config=PartialConfigAndName(
                    "a",
                    Config(
                        complus_gcserver=True,
                        complus_gcconcurrent=False,
                        complus_gcheapcount=n_heaps,
                        # no container
                    ),
                ),
                benchmark=BenchmarkAndName("nosurvive", _get_container_memory_test()),
            ),
            coreclr=coreclr,
            test_exe=built.gcperfsim_dll,
            options=BenchOptions(default_iteration_count=1),
            default_env=check_env(),
        ),
    )

    res = max(gc.HeapSizeBeforeMB for gc in proc.gcs)
    print(f"Used {res}MB (max HeapSizeBeforeMB)")
    return res


@with_slots
@dataclass(frozen=True)
class _ContainerMemoryLimitsExtra(_CommandLineArgsForGenerate):
    # Not really optional
    n_heaps: Optional[int] = argument(default=None, doc="Value for complus_gcheapcount")


def _generate_for_container_memory_limits(cmd_args: _ContainerMemoryLimitsExtra) -> None:
    def f(args: _ArgsForGenerate) -> BenchFile:
        n_heaps = non_null(cmd_args.n_heaps)

        # First, run the test outside of a container and measure the memory usage
        defaults = {
            coreclr: _measure_unconstrained_memory_usage_mb(args.built, coreclr, n_heaps)
            for coreclr in args.coreclrs.keys()
        }

        # Tests will probably fail in the lower end of this range -- test runner should handle it
        mem_limits_mb = _float_range_with_n_elements(
            min(defaults.values()) / 4, max(defaults.values()) * 1.2, 10
        )

        common_config = Config(
            complus_gcserver=True, complus_gcconcurrent=False, complus_gcheapcount=n_heaps
        )
        configs = {
            f"mem_limit_{mem_limit_mb}mb": Config(
                container=TestConfigContainer(memory_mb=mem_limit_mb)
            )
            for mem_limit_mb in mem_limits_mb
        }
        return BenchFile(
            comment=None,
            configs_vary_by=ConfigsVaryBy(name="container_memory_mb", default_values=defaults),
            coreclrs=args.coreclr_specifiers,
            options=_default_options(args),
            common_config=common_config,
            configs=configs,
            benchmarks={"nosurvive": _get_container_memory_test()},
        )

    _generate_helper(cmd_args, f)


def _generate_containers_no_survival_no_live_data(args: _ArgsForGenerate) -> BenchFile:
    common_config = Config(
        complus_gcserver=True,
        complus_gcconcurrent=False,
        container=TestConfigContainer(memory_mb=128),
    )
    n_heapses = (1, 2, 4, 8, 16, 24, 32, 48)
    configs = {f"{n_heaps}_heaps": Config(complus_gcheapcount=n_heaps) for n_heaps in n_heapses}
    benchmarks = {
        "nosurvive": to_benchmark(
            GCPerfSimArgs(tc=args.host_info.n_logical_processors, tagb=20, tlgb=0, sohsi=0),
            min_seconds=5,
        )
    }

    return BenchFile(
        comment=None,
        configs_vary_by=ConfigsVaryBy("n_heaps", default_values=None),
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args),
        common_config=common_config,
        configs=configs,
        benchmarks=benchmarks,
    )


def _generate_containers_low_survival_temporary_data(args: _ArgsForGenerate) -> BenchFile:
    common_config = Config(
        complus_gcserver=True,
        complus_gcconcurrent=False,
        container=TestConfigContainer(memory_mb=200),
    )
    n_heapses = (1, 2, 4, 8, 16, 24, 32, 48)
    configs = {f"{n_heaps}_heaps": Config(complus_gcheapcount=n_heaps) for n_heaps in n_heapses}
    benchmarks = {
        "low_survival_temp_data": to_benchmark(
            GCPerfSimArgs(
                tc=args.host_info.n_logical_processors, tagb=20, tlgb=mb_to_gb(50), sohsi=50
            ),
            min_seconds=5,
        )
    }

    return BenchFile(
        comment=None,
        configs_vary_by=ConfigsVaryBy("n_heaps", default_values=None),
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args),
        common_config=common_config,
        configs=configs,
        benchmarks=benchmarks,
    )


def _generate_containers_varying_live_mb(args: _ArgsForGenerate) -> BenchFile:
    common_config = Config(
        complus_gcserver=True,
        complus_gcconcurrent=False,
        container=TestConfigContainer(memory_mb=200),
    )
    configs = {"only_config": Config()}
    benchmarks = {
        f"low_survival_temp_data_{tlmb}gb": to_benchmark(
            GCPerfSimArgs(
                tc=args.host_info.n_logical_processors, tagb=20, tlgb=mb_to_gb(tlmb), sohsi=50
            )
        )
        for tlmb in _int_range_with_n_elements(0, 125, 11)
    }
    return BenchFile(
        comment=None,
        # TODO: shouldn't be called 'configs_vary_by', it's the benchmarks that vary.
        # Call it 'x_axis'
        configs_vary_by=ConfigsVaryBy("tlmb", default_values=None),
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args, min_seconds=5),
        common_config=common_config,
        configs=configs,
        benchmarks=benchmarks,
    )


def _generate_100pct_survival_low_live_data(args: _ArgsForGenerate) -> BenchFile:
    mem_limits_mb: Sequence[float] = (64, 128, 256, 512, 1024)

    # #threads = #heaps
    n_logical = args.host_info.n_logical_processors
    ns_threads_heaps: Sequence[int] = unique((1, 2, 4, n_logical // 2, n_logical))

    configs_for_n_threads: Dict[int, Sequence[str]] = {}

    # From experimenting -- having a higher # threads/heaps seems to cause crashes
    def mem_limit_wont_crash_oom(mem_limit_mb: float, n_threads_heaps: int) -> bool:
        return mem_limit_mb / n_threads_heaps > 4

    common_config = Config(complus_gcserver=True, complus_gcconcurrent=False)

    def tests_for_n_threads(n_threads_heaps: int) -> Mapping[str, Config]:
        cfgs = {
            f"{n_threads_heaps}_threads_heaps_{mem_limit_mb}mb": Config(
                complus_gcheapcount=n_threads_heaps,
                container=TestConfigContainer(memory_mb=mem_limit_mb),
            )
            for mem_limit_mb in mem_limits_mb
            if mem_limit_wont_crash_oom(mem_limit_mb, n_threads_heaps)
        }
        configs_for_n_threads[n_threads_heaps] = tuple(cfgs.keys())
        return cfgs

    configs = combine_mappings(*(tests_for_n_threads(n) for n in ns_threads_heaps))

    # This does gc.collect in a loop.
    # Needs to listen to nthreads
    benchmarks = {
        f"{n_threads}_threads": to_benchmark(
            GCPerfSimArgs(
                tc=n_threads,
                testKind=TestKind.highSurvival,
                totalMins=0.5,
                tagb=0,  # arbitrary
                tlgb=mb_to_gb(30),  # Low live data size -- must be less than lowest mem_limits_mb
            ),
            only_configs=configs_for_n_threads[n_threads],
        )
        for n_threads in ns_threads_heaps
    }

    return BenchFile(
        comment=None,
        configs_vary_by=ConfigsVaryBy(name="container_memory_mb", default_values=None),
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args),
        common_config=common_config,
        configs=configs,
        benchmarks=benchmarks,
    )


def _get_only_configs(
    configs: Mapping[str, Config], pred: Callable[[Config], bool]
) -> Optional[Sequence[str]]:
    if all(pred(c) for c in configs.values()):
        return None
    else:
        return [name for name, c in configs.items() if pred(c)]


def _generate_containers_low_survival_low_live_data(args: _ArgsForGenerate) -> BenchFile:

    live_data_sizes_mb: Sequence[int] = (16, 32, 64)
    n_threads_heaps = 1
    mem_limits_mb: Sequence[float] = (64, 96, 128, 192, 256)

    common_config = Config(
        complus_gcserver=True, complus_gcconcurrent=False, complus_gcheapcount=n_threads_heaps
    )

    configs = {
        f"{mem_limit_mb}mb": Config(container=TestConfigContainer(memory_mb=mem_limit_mb))
        for mem_limit_mb in mem_limits_mb
    }

    benchmarks = make_mapping(
        map(
            lambda live_data_size_mb: (
                f"{live_data_size_mb}_live_mb",
                to_benchmark(
                    GCPerfSimArgs(
                        tagb=20,
                        tc=n_threads_heaps,
                        tlgb=mb_to_gb(live_data_size_mb),
                        sohsi=50,  # 1/50 survive
                    ),
                    only_configs=_get_only_configs(
                        configs,
                        lambda c: non_null(non_null(c.container).memory_mb) > live_data_size_mb,
                    ),
                ),
            ),
            live_data_sizes_mb,
        )
    )

    return BenchFile(
        comment=None,
        configs_vary_by=ConfigsVaryBy(name="container_memory_mb", default_values=None),
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args),
        common_config=common_config,
        configs=configs,
        benchmarks=benchmarks,
    )


def _generate_containers_with_some_temp_allocations(args: _ArgsForGenerate) -> BenchFile:
    # crashes OOM with 8 heaps
    ns_threads_heaps = unique((1, 2, 4, 6))
    live_data_size_mb = 64
    container_size_mb = live_data_size_mb * 2

    common_config = Config(
        complus_gcserver=True,
        complus_gcconcurrent=False,
        container=TestConfigContainer(memory_mb=container_size_mb),
    )
    configs = {
        f"{n_threads_heaps}_heaps": Config(complus_gcheapcount=n_threads_heaps)
        for n_threads_heaps in ns_threads_heaps
    }

    benchmarks = make_mapping(
        map(
            lambda n_threads_heaps: (
                f"{n_threads_heaps}_threads",
                to_benchmark(
                    GCPerfSimArgs(
                        tagb=10, tc=n_threads_heaps, tlgb=mb_to_gb(live_data_size_mb), sohsi=30
                    ),
                    only_configs=_get_only_configs(
                        configs, lambda c: c.complus_gcheapcount == n_threads_heaps
                    ),
                ),
            ),
            ns_threads_heaps,
        )
    )

    return BenchFile(
        comment=None,
        configs_vary_by=ConfigsVaryBy(name="container_memory_mb", default_values=None),
        coreclrs=args.coreclr_specifiers,
        options=_default_options(args, min_seconds=10),
        common_config=common_config,
        configs=configs,
        benchmarks=benchmarks,
    )


def _generate_benchyaml_for_nheaps(cmd_args: _NheapsArgs) -> None:
    def f(args: _ArgsForGenerate) -> BenchFile:
        container_memory_mb = 500
        hardlimit_gb = non_null(cmd_args.live_gb) * 2
        assert hardlimit_gb <= container_memory_mb
        common_config = Config(
            complus_gcconcurrent=False,
            complus_gcheaphardlimit=mb_to_bytes(hardlimit_gb),
            container=TestConfigContainer(memory_mb=container_memory_mb),
        )

        for_workstation = {str(-1): Config(complus_gcserver=False)}  # Using workstation GC
        for_n_heaps = {
            str(n_heaps): Config(complus_gcserver=True, complus_gcheapcount=n_heaps)
            for n_heaps in range_inclusive(2, non_null(cmd_args.tc), 2)
        }
        configs = combine_mappings(for_workstation, for_n_heaps)

        return _normal_bench_file(
            args,
            common_config,
            configs,
            # Make TAMB low so it doesn't take too long
            {
                "nosurvive": to_benchmark(
                    GCPerfSimArgs(
                        tc=non_null(cmd_args.tc), tagb=50, tlgb=non_null(cmd_args.live_gb), sohsi=0
                    )
                )
            },
            configs_vary_by=_nheaps_configs_vary_by(args),
        )

    _generate_helper(cmd_args, f)


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


def hid(fn: CommandFunction) -> Command:
    return Command(hidden=True, kind=CommandKind.run, fn=fn, doc="Generates a test.")


GENERATE_COMMANDS: CommandsMapping = {
    "generate-coreclr": hid(_no_extra_args(_generate_benchyaml_for_coreclr_unit_tests)),
    "generate-gcperfsim": hid(_no_extra_args(_generate_benchyaml_for_gcperfsim)),
    "generate-gen0size-nosurvive": hid(_no_extra_args(_generate_gen0size_nosurvive)),
    "generate-nheaps": hid(_generate_benchyaml_for_nheaps),
    "generate-lomem": hid(_no_extra_args(_generate_benchyaml_for_container_nheaps)),
    "generate-container-memory-limits": hid(_generate_for_container_memory_limits),
    "generate-containers-no-survival-no-live-data": hid(
        _no_extra_args(_generate_containers_no_survival_no_live_data)
    ),
    "generate-containers-low-survival-temporary-data": hid(
        _no_extra_args(_generate_containers_low_survival_temporary_data)
    ),
    "generate-100pct-survival-low-live-data": hid(
        _no_extra_args(_generate_100pct_survival_low_live_data)
    ),
    "generate-containers-low-survival-low-live-data": hid(
        _no_extra_args(_generate_containers_low_survival_low_live_data)
    ),
    "generate-containers-with-some-temp-allocations": hid(
        _no_extra_args(_generate_containers_with_some_temp_allocations)
    ),
    "generate-containers-varying-live-mb": hid(
        _no_extra_args(_generate_containers_varying_live_mb)
    ),
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
