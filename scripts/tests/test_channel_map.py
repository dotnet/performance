from argparse import ArgumentTypeError, Namespace
import importlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
from unittest.mock import Mock
import xml.etree.ElementTree as ET

import pytest

from scripts.channel_map import ChannelMap
from scripts import run_performance_job as job
from scripts.run_performance_job import RunPerformanceJobArgs, get_target_framework


repo_root = Path(__file__).resolve().parents[2]


def test_main_selects_sdk_and_runtime_12():
    assert ChannelMap.get_branch("main") == "12.0"
    assert ChannelMap.get_quality_from_channel("main") == "daily"
    assert ChannelMap.get_target_framework_moniker("main") == "net12.0"
    assert ChannelMap.get_channel_from_target_framework_moniker("net12.0") == "main"
    assert "net12.0" in ChannelMap.get_supported_frameworks()
    assert ChannelMap.get_target_framework_moniker("main", mobile=True) == "net11.0"


@pytest.mark.parametrize(("framework", "flavor", "moniker", "legacy_flavor", "aot_mode"), [
    ("net10.0", "Mono", "wasmnet10_0", False, None),
    ("net10.0", "CoreCLR", "wasmnet10_0", True, None),
    ("net11.0", "Mono", "wasmnet11_0", False, None),
    ("net11.0", "CoreCLR", "wasmnet11_0", True, None),
    ("net12.0", "Mono", "wasmnet12_0", False, None),
    ("net12.0", "CoreCLR", "wasmnet12_0", True, None),
    ("net12.0", "Mono", "wasmnet12_0", False, "wasm"),
])
def test_wasm_runner_arguments_match_benchmarkdotnet_version(framework, flavor, moniker, legacy_flavor, aot_mode):
    from scripts import micro_benchmarks

    args = Namespace(corerun=None, cli=None, enable_pmc=False, filter=None, resume=False,
                     bdn_arguments=["--aotcompilermode", aot_mode] if aot_mode else None,
                     bdn_artifacts=None, wasm=True, wasm_runtime_flavor=flavor)
    arguments = micro_benchmarks.__get_benchmarkdotnet_arguments(framework, args)
    assert arguments[arguments.index("--runtimes") + 1] == moniker
    assert ("--wasmRuntimeFlavor" in arguments) == legacy_flavor
    if legacy_flavor:
        assert arguments[arguments.index("--wasmRuntimeFlavor") + 1] == flavor
    if aot_mode:
        assert arguments[arguments.index("--aotcompilermode") + 1] == aot_mode


def test_unknown_wasm_framework_fails_explicitly():
    from scripts import micro_benchmarks

    args = Namespace(corerun=None, cli=None, enable_pmc=False, filter=None, resume=False,
                     bdn_arguments=None, bdn_artifacts=None, wasm=True, wasm_runtime_flavor="Mono")
    with pytest.raises(ArgumentTypeError, match="Framework net99.0 is not supported for wasm"):
        micro_benchmarks.__get_benchmarkdotnet_arguments("net99.0", args)


@pytest.mark.parametrize("version", [10, 11])
def test_versioned_channels_keep_their_sdk_and_framework(version):
    for channel in (f"{version}.0", f"release/{version}.0"):
        assert ChannelMap.get_branch(channel) == f"{version}.0"
        assert ChannelMap.get_target_framework_moniker(channel) == f"net{version}.0"
        assert ChannelMap.get_target_framework_moniker(channel, mobile=True) == f"net{version}.0"
    assert ChannelMap.get_target_framework_moniker(f"nativeaot{version}.0") == f"nativeaot{version}.0"


@pytest.mark.parametrize("channel", [
    "master", "3.1", "release/3.1.1xx", "release/3.1.2xx", "release/3.1.3xx",
    "6.0", "release/6.0", "nativeaot6.0",
    "7.0", "release/7.0", "release/7.0-rc1", "release/7.0-rc2", "nativeaot7.0",
    "8.0", "9.0", "release/8.0", "release/9.0", "nativeaot8.0", "nativeaot9.0",
    "release/8.0-rc1", "release/8.0-rc2", "8.0-preview",
    *(f"release/8.0-preview{version}" for version in range(1, 8)),
])
def test_retired_channels_are_not_supported(channel):
    assert channel not in ChannelMap.get_supported_channels()
    with pytest.raises(Exception, match="is not supported"):
        ChannelMap.get_target_framework_moniker(channel)
    with pytest.raises(Exception, match="is not supported"):
        ChannelMap.get_branch(channel)


