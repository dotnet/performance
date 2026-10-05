import json
import os
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
from argparse import Namespace
from pathlib import Path
from typing import Optional

import pytest

scripts_dir = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(scripts_dir))

import micro_benchmarks
import dotnet
from build_runtime_payload import (
    WASM_CROSSGEN2_TASKS_FILES,
    WASM_CROSSGEN2_TASKS_PAYLOAD_DIR,
    build_wasm_coreclr_payload,
)
from run_performance_job import (
    get_pre_commands,
    get_run_configurations,
    get_work_item_command,
    normalize_wasm_workload_source,
    set_shell_environment_variable,
)


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


@pytest.mark.parametrize(
    "extra_args",
    [
        [],
        ["--wasm"],
        ["--wasm", "--wasm-runtime-flavor", "Mono"],
    ],
)
def test_composite_ready_to_run_requires_coreclr_wasm(extra_args):
    with pytest.raises(SystemExit):
        micro_benchmarks.__process_arguments([
            "--frameworks", "net11.0",
            "--wasm-ready-to-run-composite",
            *extra_args,
        ])


@pytest.mark.parametrize(
    ("flags", "expected_ready_to_run", "expected_composite"),
    [
        ([], False, False),
        (["--wasm-ready-to-run"], True, False),
        (["--wasm-ready-to-run-composite"], True, True),
        (["--wasm-ready-to-run", "--wasm-ready-to-run-composite"], True, True),
    ],
)
def test_ready_to_run_mode_parsing(flags, expected_ready_to_run, expected_composite):
    args = micro_benchmarks.__process_arguments([
        "--frameworks", "net11.0",
        "--wasm",
        "--wasm-runtime-flavor", "CoreCLR",
        *flags,
    ])

    assert micro_benchmarks.is_wasm_ready_to_run(args) == expected_ready_to_run
    assert micro_benchmarks.is_wasm_ready_to_run_composite(args) == expected_composite


@pytest.mark.parametrize(
    ("ready_to_run", "composite", "expected_ready_to_run", "expected_composite"),
    [
        (False, False, "false", "false"),
        (True, False, "true", "false"),
        (False, True, "true", "true"),
    ],
)
def test_ready_to_run_mode_configures_msbuild_environment(
        monkeypatch, ready_to_run, composite, expected_ready_to_run, expected_composite):
    # A stale parent value must not leak into a different mode.
    monkeypatch.setenv("PERFLAB_WASM_READY_TO_RUN", "true")
    monkeypatch.setenv("PERFLAB_WASM_READY_TO_RUN_COMPOSITE", "true")
    args = Namespace(
        wasm=True,
        wasm_runtime_flavor="CoreCLR",
        wasm_ready_to_run=ready_to_run,
        wasm_ready_to_run_composite=composite,
    )

    micro_benchmarks.configure_wasm_ready_to_run(args)

    assert os.environ["PERFLAB_WASM_READY_TO_RUN"] == expected_ready_to_run
    assert os.environ["PERFLAB_WASM_READY_TO_RUN_COMPOSITE"] == expected_composite


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
    assert "--wasm-ready-to-run-composite" not in command


def test_composite_ready_to_run_argument_is_forwarded_to_helix_work_item():
    command = get_work_item_command(
        os_group="linux",
        target_csproj="src/benchmarks/micro/MicroBenchmarks.csproj",
        architecture="x64",
        perf_lab_framework="net11.0",
        internal=True,
        wasm=True,
        bdn_artifacts_dir="/tmp/artifacts",
        wasm_coreclr=True,
        wasm_workload_source="https://example.test/cohort/v3/index.json",
        wasm_ready_to_run_composite=True,
    )

    assert "--wasm-runtime-flavor" in command
    assert "--wasm-ready-to-run-composite" in command
    assert "--wasm-ready-to-run" not in command
    source_index = command.index("--wasm-workload-source")
    assert command[source_index + 1] == "https://example.test/cohort/v3/index.json"


def test_composite_ready_to_run_is_ignored_without_coreclr_wasm():
    command = get_work_item_command(
        os_group="linux",
        target_csproj="src/benchmarks/micro/MicroBenchmarks.csproj",
        architecture="x64",
        perf_lab_framework="net11.0",
        internal=True,
        wasm=True,
        bdn_artifacts_dir="/tmp/artifacts",
        wasm_coreclr=False,
        wasm_ready_to_run_composite=True,
    )

    assert "--wasm-ready-to-run-composite" not in command


