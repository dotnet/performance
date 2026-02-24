"""
This file contains helper methods for turning build artifacts from the build step of our CI pipeline
and the Build Caching Service into a payload that can be used locally or in Helix jobs.
"""
from collections.abc import Iterable
from logging import getLogger
import os
from pathlib import Path
import shutil
import tarfile
from typing import Optional
import zipfile

from performance.common import RunCommand, iswin

__all__ = [
    "extract_archive_or_copy",
    "build_coreroot_payload",
    "build_coreroot_payload_simple",
    "build_mono_payload",
    "build_monoaot_payload",
    "build_wasm_payload",
    "build_wasm_coreclr_payload",
]


def _set_permissions_recursive(dirs: Iterable[str], mode: int) -> None:
    for directory in dirs:
        for root, _, files in os.walk(directory):
            for item in files:
                path = os.path.join(root, item)
                try:
                    os.chmod(path, mode)
                except OSError as exc:  # Permission or unsupported FS operation
                    getLogger().debug("Failed to set permissions for %s: %s", path, exc)


def extract_archive_or_copy(archive_path_or_dir: str, dest_dir: str, prefix: Optional[str] = None) -> None:
    """Extract an archive (.zip / .tar.gz) or copy from a directory into destination.

    When a `prefix` is provided only entries whose path starts with that prefix
    are considered. For archives the prefix acts as a filter. For source
    directories we treat the path *up to the last slash* as a subdirectory to
    descend into (``prefix_folder``) and the remainder as a file prefix filter.

    Examples:
        prefix = "artifacts/bin/mono/linux.x64.Release/" -> copy everything under that folder.
        prefix = "coreclr/windows.x64.Release/corerun"   -> copy only files beginning with "corerun".

    Args:
        archive_path_or_dir: Path to a directory OR a .zip / .tar.gz archive.
        dest_dir: Destination directory (created if missing).
        prefix: Optional path (and optional filename prefix) scoping extracted content.
    """
    if not os.path.exists(archive_path_or_dir):
        raise FileNotFoundError(f"Archive or directory not found: {archive_path_or_dir}")

    if os.path.exists(dest_dir) and not os.path.isdir(dest_dir):
        raise NotADirectoryError(f"Destination exists and is not a directory: {dest_dir}")

    os.makedirs(dest_dir, exist_ok=True)

    # Derive the *folder* part of the prefix (everything before the final slash)
    # so that we can both enter that folder (for directory copies) and strip it
    # from member names when filtering archive entries.
    prefix_folder = prefix or ""
    if prefix and not prefix.endswith("/") and "/" in prefix:
        prefix_folder = prefix[:prefix.rfind("/") + 1]

    getLogger().info(
        "extract_archive_or_copy: source=%s dest=%s prefix=%s (folder=%s)",
        archive_path_or_dir,
        dest_dir,
        prefix,
        prefix_folder,
    )

    if os.path.isdir(archive_path_or_dir):
        src_dir = archive_path_or_dir
        if prefix is not None:
            src_dir = os.path.join(archive_path_or_dir, prefix_folder)
            if not os.path.exists(src_dir):
                raise FileNotFoundError(f"Source folder not found in archive: {src_dir}")
            prefix = prefix[len(prefix_folder):]

        if not os.path.samefile(src_dir, dest_dir):
            if not prefix:  # Simple copy of entire subtree
                shutil.copytree(src_dir, dest_dir, dirs_exist_ok=True)
            else:
                # Selective copy: only items whose relative path starts with prefix
                for item in Path(src_dir).rglob(f"{prefix}*"):
                    if item.is_file():
                        dest_path = os.path.join(dest_dir, item.relative_to(src_dir))
                        os.makedirs(os.path.dirname(dest_path), exist_ok=True)
                        shutil.copy2(item, dest_path)
    elif archive_path_or_dir.endswith(".zip"):
        with zipfile.ZipFile(archive_path_or_dir, "r") as zip_ref:
            if prefix is None:
                zip_ref.extractall(dest_dir)
            else:
                for member in zip_ref.namelist():
                    if member.startswith(prefix):
                        relative_path = member[len(prefix_folder):]
                        if not relative_path or relative_path.endswith("/"):
                            continue  # Skip directory entries
                        output_path = os.path.join(dest_dir, relative_path)
                        os.makedirs(os.path.dirname(output_path), exist_ok=True)
                        with zip_ref.open(member) as source, open(output_path, "wb") as target:
                            target.write(source.read())
    elif archive_path_or_dir.endswith(".tar.gz"):
        with tarfile.open(archive_path_or_dir, "r:gz") as tar_ref:
            if prefix is None:
                tar_ref.extractall(dest_dir)
            else:
                for member in tar_ref.getmembers():
                    if member.name.startswith(prefix):
                        relative_path = member.name[len(prefix_folder):]
                        if not relative_path or relative_path.endswith("/"):
                            continue
                        output_path = os.path.join(dest_dir, relative_path)
                        os.makedirs(os.path.dirname(output_path), exist_ok=True)
                        source = tar_ref.extractfile(member)
                        if source is not None:
                            with source, open(output_path, "wb") as target:
                                target.write(source.read())
    else:
        raise Exception("Unsupported archive format")