def test_retired_frameworks_have_no_acquisition_channel():
    for framework in ("netcoreapp2.1", "netcoreapp3.0", "netcoreapp3.1",
                      *(f"net{major}.0" for major in range(5, 10)),
                      *(f"nativeaot{major}.0" for major in range(6, 10))):
        assert framework not in ChannelMap.get_supported_frameworks()
        with pytest.raises(Exception, match="is not supported"):
            ChannelMap.get_channel_from_target_framework_moniker(framework)
    assert ChannelMap.get_branch("net472") == "10.0"
    assert ChannelMap.get_target_framework_moniker("net472") == "net472"
    assert ChannelMap.get_branch("net48") == "LTS"
    assert ChannelMap.get_supported_frameworks() == {
        "net10.0", "net11.0", "net12.0", "nativeaot10.0", "nativeaot11.0", "net472", "net48"}


@pytest.mark.parametrize("framework", [
    "netcoreapp2.1", "netcoreapp3.0", "netcoreapp3.1",
    *(f"net{major}.0" for major in range(5, 10)),
    *(f"nativeaot{major}.0" for major in range(6, 10)),
])
def test_retired_explicit_frameworks_fail_before_payload_setup(framework):
    args = RunPerformanceJobArgs(
        run_kind="micro", architecture="x64", os_group="windows", framework=framework)
    with pytest.raises(ValueError, match="targeting is retired"):
        get_target_framework(args, "main")


def test_net472_explicit_target_is_not_retired():
    args = RunPerformanceJobArgs(
        run_kind="micro", architecture="x64", os_group="windows", framework="net472")
    assert get_target_framework(args, "main") == "net472"


@pytest.mark.parametrize(
    "run_kind",
    ["micro", "sdk_scenarios", "nativeaot_scenarios", "blazor_scenarios",
     "maui_scenarios", "maui_scenarios_android", "maui_scenarios_android_innerloop",
     "maui_scenarios_ios", "android_scenarios", "ios_scenarios"],
)
@pytest.mark.parametrize("channel", ["main", "11.0", "10.0"])
def test_job_target_is_independent_of_sdk_channel(run_kind, channel):
    args = RunPerformanceJobArgs(run_kind=run_kind, architecture="x64", os_group="windows")
    mobile = run_kind.startswith("maui_scenarios") or run_kind in ("android_scenarios", "ios_scenarios")
    expected = "net11.0" if channel == "main" and mobile else ChannelMap.get_target_framework_moniker(channel)
    assert get_target_framework(args, channel) == expected

    args.framework = "net10.0"
    assert get_target_framework(args, channel) == "net10.0"


@pytest.mark.parametrize(("run_kind", "runtime_type"), [
    ("maui_scenarios", "coreclr"), ("maui_scenarios_android", "coreclr"),
    ("maui_scenarios_android_innerloop", "coreclr"), ("maui_scenarios_ios", "coreclr"),
    ("android_scenarios", "AndroidMono"), ("ios_scenarios", "iOSCoreCLR"),
    ("micro", "AndroidMono"), ("micro", "AndroidCoreCLR"),
    ("micro", "iOSMono"), ("micro", "iOSCoreCLR"), ("micro", "iOSNativeAOT"),
])
def test_removing_mobile_override_inherits_main(monkeypatch, run_kind, runtime_type):
    monkeypatch.delitem(job.ChannelMap.channel_map["main"], "mobile_tfm")
    args = RunPerformanceJobArgs(
        run_kind=run_kind, runtime_type=runtime_type, architecture="x64", os_group="windows")
    assert get_target_framework(args, "main") == "net12.0"
    args.framework = "net10.0"
    assert get_target_framework(args, "main") == "net10.0"


