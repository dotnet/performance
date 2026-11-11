import io
import json
import os
import re
import subprocess
import sys
import tarfile
import time
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import Mock

import pytest

import ci_setup
import dotnet
from channel_map import ChannelMap
from compose_msbuild_sdk import (
    ASSETS, LIBRARIES, RUNTIME_FILES, compose, extract_archive, package_files, sha512,
    verify_archive,
)
from msbuild_inputs import configure_inputs
from msbuild_catalog import ASSET_DEFINITIONS, SCENARIOS, select_scenarios
from shared import collect_linux, msbuild, startup
from shared.msbuild_execution import execute_scenario, publish_selection
from shared.testtraits import TestTraits as ScenarioTraits
from sync_msbuild_scenarios import generate


SDK_TFM = ChannelMap.get_target_framework_moniker("main")
SDK_VERSION = SDK_TFM.removeprefix("net") + ".100"


def write_zip(path, entries):
    with zipfile.ZipFile(path, "w") as archive:
        for name, content in entries.items():
            archive.writestr(name, content)
    return path


@pytest.fixture
def composition(tmp_path):
    source = tmp_path / "source"
    source.mkdir()
    subprocess.run(["git", "init", "-q", str(source)], check=True)
    subprocess.run(["git", "-c", "user.name=Test", "-c", "user.email=test@example.invalid",
                    "commit", "-q", "--allow-empty", "-m", "Test"], cwd=source, check=True)
    sdk = write_zip(tmp_path / "sdk.zip", {
        "dotnet.exe": b"host", f"sdk/{SDK_VERSION}/MSBuild.dll": b"original",
    })
    packages = tmp_path / "packages"
    packages.mkdir()
    for name in LIBRARIES:
        write_zip(packages / f"{name}.1.0.0.nupkg",
                  {f"lib/{SDK_TFM}/{name}.dll": name.encode()})
    write_zip(packages / "Microsoft.Build.Runtime.1.0.0.nupkg",
              {f"contentFiles/any/{SDK_TFM}/{name}": name.encode() for name in RUNTIME_FILES})
    assets = {}
    for name, (_, project) in ASSETS.items():
        archive = write_zip(tmp_path / (name + ".zip"), {
            project: b"project", "NuGet.config": b"untrusted",
            "OrchardCore/.perfstar-packages/test.nupkg": b"vendored",
            **{relative: b"source" for relative in ASSET_DEFINITIONS[name]["edits"]},
        })
        assets[name] = (archive, sha512(archive))
    return SimpleNamespace(source=source, sdk=sdk, packages=packages, assets=assets,
                           output=tmp_path / "composed")


def test_overlay_and_helix_payload_preserve_source_identity(composition, tmp_path):
    item = composition
    compose(item.sdk, sha512(item.sdk), item.packages, item.output, item.source, item.assets)
    args = SimpleNamespace(run_env_vars=["USER_OPTION=value"])
    payload = tmp_path / "payload"
    configure_inputs(item.output, payload, args)
    assert (payload / f"dotnet/sdk/{SDK_VERSION}/Microsoft.Build.dll").read_bytes() == b"Microsoft.Build"
    assert (payload / f"dotnet/sdk/{SDK_VERSION}/MSBuild.dll").read_bytes() == b"MSBuild.dll"
    manifest = json.loads((payload / "msbuild-inputs/manifest.json").read_text())
    assert manifest["repository"] == "dotnet/msbuild"
    assert dotnet.get_repository(args.repository) == ("dotnet", "msbuild")
    assert args.commit_sha == manifest["commit"]
    assert args.commit_time == manifest["commit_time"]
    assert args.dotnet_versions == [SDK_VERSION]
    assert args.dotnet_path == args.install_dir == str(payload / "dotnet")
    assert "USER_OPTION=value" in args.run_env_vars
    assert "MSBuildSource=self-built-release" in args.run_env_vars