def test_workload_source_is_forwarded_to_helix_work_item():
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
        wasm_workload_source="https://example.test/cohort/v3/index.json",
    )

    source_index = command.index("--wasm-workload-source")
    assert command[source_index + 1] == "https://example.test/cohort/v3/index.json"


def test_windows_wasm_work_item_uses_windows_payload_paths():
    command = get_work_item_command(
        os_group="windows",
        target_csproj="src/benchmarks/micro/MicroBenchmarks.csproj",
        architecture="x64",
        perf_lab_framework="net11.0",
        internal=True,
        wasm=True,
        bdn_artifacts_dir="%HELIX_WORKITEM_UPLOAD_ROOT%\\artifacts",
        wasm_coreclr=True,
        wasm_ready_to_run=True,
        wasm_workload_source=(
            "%HELIX_CORRELATION_PAYLOAD%\\wasm-workload-source"),
    )

    dotnet_path_index = command.index("--dotnet-path")
    workload_source_index = command.index("--wasm-workload-source")
    assert command[dotnet_path_index + 1] == (
        "%HELIX_CORRELATION_PAYLOAD%\\dotnet\\")
    assert command[workload_source_index + 1] == (
        "%HELIX_CORRELATION_PAYLOAD%\\wasm-workload-source")
    assert all("$HELIX_CORRELATION_PAYLOAD" not in argument for argument in command)


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


def test_composite_ready_to_run_has_distinct_result_configuration():
    configurations = get_run_configurations(
        run_kind="micro",
        runtime_type="wasm_coreclr",
        codegen_type="wasm",
        r2r_run_type="r2r_composite",
        runtime_flavor="coreclr",
        javascript_engine="v8",
    )

    assert configurations["CompilationMode"] == "wasm"
    assert configurations["RuntimeType"] == "coreclr"
    assert configurations["R2RType"] == "r2r_composite"


def _wasm_targets():
    targets_path = scripts_dir.parent / "src" / "benchmarks" / "micro" / "MicroBenchmarks.Wasm.targets"
    return ET.parse(targets_path).getroot()


def test_ready_to_run_properties_select_composite_from_environment():
    root = _wasm_targets()
    mode = [
        element for group in root.findall("PropertyGroup")
        for element in group.findall("_PerformanceWasmReadyToRunComposite")
    ]
    assert [(e.text, e.attrib.get("Condition")) for e in mode] == [
        ("false", None),
        ("true", "'$(PERFLAB_WASM_READY_TO_RUN_COMPOSITE)' == 'true'"),
    ]

    r2r_group = next(
        group for group in root.findall("PropertyGroup")
        if group.find("PublishReadyToRun") is not None
    )
    assert "'$(PERFLAB_WASM_READY_TO_RUN)' == 'true'" in r2r_group.attrib["Condition"]
    properties = {element.tag: element.text for element in r2r_group}
    assert properties == {
        "PublishReadyToRun": "true",
        "PublishReadyToRunComposite": "$(_PerformanceWasmReadyToRunComposite)",
        "PublishReadyToRunContainerFormat": "wasm",
        "PublishTrimmed": "true",
        "WasmEnableWebcil": "true",
    }


def test_ready_to_run_configuration_validates_selected_composite_mode():
    target = _wasm_targets().find("./Target[@Name='ValidateWasmReadyToRunConfiguration']")
    assert target is not None
    assert "PERFLAB_WASM_READY_TO_RUN_COMPOSITE" in target.attrib["Condition"]

    conditions = [error.attrib["Condition"] for error in target.findall("Error")]
    assert "'$(PERFLAB_WASM_READY_TO_RUN)' != 'true'" in conditions
    assert any(
        "'$(PublishReadyToRunComposite)' != '$(_PerformanceWasmReadyToRunComposite)'" in condition
        for condition in conditions
    )


def test_ready_to_run_output_guard_checks_composite_image():
    target = _wasm_targets().find("./Target[@Name='ValidateWasmReadyToRunOutputs']")
    assert target is not None
    assert target.attrib["AfterTargets"] == "_CreateR2RImages"

    include = target.find("./ItemGroup/_PerformanceWasmCompositeCompilation").attrib["Include"]
    assert "WithMetadataValue('CreateCompositeImage', 'true')" in include
    image = target.find("./ItemGroup/_PerformanceWasmCompositeImage").attrib["Include"]
    assert "%(OutputR2RImage)" in image

    conditions = " ".join(error.attrib["Condition"] for error in target.findall("Error"))
    assert "'@(_ReadyToRunFilesToPublish)' == ''" in conditions
    assert "EndsWith('.r2r.wasm')" in conditions
    assert "!Exists('$(_PerformanceWasmCompositeImagePath)')" in conditions
    assert "@(_ReadyToRunCompositeBuildInput)" in conditions
    # Per-assembly mode rejects an unexpected composite plan.
    assert "'$(_PerformanceWasmReadyToRunComposite)' != 'true' and '@(_PerformanceWasmCompositeImage)' != ''" in conditions