@pytest.mark.parametrize(("branch", "channel", "run_kind", "runtime_type", "framework", "expected"), [
    ("main", None, "micro", "coreclr", None, "net12.0"),
    ("main", None, "micro", "wasm_coreclr", None, "net12.0"),
    ("main", None, "micro", "wasm", None, "net12.0"),
    ("main", None, "micro", "mono", None, "net12.0"),
    ("main", None, "micro", "AndroidMono", None, "net11.0"),
    ("main", None, "micro", "AndroidCoreCLR", None, "net11.0"),
    ("main", None, "micro", "iOSMono", None, "net11.0"),
    ("main", None, "micro", "iOSCoreCLR", None, "net11.0"),
    ("main", None, "micro", "iOSNativeAOT", None, "net11.0"),
    ("main", None, "android_scenarios", "AndroidMono", None, "net11.0"),
    ("main", None, "ios_scenarios", "iOSCoreCLR", None, "net11.0"),
    ("main", "main", "maui_scenarios_android", "coreclr", None, "net11.0"),
    ("release/10.0", None, "micro", "mono", None, "net10.0"),
    ("release/10.0", "main", "maui_scenarios_ios", "coreclr", None, "net11.0"),
    ("main", None, "micro", "mono", "net10.0", "net10.0"),
    ("main", "11.0", "micro", "coreclr", None, "net11.0"),
])
def test_job_resolves_framework_before_validation_and_identity(
        monkeypatch, branch, channel, run_kind, runtime_type, framework, expected):
    monkeypatch.setenv("BUILD_SOURCEBRANCH", f"refs/heads/{branch}")
    monkeypatch.setattr(job, "setup_loggers", Mock())
    assignments = {}

    def stop_after_build_config(name, value, **kwargs):
        assignments[name] = value
        raise RuntimeError("stop after framework resolution")

    monkeypatch.setattr(job, "set_environment_variable", stop_after_build_config)
    args = RunPerformanceJobArgs(
        run_kind=run_kind, runtime_type=runtime_type, architecture="x64", os_group="windows",
        channel=channel, framework=framework, queue="test-queue",
        performance_repo_ci=run_kind.startswith("maui_scenarios"))
    with pytest.raises(RuntimeError, match="stop after framework resolution"):
        job.run_performance_job(args)
    assert args.framework == expected
    expected_config = (
        f"x64_{channel}_{run_kind}" if args.performance_repo_ci
        else f"x64.{run_kind}.{expected}")
    assert assignments["_BuildConfig"] == expected_config


@pytest.mark.parametrize(("channel", "framework", "sdk_major", "run_kind"), [
    ("main", None, 12, "micro"), ("main", "net11.0", 12, "micro"),
    ("11.0", None, 11, "micro"), ("nativeaot11.0", None, 11, "micro"),
    ("10.0", None, 10, "micro"), ("main", None, 12, "maui_scenarios_android"),
    ("main", None, 12, "maui_scenarios_ios"), ("release/10.0", None, 10, "maui_scenarios"),
])
def test_setup_preserves_sdk_channel_with_target_override(
        tmp_path, monkeypatch, channel, framework, sdk_major, run_kind):
    from scripts import ci_setup

    for name in ("global.json", "global.net11.json", "global.net10.json"):
        shutil.copy(repo_root / name, tmp_path / name)
    monkeypatch.setattr(ci_setup, "get_repo_root_path", lambda: str(tmp_path))
    monkeypatch.setattr(ci_setup, "setup_loggers", Mock())
    install = Mock()
    monkeypatch.setattr(ci_setup, "init_tools", install)
    monkeypatch.setattr(ci_setup, "check_output", Mock(return_value=b"performance-sha"))
    monkeypatch.setattr(ci_setup.dotnet, "info", Mock())
    sdk_path = tmp_path / "sdk"
    for major in (10, 11, 12):
        sdk_directory = sdk_path / f"{major}.0.100"
        sdk_directory.mkdir(parents=True)
        (sdk_directory / ".version").write_text(
            f"sdk-sha-{major}\n{major}.0.100\nunused\n{major}.0.100-precise\n",
            encoding="utf-8")
    monkeypatch.setattr(ci_setup.dotnet, "get_sdk_path", lambda _: str(sdk_path))
    monkeypatch.setattr(ci_setup.dotnet, "get_commit_date", Mock(return_value="2026-10-27T00:00:00Z"))
    monkeypatch.setenv("PERFLAB_Framework", "")
    setup = tmp_path / "machine-setup"

    framework = get_target_framework(RunPerformanceJobArgs(
        run_kind=run_kind, architecture="x64", os_group="windows", framework=framework), channel)
    ci_setup.main(ci_setup.CiSetupArgs(
        channel=channel, framework=framework, architecture="x64", output_file=str(setup)))

    expected_framework = framework or ChannelMap.get_target_framework_moniker(channel)
    assert install.call_args.kwargs["channel"] == channel
    assert os.environ["PERFLAB_Framework"] == expected_framework
    output = setup.with_suffix(".cmd" if os.name == "nt" else ".sh").read_text()
    expected_assignment = (
        f'PERFLAB_TARGET_FRAMEWORKS={expected_framework}' if os.name == "nt"
        else f'PERFLAB_TARGET_FRAMEWORKS="{expected_framework}"')
    assert expected_assignment in output
    expected_sdk_assignment = (
        f"DOTNET_VERSION={sdk_major}.0.100-precise" if os.name == "nt"
        else f'DOTNET_VERSION="{sdk_major}.0.100-precise"')
    assert expected_sdk_assignment in output
    expected_branch = ChannelMap.get_branch(channel)
    assert (
        f"PERFLAB_BRANCH={expected_branch}" if os.name == "nt"
        else f'PERFLAB_BRANCH="{expected_branch}"') in output
    assert f"sdk-sha-{sdk_major}" in output
    assert json.loads((tmp_path / "global.json").read_text())["sdk"]["version"].startswith(f"{sdk_major}.")


