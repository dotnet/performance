import io
import json
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import Mock
import xml.etree.ElementTree as ET

import pytest

import scripts.run_performance_job as job
from scripts.run_performance_job import APT_LOCK_TIMEOUT_OPTION, get_pre_commands


def get_generated_apt_commands(*, internal: bool, runtime_type: str) -> list[str]:
    pre_commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=internal,
        runtime_type=runtime_type,
        codegen_type="jit",
        build_config="Release",
        v8_version="12.0.0",
        wasm_local_package_version="11.0.0-ci" if runtime_type == "wasm_coreclr" else None,
    )

    return [
        command.strip()
        for pre_command in pre_commands
        for command in pre_command.split(" && ")
        if command.strip().startswith(("sudo apt ", "sudo apt-get "))
    ]


@pytest.mark.parametrize(
    ("internal", "runtime_type", "expected_command_count"),
    [
        (True, "coreclr", 4),
        (False, "wasm", 6),
        (True, "wasm", 10),
        (False, "wasm_coreclr", 6),
    ],
)
def test_all_generated_apt_commands_use_lock_timeout(
    internal: bool, runtime_type: str, expected_command_count: int
):
    apt_commands = get_generated_apt_commands(
        internal=internal, runtime_type=runtime_type
    )

    assert len(apt_commands) == expected_command_count
    assert all(
        f" {APT_LOCK_TIMEOUT_OPTION} " in command for command in apt_commands
    )


def test_generated_prerequisites_do_not_poll_dpkg_lock():
    pre_commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=True,
        runtime_type="wasm",
        codegen_type="jit",
        build_config="Release",
        v8_version="12.0.0",
    )

    prerequisites = "\n".join(pre_commands)
    assert "fuser" not in prerequisites
    assert "Waiting for dpkg" not in prerequisites


def test_linux_prerequisite_failure_remains_failed_after_health_check():
    pre_commands = get_pre_commands(
        os_group="linux",
        os_distro="ubuntu",
        internal=True,
        runtime_type="coreclr",
        codegen_type="jit",
        build_config="Release",
        v8_version="12.0.0",
    )

    health_check = next(
        command for command in pre_commands if "machine_health.py" in command
    )
    assert health_check.endswith("check || true; exit 1; fi")