def test_composite_publish_guard_checks_boot_config_core_assembly_asset():
    target = _wasm_targets().find("./Target[@Name='ValidateWasmReadyToRunCompositePublishAssets']")
    assert target is not None
    assert target.attrib["AfterTargets"] == "ProcessPublishFilesForWasm"
    assert "'$(_PerformanceWasmReadyToRunComposite)' == 'true'" in target.attrib["Condition"]

    include = target.find("./ItemGroup/_PerformanceWasmCompositePublishAsset").attrib["Include"]
    assert include == (
        "@(_WasmCompositePublishStaticWebAsset->"
        "WithMetadataValue('AssetTraitValue', 'readyToRunComposite'))")


def test_ready_to_run_validates_resolved_runtime_pack_items():
    targets_path = scripts_dir.parent / "src" / "benchmarks" / "micro" / "MicroBenchmarks.Wasm.targets"
    target = ET.parse(targets_path).find("./Target[@Name='ValidateWasmReadyToRunConfiguration']")

    assert target is not None
    assert target.attrib["DependsOnTargets"] == "UpdateTargetingAndRuntimePack"

    conditions = [error.attrib["Condition"] for error in target.findall("Error")]
    assert any(
        "_PerformanceWasmResolvedRuntimePack->'%(NuGetPackageId)'" in condition
        for condition in conditions
    )
    assert any(
        "_PerformanceWasmResolvedFrameworkReference->'%(RuntimePackName)'" in condition
        for condition in conditions
    )
    assert all("_PerformanceWasmRuntimePackName" not in condition for condition in conditions)


@pytest.mark.parametrize("ready_to_run", ["false", "true"])
@pytest.mark.parametrize(
    ("use_mono_runtime", "initial_flavor", "expected_flavor"),
    [
        ("false", "", "CoreCLR"),
        ("false", "Mono", "CoreCLR"),
        ("true", "", ""),
        ("true", "Mono", "Mono"),
        ("", "", ""),
        ("", "Mono", "Mono"),
    ],
)
def test_wasm_runtime_flavor_after_generated_project_imports(
        tmp_path, ready_to_run, use_mono_runtime, initial_flavor, expected_flavor):
    dotnet_cli = shutil.which("dotnet")
    if dotnet_cli is None:
        pytest.skip("A .NET SDK is required for MSBuild property evaluation.")

    micro_dir = scripts_dir.parent / "src" / "benchmarks" / "micro"
    project = ET.Element("Project")
    properties = ET.SubElement(project, "PropertyGroup")
    for name, value in {
        "UseMonoRuntime": "true",
        "RuntimeFlavor": initial_flavor,
        "PERFLAB_WASM_READY_TO_RUN": ready_to_run,
        "PublishReadyToRun": "false",
    }.items():
        ET.SubElement(properties, name).text = value

    # BenchmarkDotNet overrides UseMonoRuntime after .props but before .targets.
    ET.SubElement(project, "Import", Project=str(micro_dir / "MicroBenchmarks.Wasm.props"))
    properties = ET.SubElement(project, "PropertyGroup")
    ET.SubElement(properties, "UseMonoRuntime").text = use_mono_runtime
    ET.SubElement(project, "Import", Project=str(micro_dir / "MicroBenchmarks.Wasm.targets"))
    project_path = tmp_path / "GeneratedWasm.proj"
    ET.ElementTree(project).write(project_path, encoding="utf-8")

    result = subprocess.run(
        [
            dotnet_cli, "msbuild", str(project_path), "-nologo",
            "-getProperty:UseMonoRuntime,RuntimeFlavor,PublishReadyToRun",
        ],
        cwd=tmp_path,
        capture_output=True,
        text=True,
        timeout=60,
    )

    assert result.returncode == 0, result.stdout + result.stderr
    evaluated = json.loads(result.stdout)["Properties"]
    assert evaluated["UseMonoRuntime"] == use_mono_runtime
    assert evaluated["RuntimeFlavor"] == expected_flavor
    assert evaluated["PublishReadyToRun"] == (
        "true" if use_mono_runtime == "false" and ready_to_run == "true" else "false"
    )