@pytest.mark.parametrize(("frameworks", "sdk_major"), [
    (["net10.0"], 10), (["net11.0"], 11), (["net12.0"], 12),
    (["net10.0", "net11.0"], 11), (["net11.0", "net12.0"], 12),
    (["net472", "net11.0"], 11), (["net472"], 12),
])
@pytest.mark.parametrize("fail", [False, True])
def test_benchmarks_ci_selects_and_restores_sdk_pin(tmp_path, monkeypatch, frameworks, sdk_major, fail):
    from scripts import benchmarks_ci

    for name in ("global.json", "global.net10.json", "global.net11.json"):
        shutil.copy(repo_root / name, tmp_path / name)
    monkeypatch.setattr(benchmarks_ci, "get_repo_root_path", lambda: str(tmp_path))
    global_json = tmp_path / "global.json"
    original_content = global_json.read_bytes()

    def run():
        with benchmarks_ci.__use_compatible_global_json(frameworks):
            assert json.loads(global_json.read_text())["sdk"]["version"].startswith(f"{sdk_major}.")
            if fail:
                raise RuntimeError("benchmark failure")

    if fail:
        with pytest.raises(RuntimeError, match="benchmark failure"):
            run()
    else:
        run()
    assert global_json.read_bytes() == original_content


@pytest.mark.parametrize(("run_kind", "runtime_type", "requested", "expected"), [
    ("micro", "coreclr", None, "net12.0"),
    ("micro", "wasm_coreclr", None, "net12.0"),
    ("micro", "wasm", None, "net12.0"),
    ("micro", "mono", None, "net12.0"),
    ("micro", "AndroidMono", None, "net11.0"),
    ("micro", "AndroidCoreCLR", None, "net11.0"),
    ("micro", "iOSMono", None, "net11.0"),
    ("micro", "iOSCoreCLR", None, "net11.0"),
    ("micro", "iOSNativeAOT", None, "net11.0"),
    ("android_scenarios", "AndroidMono", None, "net11.0"),
    ("ios_scenarios", "iOSCoreCLR", None, "net11.0"),
    ("micro", "mono", "net10.0", "net10.0"),
])
def test_runtime_pipeline_only_creates_explicit_framework_matrix(run_kind, runtime_type, requested, expected):
    template = (repo_root / "eng" / "pipelines" / "templates" / "runtime-perf-job.yml").read_text()
    assert "  framework: ''" in template
    assert """    ${{ if ne(parameters.framework, '') }}:
      frameworks:
      - ${{ parameters.framework }}
""" in template
    assert template.count("frameworks:") == 1
    assert "net11.0" not in template and "net12.0" not in template
    shared = (repo_root / "eng" / "pipelines" / "templates" / "run-performance-job.yml").read_text()
    assert "frameworks: []" in shared
    assert "${{ each framework in parameters.frameworks }}:" in shared
    assert "'--framework $(_Framework)'" in shared
    frameworks = [requested] if requested else []
    assert (len(frameworks) or 1) == 1
    args = RunPerformanceJobArgs(
        run_kind=run_kind, runtime_type=runtime_type, architecture="x64", os_group="linux",
        framework=frameworks[0] if frameworks else None)
    assert get_target_framework(args, "main") == expected


