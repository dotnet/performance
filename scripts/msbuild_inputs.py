"""Validate and stage a composed MSBuild SDK for the existing Helix scenario runner."""

import json
from pathlib import Path
import re
import shutil

from compose_msbuild_sdk import ASSETS, LIBRARIES, RUNTIME_FILES, sha512, verify_archive
from channel_map import ChannelMap
from msbuild_catalog import CATALOG, CATALOG_SHA256


def verify_sdk(sdk, manifest, target_os):
    sdk = Path(sdk)
    if (manifest.get("os", "windows") != target_os
            or manifest.get("architecture", "x64") != "x64"):
        raise ValueError("Composed SDK platform does not match the target worker.")
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?", manifest["base_sdk_version"]):
        raise ValueError("Invalid base SDK version in input manifest.")
    required_major = re.search(r"\d+", ChannelMap.get_target_framework_moniker("main")).group()
    if manifest["base_sdk_version"].split(".")[0] != required_major:
        raise ValueError(f"Channel main requires a {required_major}.x base SDK.")
    host_name = "dotnet.exe" if target_os == "windows" else "dotnet"
    if not (sdk / host_name).is_file():
        raise FileNotFoundError(sdk / host_name)
    sdk_directory = sdk / "sdk" / manifest["base_sdk_version"]
    expected_files = {name + ".dll" for name in LIBRARIES} | set(RUNTIME_FILES)
    if set(manifest["overlay_sha512"]) != expected_files:
        raise ValueError("Manifest must identify every MSBuild overlay file.")
    for name, checksum in manifest["overlay_sha512"].items():
        if Path(name).name != name or sha512(sdk_directory / name) != checksum:
            raise ValueError(f"Overlay checksum mismatch: {name}")


def configure_inputs(directory, payload, ci_args, target_os="windows", required_assets=None):
    root = Path(directory)
    manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
    if (manifest["repository"] != "dotnet/msbuild" or manifest["branch"] != "main"
            or manifest["build_configuration"] != "Release"
            or not re.fullmatch(r"[0-9a-f]{40}", manifest["commit"])):
        raise ValueError("Expected a Release MSBuild main-branch input manifest.")
    sdk = root / "sdk"
    verify_sdk(sdk, manifest, target_os)
    host_name = "dotnet.exe" if target_os == "windows" else "dotnet"
    assets = manifest["assets"]
    missing = set(required_assets or []) - assets.keys()
    if missing:
        raise ValueError(f"Selected scenarios require missing assets: {', '.join(sorted(missing))}")
    if not assets or set(assets) - ASSETS.keys():
        raise ValueError("Input manifest must identify known version-pinned workload assets.")
    for name in assets:
        version = ASSETS[name][0]
        if manifest["assets"][name]["version"] != version:
            raise ValueError(f"Unexpected {name} version.")
        verify_archive(root / "assets" / (name + ".zip"), manifest["assets"][name]["sha512"])
    destination = Path(payload) / "dotnet"
    shutil.copytree(sdk, destination)
    if target_os == "linux":
        for name in set(manifest["executable_files"]) | {host_name}:
            executable = (destination / name).resolve()
            if not executable.is_relative_to(destination.resolve()) or not executable.is_file():
                raise ValueError(f"Invalid executable in SDK manifest: {name}")
            executable.chmod(executable.stat().st_mode | 0o111)
    inputs = Path(payload) / "msbuild-inputs"
    shutil.copytree(root / "assets", inputs / "assets")
    shutil.copyfile(root / "manifest.json", inputs / "manifest.json")
    ci_args.install_dir = str(destination)
    ci_args.dotnet_path = str(destination)
    ci_args.repository = "dotnet/msbuild"
    ci_args.commit_sha = manifest["commit"]
    ci_args.commit_time = manifest["commit_time"]
    ci_args.branch = "main"
    ci_args.dotnet_versions = [manifest["base_sdk_version"]]
    metadata = {
        "MSBuildSource": "self-built-release",
        "DOTNET_CLI_ENABLEAOT": "0",
        "BaseSdkSha512": manifest["base_sdk_sha512"],
        "MSBuildScenarioCatalog": CATALOG_SHA256,
        "MSBuildSourceInventory": CATALOG["source_inventory_sha256"],
    }
    metadata.update({f"PerfStarAsset_{name}_version": entry["version"] for name, entry in assets.items()})
    ci_args.run_env_vars = (ci_args.run_env_vars or []) + [
        f"{key}={value}" for key, value in metadata.items()
    ]
