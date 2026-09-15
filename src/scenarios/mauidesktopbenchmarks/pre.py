"""
Prepare the MAUI desktop BenchmarkDotNet source payload on the build agent.
"""

import json
import os
import re
import shutil
import xml.etree.ElementTree as ET
from argparse import ArgumentParser, Namespace
from pathlib import Path
from typing import Final

from performance.common import RunCommand, get_repo_root_path, remove_directory
from performance.logger import getLogger, setup_loggers


MAUI_REPOSITORY: Final[str] = "https://github.com/dotnet/maui.git"
FRAMEWORK_BRANCH_PATTERN: Final[re.Pattern[str]] = re.compile(r"^(net\d+\.\d+)")
BENCHMARK_DOTNET_VERSION_TOKEN: Final[str] = "@BENCHMARK_DOTNET_VERSION@"
PLATFORM_MARKER: Final[str] = "<!-- Library: The Real TFMs -->"

MAUI_SPARSE_CHECKOUT_DIRECTORIES: Final[list[str]] = [
    "src/Core",
    "src/Controls",
    "src/Graphics",
    "src/SingleProject",
    "src/Workload",
    "src/Essentials",
    "eng",
    ".config",
]

MAUI_BENCHMARK_PROJECTS: Final[dict[str, str]] = {
    "core": "src/Core/tests/Benchmarks/Core.Benchmarks.csproj",
    "xaml": "src/Controls/tests/Xaml.Benchmarks/Microsoft.Maui.Controls.Xaml.Benchmarks.csproj",
    "graphics": "src/Graphics/tests/Graphics.Benchmarks/Graphics.Benchmarks.csproj",
}


def get_maui_branch(framework: str) -> str:
    match = FRAMEWORK_BRANCH_PATTERN.match(framework)
    if match is None:
        raise ValueError(f"Cannot determine the MAUI branch from framework '{framework}'.")
    return match.group(1)


def get_benchmark_dotnet_version(performance_root: Path) -> str:
    versions_path = performance_root / "eng" / "Versions.props"
    root = ET.parse(versions_path).getroot()
    version = root.find(".//BenchmarkDotNetVersion")
    if version is None or version.text is None or not version.text.strip():
        raise RuntimeError(f"BenchmarkDotNetVersion was not found in {versions_path}.")
    return version.text.strip()


def write_text(path: Path, content: str) -> None:
    with path.open("w", encoding="utf-8", newline="") as output:
        output.write(content)


def insert_project_import(project_path: Path, targets_path: Path) -> None:
    content = project_path.read_text(encoding="utf-8-sig")
    relative_targets = os.path.relpath(targets_path, project_path.parent).replace("/", "\\")
    import_line = f'  <Import Project="{relative_targets}" />'

    if import_line in content:
        return

    closing_project = content.rfind("</Project>")
    if closing_project < 0:
        raise RuntimeError(f"Cannot inject the PerfLab import into {project_path}: </Project> was not found.")

    newline = "\r\n" if "\r\n" in content else "\n"
    content = content[:closing_project] + import_line + newline + content[closing_project:]
    write_text(project_path, content)


def insert_platform_import(directory_props_path: Path, props_path: Path) -> None:
    content = directory_props_path.read_text(encoding="utf-8-sig")
    relative_props = os.path.relpath(props_path, directory_props_path.parent).replace("/", "\\")
    import_line = f'  <Import Project="{relative_props}" />'

    if import_line in content:
        return

    marker_index = content.find(PLATFORM_MARKER)
    if marker_index < 0:
        raise RuntimeError(
            f"Cannot inject the PerfLab platform import into {directory_props_path}: "
            f"'{PLATFORM_MARKER}' was not found."
        )

    property_group_index = content.rfind("<PropertyGroup>", 0, marker_index)
    if property_group_index < 0:
        raise RuntimeError(
            f"Cannot inject the PerfLab platform import into {directory_props_path}: "
            "the platform PropertyGroup was not found."
        )

    line_start = content.rfind("\n", 0, property_group_index) + 1
    newline = "\r\n" if "\r\n" in content else "\n"
    content = content[:line_start] + import_line + newline + newline + content[line_start:]
    write_text(directory_props_path, content)


