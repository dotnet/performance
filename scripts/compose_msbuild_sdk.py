"""Compose a Windows or Linux x64 SDK using PerfStar's shipping-package overlay."""

import argparse
import hashlib
import json
import posixpath
import re
import shutil
import subprocess
import tarfile
import tempfile
import urllib.parse
import urllib.request
import zipfile
from pathlib import Path

from msbuild_catalog import ASSET_DEFINITIONS, select_scenarios


LIBRARIES = (
    "Microsoft.Build",
    "Microsoft.Build.Framework",
    "Microsoft.Build.Tasks.Core",
    "Microsoft.Build.Utilities.Core",
    "Microsoft.NET.StringTools",
)
RUNTIME_FILES = (
    "MSBuild.dll",
    "Microsoft.Common.CrossTargeting.targets",
    "Microsoft.Common.CurrentVersion.targets",
    "Microsoft.Common.targets",
    "Microsoft.CSharp.CrossTargeting.targets",
    "Microsoft.CSharp.CurrentVersion.targets",
    "Microsoft.CSharp.targets",
    "Microsoft.Managed.targets",
    "Microsoft.Managed.Before.targets",
    "Microsoft.Managed.After.targets",
    "Microsoft.NET.props",
    "Microsoft.NETFramework.CurrentVersion.props",
    "Microsoft.NETFramework.CurrentVersion.targets",
    "Microsoft.NETFramework.props",
    "Microsoft.NETFramework.targets",
    "Microsoft.VisualBasic.CrossTargeting.targets",
    "Microsoft.VisualBasic.CurrentVersion.targets",
    "Microsoft.VisualBasic.targets",
    "Microsoft.Common.tasks",
)
ASSETS = {name: (item["version"], item["project"]) for name, item in ASSET_DEFINITIONS.items()}


def sha512(path):
    digest = hashlib.sha512()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def verify_archive(path, expected_hash):
    if not re.fullmatch(r"[0-9a-fA-F]{128}", expected_hash):
        raise ValueError("An explicit SHA-512 checksum is required.")
    if sha512(path) != expected_hash.lower():
        raise ValueError(f"SHA-512 mismatch for {path}")