def test_coreclr_payload_detects_local_toolchain_package_version(tmp_path):
    artifact = tmp_path / "artifact" / "staging"
    ref_pack = artifact / "dotnet-none" / "packs" / "Microsoft.NETCore.App.Ref" / "11.0.0-rc.1.26431.109"
    shared_framework = artifact / "dotnet-none" / "shared" / "Microsoft.NETCore.App" / "11.0.0-rc.1.26431.109"
    runtime_pack = artifact / "microsoft.netcore.app.runtime.browser-wasm" / "Release"
    built_nugets = artifact / "built-nugets"
    ref_pack.mkdir(parents=True)
    shared_framework.mkdir(parents=True)
    runtime_pack.mkdir(parents=True)
    built_nugets.mkdir(parents=True)
    (shared_framework / "System.Private.CoreLib.dll").touch()
    (built_nugets / "Microsoft.NET.Sdk.WebAssembly.Pack.11.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NETCore.App.Crossgen2.linux-x64.11.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NET.ILLink.Tasks.11.0.0-ci.nupkg").touch()

    payload = tmp_path / "payload"
    version = build_wasm_coreclr_payload(str(artifact.parent), str(payload))

    assert version == "11.0.0-ci"
    assert (
        payload
        / "dotnet"
        / "shared"
        / "Microsoft.NETCore.App"
        / "11.0.0-ci"
        / "System.Private.CoreLib.dll"
    ).is_file()


def _write_coreclr_artifact(tmp_path, crossgen2_tasks_files=()):
    artifact = tmp_path / "artifact" / "staging"
    shared_framework = artifact / "dotnet-none" / "shared" / "Microsoft.NETCore.App" / "11.0.0-ci"
    built_nugets = artifact / "built-nugets"
    shared_framework.mkdir(parents=True)
    built_nugets.mkdir(parents=True)
    (built_nugets / "Microsoft.NET.Sdk.WebAssembly.Pack.11.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NETCore.App.Crossgen2.linux-x64.11.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NET.ILLink.Tasks.11.0.0-ci.nupkg").touch()
    if crossgen2_tasks_files:
        crossgen2_tasks = artifact / "Crossgen2Tasks"
        crossgen2_tasks.mkdir()
        for name in crossgen2_tasks_files:
            (crossgen2_tasks / name).write_text(name)
    return artifact.parent


def test_coreclr_payload_stages_crossgen2_tasks_shim(tmp_path):
    artifact = _write_coreclr_artifact(
        tmp_path, (*WASM_CROSSGEN2_TASKS_FILES, "Crossgen2Tasks.deps.json"))
    payload = tmp_path / "payload"

    build_wasm_coreclr_payload(str(artifact), str(payload))

    staged = payload / WASM_CROSSGEN2_TASKS_PAYLOAD_DIR
    for name in (*WASM_CROSSGEN2_TASKS_FILES, "Crossgen2Tasks.deps.json"):
        assert (staged / name).read_text() == name


def test_coreclr_payload_without_crossgen2_tasks_shim(tmp_path):
    artifact = _write_coreclr_artifact(tmp_path)
    payload = tmp_path / "payload"

    build_wasm_coreclr_payload(str(artifact), str(payload))

    assert not (payload / WASM_CROSSGEN2_TASKS_PAYLOAD_DIR).exists()


def test_coreclr_payload_rejects_incomplete_crossgen2_tasks_shim(tmp_path):
    artifact = _write_coreclr_artifact(tmp_path, ("Crossgen2Tasks.dll",))

    with pytest.raises(ValueError, match="Microsoft.NET.CrossGen.props"):
        build_wasm_coreclr_payload(str(artifact), str(tmp_path / "payload"))


def test_coreclr_pre_commands_export_crossgen2_tasks_dir():
    commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=False,
        runtime_type="wasm_coreclr",
        codegen_type="wasm",
        build_config="Release",
        v8_version="15.1.206",
        wasm_local_package_version="11.0.0-ci",
        wasm_crossgen2_tasks_dir="$HELIX_CORRELATION_PAYLOAD/crossgen2-tasks",
    )

    assert any(
        "export PERFLAB_WASM_CROSSGEN2_TASKS_DIR=$HELIX_CORRELATION_PAYLOAD/crossgen2-tasks" in command
        for command in commands
    )


def test_coreclr_pre_commands_omit_crossgen2_tasks_dir_by_default():
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

    assert not any("PERFLAB_WASM_CROSSGEN2_TASKS_DIR" in command for command in commands)