def clone_maui(branch: str, destination: Path) -> str:
    RunCommand(
        [
            "git",
            "clone",
            "-c",
            "core.longpaths=true",
            "--depth",
            "1",
            "--filter=blob:none",
            "--sparse",
            "--branch",
            branch,
            MAUI_REPOSITORY,
            str(destination),
        ],
        verbose=True,
    ).run()

    RunCommand(
        ["git", "sparse-checkout", "set", *MAUI_SPARSE_CHECKOUT_DIRECTORIES],
        verbose=True,
    ).run(str(destination))

    return RunCommand(
        ["git", "rev-parse", "HEAD"],
        verbose=True,
        echo=False,
    ).run_and_get_stdout(str(destination)).strip()


def stage_overlay(performance_root: Path, maui_root: Path, benchmark_dotnet_version: str) -> Path:
    overlay_source = Path(__file__).parent / "overlay"
    overlay_destination = maui_root / ".perflab"
    shutil.copytree(overlay_source, overlay_destination)

    targets_path = overlay_destination / "MauiPerfLab.targets"
    targets_content = targets_path.read_text(encoding="utf-8")
    if targets_content.count(BENCHMARK_DOTNET_VERSION_TOKEN) != 2:
        raise RuntimeError(
            f"Expected two {BENCHMARK_DOTNET_VERSION_TOKEN} tokens in {targets_path}."
        )
    write_text(
        targets_path,
        targets_content.replace(BENCHMARK_DOTNET_VERSION_TOKEN, benchmark_dotnet_version),
    )

    harness_output = overlay_destination / "harness"
    RunCommand(
        [
            "dotnet",
            "publish",
            str(
                performance_root
                / "src"
                / "harness"
                / "BenchmarkDotNet.Extensions"
                / "BenchmarkDotNet.Extensions.csproj"
            ),
            "-c",
            "Release",
            "-f",
            "net8.0",
            "-o",
            str(harness_output),
        ],
        verbose=True,
    ).run(str(performance_root))

    required_harness_files = [
        "BenchmarkDotNet.Extensions.dll",
        "Reporting.dll",
        "Newtonsoft.Json.dll",
        "Microsoft.DotNet.PlatformAbstractions.dll",
    ]
    missing_files = [
        file_name
        for file_name in required_harness_files
        if not (harness_output / file_name).is_file()
    ]
    if missing_files:
        raise RuntimeError(
            f"The published PerfLab harness is missing required files: {', '.join(missing_files)}."
        )

    return targets_path


def prepare_payload(framework: str, output_directory: Path) -> None:
    log = getLogger()
    performance_root = Path(get_repo_root_path())
    branch = get_maui_branch(framework)
    benchmark_dotnet_version = get_benchmark_dotnet_version(performance_root)

    if output_directory.exists():
        remove_directory(str(output_directory))
    output_directory.mkdir(parents=True)

    maui_root = output_directory / "maui"
    commit = clone_maui(branch, maui_root)
    log.info(f"Resolved dotnet/maui {branch} to {commit}.")

    targets_path = stage_overlay(performance_root, maui_root, benchmark_dotnet_version)
    insert_platform_import(
        maui_root / "Directory.Build.props",
        maui_root / ".perflab" / "MauiPerfLab.props",
    )
    for project in MAUI_BENCHMARK_PROJECTS.values():
        project_path = maui_root / Path(project)
        if not project_path.is_file():
            raise FileNotFoundError(f"MAUI benchmark project was not found: {project_path}.")
        insert_project_import(project_path, targets_path)

    validation_logs = output_directory / "logs"
    validation_logs.mkdir()
    RunCommand(
        [
            "dotnet",
            "build",
            MAUI_BENCHMARK_PROJECTS["graphics"],
            "-c",
            "Release",
            f"-bl:{validation_logs / 'maui-graphics-validation.binlog'}",
        ],
        verbose=True,
    ).run(str(maui_root))
    remove_directory(str(maui_root / "artifacts"))

    manifest = {
        "repository": "dotnet/maui",
        "branch": branch,
        "commit": commit,
        "framework": framework,
        "benchmarkDotNetVersion": benchmark_dotnet_version,
        "projects": MAUI_BENCHMARK_PROJECTS,
    }
    (output_directory / "maui-source.json").write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
    )


def parse_args() -> Namespace:
    parser = ArgumentParser()
    parser.add_argument("--framework", required=True)
    parser.add_argument("--output", required=True, type=Path)
    return parser.parse_args()


if __name__ == "__main__":
    setup_loggers(True)
    arguments = parse_args()
    prepare_payload(arguments.framework, arguments.output.resolve())
