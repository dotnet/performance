import os
import sys
from argparse import Namespace
from pathlib import Path

import pytest

scripts_dir = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(scripts_dir))

import micro_benchmarks
from run_performance_job import get_run_configurations, get_work_item_command


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