def _crossgen2_tasks_dir(tmp_path, files=WASM_CROSSGEN2_TASKS_FILES):
    tasks_dir = tmp_path / "crossgen2-tasks"
    tasks_dir.mkdir()
    for name in files:
        (tasks_dir / name).touch()
    return tasks_dir


def _ready_to_run_args(composite):
    return Namespace(
        wasm=True,
        wasm_runtime_flavor="CoreCLR",
        wasm_ready_to_run=not composite,
        wasm_ready_to_run_composite=composite,
    )


def test_composite_ready_to_run_activates_crossgen2_tasks_shim(tmp_path, monkeypatch):
    tasks_dir = _crossgen2_tasks_dir(tmp_path)
    monkeypatch.setenv("PERFLAB_WASM_CROSSGEN2_TASKS_DIR", str(tasks_dir))
    monkeypatch.setenv("Crossgen2SdkOverridePropsPath", "/stale/Microsoft.NET.CrossGen.props")
    monkeypatch.setenv("Crossgen2SdkOverrideTargetsPath", "/stale/Microsoft.NET.CrossGen.targets")

    micro_benchmarks.configure_wasm_ready_to_run(_ready_to_run_args(composite=True))

    assert os.environ["Crossgen2SdkOverridePropsPath"] == str(tasks_dir / "Microsoft.NET.CrossGen.props")
    assert os.environ["Crossgen2SdkOverrideTargetsPath"] == str(tasks_dir / "Microsoft.NET.CrossGen.targets")


def test_per_assembly_ready_to_run_keeps_sdk_ready_to_run_tasks(tmp_path, monkeypatch):
    monkeypatch.setenv("PERFLAB_WASM_CROSSGEN2_TASKS_DIR", str(_crossgen2_tasks_dir(tmp_path)))
    # Inherited shim paths must not leak into a per-assembly run.
    monkeypatch.setenv("Crossgen2SdkOverridePropsPath", "/stale/Microsoft.NET.CrossGen.props")
    monkeypatch.setenv("Crossgen2SdkOverrideTargetsPath", "/stale/Microsoft.NET.CrossGen.targets")

    micro_benchmarks.configure_wasm_ready_to_run(_ready_to_run_args(composite=False))

    assert "Crossgen2SdkOverridePropsPath" not in os.environ
    assert "Crossgen2SdkOverrideTargetsPath" not in os.environ


def test_composite_ready_to_run_without_shim_keeps_sdk_ready_to_run_tasks(monkeypatch):
    monkeypatch.delenv("PERFLAB_WASM_CROSSGEN2_TASKS_DIR", raising=False)
    # A stale inherited shim must not be used when this run has none.
    monkeypatch.setenv("Crossgen2SdkOverridePropsPath", "/stale/Microsoft.NET.CrossGen.props")
    monkeypatch.setenv("Crossgen2SdkOverrideTargetsPath", "/stale/Microsoft.NET.CrossGen.targets")

    micro_benchmarks.configure_wasm_ready_to_run(_ready_to_run_args(composite=True))

    assert "Crossgen2SdkOverridePropsPath" not in os.environ
    assert "Crossgen2SdkOverrideTargetsPath" not in os.environ


def test_composite_ready_to_run_rejects_incomplete_shim(tmp_path, monkeypatch):
    tasks_dir = _crossgen2_tasks_dir(tmp_path, ("Crossgen2Tasks.dll", "Microsoft.NET.CrossGen.props"))
    monkeypatch.setenv("PERFLAB_WASM_CROSSGEN2_TASKS_DIR", str(tasks_dir))
    monkeypatch.delenv("Crossgen2SdkOverridePropsPath", raising=False)
    monkeypatch.delenv("Crossgen2SdkOverrideTargetsPath", raising=False)

    with pytest.raises(FileNotFoundError, match="Microsoft.NET.CrossGen.targets"):
        micro_benchmarks.configure_wasm_ready_to_run(_ready_to_run_args(composite=True))