def test_sdk_tooling_installs_share_root_and_select_main_last():
    from scripts import dotnet

    template = (repo_root / "eng" / "pipelines" / "sdk-perf-jobs.yml").read_text()
    steps = template.split("    steps:\n", 1)[1].split("- ${{ if parameters.runPublicJobs }}:", 1)[0]
    root = r"$(Build.SourcesDirectory)\tools\dotnet\x64"
    tasks = steps.split("- task: UseDotNet@2")
    assert len(tasks) == 2
    inputs = tasks[1].split("- pwsh:", 1)[0]
    assert "version: 10.0.x\n" in inputs
    assert f"installationPath: '{root}'" in inputs
    assert "includePreviewVersions: true" in inputs
    assert steps.count("python scripts\\dotnet.py install") == 1
    assert f'python scripts\\dotnet.py install --channels main --architecture x64 --install-dir "{root}"' in steps
    assert f'##vso[task.prependpath]{root}' in steps
    assert steps.index("version: 10.0.x") < steps.index("python scripts\\dotnet.py install") < steps.index("dotnet test")
    assert "--framework net10.0" in steps
    assert "Startup.Tests\\Startup.Tests.csproj --configuration Release --framework net10.0 --nologo" in steps
    assert steps.count("dotnet test ") == 5
    assert "8.0.x" not in steps and "9.0.x" not in steps
    args = dotnet.__process_arguments([
        "install", "--channels", "main", "--architecture", "x64", "--install-dir", root])
    assert args.channels == ["main"]
    assert args.architecture == "x64"
    assert args.install_dir == root


@pytest.mark.parametrize(("group", "caller_count"), [
    ("runPublicJobs", 15), ("runPrivateJobs", 13), ("runScheduledPrivateJobs", 10),
])
def test_automatic_sdk_matrix_callers_only_select_main(group, caller_count):
    template = (repo_root / "eng" / "pipelines" / "sdk-perf-jobs.yml").read_text()
    groups = re.split(r"^- \$\{\{ if parameters\.(\w+) \}\}:\s*$", template, flags=re.MULTILINE)
    bodies = [body for name, body in zip(groups[1::2], groups[2::2]) if name == group]
    assert bodies
    lists = []
    for body in bodies:
        assert not re.search(r"\b(?:channel|framework|frameworks|dotnetVersionsLinks):", body)
        lists.extend(re.findall(r"^\s+channels:[^\n]*\n((?:[ \t]+- [^\n]+\n)+)", body, flags=re.MULTILINE))
    assert len(lists) == caller_count
    for channel_list in lists:
        channels = re.findall(r"^\s+- (\S+)", channel_list, flags=re.MULTILINE)
        assert channels == ["main"]
        assert all(ChannelMap.get_target_framework_moniker(channel) == "net12.0" for channel in channels)
    matrix = (repo_root / "eng" / "pipelines" / "templates" / "run-performance-job.yml").read_text()
    assert "${{ each channel in parameters.channels }}:" in matrix
    assert "_Channel: ${{ channel }}" in matrix
    assert "'--channel $(_Channel)'" in matrix