@pytest.fixture
def targeting_pack_payload(tmp_path, monkeypatch):
    payload = tmp_path / "performance"
    harness = payload / "src" / "harness" / "BenchmarkDotNet.Extensions"
    harness.mkdir(parents=True)
    (harness / "BenchmarkDotNet.Extensions.csproj").write_text(
        "<Project><PropertyGroup><TargetFramework>net8.0</TargetFramework>"
        "</PropertyGroup></Project>")
    dotnet_root = payload / "tools" / "dotnet" / "x64"
    sdk_path = dotnet_root / "sdk" / "11.0.100"
    sdk_path.mkdir(parents=True)
    bundled_versions = ET.Element("Project")
    items = ET.SubElement(bundled_versions, "ItemGroup")
    for framework in ("net8.0", "net9.0", "net11.0"):
        for package in job.TARGETING_PACK_COMPONENTS:
            ET.SubElement(items, "KnownFrameworkReference", {
                "Include": package.removesuffix(".Ref"),
                "TargetFramework": framework,
                "TargetingPackName": package,
                "TargetingPackVersion": "8.0.33",
            })
    ET.ElementTree(bundled_versions).write(
        sdk_path / "Microsoft.NETCoreSdk.BundledVersions.props")
    monkeypatch.setattr(job.dotnet, "get_base_path", Mock(return_value=str(sdk_path)))

    versions = {
        "net8.0": {
            "Microsoft.NETCore.App.Ref": "8.0.32",
            "Microsoft.AspNetCore.App.Ref": "8.0.31",
            "Microsoft.WindowsDesktop.App.Ref": "8.0.30",
        },
        "net9.0": {
            "Microsoft.NETCore.App.Ref": "9.0.21",
            "Microsoft.AspNetCore.App.Ref": "9.0.20",
            "Microsoft.WindowsDesktop.App.Ref": "9.0.19",
        },
    }

    def release_metadata(url, **kwargs):
        channel = url.split("/")[-2]
        components = {
            job.TARGETING_PACK_COMPONENTS[package]: {"version": version}
            for package, version in versions[f"net{channel}"].items()
        }
        return io.BytesIO(json.dumps({
            "channel-version": channel,
            "support-phase": "active",
            "releases": [
                {name: {"version": f"{channel}.99-preview.1"} for name in components},
                {name: {"version": f"{channel}.9"} for name in components},
                components,
            ],
        }).encode())

    requests = Mock(side_effect=release_metadata)
    monkeypatch.setattr(job.urllib.request, "urlopen", requests)
    restored_projects = []

    def restore(command, **kwargs):
        def run(cwd):
            project = ET.parse(command[2])
            restored_projects.append(project)
            sources = ET.parse(command[command.index("--configfile") + 1])
            assert sources.find("./packageSources/clear") is not None
            assert len(sources.findall("./packageSources/add")) == 1
            package_root = Path(command[command.index("--packages") + 1])
            for package in project.iter("PackageDownload"):
                for version in package.get("Version").split(";"):
                    version = version.strip("[]")
                    framework = "net" + ".".join(version.split(".")[:2])
                    reference_dir = (
                        package_root / package.get("Include").lower() / version /
                        "ref" / framework)
                    reference_dir.mkdir(parents=True)
                    (reference_dir / "Reference.dll").write_bytes(b"reference")
        return SimpleNamespace(run=run)

    commands = Mock(side_effect=restore)
    monkeypatch.setattr(job, "RunCommand", commands)

    def selected_packs(command, **kwargs):
        framework = next(
            argument.split("=", 1)[1] for argument in command
            if argument.startswith("-p:TargetFramework="))
        return json.dumps({"Items": {"TargetingPack": [
            {
                "NuGetPackageId": package,
                "NuGetPackageVersion": version,
                "PackageDirectory": str(dotnet_root / "packs" / package / version),
            }
            for package, version in versions[framework].items()
        ]}})

    selection = Mock(side_effect=selected_packs)
    monkeypatch.setattr(job, "check_output", selection)
    return SimpleNamespace(
        payload=payload, dotnet_root=dotnet_root, sdk_path=sdk_path,
        versions=versions, requests=requests, commands=commands,
        restored_projects=restored_projects, selection=selection)


def test_released_targeting_packs_use_latest_stable_component_versions(targeting_pack_payload):
    versions = job.get_released_targeting_pack_versions(
        "net8.0", set(targeting_pack_payload.versions["net8.0"]))

    assert versions == targeting_pack_payload.versions["net8.0"]


@pytest.mark.parametrize("os_group", ["windows", "linux"])
def test_downlevel_targeting_packs_are_staged_once_without_changing_current_packs(
        targeting_pack_payload, os_group):
    state = targeting_pack_payload
    current_pack = state.dotnet_root / "packs" / "Microsoft.NETCore.App.Ref" / "11.0.0"
    current_pack.mkdir(parents=True)
    (current_pack / "marker").write_text("daily")

    job.provision_downlevel_targeting_packs(
        str(state.payload), str(state.dotnet_root), "net11.0", os_group)

    expected = dict(state.versions["net8.0"])
    if os_group != "windows":
        del expected["Microsoft.WindowsDesktop.App.Ref"]
    for package, version in expected.items():
        assert (state.dotnet_root / "packs" / package / version /
                "ref" / "net8.0" / "Reference.dll").is_file()
    overrides = ET.parse(state.payload / "eng" / "DownlevelTargetingPacks.props")
    metadata = overrides.findall(".//TargetingPackVersion")
    assert len(metadata) == len(expected)
    for item in metadata:
        assert "'net8.0'" in item.get("Condition")
        assert "'net11.0'" not in item.get("Condition")
    assert {item.text for item in metadata} == set(expected.values())
    assert (current_pack / "marker").read_text() == "daily"
    assert state.requests.call_count == 1
    assert state.commands.call_count == 1