def test_coreclr_payload_aliases_sdk_framework_during_major_version_rollover(tmp_path):
    artifact = tmp_path / "artifact" / "staging"
    dotnet = artifact / "dotnet-none"
    installed_version = "12.0.0-alpha.1.26458.117"
    shared_framework = dotnet / "shared" / "Microsoft.NETCore.App" / installed_version
    built_nugets = artifact / "built-nugets"
    shared_framework.mkdir(parents=True)
    built_nugets.mkdir(parents=True)
    (shared_framework / "System.Private.CoreLib.dll").write_text("runtime")
    (built_nugets / "Microsoft.NET.Sdk.WebAssembly.Pack.12.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NETCore.App.Crossgen2.linux-x64.12.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NET.ILLink.Tasks.12.0.0-ci.nupkg").touch()

    payload = tmp_path / "payload"
    version = build_wasm_coreclr_payload(str(artifact.parent), str(payload))

    aliased_framework = (
        payload / "dotnet" / "shared" / "Microsoft.NETCore.App" / "12.0.0-ci"
    )
    assert version == "12.0.0-ci"
    assert (aliased_framework / "System.Private.CoreLib.dll").read_text() == "runtime"


def test_coreclr_payload_rejects_missing_compatible_sdk_framework(tmp_path):
    artifact = tmp_path / "artifact" / "staging"
    (artifact / "dotnet-none" / "shared" / "Microsoft.NETCore.App" / "11.0.0").mkdir(
        parents=True
    )
    built_nugets = artifact / "built-nugets"
    built_nugets.mkdir(parents=True)
    (built_nugets / "Microsoft.NET.Sdk.WebAssembly.Pack.12.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NETCore.App.Crossgen2.linux-x64.12.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NET.ILLink.Tasks.12.0.0-ci.nupkg").touch()

    with pytest.raises(ValueError, match="Expected one installed Microsoft.NETCore.App 12.0"):
        build_wasm_coreclr_payload(str(artifact.parent), str(tmp_path / "payload"))


def test_coreclr_payload_rejects_mismatched_toolchain_package_versions(tmp_path):
    artifact = tmp_path / "artifact" / "staging"
    (artifact / "dotnet-none").mkdir(parents=True)
    built_nugets = artifact / "built-nugets"
    built_nugets.mkdir(parents=True)
    (built_nugets / "Microsoft.NET.Sdk.WebAssembly.Pack.11.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NETCore.App.Crossgen2.linux-x64.11.0.1-ci.nupkg").touch()
    (built_nugets / "Microsoft.NET.ILLink.Tasks.11.0.0-ci.nupkg").touch()

    with pytest.raises(ValueError, match="versions do not match"):
        build_wasm_coreclr_payload(str(artifact.parent), str(tmp_path / "payload"))


def test_coreclr_payload_requires_matching_illink_package(tmp_path):
    artifact = tmp_path / "artifact" / "staging"
    (artifact / "dotnet-none").mkdir(parents=True)
    built_nugets = artifact / "built-nugets"
    built_nugets.mkdir(parents=True)
    (built_nugets / "Microsoft.NET.Sdk.WebAssembly.Pack.11.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NETCore.App.Crossgen2.linux-x64.11.0.0-ci.nupkg").touch()
    (built_nugets / "Microsoft.NET.ILLink.Tasks.11.0.1-ci.nupkg").touch()

    with pytest.raises(ValueError, match="ILLink package versions do not match"):
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
    assert any("RestoreAdditionalProjectSources" in command for command in commands)


@pytest.mark.parametrize("runtime_type", ["wasm", "wasm_coreclr"])
def test_windows_wasm_pre_commands_fail_clearly(runtime_type):
    with pytest.raises(ValueError, match="not supported on Windows"):
        get_pre_commands(
            os_group="windows",
            os_distro=None,
            internal=False,
            runtime_type=runtime_type,
            codegen_type="wasm",
            build_config="Release",
            v8_version="15.1.206",
            wasm_local_package_version=(
                "11.0.0-ci" if runtime_type == "wasm_coreclr" else None),
        )


@pytest.mark.parametrize(
    ("os_group", "expected"),
    [
        ("windows", 'set "NAME=value"'),
        ("linux", "export NAME=value"),
    ],
)
def test_shell_environment_variable_command(os_group, expected):
    assert set_shell_environment_variable(os_group, "NAME", "value") == expected


def test_coreclr_sdk_pre_commands_do_not_enable_private_package_overrides():
    commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=False,
        runtime_type="wasm_coreclr",
        codegen_type="wasm",
        build_config="Release",
        v8_version="15.1.206",
        wasm_workload_source="https://example.test/cohort/v3/index.json",
    )

    assert not any("PERFLAB_WASM_PACKAGE_VERSION" in command for command in commands)
    assert not any("RestoreAdditionalProjectSources" in command for command in commands)


