"""
Build and run one staged MAUI desktop BenchmarkDotNet suite on Helix.
"""

import csv
import glob
import json
import os
import shutil
import sys
from argparse import ArgumentParser, Namespace
from io import StringIO
from pathlib import Path
from subprocess import list2cmdline
from typing import TypedDict, cast

from performance.common import RunCommand, helixpayload, helixuploadroot, remove_directory
from performance.logger import setup_loggers


PAYLOAD_DIRECTORY_NAME: str = "mauidesktopbenchmarks"
CORE_EXCLUSION_FILTER: str = "*MauiLoggerWithLoggerMinLevelErrorBenchmarker*"


class MauiSourceManifest(TypedDict):
    repository: str
    branch: str
    commit: str
    framework: str
    benchmarkDotNetVersion: str
    projects: dict[str, str]


def load_manifest(path: Path) -> MauiSourceManifest:
    with path.open(encoding="utf-8") as manifest_file:
        value: object = json.load(manifest_file)

    if not isinstance(value, dict):
        raise RuntimeError(f"Invalid MAUI source manifest at {path}.")

    raw_manifest = cast(dict[object, object], value)
    string_values: dict[str, str] = {}
    for key in ["repository", "branch", "commit", "framework", "benchmarkDotNetVersion"]:
        field_value = raw_manifest.get(key)
        if not isinstance(field_value, str) or not field_value:
            raise RuntimeError(f"Invalid '{key}' value in {path}.")
        string_values[key] = field_value

    projects_value = raw_manifest.get("projects")
    if not isinstance(projects_value, dict):
        raise RuntimeError(f"Invalid 'projects' value in {path}.")

    projects: dict[str, str] = {}
    for name, project in cast(dict[object, object], projects_value).items():
        if not isinstance(name, str) or not isinstance(project, str):
            raise RuntimeError(f"Invalid 'projects' value in {path}.")
        projects[name] = project

    return MauiSourceManifest(
        repository=string_values["repository"],
        branch=string_values["branch"],
        commit=string_values["commit"],
        framework=string_values["framework"],
        benchmarkDotNetVersion=string_values["benchmarkDotNetVersion"],
        projects=projects,
    )


def contains_option(arguments: list[str], option: str) -> bool:
    return any(argument == option or argument.startswith(f"{option}=") for argument in arguments)


def parse_bdn_arguments(value: str) -> list[str]:
    if not value:
        return []
    return next(csv.reader(StringIO(value), delimiter=" ", skipinitialspace=True))


def remove_category_filter(arguments: list[str]) -> list[str]:
    filtered: list[str] = []
    index = 0
    while index < len(arguments):
        argument = arguments[index]
        if argument == "--anyCategories":
            index += 1
            while index < len(arguments) and not arguments[index].startswith("--"):
                index += 1
            continue
        if argument.startswith("--anyCategories="):
            index += 1
            continue
        filtered.append(argument)
        index += 1
    return filtered


def get_bdn_arguments(
    suite: str,
    configured_arguments: list[str],
    extra_arguments: list[str],
) -> list[str]:
    arguments = remove_category_filter(configured_arguments) + extra_arguments
    if not contains_option(arguments, "--filter"):
        arguments[0:0] = ["--filter", "*"]
    if suite == "core" and not contains_option(arguments, "--exclusion-filter"):
        arguments.extend(["--exclusion-filter", CORE_EXCLUSION_FILTER])
    return arguments


def parse_args() -> tuple[Namespace, list[str]]:
    parser = ArgumentParser()
    parser.add_argument("--framework", required=True)
    parser.add_argument("--suite", choices=["core", "xaml", "graphics"], required=True)
    parser.add_argument("--bdn-arguments", default="")
    parser.add_argument("--upload-to-perflab-container", action="store_true")
    return parser.parse_known_args()