def test_multiple_downlevel_frameworks_share_one_restore(targeting_pack_payload):
    state = targeting_pack_payload

    job.provision_downlevel_targeting_packs(
        str(state.payload), str(state.dotnet_root), "net9.0", "windows")

    downloads = list(state.restored_projects[0].iter("PackageDownload"))
    assert len(downloads) == 3
    for package in downloads:
        name = package.get("Include")
        assert set(package.get("Version").split(";")) == {
            f"[{state.versions[framework][name]}]"
            for framework in ("net8.0", "net9.0")
        }
    assert state.requests.call_count == 2
    assert state.commands.call_count == 1
    assert state.selection.call_count == 2


def test_current_framework_needs_no_downlevel_override(targeting_pack_payload, monkeypatch):
    state = targeting_pack_payload
    sdk_path = state.sdk_path.with_name("8.0.426")
    state.sdk_path.rename(sdk_path)
    monkeypatch.setattr(job.dotnet, "get_base_path", Mock(return_value=str(sdk_path)))

    job.provision_downlevel_targeting_packs(
        str(state.payload), str(state.dotnet_root), "net8.0", "windows")

    state.requests.assert_not_called()
    state.commands.assert_not_called()
    assert not (state.payload / "eng" / "DownlevelTargetingPacks.props").exists()


def test_unshipped_downlevel_framework_keeps_sdk_packs(targeting_pack_payload):
    state = targeting_pack_payload
    state.requests.side_effect = lambda *args, **kwargs: io.BytesIO(json.dumps({
        "channel-version": "8.0", "support-phase": "preview",
        "releases": [{"runtime": {"version": "8.0.0-rc.2"}}],
    }).encode())

    job.provision_downlevel_targeting_packs(
        str(state.payload), str(state.dotnet_root), "net11.0", "windows")

    state.commands.assert_not_called()
    assert not (state.payload / "eng" / "DownlevelTargetingPacks.props").exists()


def test_missing_released_component_fails_instead_of_using_daily_packs(targeting_pack_payload):
    state = targeting_pack_payload
    state.requests.side_effect = lambda *args, **kwargs: io.BytesIO(json.dumps({
        "channel-version": "8.0", "support-phase": "active",
        "releases": [{"runtime": {"version": "8.0.32"}}],
    }).encode())

    with pytest.raises(ValueError, match="No publicly released Microsoft.AspNetCore.App.Ref"):
        job.provision_downlevel_targeting_packs(
            str(state.payload), str(state.dotnet_root), "net11.0", "windows")

    state.commands.assert_not_called()


def test_missing_restored_pack_fails_before_writing_overrides(targeting_pack_payload):
    state = targeting_pack_payload
    state.commands.side_effect = None
    state.commands.return_value.run.return_value = 0

    with pytest.raises(ValueError, match="NuGet did not restore"):
        job.provision_downlevel_targeting_packs(
            str(state.payload), str(state.dotnet_root), "net11.0", "windows")

    assert not (state.payload / "eng" / "DownlevelTargetingPacks.props").exists()


@pytest.mark.parametrize("include_valid_profile", [False, True])
def test_ignored_sdk_override_fails_payload_preparation(
        targeting_pack_payload, include_valid_profile):
    state = targeting_pack_payload
    state.selection.side_effect = None
    selections = [{
        "NuGetPackageId": "Microsoft.NETCore.App.Ref",
        "NuGetPackageVersion": "8.0.33",
    }]
    if include_valid_profile:
        selections.extend({
            "NuGetPackageId": package, "NuGetPackageVersion": version,
            "PackageDirectory": str(state.dotnet_root / "packs" / package / version),
        } for package, version in state.versions["net8.0"].items())
    state.selection.return_value = json.dumps({"Items": {"TargetingPack": selections}})

    with pytest.raises(ValueError, match="did not honor.*override"):
        job.provision_downlevel_targeting_packs(
            str(state.payload), str(state.dotnet_root), "net11.0", "windows")


def test_sdk_must_locate_staged_packs(targeting_pack_payload):
    state = targeting_pack_payload
    state.selection.side_effect = None
    state.selection.return_value = json.dumps({"Items": {"TargetingPack": [
        {"NuGetPackageId": package, "NuGetPackageVersion": version}
        for package, version in state.versions["net8.0"].items()
    ]}})

    with pytest.raises(ValueError, match="could not locate staged"):
        job.provision_downlevel_targeting_packs(
            str(state.payload), str(state.dotnet_root), "net11.0", "windows")
