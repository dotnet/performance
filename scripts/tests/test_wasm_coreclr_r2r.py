import os
import sys
from argparse import Namespace
from pathlib import Path

import pytest

scripts_dir = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(scripts_dir))

import micro_benchmarks
from build_runtime_payload import build_wasm_coreclr_payload
from run_performance_job import get_pre_commands, get_run_configurations, get_work_item_command


def test_ready_to_run_requires_coreclr_wasm():
    with pytest.raises(SystemExit):
        micro_benchmarks.__process_arguments([
            "--frameworks", "net11.0",
            "--wasm-ready-to-run",
        ])


def test_ready_to_run_configures_msbuild_environment(monkeypatch):
    monkeypatch.delenv("PERFLAB_WASM_READY_TO_RUN", raising=False)
    args = Namespace(
        wasm=True,
        wasm_runtime_flavor="CoreCLR",
        wasm_ready_to_run=True,
    )

    micro_benchmarks.configure_wasm_ready_to_run(args)

    assert os.environ["PERFLAB_WASM_READY_TO_RUN"] == "true"


def test_ready_to_run_argument_is_forwarded_to_helix_work_item():
    command = get_work_item_command(
        os_group="linux",
        target_csproj="src/benchmarks/micro/MicroBenchmarks.csproj",
        architecture="x64",
        perf_lab_framework="net11.0",
        internal=True,
        wasm=True,
        bdn_artifacts_dir="/tmp/artifacts",
        wasm_coreclr=True,
        wasm_ready_to_run=True,
    )

    assert "--wasm-runtime-flavor" in command
    assert "--wasm-ready-to-run" in command


def test_ready_to_run_has_distinct_result_configuration():
    configurations = get_run_configurations(
        run_kind="micro",
        runtime_type="wasm_coreclr",
        codegen_type="wasm",
        r2r_run_type="r2r",
        runtime_flavor="coreclr",
        javascript_engine="v8",
    )

    assert configurations["CompilationMode"] == "wasm"
    assert configurations["RuntimeType"] == "coreclr"
    assert configurations["R2RType"] == "r2r"


def test_coreclr_payload_detects_local_toolchain_package_version(tmp_path):
    artifact = tmp_path / "artifact" / "staging"
    ref_pack = artifact / "dotnet-none" / "packs" / "Microsoft.NETCore.App.Ref" / "11.0.0-rc.1.26431.109"
    runtime_pack = artifact / "microsoft.netcore.app.runtime.browser-wasm" / "Release"
    built_nugets = artifact / "built-nugets"
    ref_pack.mkdir(parents=True)
    runtime_pack.mkdir(parents=True)
    built_nugets.mkdir(parents=True)
    (built_nugets / "Microsoft.NET.Sdk.WebAssembly.Pack.11.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NETCore.App.Crossgen2.linux-x64.11.0.0-ci.nupkg").touch()

    version = build_wasm_coreclr_payload(str(artifact.parent), str(tmp_path / "payload"))

    assert version == "11.0.0-ci"


def test_coreclr_payload_rejects_mismatched_toolchain_package_versions(tmp_path):
    artifact = tmp_path / "artifact" / "staging"
    (artifact / "dotnet-none").mkdir(parents=True)
    built_nugets = artifact / "built-nugets"
    built_nugets.mkdir(parents=True)
    (built_nugets / "Microsoft.NET.Sdk.WebAssembly.Pack.11.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NETCore.App.Crossgen2.linux-x64.11.0.1-ci.nupkg").touch()

    with pytest.raises(ValueError, match="versions do not match"):
        build_wasm_coreclr_payload(str(artifact.parent), str(tmp_path / "payload"))


def test_coreclr_pre_commands_export_local_toolchain_package_version():
    commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=False,
        runtime_type="wasm_coreclr",
        codegen_type="wasm",
        build_config="Release",
        v8_version="15.1.206",
        wasm_local_package_version="11.0.0-ci",
    )

    assert any("PERFLAB_WASM_PACKAGE_VERSION=11.0.0-ci" in command for command in commands)