def download_verified(url, destination, expected_hash):
    parsed = urllib.parse.urlsplit(url)
    if parsed.scheme != "https" or not parsed.netloc or parsed.username or parsed.password:
        raise ValueError("Archive downloads require HTTPS URLs without embedded credentials.")
    if not re.fullmatch(r"[0-9a-fA-F]{128}", expected_hash):
        raise ValueError("An explicit SHA-512 checksum is required.")
    destination = Path(destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    with urllib.request.urlopen(url, timeout=120) as response:
        if urllib.parse.urlsplit(response.geturl()).scheme != "https":
            raise ValueError("Archive download redirected to a non-HTTPS URL.")
        with destination.open("wb") as output:
            shutil.copyfileobj(response, output)
    verify_archive(destination, expected_hash)


def resolve_tar_file(entry, members):
    seen = set()
    while entry.issym() or entry.islnk():
        name = posixpath.normpath(entry.name)
        if name in seen:
            raise ValueError(f"Cyclic archive link: {name}")
        seen.add(name)
        target = posixpath.normpath(
            posixpath.join(posixpath.dirname(entry.name), entry.linkname)
            if entry.issym() else entry.linkname)
        if target not in members:
            raise ValueError(f"Archive link target is missing: {target}")
        entry = members[target]
    if not entry.isfile():
        raise ValueError(f"Archive link does not reference a regular file: {entry.name}")
    return entry


def extract_archive(archive, destination):
    destination = Path(destination).resolve()
    destination.mkdir(parents=True, exist_ok=True)
    if not zipfile.is_zipfile(archive):
        with tarfile.open(archive, "r:gz") as package:
            entries = package.getmembers()
            members = {posixpath.normpath(entry.name): entry for entry in entries}
            for entry in entries:
                if not (entry.isfile() or entry.isdir() or entry.issym() or entry.islnk()):
                    raise ValueError(f"Unsupported SDK archive entry: {entry.name}")
                target = (destination / entry.name).resolve()
                if not target.is_relative_to(destination):
                    raise ValueError(f"Archive entry escapes its destination: {entry.name}")
                if entry.issym() or entry.islnk():
                    link_base = target.parent if entry.issym() else destination
                    if not (link_base / entry.linkname).resolve().is_relative_to(destination):
                        raise ValueError(f"Archive link escapes its destination: {entry.name}")
            package.extractall(destination, members=[
                entry for entry in entries if entry.isfile() or entry.isdir()], filter="data")
            # Materialize in-archive file links so Windows composition needs no symlink privilege.
            for entry in entries:
                if entry.issym() or entry.islnk():
                    target = destination / entry.name
                    target.parent.mkdir(parents=True, exist_ok=True)
                    original = resolve_tar_file(entry, members)
                    shutil.copy2(destination / original.name, target)
        return
    with zipfile.ZipFile(archive) as package:
        for entry in package.infolist():
            target = (destination / entry.filename.replace("\\", "/")).resolve()
            if not target.is_relative_to(destination):
                raise ValueError(f"Archive entry escapes its destination: {entry.filename}")
        package.extractall(destination)


def package_files(packages_dir, sdk_major):
    result = {}
    for package_name in (*LIBRARIES, "Microsoft.Build.Runtime"):
        packages = [
            path for path in Path(packages_dir).glob("*.nupkg")
            if re.match(rf"^{re.escape(package_name)}\.\d", path.name)
        ]
        if len(packages) != 1:
            raise ValueError(f"Expected one {package_name} package, found {len(packages)}.")
        prefix = "contentFiles/any/" if package_name == "Microsoft.Build.Runtime" else "lib/"
        names = RUNTIME_FILES if package_name == "Microsoft.Build.Runtime" else (package_name + ".dll",)
        with zipfile.ZipFile(packages[0]) as package:
            tfms = {
                name[len(prefix):].split("/")[0]
                for name in package.namelist() if name.startswith(prefix)
            }
            compatible = [
                tfm for tfm in tfms
                if re.fullmatch(r"net\d+\.\d+", tfm)
                and tuple(map(int, tfm[3:].split("."))) <= (sdk_major, 0)
            ]
            if not compatible:
                raise ValueError(f"{package_name} has no TFM compatible with SDK {sdk_major}.")
            tfm = max(compatible, key=lambda value: tuple(map(int, value[3:].split("."))))
            for name in names:
                result[name] = package.read(f"{prefix}{tfm}/{name}")
    return result


def compose(sdk_archive, sdk_hash, packages_dir, output, source_dir, assets, target_os="windows"):
    if target_os not in ("windows", "linux"):
        raise ValueError(f"Unsupported SDK platform: {target_os}")
    output = Path(output)
    if output.exists():
        raise FileExistsError(f"Use a new composition directory: {output}")
    verify_archive(sdk_archive, sdk_hash)
    source_dir = Path(source_dir)
    commit = subprocess.check_output(
        ["git", "rev-parse", "HEAD"], cwd=source_dir, text=True).strip()
    commit_time = subprocess.check_output(
        ["git", "show", "-s", "--format=%ci", "HEAD"], cwd=source_dir, text=True).strip()
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("MSBuild source must resolve to a full commit SHA.")
    if subprocess.check_output(
            ["git", "status", "--porcelain"], cwd=source_dir, text=True).strip():
        raise ValueError("MSBuild source has uncommitted changes.")
    sdk_root = output / "sdk"
    extract_archive(sdk_archive, sdk_root)
    executable_files = []
    if target_os == "linux":
        with tarfile.open(sdk_archive, "r:gz") as archive:
            entries = archive.getmembers()
            members = {posixpath.normpath(entry.name): entry for entry in entries}
            executable_files = [
                str(Path(entry.name)).replace("\\", "/")
                for entry in entries
                if (entry.isfile() or entry.issym() or entry.islnk())
                and resolve_tar_file(entry, members).mode & 0o111
            ]
    host = sdk_root / ("dotnet.exe" if target_os == "windows" else "dotnet")
    if not host.is_file():
        raise FileNotFoundError(host)
    if target_os == "linux":
        host.chmod(host.stat().st_mode | 0o111)
    sdk_versions = list((sdk_root / "sdk").iterdir())
    if len(sdk_versions) != 1 or not sdk_versions[0].is_dir():
        raise ValueError("The base archive must contain exactly one SDK version.")
    sdk_version = sdk_versions[0].name
    files = package_files(packages_dir, int(sdk_version.split(".")[0]))
    for name, content in files.items():
        (sdk_versions[0] / name).write_bytes(content)
    asset_manifest = {}
    if not assets or set(assets) - ASSETS.keys():
        raise ValueError("Composition requires known, version-pinned workload assets.")
    for name, (archive, expected_hash) in assets.items():
        verify_archive(archive, expected_hash)
        with zipfile.ZipFile(archive) as package:
            required = [ASSETS[name][1], *ASSET_DEFINITIONS[name]["edits"]]
            missing = set(required) - set(package.namelist())
            if missing:
                raise ValueError(f"{name} archive is missing declared paths: {sorted(missing)}")
        destination = output / "assets" / (name + ".zip")
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(archive, destination)
        asset_manifest[name] = {
            "version": ASSETS[name][0], "sha512": expected_hash.lower(),
            "path": f"assets/{name}.zip",
        }
    manifest = {
        "repository": "dotnet/msbuild",
        "branch": "main",
        "commit": commit,
        "commit_time": commit_time,
        "build_configuration": "Release",
        "os": target_os,
        "architecture": "x64",
        "executable_files": executable_files,
        "base_sdk_version": sdk_version,
        "base_sdk_sha512": sdk_hash.lower(),
        "overlay_sha512": {
            name: hashlib.sha512(content).hexdigest() for name, content in files.items()
        },
        "assets": asset_manifest,
    }
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--os", choices=("windows", "linux"), default="windows")
    sdk_source = parser.add_mutually_exclusive_group(required=True)
    sdk_source.add_argument("--sdk-archive")
    sdk_source.add_argument("--sdk-url")
    parser.add_argument("--sdk-sha512", required=True)
    parser.add_argument("--packages-directory", required=True)
    parser.add_argument("--source-directory", required=True)
    parser.add_argument("--output-directory", required=True)
    asset_source = parser.add_mutually_exclusive_group()
    asset_source.add_argument("--asset-manifest")
    asset_source.add_argument("--asset-manifest-url")
    parser.add_argument("--asset-manifest-sha512")
    parser.add_argument("--scenarios", default="")
    for name in ("net8-console-app", "orchard-core"):
        source = parser.add_mutually_exclusive_group()
        source.add_argument(f"--{name}-archive")
        source.add_argument(f"--{name}-url")
        parser.add_argument(f"--{name}-sha512")
    args = parser.parse_args()
    if args.asset_manifest_url and not args.asset_manifest_sha512:
        parser.error("--asset-manifest-url requires --asset-manifest-sha512.")
    with tempfile.TemporaryDirectory(prefix="msbuild-sdk-") as downloads:
        def get_archive(name, archive, url, checksum):
            if archive:
                return archive
            suffix = ".tar.gz" if name == "sdk" and args.os == "linux" else ".zip"
            destination = Path(downloads) / (name + suffix)
            download_verified(url, destination, checksum)
            return destination

        sdk_archive = get_archive("sdk", args.sdk_archive, args.sdk_url, args.sdk_sha512)
        assets = {}
        if args.asset_manifest or args.asset_manifest_url:
            manifest_path = get_archive("asset-manifest", args.asset_manifest,
                                        args.asset_manifest_url, args.asset_manifest_sha512)
            if args.asset_manifest_sha512:
                verify_archive(manifest_path, args.asset_manifest_sha512)
            definitions = json.loads(Path(manifest_path).read_text(encoding="utf-8"))["assets"]
            needed = {item["asset"] for item in select_scenarios(args.scenarios, args.os)}
            for name in sorted(needed):
                entry = definitions[name]
                if entry["version"] != ASSETS[name][0]:
                    raise ValueError(f"Unexpected staged version for {name}.")
                local = entry.get("path")
                if args.asset_manifest_url and local:
                    raise ValueError("Remote asset manifests must use HTTPS URLs, not submitter file paths.")
                if local:
                    local = str((Path(manifest_path).parent / local).resolve())
                archive = get_archive(name, local, entry.get("url"), entry["sha512"])
                assets[name] = (archive, entry["sha512"])
        else:
            for name in ("net8-console-app", "orchard-core"):
                key = name.replace("-", "_")
                checksum = getattr(args, key + "_sha512")
                if not checksum:
                    parser.error("Provide an asset manifest or both legacy archive/checksum pairs.")
                archive = get_archive(name, getattr(args, key + "_archive"),
                                      getattr(args, key + "_url"), checksum)
                assets[name] = (archive, checksum)
        compose(sdk_archive, args.sdk_sha512, args.packages_directory,
                args.output_directory, args.source_directory, assets, args.os)


if __name__ == "__main__":
    main()
