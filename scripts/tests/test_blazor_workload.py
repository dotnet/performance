import importlib
import json
import os
from pathlib import Path
import runpy
import shlex
import shutil
import subprocess
import sys
from unittest.mock import Mock
from types import SimpleNamespace
import xml.etree.ElementTree as ET

import pytest


repo_root = Path(__file__).resolve().parents[2]


@pytest.fixture
def scenario_commands(monkeypatch):
    monkeypatch.syspath_prepend(str(repo_root / "scripts"))
    monkeypatch.syspath_prepend(str(repo_root / "src" / "scenarios"))
    return (
        importlib.import_module("shared.precommands").PreCommands,
        importlib.import_module("shared.postcommands").PostCommands,
    )


@pytest.fixture(params=["true", "false"], ids=["windows", "unix"])
def blazor_items(request, tmp_path):
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        pytest.skip("MSBuild evaluation requires a .NET SDK")

    project = ET.parse(repo_root / "eng" / "performance" / "blazor_scenarios.proj")
    # Evaluate the real work items without downloading the Helix SDK.
    project.getroot().attrib.pop("Sdk")
    project.find("Import").set(
        "Project", str(repo_root / "eng" / "performance" / "PreparePayloadWorkItems.targets")
    )
    project_path = tmp_path / "blazor.proj"
    project.write(project_path, encoding="utf-8")
    payload = tmp_path / "correlation payload"
    result = subprocess.run(
        [
            dotnet, "msbuild", str(project_path), "-nologo",
            "-getItem:PreparePayloadWorkItem,HelixWorkItem",
            f"-p:CorrelationPayloadDirectory={payload}{os.sep}",
            f"-p:TargetsWindows={request.param}",
            "-p:Python=python",
        ],
        cwd=tmp_path,
        check=True,
        capture_output=True,
        text=True,
    )
    return json.loads(result.stdout)["Items"], payload, request.param == "true"


def test_workload_is_prepared_once_in_payload(blazor_items):
    items, payload, windows = blazor_items
    preparation, = items["PreparePayloadWorkItem"]
    executable = payload / "dotnet" / ("dotnet.exe" if windows else "dotnet")
    config = payload / "performance" / "NuGet.config"

    assert preparation["Command"] == (
        f'"{executable}" workload install wasm-tools --skip-manifest-update '
        f'--configfile "{config}"'
    )
    assert Path(preparation["WorkingDirectory"]) == payload / "performance"


@pytest.mark.parametrize("exit_code", [0, 23])
def test_preparation_propagates_workload_exit_code(blazor_items, tmp_path, exit_code):
    _, payload, windows = blazor_items
    working_directory = payload / "performance"
    working_directory.mkdir(parents=True)
    # Substitute only the executable; run the real preparation target and arguments.
    (working_directory / "workload").write_text(
        "import sys\n"
        "assert sys.argv[1:4] == ['install', 'wasm-tools', '--skip-manifest-update']\n"
        "print('Workload preparation executed')\n"
        f"sys.exit({exit_code})\n",
        encoding="utf-8",
    )
    result = subprocess.run(
        [
            shutil.which("dotnet"), "msbuild", str(tmp_path / "blazor.proj"), "-nologo",
            "-t:PreparePayloadWorkItems",
            f"-p:CorrelationPayloadDirectory={payload}{os.sep}",
            f"-p:TargetsWindows={str(windows).lower()}",
            f"-p:_BlazorDotNet={sys.executable}",
        ],
        cwd=tmp_path,
        capture_output=True,
        text=True,
    )

    assert "Workload preparation executed" in result.stdout
    if exit_code:
        assert result.returncode != 0
        assert "MSB3073" in result.stdout
    else:
        assert result.returncode == 0, result.stdout + result.stderr


def test_all_work_items_leave_shared_workload_untouched(
    blazor_items, scenario_commands, monkeypatch
):
    items, _, _ = blazor_items
    PreCommands, PostCommands = scenario_commands
    run = Mock(side_effect=AssertionError("A work item must not modify the workload"))
    monkeypatch.setattr(subprocess, "run", run)

    assert len(items["HelixWorkItem"]) == 8
    for item in items["HelixWorkItem"]:
        command = item["Command"].split(" && ", 1)[0]
        monkeypatch.setattr(sys, "argv", shlex.split(command)[1:])
        pre = PreCommands()
        assert pre.has_workload
        assert pre.readonly_dotnet
        pre.install_workload("wasm-tools")
        pre.uninstall_workload("wasm-tools")

        monkeypatch.setattr(sys, "argv", shlex.split(item["PostCommands"])[1:])
        post = PostCommands()
        assert post.readonly_dotnet
        post.uninstall_workload("wasm-tools")

    run.assert_not_called()