def test_mono_wasm_pre_commands_preserve_private_package_source():
    commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=False,
        runtime_type="wasm",
        codegen_type="wasm",
        build_config="Release",
        v8_version="15.1.206",
        wasm_workload_source="https://example.test/cohort/v3/index.json",
    )

    assert any("RestoreAdditionalProjectSources" in command for command in commands)


def test_non_r2r_coreclr_command_ignores_shared_workload_source():
    command = get_work_item_command(
        os_group="linux",
        target_csproj="src/benchmarks/micro/MicroBenchmarks.csproj",
        architecture="x64",
        perf_lab_framework="net11.0",
        internal=True,
        wasm=True,
        bdn_artifacts_dir="/tmp/artifacts",
        wasm_coreclr=True,
        wasm_ready_to_run=False,
        wasm_workload_source="https://example.test/cohort/v3/index.json",
    )

    assert "--wasm-workload-source" not in command


def test_pipeline_scopes_workload_source_to_coreclr_r2r():
    template = (
        scripts_dir.parent
        / "eng"
        / "pipelines"
        / "templates"
        / "run-performance-job.yml"
    ).read_text(encoding="utf-8")

    condition = (
        "and(ne(parameters.wasmWorkloadSource, ''), "
        "eq(parameters.runtimeType, 'wasm_coreclr'), "
        "in(parameters.r2rRunType, 'r2r', 'r2r_composite'))"
    )
    assert condition in template


def test_pipeline_defines_coreclr_composite_r2r_lane():
    jobs = (
        scripts_dir.parent / "eng" / "pipelines" / "runtime-wasm-perf-jobs.yml"
    ).read_text(encoding="utf-8")
    release_exclusion = (
        "  - ${{ if not(startswith(variables['Build.SourceBranch'], "
        "'refs/heads/release')) }}:\n"
    )

    def lane(identifier):
        blocks = [
            block for block in jobs.split(release_exclusion)[1:]
            if f"additionalJobIdentifier: {identifier}\n" in block
        ]
        assert len(blocks) == 1
        # Stop at the next job's leading comment.
        return blocks[0].split("\n\n")[0]

    def without_lane_identity(block):
        return [
            line for line in block.splitlines()
            if not line.strip().startswith(("r2rRunType:", "additionalJobIdentifier:"))
        ]

    per_assembly = lane("coreclr_r2r_v8")
    composite = lane("coreclr_r2r_composite_v8")

    assert "r2rRunType: 'r2r'\n" in per_assembly
    assert "r2rRunType: 'r2r_composite'\n" in composite
    assert without_lane_identity(composite) == without_lane_identity(per_assembly)


@pytest.mark.parametrize("source", [None, "", "   "])
def test_empty_workload_source_is_not_enabled(source):
    assert normalize_wasm_workload_source(source) is None


def test_workload_source_is_trimmed():
    assert normalize_wasm_workload_source(
        "  https://example.test/cohort/v3/index.json  "
    ) == "https://example.test/cohort/v3/index.json"


@pytest.mark.parametrize(
    ("system", "architecture", "libc", "expected"),
    [
        ("Darwin", "arm64", "", "osx-arm64"),
        ("Darwin", "x64", "", "osx-x64"),
        ("Linux", "x64", "glibc", "linux-x64"),
        ("Linux", "x64", "musl", "linux-musl-x64"),
        ("Windows", "x64", "", "win-x64"),
    ],
)
def test_crossgen2_host_rid(system, architecture, libc, expected):
    assert dotnet.get_host_rid(architecture, system=system, libc=libc) == expected


def _write_bundled_versions(
        dotnet_root: Path,
        sdk_version: str = "11.0.100-preview.1.12345.1",
        product_version: str = "11.0.0-preview.1.12345.1",
        illink_version: Optional[str] = None) -> None:
    sdk_dir = dotnet_root / "sdk" / sdk_version
    sdk_dir.mkdir(parents=True)
    (sdk_dir / "Microsoft.NETCoreSdk.BundledVersions.props").write_text(
        f"""<Project>
  <ItemGroup>
    <KnownFrameworkReference Include="Microsoft.NETCore.App"
      TargetFramework="net11.0"
      DefaultRuntimeFrameworkVersion="{product_version}"
      TargetingPackName="Microsoft.NETCore.App.Ref"
      TargetingPackVersion="{product_version}"
      RuntimePackRuntimeIdentifiers="linux-x64;osx-x64;osx-arm64;browser-wasm" />
    <KnownCrossgen2Pack Include="Microsoft.NETCore.App.Crossgen2"
      TargetFramework="net11.0"
      Crossgen2PackVersion="{product_version}"
      Crossgen2PortableRuntimeIdentifiers="linux-x64;osx-x64;osx-arm64" />
    <KnownILLinkPack Include="Microsoft.NET.ILLink.Tasks"
      TargetFramework="net11.0"
      ILLinkPackVersion="{illink_version or product_version}" />
    <KnownWebAssemblySdkPack Include="Microsoft.NET.Sdk.WebAssembly.Pack"
      TargetFramework="net11.0"
      WebAssemblySdkPackVersion="{product_version}" />
  </ItemGroup>
</Project>
""",
        encoding="utf-8",
    )