@pytest.mark.parametrize("version", ["3.1", 6, 7, 8, 9])
def test_retired_sdk_pins_are_removed(version):
    assert not (repo_root / f"global.net{version}.json").exists()
    assert f"global.net{version}.json" not in (repo_root / "scripts" / "ci_setup.py").read_text()
    for config in (repo_root / "NuGet.config", repo_root / "src" / "benchmarks" / "micro" / "wasm" / "nuget.config"):
        sources = ET.parse(config).findall("./packageSources/add")
        assert not {f"dotnet{version}", f"dotnet{version}-transport"} & {item.get("key") for item in sources}


def test_gc_launch_assets_do_not_select_retired_frameworks():
    gc_root = repo_root / "src" / "benchmarks" / "gc" / "GC.Infrastructure"
    commands = [
        *gc_root.joinpath("Configurations", "ASPNetBenchmarks").glob("*.csv"),
        *gc_root.joinpath("GC.Infrastructure", "Commands", "RunCommand", "BaseSuite").glob("*.csv"),
    ]
    assert commands
    frameworks = []
    for path in commands:
        frameworks.extend(re.findall(r"--application\.framework (\S+)", path.read_text(encoding="utf-8-sig")))
    assert "net10.0" in frameworks
    assert all(not re.fullmatch(r"net(?:coreapp)?[1-9]\.\d+", framework) for framework in frameworks)
    for path in gc_root.joinpath("Notebooks", "Examples").glob("*.ipynb"):
        notebook = json.loads(path.read_text(encoding="utf-8-sig"))
        for cell in notebook["cells"]:
            for line in cell.get("source", []):
                if "#r " in line and "GC.Analysis.API.dll" in line:
                    assert "\\net10.0\\" in line


@pytest.mark.parametrize("project", [
    "src/benchmarks/micro-fsharp/MicrobenchmarksFSharp.fsproj",
    "src/benchmarks/real-world/FSharp/FSharp.fsproj",
    "src/benchmarks/real-world/Roslyn/CompilerBenchmarks.csproj",
    "src/benchmarks/real-world/PowerShell.Benchmarks/PowerShell.Benchmarks.csproj",
    "src/benchmarks/real-world/Microsoft.ML.Benchmarks/Microsoft.ML.Benchmarks.csproj",
    "src/benchmarks/real-world/Akade.IndexedSet.Benchmarks/Akade.IndexedSet.Benchmarks.csproj",
    "src/benchmarks/real-world/ImageSharp/ImageSharp.Benchmarks.csproj",
    "src/benchmarks/real-world/bepuphysics2/DemoBenchmarks.csproj",
    "src/benchmarks/real-world/ILLink/SampleProject/HelloWorld.template.csproj",
    "src/scenarios/blazorlocalized/src/BlazorLocalized/BlazorLocalized.csproj",
    "src/scenarios/blazorlocalized/src/BlazorLocalized.ClassLibrary/BlazorLocalized.ClassLibrary.csproj",
    *[
        f"src/scenarios/{scenario}/src/{project}/{project}.csproj"
        for scenario in ("blazorpizza", "blazorpizzaaot")
        for project in ("BlazingComponents", "BlazingPizza.Client", "BlazingPizza.ComponentsLibrary", "BlazingPizza.Shared")
    ],
])
def test_standalone_benchmark_defaults_use_net10(project):
    result = subprocess.run(
        [shutil.which("dotnet"), "msbuild", str(repo_root / project), "-nologo",
         "-p:PERFLAB_TARGET_FRAMEWORKS=", "-getProperty:TargetFrameworks"],
        cwd=repo_root, check=True, capture_output=True, text=True,
    )
    assert result.stdout.strip() == "net10.0"


@pytest.mark.parametrize(("project", "framework"), [
    ("src/tools/ResultsComparer/ResultsComparer.csproj", "net10.0"),
    ("src/tools/PerfLabGenericEventSourceForwarder/PerfLabGenericEventSourceForwarder/PerfLabGenericEventSourceForwarder.csproj", "net10.0"),
    ("src/benchmarks/other/docker/docker-harness.csproj", "net10.0"),
    ("src/scenarios/weblarge3.0/src/mvc/mvc.csproj", "net10.0"),
    ("src/scenarios/staticwinformstemplate/src/staticwinformstemplate.csproj", "net10.0-windows"),
    ("src/scenarios/windowsformslarge/src/WinformsNetCorePerfApp1.csproj", "net10.0-windows"),
    ("src/scenarios/wpfsfc/src/wpfsfc.csproj", "net10.0-windows"),
])
def test_older_standalone_targets_are_migrated(project, framework):
    result = subprocess.run(
        [shutil.which("dotnet"), "msbuild", str(repo_root / project), "-nologo",
         "-p:PERFLAB_TARGET_FRAMEWORKS=", "-getProperty:TargetFramework"],
        cwd=repo_root, check=True, capture_output=True, text=True,
    )
    assert result.stdout.strip() == framework