def run_suite(arguments: Namespace, extra_bdn_arguments: list[str]) -> None:
    payload_value = helixpayload()
    if not isinstance(payload_value, str) or not payload_value:
        raise RuntimeError("HELIX_CORRELATION_PAYLOAD is not set.")

    payload_root = Path(payload_value)
    prepared_payload = payload_root / "scenarios_out" / PAYLOAD_DIRECTORY_NAME
    manifest_path = prepared_payload / "maui-source.json"
    manifest = load_manifest(manifest_path)

    if arguments.framework != manifest["framework"]:
        raise RuntimeError(
            f"Requested framework '{arguments.framework}' does not match the prepared "
            f"framework '{manifest['framework']}'."
        )

    project_relative_path = manifest["projects"][arguments.suite]
    staged_maui_root = prepared_payload / "maui"
    workitem_root = Path(os.environ.get("HELIX_WORKITEM_ROOT", os.getcwd()))
    maui_root = workitem_root / "maui"
    if maui_root.exists():
        remove_directory(str(maui_root))
    shutil.copytree(staged_maui_root, maui_root)

    project_path = maui_root / Path(project_relative_path)
    if not project_path.is_file():
        raise FileNotFoundError(f"Prepared MAUI benchmark project was not found: {project_path}.")

    dotnet_root = payload_root / "dotnet"
    dotnet_executable = dotnet_root / ("dotnet.exe" if os.name == "nt" else "dotnet")
    if not dotnet_executable.is_file():
        raise FileNotFoundError(f"Prepared dotnet executable was not found: {dotnet_executable}.")

    artifacts_path = workitem_root / "BenchmarkDotNet.Artifacts"
    artifacts_path.mkdir(parents=True, exist_ok=True)

    os.environ["DOTNET_ROOT"] = str(dotnet_root)
    os.environ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
    os.environ["PATH"] = str(dotnet_root) + os.pathsep + os.environ.get("PATH", "")
    os.environ["MAUI_PERFLAB_ARTIFACTS_PATH"] = str(artifacts_path)
    os.environ["PERFLAB_DATA_MAUI_REPOSITORY"] = manifest["repository"]
    os.environ["PERFLAB_DATA_MAUI_BRANCH"] = manifest["branch"]
    os.environ["PERFLAB_DATA_MAUI_COMMIT"] = manifest["commit"]
    if arguments.suite == "xaml":
        os.environ["MAUI_PERFLAB_IN_PROCESS"] = "1"
        os.environ["PERFLAB_DATA_MAUI_TOOLCHAIN"] = "InProcessEmit"
    else:
        os.environ.pop("MAUI_PERFLAB_IN_PROCESS", None)
        os.environ["PERFLAB_DATA_MAUI_TOOLCHAIN"] = "Default"

    upload_root_value = helixuploadroot()
    if isinstance(upload_root_value, str) and upload_root_value:
        shutil.copy2(manifest_path, Path(upload_root_value) / manifest_path.name)

    build_tasks_binlog = workitem_root / "maui-build-tasks.binlog"
    if arguments.suite != "graphics":
        RunCommand(["dotnet", "tool", "restore"], verbose=True).run(str(maui_root))
        RunCommand(
            [
                "dotnet",
                "build",
                "Microsoft.Maui.BuildTasks.slnf",
                "-c",
                "Release",
                "-p:BuildTaskOnlyBuild=true",
                f"-bl:{build_tasks_binlog}",
            ],
            verbose=True,
        ).run(str(maui_root))

        if isinstance(upload_root_value, str) and upload_root_value:
            shutil.copy2(build_tasks_binlog, Path(upload_root_value) / build_tasks_binlog.name)

    bdn_arguments = get_bdn_arguments(
        arguments.suite,
        parse_bdn_arguments(arguments.bdn_arguments),
        extra_bdn_arguments,
    )
    benchmarks_command = [
        sys.executable,
        str(payload_root / "performance" / "scripts" / "benchmarks_ci.py"),
        "-f",
        arguments.framework,
        "-c",
        "Release",
        "--csproj",
        str(project_path),
        "--dotnet-path",
        str(dotnet_root),
        "--bdn-artifacts",
        str(artifacts_path),
        "--bdn-arguments",
        list2cmdline(bdn_arguments),
    ]
    if arguments.upload_to_perflab_container:
        benchmarks_command.append("--upload-to-perflab-container")

    RunCommand(benchmarks_command, verbose=True).run(str(maui_root))

    if arguments.upload_to_perflab_container:
        reports = glob.glob(str(artifacts_path / "**" / "*perf-lab-report.json"), recursive=True)
        if not reports:
            raise RuntimeError("No PerfLab reports were produced for an official MAUI benchmark run.")


if __name__ == "__main__":
    setup_loggers(True)
    parsed_arguments, unknown_arguments = parse_args()
    run_suite(parsed_arguments, unknown_arguments)