def build_coreroot_payload(
    runtime_repo_dir: str,
    core_root_dest: str,
    os_group: str,
    architecture: str,
    coreclr_archive_or_dir: Optional[str] = None,
    libraries_config: Optional[str] = None,
    cross_build: bool = False,
    clean_artifacts: bool = False,
) -> None:
    """Generate a CoreCLR `Core_Root` payload by re-running test layout script.

    Args:
        runtime_repo_dir: Root of a dotnet/runtime clone.
        core_root_dest: Destination directory to copy the final Core_Root into.
        os_group: Target OS group (e.g. "windows", "linux", "osx"). On Windows host
                  only "windows" is supported (cross-build restrictions).
        architecture: Target architecture (x64, arm64, etc.).
        coreclr_archive_or_dir: Optional path to a built CoreCLR (zip, tar.gz, or dir).
                                Defaults to runtime `artifacts/bin`.
        libraries_config: Optional libraries configuration (e.g. "Release") to pass through.
        clean_artifacts: If True, remove previous `artifacts/bin` & `artifacts/tests` first.
    """
    if not os.path.exists(runtime_repo_dir):
        raise Exception("Runtime repo directory not found")

    if iswin() and os_group != "windows":
        raise Exception(f"Unable to build Core_Root for {os_group} on Windows")

    artifacts_dir = os.path.join(runtime_repo_dir, "artifacts")
    artifacts_bin_dir = os.path.join(artifacts_dir, "bin")
    artifacts_tests_dir = os.path.join(artifacts_dir, "tests")

    coreclr_archive_or_dir = coreclr_archive_or_dir or artifacts_bin_dir  # default to bin dir
    if not os.path.exists(coreclr_archive_or_dir):
        raise Exception("CoreCLR build not found")

    if clean_artifacts:
        getLogger().debug("Cleaning artifact directories prior to layout generation")
        if os.path.exists(artifacts_bin_dir) and not os.path.samefile(coreclr_archive_or_dir, artifacts_bin_dir):
            shutil.rmtree(artifacts_bin_dir)
        if os.path.exists(artifacts_tests_dir):
            shutil.rmtree(artifacts_tests_dir)

    extract_archive_or_copy(coreclr_archive_or_dir, artifacts_bin_dir)

    build_file = "build.cmd" if iswin() else "build.sh"
    build_script = os.path.join(runtime_repo_dir, "src", "tests", build_file)
    if not os.path.exists(build_script):
        raise Exception(f"Build script not found at path: {build_script}")

    generate_layout_command = [build_script, "release", architecture, "generatelayoutonly"]

    if not iswin():
        generate_layout_command.extend(["-os", os_group])

    if cross_build:
        generate_layout_command.append("-cross")

    if libraries_config:
        generate_layout_command.append(f"/p:LibrariesConfiguration={libraries_config}")

    RunCommand(generate_layout_command, verbose=True).run(runtime_repo_dir)

    core_root_dir = os.path.join(
        artifacts_tests_dir,
        "coreclr",
        f"{os_group}.{architecture}.Release",
        "Tests",
        "Core_Root",
    )
    if not os.path.exists(core_root_dir):
        raise Exception(f"Core_Root directory not found in expected location: {core_root_dir}")

    shutil.copytree(
        core_root_dir,
        core_root_dest,
        dirs_exist_ok=True,
        ignore=shutil.ignore_patterns("*.pdb"),  # Exclude PDBs (not needed in payloads)
    )

