import os
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
    (built_nugets / "Microsoft.NET.ILLink.Tasks.11.0.0-ci.nupkg").touch()

    version = build_wasm_coreclr_payload(str(artifact.parent), str(tmp_path / "payload"))

    assert version == "11.0.0-ci"


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
        "--no-http-cache",
    ]