@pytest.mark.parametrize(
    ("name", "property_argument"),
    [
        ("SOD - Localized App - Publish", "/p:WasmNativeWorkload=false"),
        ("SOD - Localized App - Publish - AOT", "/p:RunAOTCompilation=true"),
    ],
)
def test_localized_publish_mode_is_forwarded(
    blazor_items, scenario_commands, monkeypatch, name, property_argument
):
    items, _, _ = blazor_items
    PreCommands, _ = scenario_commands
    item = next(item for item in items["HelixWorkItem"] if item["Identity"] == name)
    command = item["Command"].split(" && ", 1)[0]
    monkeypatch.setattr(sys, "argv", shlex.split(command)[1:])
    pre = PreCommands()
    pre.project = Mock()
    pre.execute()

    assert pre.project.publish.call_args.args[6] == [
        "/p:_TrimmerDumpDependencies=true",
        "/warnaserror:NU1602,NU1604",
        property_argument,
    ]


def test_standalone_workload_install_is_unchanged(scenario_commands, monkeypatch):
    PreCommands, _ = scenario_commands
    monkeypatch.setattr(sys, "argv", ["pre.py", "publish"])
    run = Mock()
    monkeypatch.setattr(subprocess, "run", run)

    PreCommands().install_workload("wasm-tools")

    run.assert_called_once_with(
        ["dotnet", "workload", "install", "wasm-tools", "--skip-manifest-update"],
        check=True,
    )


def test_readonly_sdk_requires_preinstalled_workload(scenario_commands, monkeypatch):
    PreCommands, _ = scenario_commands
    monkeypatch.setattr(sys, "argv", ["pre.py", "publish", "--readonly-dotnet"])
    run = Mock()
    monkeypatch.setattr(subprocess, "run", run)

    with pytest.raises(Exception, match="has_workload=false"):
        PreCommands().install_workload("wasm-tools")

    run.assert_not_called()


@pytest.mark.parametrize("framework", ["net10.0", "net11.0", "net12.0"])
def test_linker_dump_uses_supported_targets(scenario_commands, tmp_path, framework):
    get_linker_dump = importlib.import_module("shared.runner").get_linker_dump
    old = tmp_path / "obj" / "Release" / "net5.0" / "browser-wasm" / "linked" / "linker-dependencies.xml.gz"
    old.parent.mkdir(parents=True)
    old.touch()
    assert get_linker_dump(str(tmp_path)) is None
    current = tmp_path / "obj" / "Release" / framework / "browser-wasm" / "linked" / old.name
    current.parent.mkdir(parents=True)
    current.touch()
    assert get_linker_dump(str(tmp_path)) == str(current)


def test_multiple_current_linker_dumps_fail_explicitly(scenario_commands, tmp_path):
    get_linker_dump = importlib.import_module("shared.runner").get_linker_dump
    for framework in ("net10.0", "net12.0"):
        dump = tmp_path / "obj" / "Release" / framework / "browser-wasm" / "linked" / "linker-dependencies.xml.gz"
        dump.parent.mkdir(parents=True)
        dump.touch()
    with pytest.raises(ValueError, match="Multiple current-target linker dumps"):
        get_linker_dump(str(tmp_path))


@pytest.mark.parametrize("scenario", ["blazor", "blazoraot", "blazorminapp", "blazorminappaot", "blazorpizza", "blazorpizzaaot"])
def test_blazor_runner_forwards_current_linker_dump(scenario_commands, monkeypatch, scenario):
    runner = importlib.import_module("shared.runner")
    dump = Mock(return_value="current-linker-dump")
    run = Mock()
    monkeypatch.setattr(runner, "get_linker_dump", dump)
    monkeypatch.setattr(runner, "Runner", run)
    runpy.run_path(str(repo_root / "src" / "scenarios" / scenario / "test.py"), run_name="__main__")
    dump.assert_called_once()
    assert run.call_args.args[0].artifact == "current-linker-dump"
    run.return_value.run.assert_called_once()


@pytest.mark.parametrize("scenario", ["blazor", "blazorpizza"])
def test_current_blazor_precommand_uses_current_workload(scenario_commands, monkeypatch, scenario):
    commands = importlib.import_module("shared.precommands")
    pre = Mock()
    pre.framework = "net12.0"
    monkeypatch.setattr(commands, "PreCommands", Mock(return_value=pre))
    monkeypatch.setitem(sys.modules, "test", SimpleNamespace(EXENAME="test"))
    monkeypatch.setattr("performance.logger.setup_loggers", Mock())
    runpy.run_path(str(repo_root / "src" / "scenarios" / scenario / "pre.py"))
    pre.uninstall_workload.assert_called_once_with("wasm-tools")
    pre.execute.assert_called_once_with(["-p:WasmNativeWorkload=false"])