def build_coreroot_payload_simple(
    core_root_dest: str,
    tfm: str,
    os_group: str,
    architecture: str,
    coreclr_archive_or_dir: str,
    libraries_config: str = "Release"
):
    """Generate a CoreCLR `Core_Root` manually.

    This is a simplified version of `build_coreroot_payload` that does not require a clone of the runtime
    repository. If the runtime repository is available, build_coreroot_payload should be used instead to ensure
    accuracy in case the layout generation code changes.

    This Core_Root can't be used to run the tests in the dotnet/runtime repository as it does not create the targeting
    pack, however the targeting pack is not needed to run the performance tests.
    """
    libraries_path_segment = f"{tfm}-{os_group}-{libraries_config}-{architecture}"
    extract_archive_or_copy(coreclr_archive_or_dir, core_root_dest, prefix=f"runtime/{libraries_path_segment}/")
    extract_archive_or_copy(coreclr_archive_or_dir, core_root_dest, prefix=f"native/{libraries_path_segment}/")
    extract_archive_or_copy(coreclr_archive_or_dir, core_root_dest, prefix=f"coreclr/{os_group}.{architecture}.{libraries_config}/")

    # chmod +x corerun
    if os_group != "windows":
        corerun_executable = os.path.join(core_root_dest, "corerun")
        if os.path.exists(corerun_executable):
            os.chmod(corerun_executable, 0o755)

def build_mono_payload(
    mono_payload_dst: str,
    os_group: str,
    framework: str,
    build_config: str,
    architecture: str,
    product_version: str,
    runtime_repo_dir: Optional[str] = None,
    mono_archive_or_dir: Optional[str] = None,
) -> None:
    """Assemble a Mono testhost payload with corerun host files.

    Copies testhost for the specified framework/OS/arch/config plus the `corerun`
    host executable(s) from the coreclr layout to the shared framework directory
    for the requested `product_version` (so BDN can launch with corerun).
    """
    if mono_archive_or_dir is None:
        if runtime_repo_dir is None:
            raise Exception("Please provide a path to the built mono artifacts")
        mono_archive_or_dir = os.path.join(runtime_repo_dir, "artifacts", "bin")

    build_config_upper = build_config.title()  # release -> Release

    extract_archive_or_copy(
        mono_archive_or_dir,
        mono_payload_dst,
        prefix=f"testhost/{framework}-{os_group}-{build_config_upper}-{architecture}/",
    )
    corerun_target = os.path.join(
        mono_payload_dst, "shared", "Microsoft.NETCore.App", product_version
    )
    # No trailing slash: we only want files starting with "corerun" (e.g., corerun, corerun.exe)
    extract_archive_or_copy(
        mono_archive_or_dir,
        corerun_target,
        prefix=f"coreclr/{os_group}.{architecture}.{build_config_upper}/corerun",
    )