def test_cohort_uses_exact_sdk_product_version_and_host_crossgen2(tmp_path):
    sdk_version = "11.0.100-preview.1.12345.1"
    product_version = "11.0.0-preview.1.12345.1"
    _write_bundled_versions(tmp_path, sdk_version, product_version)

    cohort = dotnet.get_wasm_workload_cohort(
        str(tmp_path), sdk_version, "net11.0", "osx-arm64")

    assert cohort.product_version == product_version
    assert cohort.workload_id == "wasm-tools"
    assert dotnet.WasmPackage(
        "Microsoft.NETCore.App.Crossgen2.osx-arm64",
        product_version,
    ) in cohort.packages
    assert {package.version for package in cohort.packages} == {product_version}


def test_cohort_rejects_mismatched_sdk_package_versions(tmp_path):
    _write_bundled_versions(
        tmp_path,
        illink_version="11.0.0-preview.1.12345.2",
    )

    with pytest.raises(ValueError, match="does not define one coherent"):
        dotnet.get_wasm_workload_cohort(
            str(tmp_path),
            "11.0.100-preview.1.12345.1",
            "net11.0",
            "linux-x64",
        )


def test_local_cohort_source_requires_all_exact_packages(tmp_path, monkeypatch):
    sdk_version = "11.0.100-preview.1.12345.1"
    product_version = "11.0.0-preview.1.12345.1"
    dotnet_root = tmp_path / "dotnet"
    package_source = tmp_path / "packages"
    package_source.mkdir()
    _write_bundled_versions(dotnet_root, sdk_version, product_version)
    monkeypatch.setenv("DOTNET_ROOT", str(dotnet_root))
    monkeypatch.setattr(dotnet, "get_host_rid", lambda architecture: "linux-x64")

    for package_id in (
        "Microsoft.NETCore.App.Runtime.browser-wasm",
        "Microsoft.NETCore.App.Ref",
        "Microsoft.NET.Sdk.WebAssembly.Pack",
        "Microsoft.NETCore.App.Crossgen2.linux-x64",
    ):
        (package_source / f"{package_id}.{product_version}.nupkg").touch()

    with pytest.raises(ValueError, match="Microsoft.NET.ILLink.Tasks"):
        dotnet.install_wasm_workload(
            architecture="x64",
            target_framework_monikers=["net11.0"],
            package_source=str(package_source),
            sdk_versions=[sdk_version],
            verbose=False,
        )


def test_cohort_source_rejects_multiple_sdk_versions():
    with pytest.raises(ValueError, match="at most one SDK version"):
        dotnet.install_wasm_workload(
            architecture="x64",
            target_framework_monikers=["net11.0"],
            package_source="https://example.test/cohort/v3/index.json",
            sdk_versions=["11.0.100-preview.1", "11.0.100-preview.2"],
            verbose=False,
        )


def test_generated_workload_commands_pin_workload_and_coreclr_r2r_cohort():
    cohort = dotnet.WasmWorkloadCohort(
        sdk_version="11.0.100-preview.1.12345.1",
        product_version="11.0.0-preview.1.12345.1",
        host_rid="linux-x64",
        workload_id="wasm-tools",
        packages=(),
    )

    commands = dotnet.get_wasm_workload_commands(
        "/dotnet/dotnet",
        cohort,
        "/tmp/NuGet.Config",
        "/tmp/WasmCohort.csproj",
        "/tmp/packages",
    )

    assert commands[0] == [
        "/dotnet/dotnet",
        "restore",
        "/tmp/WasmCohort.csproj",
        "--packages",
        "/tmp/packages",
        "--configfile",
        "/tmp/NuGet.Config",
        "--no-http-cache",
    ]
    assert commands[1] == [
        "/dotnet/dotnet",
        "workload",
        "install",
        "wasm-tools",
        "--skip-manifest-update",
        "--configfile",
        "/tmp/NuGet.Config",
        "--no-cache",
    ]
