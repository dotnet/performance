from argparse import ArgumentTypeError, Namespace
import importlib
import json
import os
from pathlib import Path
import shutil
import subprocess
from unittest.mock import Mock
import xml.etree.ElementTree as ET

import pytest

from scripts.channel_map import ChannelMap
from scripts.run_performance_job import RunPerformanceJobArgs, get_target_framework


repo_root = Path(__file__).resolve().parents[2]


def test_main_selects_sdk_and_runtime_12():
    assert ChannelMap.get_branch("main") == "12.0"
    assert ChannelMap.get_quality_from_channel("main") == "daily"
    assert ChannelMap.get_target_framework_moniker("main") == "net12.0"
    assert ChannelMap.get_channel_from_target_framework_moniker("net12.0") == "main"
    assert "net12.0" in ChannelMap.get_supported_frameworks()


@pytest.mark.parametrize(("framework", "flavor", "moniker", "legacy_flavor"), [
    ("net11.0", "Mono", "wasmnet11_0", False),
    ("net11.0", "CoreCLR", "wasmnet11_0", True),
    ("net12.0", "CoreCLR", "corewasm12.0", True),
])
def test_wasm_runner_arguments_match_benchmarkdotnet_version(framework, flavor, moniker, legacy_flavor):
    from scripts import micro_benchmarks

    args = Namespace(corerun=None, cli=None, enable_pmc=False, filter=None, resume=False,
                     bdn_arguments=None, bdn_artifacts=None, wasm=True, wasm_runtime_flavor=flavor)
    arguments = micro_benchmarks.__get_benchmarkdotnet_arguments(framework, args)
    assert arguments[arguments.index("--runtimes") + 1] == moniker
    assert ("--wasmRuntimeFlavor" in arguments) == legacy_flavor
    if legacy_flavor:
        assert arguments[arguments.index("--wasmRuntimeFlavor") + 1] == flavor


def test_net12_mono_wasm_fails_explicitly():
    from scripts import micro_benchmarks

    args = Namespace(corerun=None, cli=None, enable_pmc=False, filter=None, resume=False,
                     bdn_arguments=None, bdn_artifacts=None, wasm=True, wasm_runtime_flavor="Mono")
    with pytest.raises(ArgumentTypeError, match="Mono WebAssembly is not supported on net12.0"):
        micro_benchmarks.__get_benchmarkdotnet_arguments("net12.0", args)


@pytest.mark.parametrize("version", [8, 9, 10, 11])
def test_versioned_channels_keep_their_sdk_and_framework(version):
    for channel in (f"{version}.0", f"release/{version}.0"):
        assert ChannelMap.get_branch(channel) == f"{version}.0"
        assert ChannelMap.get_target_framework_moniker(channel) == f"net{version}.0"
    assert ChannelMap.get_target_framework_moniker(f"nativeaot{version}.0") == f"nativeaot{version}.0"


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


@pytest.mark.parametrize(("channel", "framework", "sdk_major"), [
    ("main", None, 12), ("main", "net11.0", 12), ("11.0", None, 11),
    ("nativeaot11.0", None, 11), ("10.0", None, 10),
])
def test_setup_preserves_sdk_channel_with_target_override(
        tmp_path, monkeypatch, channel, framework, sdk_major):
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
    assert f"sdk-sha-{sdk_major}" in output
    assert json.loads((tmp_path / "global.json").read_text())["sdk"]["version"].startswith(f"{sdk_major}.")


@pytest.mark.parametrize(("frameworks", "sdk_major"), [
    (["net8.0"], 8), (["net11.0"], 11), (["net12.0"], 12),
    (["net8.0", "net11.0"], 11), (["net11.0", "net12.0"], 12),
    (["net472", "net11.0"], 11), (["net472"], 12),
])
@pytest.mark.parametrize("fail", [False, True])
def test_benchmarks_ci_selects_and_restores_sdk_pin(tmp_path, monkeypatch, frameworks, sdk_major, fail):
    from scripts import benchmarks_ci

    for name in ("global.json", "global.net8.json", "global.net11.json"):
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


def test_runtime_pipeline_framework_overrides_precede_mobile_defaults():
    template = (repo_root / "eng" / "pipelines" / "templates" / "runtime-perf-job.yml").read_text()
    assert "  framework: ''" in template
    assert """    frameworks:
    - ${{ if ne(parameters.framework, '') }}:
      - ${{ parameters.framework }}
    - ${{ elseif in(parameters.runtimeType, 'AndroidMono', 'AndroidCoreCLR', 'iOSMono', 'iOSCoreCLR', 'iOSNativeAOT') }}:
      - net11.0
    - ${{ else }}:
      - net12.0
""" in template


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


@pytest.mark.parametrize("framework", ["net8.0", "net9.0", "net10.0", "net11.0", "net12.0"])
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


@pytest.mark.parametrize(("framework", "extensions", "system", "runner"), [
    ("net8.0", "8.0.0", "8.0.0", "0.16.0-nightly.20260801.595"),
    ("net9.0", "9.0.0", "9.0.0", "0.16.0-nightly.20260801.595"),
    ("net10.0", "10.0.3", "10.0.0", "0.16.0-nightly.20260801.595"),
    ("net11.0", "11.0.0", "11.0.0", "0.16.0-nightly.20260801.595"),
    ("net12.0", "11.0.0", "11.0.0", "0.16.0-nightly.20261014.649"),
])
def test_microbenchmarks_select_intended_packages(framework, extensions, system, runner):
    result = subprocess.run(
        [shutil.which("dotnet"), "msbuild",
         str(repo_root / "src" / "benchmarks" / "micro" / "MicroBenchmarks.csproj"),
         "-nologo", f"-p:TargetFramework={framework}", f"-p:PERFLAB_TARGET_FRAMEWORKS={framework}",
         "-getProperty:ExtensionsVersion,SystemVersion,BenchmarkDotNetVersion,SupportedTargetFrameworks"],
        cwd=repo_root, check=True, capture_output=True, text=True,
    )
    properties = json.loads(result.stdout)["Properties"]
    assert properties["ExtensionsVersion"] == extensions
    assert properties["SystemVersion"] == system
    assert properties["BenchmarkDotNetVersion"] == runner
    assert "net12.0" in properties["SupportedTargetFrameworks"].split(";")


@pytest.mark.parametrize(("framework", "expected"), [("net11.0", "11.0.0"), ("net12.0", "12.0.0-alpha.1")])
def test_next_library_pins_do_not_change_net11_measurements(framework, expected):
    result = subprocess.run(
        [shutil.which("dotnet"), "msbuild",
         str(repo_root / "src" / "benchmarks" / "micro" / "MicroBenchmarks.csproj"),
         "-nologo", f"-p:TargetFramework={framework}", f"-p:PERFLAB_TARGET_FRAMEWORKS={framework}",
         "-p:MicrosoftExtensionsLoggingPackageVersion=12.0.0-alpha.1",
         "-p:SystemThreadingChannelsPackageVersion=12.0.0-alpha.1",
         "-getProperty:ExtensionsVersion,SystemVersion"],
        cwd=repo_root, check=True, capture_output=True, text=True,
    )
    assert set(json.loads(result.stdout)["Properties"].values()) == {expected}


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
         f"-p:PERFLAB_Framework={framework}", "-getItem:Framework"],
        cwd=tmp_path, check=True, capture_output=True, text=True,
    )
    frameworks = [item["Identity"] for item in json.loads(result.stdout)["Items"]["Framework"]]
    assert framework in frameworks
    assert ("net12.0" in frameworks) == (framework == "net12.0")