def test_shared_harness_and_tooling_floor():
    harness = ET.parse(repo_root / "src" / "harness" / "BenchmarkDotNet.Extensions" / "BenchmarkDotNet.Extensions.csproj")
    assert harness.findtext(".//TargetFramework") == "net10.0"
    references = harness.findall(".//ProjectReference")
    assert references[0].get("SetTargetFramework") == "TargetFramework=net10.0"
    assert references[1].get("SetTargetFramework") == "TargetFramework=netstandard2.0"
    result = subprocess.run(
        [shutil.which("dotnet"), "msbuild",
         str(repo_root / "src" / "tools" / "ScenarioMeasurement" / "Startup.Tests" / "Startup.Tests.csproj"),
         "-nologo", "-p:PERFLAB_TARGET_FRAMEWORKS=", "-getProperty:TargetFramework"],
        cwd=repo_root, check=True, capture_output=True, text=True,
    )
    assert result.stdout.strip() == "net10.0"


def test_mobile_main_uses_existing_dependency_branches(monkeypatch):
    monkeypatch.syspath_prepend(str(repo_root / "scripts"))
    monkeypatch.syspath_prepend(str(repo_root / "src" / "scenarios"))
    maui = importlib.import_module("shared.mauisharedpython")
    args = RunPerformanceJobArgs(
        run_kind="maui_scenarios_android", architecture="x64", os_group="windows")
    framework = get_target_framework(args, "main")
    assert maui._get_repo_nuget_config_url("dotnet/maui", framework + "-android") == (
        "https://raw.githubusercontent.com/dotnet/maui/net11.0/NuGet.config")
    assert maui._get_repo_nuget_config_url("dotnet/macios", framework + "-ios") == (
        "https://raw.githubusercontent.com/dotnet/macios/net11.0/NuGet.config")


@pytest.mark.parametrize("framework", ["net10.0", "net11.0", "net12.0"])
def test_blazor_package_versions_match_target(tmp_path, framework):
    project = tmp_path / "blazor.proj"
    project.write_text(
        '<Project><Import Project="'
        + str(repo_root / "src" / "scenarios" / "build-common" / "Blazor.PackageVersions.props")
        + '" /></Project>',
        encoding="utf-8",
    )
    result = subprocess.run(
        [shutil.which("dotnet"), "msbuild", str(project), "-nologo",
         f"-p:TargetFrameworks={framework}", "-getProperty:AspNetCoreVersion,BlazorVersion,SystemNetHttpJsonVersion"],
        cwd=tmp_path, check=True, capture_output=True, text=True,
    )
    expected_version = framework.removeprefix("net") + ".0-*"
    assert set(json.loads(result.stdout)["Properties"].values()) == {expected_version}


@pytest.mark.parametrize(("framework", "extensions", "system"), [
    ("net10.0", "10.0.3", "10.0.0"),
    ("net11.0", "11.0.0", "11.0.0"),
    ("net12.0", "11.0.0", "11.0.0"),
])
def test_microbenchmarks_select_intended_packages(framework, extensions, system):
    result = subprocess.run(
        [shutil.which("dotnet"), "msbuild",
         str(repo_root / "src" / "benchmarks" / "micro" / "MicroBenchmarks.csproj"),
         "-nologo", f"-p:TargetFramework={framework}", f"-p:PERFLAB_TARGET_FRAMEWORKS={framework}",
         "-getProperty:ExtensionsVersion,SystemVersion,BenchmarkDotNetVersion,SupportedTargetFrameworks",
         "-getItem:PackageReference"],
        cwd=repo_root, check=True, capture_output=True, text=True,
    )
    properties = json.loads(result.stdout)["Properties"]
    assert properties["ExtensionsVersion"] == extensions
    assert properties["SystemVersion"] == system
    runner = ET.parse(repo_root / "eng" / "Versions.props").findtext(".//BenchmarkDotNetVersion")
    assert properties["BenchmarkDotNetVersion"] == runner
    assert "net12.0" in properties["SupportedTargetFrameworks"].split(";")
    assert not {"net8.0", "net9.0"} & set(properties["SupportedTargetFrameworks"].split(";"))
    tensors = [item["Version"] for item in json.loads(result.stdout)["Items"]["PackageReference"]
               if item["Identity"] == "System.Numerics.Tensors"]
    assert tensors == ["11.0.0"]