def build_monoaot_payload(
    monoaot_artifacts_archive_or_dir: str, payload_dest: str, architecture: str
) -> None:
    """Build a Mono AOT payload consisting of cross tools + runtime pack.

    The function expects either an artifacts directory or an archive containing
    the structure produced by Mono AOT builds. Two extractions are performed:
      1. Cross compiler / AOT toolchain for the given architecture.
      2. The runtime pack which is copied to the `pack` directory in the payload.
    """
    pack_dir = os.path.join(payload_dest, "pack")
    os.makedirs(pack_dir, exist_ok=True)

    extract_archive_or_copy(
        monoaot_artifacts_archive_or_dir,
        payload_dest,
        prefix=f"artifacts/bin/mono/linux.{architecture}.Release/cross/linux-{architecture}/",
    )

    extract_archive_or_copy(
        monoaot_artifacts_archive_or_dir,
        pack_dir,
        prefix=f"artifacts/bin/microsoft.netcore.app.runtime.linux-{architecture}/Release/",
    )
    
def build_wasm_payload(
    browser_wasm_archive_or_dir: str,
    payload_parent_dir: str,  # wasm creates two payload directories
) -> None:
    """Create the WASM payload directories (dotnet, built-nugets).

    The archive/directory layout is expected to contain a `staging/` folder with
    `dotnet-latest` and `built-nugets` subfolders.
    """
    wasm_dotnet_dir = os.path.join(payload_parent_dir, "dotnet")
    wasm_built_nugets_dir = os.path.join(payload_parent_dir, "built-nugets")

    extract_archive_or_copy(
        browser_wasm_archive_or_dir, wasm_dotnet_dir, prefix="staging/dotnet-latest/"
    )

    extract_archive_or_copy(
        browser_wasm_archive_or_dir, wasm_built_nugets_dir, prefix="staging/built-nugets/"
    )

    _set_permissions_recursive([wasm_dotnet_dir, wasm_built_nugets_dir], mode=0o664) # rw-rw-r--


def build_wasm_coreclr_payload(
    browser_wasm_coreclr_archive_or_dir: str,
    payload_parent_dir: str,
) -> None:
    """Create a WASM CoreCLR-only payload (dotnet).

    This is a self-contained payload for running CoreCLR WASM benchmarks without
    requiring Mono artifacts. The archive/directory layout is expected to contain
    a `staging/` folder with `dotnet-none` (SDK) and
    `microsoft.netcore.app.runtime.browser-wasm` (CoreCLR runtime pack) subfolders.
    """

    wasm_dotnet_dir = os.path.join(payload_parent_dir, "dotnet")

    # Extract the SDK from dotnet-none
    extract_archive_or_copy(
        browser_wasm_coreclr_archive_or_dir, wasm_dotnet_dir, prefix="staging/dotnet-none/"
    )

    # Determine version from the runtime pack directory structure
    runtime_pack_src = os.path.join(
        browser_wasm_coreclr_archive_or_dir if os.path.isdir(browser_wasm_coreclr_archive_or_dir) else "",
        "staging", "microsoft.netcore.app.runtime.browser-wasm", "Release"
    )
    if os.path.isdir(runtime_pack_src):
        # Get version from NuGet.config or infer from directory
        # For now, read version from the SDK's Microsoft.NETCore.App.Ref pack
        ref_pack_parent = os.path.join(wasm_dotnet_dir, "packs", "Microsoft.NETCore.App.Ref")
        if os.path.isdir(ref_pack_parent):
            versions = os.listdir(ref_pack_parent)
            if versions:
                pack_version = versions[0]
                coreclr_pack_dest = os.path.join(
                    wasm_dotnet_dir, "packs", "Microsoft.NETCore.App.Runtime.browser-wasm", pack_version
                )
                extract_archive_or_copy(
                    browser_wasm_coreclr_archive_or_dir,
                    coreclr_pack_dest,
                    prefix="staging/microsoft.netcore.app.runtime.browser-wasm/Release/",
                )
                getLogger().info("Installed CoreCLR browser-wasm runtime pack version %s", pack_version)
        else:
            getLogger().warning("Microsoft.NETCore.App.Ref pack not found – cannot determine version")

    _set_permissions_recursive([wasm_dotnet_dir], mode=0o664)