def test_linux_machine_setup_preserves_asset_metadata(composition, tmp_path, monkeypatch):
    item = composition
    compose(item.sdk, sha512(item.sdk), item.packages, item.output, item.source, item.assets)
    args = ci_setup.CiSetupArgs(
        channel="main", output_file=str(tmp_path / "machine-setup.sh"),
        local_build=True, target_windows=False)
    configure_inputs(item.output, tmp_path / "payload", args)
    monkeypatch.setattr(ci_setup, "setup_loggers", lambda **kwargs: None)
    monkeypatch.setattr(dotnet, "setup_dotnet", lambda *args: None)
    monkeypatch.setattr(dotnet, "info", lambda **kwargs: None)
    monkeypatch.setattr(ci_setup, "check_output", lambda *args: b"0" * 40)
    monkeypatch.setattr(ci_setup, "set_environment_variable", lambda *args: None)
    ci_setup.main(args)
    script = Path(args.output_file).read_text()
    identifiers = re.findall(r"^export ([^=]+)=", script, re.MULTILINE)
    assert identifiers and all(re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", key) for key in identifiers)
    assert 'export PERFLAB_REPO="dotnet/msbuild"' in script
    for name, (version, _) in ASSETS.items():
        key = f"PerfStarAsset_{name.replace('-', '_')}_version"
        assert f'export {key}="{version}"' in script
        assert f'export PERFLAB_DATA_{key}="{version}"' in script


def test_input_validation_rejects_corrupted_overlay(composition, tmp_path):
    item = composition
    compose(item.sdk, sha512(item.sdk), item.packages, item.output, item.source, item.assets)
    (item.output / f"sdk/sdk/{SDK_VERSION}/MSBuild.dll").write_bytes(b"changed")
    with pytest.raises(ValueError, match="Overlay checksum"):
        configure_inputs(item.output, tmp_path / "payload", SimpleNamespace(run_env_vars=None))


def test_overlay_requires_all_files_and_compatible_runtime(composition):
    sdk_major = int(SDK_VERSION.split(".")[0])
    with pytest.raises(ValueError, match="compatible"):
        package_files(composition.packages, sdk_major - 1)
    (composition.packages / "Microsoft.Build.Framework.1.0.0.nupkg").unlink()
    with pytest.raises(ValueError, match="Microsoft.Build.Framework"):
        package_files(composition.packages, sdk_major)


def test_overlay_selects_highest_compatible_package_tfm(composition):
    sdk_major = int(SDK_VERSION.split(".")[0])
    for package in composition.packages.glob("*.nupkg"):
        runtime = package.name.startswith("Microsoft.Build.Runtime.")
        prefix = "contentFiles/any/" if runtime else "lib/"
        names = RUNTIME_FILES if runtime else (package.name.split(".1.0.0")[0] + ".dll",)
        write_zip(package, {
            f"{prefix}net{major}.{minor}/{name}": f"{major}.{minor}:{name}".encode()
            for major, minor in ((sdk_major - 2, 0), (sdk_major - 1, 0), (sdk_major, 1))
            for name in names
        })
    files = package_files(composition.packages, sdk_major)
    assert files["MSBuild.dll"] == f"{sdk_major - 1}.0:MSBuild.dll".encode()
    assert files["Microsoft.Build.dll"] == f"{sdk_major - 1}.0:Microsoft.Build.dll".encode()


def test_archive_validation_rejects_bad_checksum_and_traversal(tmp_path):
    archive = write_zip(tmp_path / "bad.zip", {"../escaped": b"bad"})
    with pytest.raises(ValueError, match="SHA-512 mismatch"):
        verify_archive(archive, "0" * 128)
    with pytest.raises(ValueError, match="escapes"):
        extract_archive(archive, tmp_path / "extracted")
    assert not (tmp_path / "escaped").exists()


def test_prepare_pins_sdk_and_internal_restore_sources(composition, monkeypatch, tmp_path):
    item = composition
    compose(item.sdk, sha512(item.sdk), item.packages, item.output, item.source, item.assets)
    work = tmp_path / "work"
    work.mkdir()
    monkeypatch.chdir(work)
    monkeypatch.setenv("PERFLAB_MSBUILD_INPUTS", str(item.output))
    msbuild.prepare("orchard-core")
    config = (work / "app/NuGet.config").read_text()
    assert "dotnet-public" in config
    assert "perfstar-vendored" in config
    assert "untrusted" not in config
    assert json.loads((work / "app/global.json").read_text())["sdk"]["version"] == SDK_VERSION
    with pytest.raises(FileExistsError):
        msbuild.prepare("orchard-core")


@pytest.mark.parametrize("mt", [False, True])
def test_measured_rebuild_excludes_setup_and_diagnostics(tmp_path, monkeypatch, mt):
    monkeypatch.chdir(tmp_path)
    (tmp_path / "app").mkdir()
    project = tmp_path / "app" / ASSETS["net8-console-app"][1]
    project.parent.mkdir(parents=True)
    project.write_text("project")
    monkeypatch.setenv("MSBUILDDISABLENODEREUSE", "1")
    monkeypatch.setenv("UseSharedCompilation", "false")
    monkeypatch.setenv("MSBUILDDEBUGCOMM", "1")
    calls = []

    def execute(command, **kwargs):
        assert kwargs["check"] and kwargs["timeout"] == 900
        assert kwargs["cwd"] == "app"
        assert "MSBUILDDISABLENODEREUSE" not in kwargs["env"]
        assert "UseSharedCompilation" not in kwargs["env"]
        assert "MSBUILDDEBUGCOMM" not in kwargs["env"]
        assert kwargs["env"]["PATH"].split(os.pathsep)[0] == str(tmp_path)
        assert kwargs["env"]["DOTNET_CLI_ENABLEAOT"] == "0"
        assert kwargs["env"]["NUGET_HTTP_CACHE_PATH"] == str(tmp_path / "http-cache")
        calls.append(command)

    monkeypatch.setattr(subprocess, "run", execute)
    clock = iter(range(0, 40_000_000, 1_000_000))
    monkeypatch.setattr(time, "perf_counter_ns", lambda: next(clock))
    results = msbuild.measure("net8-console-app", tmp_path / "dotnet.exe", 2, 900, mt, True)
    assert results == [1.0, 1.0]
    assert len(calls) == 9
    assert "/t:restore" in calls[0]
    assert "/t:rebuild" not in calls[1]
    assert calls[2][1:] == ["build-server", "shutdown"]
    assert calls[4][1:] == ["build-server", "shutdown"]
    for index in (3, 5):
        assert "/t:rebuild" in calls[index]
        assert "--no-restore" not in calls[index]
        assert ("/mt" if mt else "/mt:false") in calls[index]
        assert not any(arg.startswith("/bl:") for arg in calls[index])
    assert any(arg.startswith("/bl:") for arg in calls[7])
    assert json.loads((tmp_path / "traces/build-wall-clock.json").read_text()) == results


def test_failed_measured_build_does_not_emit_success_samples(tmp_path, monkeypatch):
    monkeypatch.chdir(tmp_path)
    (tmp_path / "app").mkdir()
    project = tmp_path / "app" / ASSETS["net8-console-app"][1]
    project.parent.mkdir(parents=True)
    project.write_text("project")
    calls = []

    def execute(command, **kwargs):
        calls.append(command)
        if "/t:rebuild" in command:
            raise subprocess.CalledProcessError(1, command)

    monkeypatch.setattr(subprocess, "run", execute)
    with pytest.raises(subprocess.CalledProcessError):
        msbuild.measure("net8-console-app", tmp_path / "dotnet.exe", 1, 900, False)
    assert calls[-1][1:] == ["build-server", "shutdown"]
    assert not (tmp_path / "traces/build-wall-clock.json").exists()


@pytest.mark.parametrize("failure", [None, "measurement", "parser", "upload"])
def test_msbuild_preserves_artifacts_and_uploads_report_without_duplicate_success_copy(
        tmp_path, monkeypatch, failure):
    monkeypatch.chdir(tmp_path)
    inputs = tmp_path / "inputs"
    sdk = inputs / "sdk"
    sdk.mkdir(parents=True)
    (sdk / ("dotnet.exe" if os.name == "nt" else "dotnet")).write_bytes(b"host")
    (inputs / "manifest.json").write_text("{}")
    monkeypatch.setenv("PERFLAB_MSBUILD_INPUTS", str(inputs))
    monkeypatch.setenv("DOTNET_ROOT", str(sdk))
    monkeypatch.setattr(msbuild, "verify_sdk", lambda *args: None)
    upload_directory = tmp_path / "upload"
    monkeypatch.setattr(msbuild, "helixuploaddir", lambda: str(upload_directory))
    monkeypatch.setattr(startup, "helixuploaddir", lambda: str(upload_directory))
    monkeypatch.setattr(startup, "runninginlab", lambda: True)
    copies = []
    copytree = msbuild.shutil.copytree

    def copy_artifacts(source, destination, **kwargs):
        copies.append(str(source))
        return copytree(source, destination, **kwargs)

    monkeypatch.setattr(msbuild.shutil, "copytree", copy_artifacts)
    monkeypatch.setattr(startup, "copytree", copy_artifacts)
    wrapper = startup.StartupWrapper.__new__(startup.StartupWrapper)
    wrapper.startuppath = "Startup"
    wrapper.reportjson = "traces/perf-lab-report.json"
    monkeypatch.setattr(msbuild, "StartupWrapper", lambda: wrapper)

    def measure(*args):
        Path("traces").mkdir()
        Path("traces/diagnostic.nettrace").write_bytes(b"trace")
        if failure == "measurement":
            raise RuntimeError("measurement failed")

    def parse():
        if failure == "parser":
            raise subprocess.CalledProcessError(1, ["Startup"])
        Path(wrapper.reportjson).write_text("{}")

    monkeypatch.setattr(msbuild, "measure", measure)
    monkeypatch.setattr(startup, "RunCommand", lambda *args, **kwargs: SimpleNamespace(run=parse))
    upload_report = Mock(return_value=1 if failure == "upload" else 0)
    monkeypatch.setitem(sys.modules, "upload", SimpleNamespace(upload=upload_report))
    traits = ScenarioTraits(exename="net8-console-app", upload_to_perflab_container=True)

    if failure:
        with pytest.raises((RuntimeError, subprocess.CalledProcessError, SystemExit)):
            msbuild.run(traits, 1, 900, False, False)
    else:
        msbuild.run(traits, 1, 900, False, False)

    assert (upload_directory / "traces/diagnostic.nettrace").read_bytes() == b"trace"
    if failure != "upload":
        assert len(copies) == 1
    if failure not in ("measurement", "parser"):
        upload_report.assert_called_once()
        assert (upload_directory / "traces/perf-lab-report.json").is_file()
    else:
        upload_report.assert_not_called()


def write_tar(path, entries):
    with tarfile.open(path, "w:gz") as archive:
        for name, content in entries.items():
            entry = tarfile.TarInfo(name)
            entry.size = len(content)
            entry.mode = 0o755 if name.endswith("dotnet") else 0o644
            archive.addfile(entry, io.BytesIO(content))
    return path


def test_linux_composition_and_payload_restore_executable_permissions(composition, tmp_path):
    item = composition
    archive = write_tar(tmp_path / "sdk.tar.gz", {
        "dotnet": b"linux host", f"sdk/{SDK_VERSION}/MSBuild.dll": b"original",
    })
    compose(archive, sha512(archive), item.packages, item.output, item.source,
            item.assets, "linux")
    with pytest.raises(ValueError, match="platform"):
        configure_inputs(item.output, tmp_path / "wrong-platform", SimpleNamespace())
    (item.output / "sdk/dotnet").chmod(0o644)
    args = SimpleNamespace(run_env_vars=None)
    payload = tmp_path / "payload"
    configure_inputs(item.output, payload, args, "linux")
    assert (payload / "dotnet/dotnet").read_bytes() == b"linux host"
    if os.name != "nt":
        assert os.access(payload / "dotnet/dotnet", os.X_OK)
    assert json.loads((payload / "msbuild-inputs/manifest.json").read_text())["os"] == "linux"


@pytest.mark.parametrize("link_name", ["resource.dll", "../escape.dll"])
def test_tar_materializes_safe_links_and_rejects_escaping_links(tmp_path, link_name):
    archive = write_tar(tmp_path / "sdk.tar.gz", {"original.dll": b"content"})
    with tarfile.open(archive, "r:gz") as source:
        entries = [(member, source.extractfile(member).read()) for member in source]
    with tarfile.open(archive, "w:gz") as output:
        for member, content in entries:
            output.addfile(member, io.BytesIO(content))
        link = tarfile.TarInfo(link_name)
        link.type = tarfile.SYMTYPE
        link.linkname = "original.dll"
        output.addfile(link)
    if link_name.startswith(".."):
        with pytest.raises(ValueError, match="escapes"):
            extract_archive(archive, tmp_path / "sdk")
    else:
        extract_archive(archive, tmp_path / "sdk")
        assert (tmp_path / "sdk/resource.dll").read_bytes() == b"content"
        assert not (tmp_path / "sdk/resource.dll").is_symlink()


def test_sdk_links_reuse_extracted_files_instead_of_redecompressing_archive(tmp_path, monkeypatch):
    archive = tmp_path / "sdk.tar.gz"
    with tarfile.open(archive, "w:gz") as output:
        original = tarfile.TarInfo("original.dll")
        original.size = 7
        output.addfile(original, io.BytesIO(b"content"))
        for name, target in (("first.dll", "original.dll"), ("second.dll", "first.dll")):
            link = tarfile.TarInfo(name)
            link.type = tarfile.SYMTYPE
            link.linkname = target
            output.addfile(link)

    def reject_extractfile(*args, **kwargs):
        raise AssertionError("SDK link materialization must reuse the extracted regular file.")

    monkeypatch.setattr(tarfile.TarFile, "extractfile", reject_extractfile)
    extract_archive(archive, tmp_path / "sdk")
    assert (tmp_path / "sdk/second.dll").read_bytes() == b"content"


def test_linux_profile_is_a_separate_unmeasured_build(tmp_path, monkeypatch):
    monkeypatch.chdir(tmp_path)
    (tmp_path / "app").mkdir()
    project = tmp_path / "app" / ASSETS["net8-console-app"][1]
    project.parent.mkdir(parents=True)
    project.write_text("project")
    calls = []
    monkeypatch.setattr(collect_linux, "validate_collector", lambda: ["dotnet-trace"])
    monkeypatch.setattr(subprocess, "run", lambda command, **kwargs: calls.append(command))
    profile_calls = []

    def profile(collector, build, traces, env, timeout, *, recording_timeout):
        assert collector == ["dotnet-trace"]
        assert timeout == 900 and recording_timeout == 1800
        assert len(calls) == 5
        profile_calls.append(True)
        build()

    monkeypatch.setattr(collect_linux, "collect_linux", profile)
    values = msbuild.measure("net8-console-app", tmp_path / "dotnet", 1, 900, False,
                             linux_trace=True)
    assert len(values) == 1 and profile_calls == [True]
    assert not any(argument.startswith("/bl:") for argument in calls[3])
    assert any(argument.startswith("/bl:") for argument in calls[5])


@pytest.mark.skipif(sys.platform != "linux", reason="Linux PTY and signal lifecycle")
@pytest.mark.parametrize("fail", [False, True])
def test_collect_linux_waits_for_recording_and_finalizes_on_build_failure(tmp_path, fail, monkeypatch):
    fake = tmp_path / "fake_collector.py"
    fake.write_text(
        "import pathlib,signal,sys,time\n"
        "out=pathlib.Path(sys.argv[sys.argv.index('--output')+1])\n"
        "def stop(*_):\n"
        "    out.write_bytes(b'finalized trace')\n"
        "    sys.exit(0)\n"
        "signal.signal(signal.SIGINT,stop)\n"
        "print('Recording trace in progress.',flush=True)\n"
        "while True: time.sleep(0.1)\n")

    def build():
        if fail:
            raise ValueError("build failure")

    popen = subprocess.Popen
    finalization_budgets = []

    def start(*args, **kwargs):
        command = args[0]
        assert command[command.index("--duration") + 1] == "00:00:31:00"
        process = popen(*args, **kwargs)
        wait = process.wait

        def wait_for_finalization(*args, **kwargs):
            finalization_budgets.append(kwargs.get("timeout"))
            return wait(*args, **kwargs)

        process.wait = wait_for_finalization
        return process

    monkeypatch.setattr(collect_linux.subprocess, "Popen", start)
    if fail:
        with pytest.raises(ValueError, match="build failure"):
            collect_linux.collect_linux([sys.executable, str(fake)], build, tmp_path,
                                        os.environ.copy(), 900, recording_timeout=1800)
    else:
        collect_linux.collect_linux([sys.executable, str(fake)], build, tmp_path,
                                    os.environ.copy(), 900, recording_timeout=1800)
    assert (tmp_path / "diagnostic.nettrace").read_bytes() == b"finalized trace"
    assert finalization_budgets == [900]


def test_collect_linux_refuses_unsupported_kernel_before_build(monkeypatch):
    monkeypatch.setattr(collect_linux.platform, "system", lambda: "Linux")
    monkeypatch.setattr(collect_linux.platform, "release", lambda: "5.15.0")
    with pytest.raises(RuntimeError, match="kernel 6.4"):
        collect_linux.validate_collector()


@pytest.mark.skipif(sys.platform != "linux", reason="Linux PTY and signal lifecycle")
def test_collect_linux_startup_failure_never_runs_diagnostic_build(tmp_path):
    fake = tmp_path / "failed_collector.py"
    fake.write_text("import sys\nprint('kernel permission denied', flush=True)\nsys.exit(1)\n")
    builds = []
    with pytest.raises(RuntimeError, match="before recording"):
        collect_linux.collect_linux([sys.executable, str(fake)], lambda: builds.append(True),
                                    tmp_path, os.environ.copy(), 10, recording_timeout=20)
    assert builds == []
    assert "kernel permission denied" in (tmp_path / "collect-linux.log").read_text()


@pytest.mark.parametrize("selection", ["override", "payload", "path"])
def test_collector_selection_prefers_override_then_pinned_payload(tmp_path, monkeypatch, selection):
    tools = {}
    for name in ("override", "payload", "path"):
        tool = tmp_path / name / "dotnet-trace" / "dotnet-trace"
        tool.parent.mkdir(parents=True)
        tool.write_bytes(b"collector")
        tools[name] = tool
    monkeypatch.setattr(collect_linux.platform, "system", lambda: "Linux")
    monkeypatch.setattr(collect_linux.platform, "release", lambda: "6.4.0")
    monkeypatch.setattr(os, "geteuid", lambda: 0, raising=False)
    monkeypatch.delenv("PERFLAB_DOTNET_TRACE", raising=False)
    monkeypatch.delenv("HELIX_CORRELATION_PAYLOAD", raising=False)
    if selection == "override":
        monkeypatch.setenv("PERFLAB_DOTNET_TRACE", str(tools["override"]))
    if selection != "path":
        monkeypatch.setenv("HELIX_CORRELATION_PAYLOAD", str(tmp_path / "payload"))
    monkeypatch.setattr(collect_linux.shutil, "which", lambda _: str(tools["path"]))
    calls = []
    monkeypatch.setattr(subprocess, "run", lambda command, **kwargs: calls.append(command))
    assert collect_linux.validate_collector() == [str(tools[selection])]
    assert calls == [[str(tools[selection]), "collect-linux", "--help"]]


@pytest.mark.parametrize("location", ["local", "payload", "override"])
def test_evaluation_hook_resolves_independently_of_working_directory(tmp_path, monkeypatch, location):
    monkeypatch.chdir(tmp_path)
    project = tmp_path / "app" / ASSET_DEFINITIONS["net8-console-app"]["project"]
    project.parent.mkdir(parents=True)
    project.write_text("project")
    artifacts = tmp_path / "repo-artifacts"
    payload = tmp_path / "payload"
    hook = (
        tmp_path / "override.dll" if location == "override" else
        (payload if location == "payload" else artifacts) / "startup/PerfStar.EvaluationMetrics.StartupHook.dll"
    )
    hook.parent.mkdir(parents=True, exist_ok=True)
    hook.write_bytes(b"hook")
    monkeypatch.delenv("PERFLAB_EVALUATION_HOOK", raising=False)
    monkeypatch.delenv("HELIX_CORRELATION_PAYLOAD", raising=False)
    if location != "local":
        monkeypatch.setenv("HELIX_CORRELATION_PAYLOAD", str(payload))
    if location == "override":
        monkeypatch.setenv("PERFLAB_EVALUATION_HOOK", str(hook))
    monkeypatch.setattr("shared.msbuild_execution.get_artifacts_directory", lambda: str(artifacts))
    calls = []
    monkeypatch.setattr(subprocess, "run", lambda command, **kwargs: calls.append(kwargs["env"]))
    execute_scenario(
        {"asset": "net8-console-app", "kind": "rebuild", "mt": False},
        tmp_path / "dotnet", 1, 900, evaluation_collection="current")
    measured = [env for env in calls if "PERFSTAR_EVALUATION_METRICS_SESSION" in env]
    assert measured and all(env["DOTNET_STARTUP_HOOKS"].split(os.pathsep)[-1] == str(hook) for env in measured)


@pytest.mark.parametrize("kind", ["warm-inc-build", "warm-restore", "project-query-evaluation"])
def test_diagnostic_budget_covers_priming_operations_and_shutdowns(tmp_path, monkeypatch, kind):
    monkeypatch.chdir(tmp_path)
    asset = "orchard-core" if kind == "project-query-evaluation" else "net8-console-app"
    project_path = (
        "OrchardCore/src/OrchardCore.Modules/OrchardCore.Admin/OrchardCore.Admin.csproj"
        if kind == "project-query-evaluation" else ASSET_DEFINITIONS[asset]["project"]
    )
    project = tmp_path / "app" / project_path
    project.parent.mkdir(parents=True)
    project.write_text("project")
    for relative in ASSET_DEFINITIONS[asset]["edits"]:
        edit = tmp_path / "app" / relative
        edit.parent.mkdir(parents=True, exist_ok=True)
        edit.write_text("source")
    hook = tmp_path / "hook.dll"
    hook.write_bytes(b"hook")
    monkeypatch.setenv("PERFLAB_EVALUATION_HOOK", str(hook))
    monkeypatch.setattr(collect_linux, "validate_collector", lambda: ["collector"])
    calls = []
    monkeypatch.setattr(subprocess, "run", lambda command, **kwargs: calls.append(command))
    budget = 3600 if kind == "project-query-evaluation" else 2700

    def profile(collector, build, traces, env, timeout, *, recording_timeout):
        assert recording_timeout == budget
        assert timeout == 900
        count = len(calls)
        build()
        assert (len(calls) - count) * timeout == recording_timeout

    monkeypatch.setattr(collect_linux, "collect_linux", profile)
    execute_scenario(
        {"asset": asset, "kind": kind, "mt": True}, tmp_path / "dotnet", 1, 900,
        linux_trace=True, evaluation_collection="current")


def test_linux_retries_and_diagnostic_priming_preserve_original_logs(tmp_path, monkeypatch):
    monkeypatch.chdir(tmp_path)
    project = tmp_path / "app" / ASSET_DEFINITIONS["net8-console-app"]["project"]
    project.parent.mkdir(parents=True)
    project.write_text("project")
    (project.parent / "Program.cs").write_text("source")
    monkeypatch.setattr(sys, "platform", "linux")

    def run(command, **kwargs):
        log = kwargs["stdout"]
        if Path(log.name).name == "measured-0.log":
            log.write("original failure evidence")
            raise subprocess.CalledProcessError(1, command)
        log.write("successful " + Path(log.name).name)

    monkeypatch.setattr(subprocess, "run", run)
    values = execute_scenario(
        {"asset": "net8-console-app", "kind": "warm-inc-build", "mt": True},
        tmp_path / "dotnet", 1, 900, diagnostics=True)
    traces = tmp_path / "traces"
    assert len(values) == 1
    assert (traces / "measured-0.log").read_text() == "original failure evidence"
    for name in ("measured-0-attempt-1", "prime-0", "prime-0-attempt-1", "diagnostic-prime-0"):
        assert (traces / f"{name}.log").read_text() == f"successful {name}.log"
    assert json.loads((traces / "build-wall-clock.json").read_text())["recollected_attempts"] == 1


@pytest.mark.parametrize("kind", ["warm-inc-build", "warm-restore", "cold-restore", "restore", "rebuild-nr-false"])
def test_operation_lifecycles_preserve_edits_restore_outputs_and_node_reuse(tmp_path, monkeypatch, kind):
    monkeypatch.chdir(tmp_path)
    asset = ASSET_DEFINITIONS["net8-console-app"]
    project = tmp_path / "app" / asset["project"]
    project.parent.mkdir(parents=True)
    project.write_text("project")
    edit = tmp_path / "app" / asset["edits"][0]
    edit.write_text("initial")
    obj = project.parent / "obj"
    obj.mkdir()
    (obj / "project.assets.json").write_text("restore-output")
    calls = []

    def run(command, **kwargs):
        calls.append((list(command), edit.read_text(), obj.exists()))

    monkeypatch.setattr(subprocess, "run", run)
    hook = tmp_path / "hook.dll"
    hook.write_bytes(b"test-hook")
    monkeypatch.setenv("PERFLAB_EVALUATION_HOOK", str(hook))
    definition = {"asset": "net8-console-app", "kind": kind, "mt": True}
    execute_scenario(definition, tmp_path / "dotnet", 1, 900, evaluation_collection="current")
    measured = [call for call in calls if call[0][1] == "build" and call != calls[0]]
    if kind == "warm-inc-build":
        assert len(measured) == 2
        assert measured[0][1].count("// Hello world") == 1
        assert measured[1][1].count("// Hello world") == 2
        assert calls[-1][0][1:] == ["build-server", "shutdown"]
    elif kind == "warm-restore":
        assert len(measured) == 2 and all(call[2] for call in calls)
        assert all("/t:restore" in call[0] for call in measured)
    elif kind in ("restore", "cold-restore"):
        assert obj.exists() is False
        assert "/t:restore" in measured[-1][0]
        assert len(measured) == 1
    else:
        assert "/nr:false" in measured[-1][0] and "/t:rebuild" in measured[-1][0]
    data = json.loads((tmp_path / "traces/build-wall-clock.json").read_text())
    assert len(data["samples"]) == 1
    request = data["sessions"][0]
    if kind in ("warm-inc-build", "warm-restore"):
        assert request["priming_window"]["start"] < request["priming_window"]["end"]
        assert request["priming_window"]["end"] <= request["window"]["start"] < request["window"]["end"]
    else:
        assert "priming_window" not in request and "window" not in request


def test_publish_evaluation_uses_only_sorted_modules_and_themes(tmp_path):
    orchard = tmp_path / "OrchardCore"
    for path in ("src/OrchardCore.Modules/B/B.csproj", "src/OrchardCore.Themes/A/A.csproj",
                 "src/OrchardCore.Cms.Web/Web.csproj"):
        project = orchard / path
        project.parent.mkdir(parents=True)
        project.write_text("<Project />")
    props = orchard / "src/OrchardCore.Build/TargetFrameworks.props"
    props.parent.mkdir(parents=True)
    props.write_text(
        "<Project><PropertyGroup><DefaultTargetFramework>net9.0</DefaultTargetFramework></PropertyGroup></Project>")
    path, framework = publish_selection(tmp_path)
    assert framework == "net9.0"
    text = path.read_text()
    assert "B/B.csproj" in text and "A/A.csproj" in text
    assert "Cms.Web" not in text


def test_queries_alternate_order_without_mixing_variant_samples(tmp_path, monkeypatch):
    monkeypatch.chdir(tmp_path)
    project = tmp_path / "app/OrchardCore/src/OrchardCore.Modules/OrchardCore.Admin/OrchardCore.Admin.csproj"
    project.parent.mkdir(parents=True)
    project.write_text("<Project />")
    hook = tmp_path / "hook.dll"
    hook.write_bytes(b"hook")
    monkeypatch.setenv("PERFLAB_EVALUATION_HOOK", str(hook))
    calls = []
    monkeypatch.setattr(subprocess, "run", lambda command, **kwargs: calls.append((command, kwargs["env"])))
    definition = SCENARIOS["orchard-core-project-query-evaluation-mt-dotnet"]
    execute_scenario(definition, tmp_path / "dotnet", 2, 900, evaluation_collection="current")
    queries = [command for command, _ in calls if command[1] == "msbuild"]
    assert [command[-1] for command in queries] == [
        "-getProperty:TargetFramework", "-getItem:ProjectReference",
        "-getProperty:TargetFramework", "-getItem:ProjectReference",
        "-getItem:ProjectReference", "-getProperty:TargetFramework",
    ]
    measured_envs = [
        env for command, env in calls
        if command[1] == "msbuild" and "PERFSTAR_EVALUATION_METRICS_SESSION" in env
    ]
    assert len({env["PERFSTAR_EVALUATION_METRICS_SESSION"] for env in measured_envs}) == 4
    assert all(env["MSBUILDUSESERVER"] == "1" for env in measured_envs)
    assert all(env["PERFSTAR_EVALUATION_METRICS_DIR"] ==
               str(tmp_path / "traces/.perfstar-evaluation-metrics") for env in measured_envs)
    assert not (tmp_path / "app/.perfstar-evaluation-metrics").exists()
    data = json.loads((tmp_path / "traces/build-wall-clock.json").read_text())
    assert len(data["sessions"]) == 4
    assert [request["variant"] for request in data["sessions"]] == [
        "get-property", "get-item", "get-item", "get-property"]
    assert all(len(counter["samples"]) == 2 for counter in data["counters"])


def test_selection_rejects_disabled_unknown_and_duplicate_scenarios():
    assert len(select_scenarios()) == 45
    for selection in ("orchard-core-inc-build-dotnet", "missing", "xunit-rebuild-dotnet,xunit-rebuild-dotnet"):
        with pytest.raises(ValueError):
            select_scenarios(selection)
    selected = select_scenarios("xunit-rebuild-dotnet;aspire-warm-inc-build-mt-dotnet", "linux")
    assert [item["iterations"] for item in selected] == [1, 1]


def test_generated_helix_catalog_exactly_matches_runtime_scenarios():
    repo = Path(__file__).resolve().parents[2]
    project = ET.parse(repo / "eng/performance/MSBuildScenarios.Generated.props")
    names = [entry.attrib["Include"] for entry in project.findall(".//MSBuildScenario")]
    assert len(names) == len(set(names))
    assert set(names) == set(SCENARIOS)


def test_sdk_timestamp_normalization_changes_only_future_dates(tmp_path, monkeypatch):
    cutoff = 2_000_000_000_000_000_000
    monkeypatch.setattr(time, "time_ns", lambda: cutoff)
    future = tmp_path / "future.dll"
    past = tmp_path / "past.dll"
    future.write_bytes(b"future bytes")
    past.write_bytes(b"past bytes")
    os.utime(future, ns=(cutoff, cutoff + 10_000_000_000))
    os.utime(past, ns=(cutoff, cutoff - 10_000_000_000))
    assert msbuild.normalize_future_sdk_timestamps(tmp_path) == 1
    assert future.stat().st_mtime_ns <= cutoff
    assert future.stat().st_mtime_ns >= cutoff - 100
    assert past.stat().st_mtime_ns == cutoff - 10_000_000_000
    assert future.read_bytes() == b"future bytes" and past.read_bytes() == b"past bytes"


def test_selected_input_assets_must_be_present(composition, tmp_path):
    item = composition
    compose(item.sdk, sha512(item.sdk), item.packages, item.output, item.source,
            {"net8-console-app": item.assets["net8-console-app"]})
    with pytest.raises(ValueError, match="missing assets: aspire"):
        configure_inputs(item.output, tmp_path / "payload", SimpleNamespace(),
                         required_assets={"aspire"})


def test_catalog_import_preserves_enabled_routes_and_is_line_ending_independent(tmp_path):
    root = tmp_path / "tests"
    root.mkdir()
    scenario = {
        "name": "net8-console-app-rebuild-dotnet", "asset": "net8-console-app",
        "scenario_kind": "rebuild", "build_app": "dotnet", "mt": False,
        "configured_iterations": 10, "supported_os": ["windows", "linux"],
    }
    inventory = json.dumps({
        "schema_version": 2, "scenarios": [scenario, {"name": "disabled", "enabled": False}],
    }, indent=2)
    path = root / "ScenarioInventory.json"
    path.write_bytes(inventory.encode())
    (root / "ScenarioMetadata.json").write_text(json.dumps({
        "assets": [{"name": "net8-console-app", "version": "0.0.1"}]}))
    expected = generate(tmp_path)
    path.write_bytes(inventory.replace("\n", "\r\n").encode())
    assert generate(tmp_path) == expected
    imported = json.loads(expected[0])["scenarios"]
    assert len(imported) == 1 and imported[0]["iterations"] == 10
    scenario["scenario_kind"] = "unimplemented-new-operation"
    path.write_text(json.dumps({"schema_version": 2, "scenarios": [scenario]}))
    with pytest.raises(ValueError, match="Unimplemented enabled"):
        generate(tmp_path)