@pytest.mark.parametrize(("framework", "extensions", "system", "tensors"), [
    ("net10.0", "10.0.3", "10.0.0", "12.0.0-alpha.1"),
    ("net11.0", "11.0.0", "11.0.0", "11.0.0"),
    ("net12.0", "12.0.0-alpha.1", "12.0.0-alpha.1", "12.0.0-alpha.1"),
])
def test_next_library_pins_do_not_change_net11_measurements(framework, extensions, system, tensors):
    result = subprocess.run(
        [shutil.which("dotnet"), "msbuild",
         str(repo_root / "src" / "benchmarks" / "micro" / "MicroBenchmarks.csproj"),
         "-nologo", f"-p:TargetFramework={framework}", f"-p:PERFLAB_TARGET_FRAMEWORKS={framework}",
         "-p:MicrosoftExtensionsLoggingPackageVersion=12.0.0-alpha.1",
         "-p:SystemThreadingChannelsPackageVersion=12.0.0-alpha.1",
         "-getProperty:ExtensionsVersion,SystemVersion", "-getItem:PackageReference"],
        cwd=repo_root, check=True, capture_output=True, text=True,
    )
    evaluated = json.loads(result.stdout)
    assert evaluated["Properties"] == {"ExtensionsVersion": extensions, "SystemVersion": system}
    assert [item["Version"] for item in evaluated["Items"]["PackageReference"]
            if item["Identity"] == "System.Numerics.Tensors"] == [tensors]


@pytest.mark.parametrize("framework", ["net10.0", "net11.0", "net12.0"])
def test_sdk_scenarios_include_current_framework(tmp_path, framework):
    project = ET.parse(repo_root / "eng" / "performance" / "sdk_scenarios.proj")
    project.getroot().attrib.pop("Sdk")
    for item in project.findall("Import"):
        item.set("Project", str(repo_root / "eng" / "performance" / item.get("Project")))
    project_path = tmp_path / "scenarios.proj"
    project.write(project_path, encoding="utf-8")
    result = subprocess.run(
        [shutil.which("dotnet"), "msbuild", str(project_path), "-nologo",
         f"-p:PERFLAB_Framework={framework}", "-getItem:Framework,SDKWorkItem,HelixWorkItem"],
        cwd=tmp_path, check=True, capture_output=True, text=True,
    )
    items = json.loads(result.stdout)["Items"]
    frameworks = [item["Identity"] for item in items["Framework"]]
    assert frameworks == {
        "net10.0": ["net10.0"], "net11.0": ["net11.0"],
        "net12.0": ["net10.0", "net11.0", "net12.0"],
    }[framework]
    for kind in ("SDKWorkItem", "HelixWorkItem"):
        assert items[kind]
        assert not re.search(r"net(?:coreapp)?[1-9]\.\d+", json.dumps(items[kind]))
    assert any(item["Identity"] == f"SDK Console Template {framework}" for item in items["SDKWorkItem"])
    assert not any(item["Identity"].startswith("Hot Reload") for item in items["HelixWorkItem"])


def test_old_only_runner_and_sdk_examples_are_retired():
    assert not (repo_root / "scripts" / "benchmarks_monthly.py").exists()
    project = (repo_root / "eng" / "performance" / "sdk_scenarios.proj").read_text()
    assert not re.search(r"net(?:coreapp)?[1-9]\.\d+|Exclude=|Hot Reload|weblarge3\.0", project)
