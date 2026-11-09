"""Prepare PerfStar workloads and report untraced MSBuild scenario measurements."""

import json
import os
import shutil
import sys
import time
import xml.etree.ElementTree as ET
from logging import getLogger
from pathlib import Path

from compose_msbuild_sdk import ASSETS, extract_archive, verify_archive
from msbuild_catalog import ASSET_DEFINITIONS, SCENARIOS, CATALOG_SHA256
from msbuild_inputs import verify_sdk
from performance.common import extension
from shared import const
from shared.msbuild_execution import execute_scenario
from shared.startup import StartupWrapper
from shared.util import helixuploaddir


def input_directory() -> Path:
    root = os.environ.get("PERFLAB_MSBUILD_INPUTS")
    if not root:
        payload = os.environ.get("HELIX_CORRELATION_PAYLOAD")
        if not payload:
            raise ValueError("Set PERFLAB_MSBUILD_INPUTS to the composed input directory.")
        root = os.path.join(payload, "msbuild-inputs")
    return Path(root)


def prepare(asset: str) -> None:
    root = input_directory()
    manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
    entry = manifest["assets"][asset]
    if entry["version"] != ASSETS[asset][0]:
        raise ValueError(f"Unexpected {asset} asset version: {entry['version']}")
    archive = root / "assets" / (asset + ".zip")
    verify_archive(archive, entry["sha512"])
    if Path(const.APPDIR).exists():
        raise FileExistsError("Use a fresh work directory; app already exists.")
    extract_archive(archive, const.APPDIR)
    if not (Path(const.APPDIR) / ASSETS[asset][1]).is_file():
        raise FileNotFoundError(ASSETS[asset][1])
    for relative in ASSET_DEFINITIONS[asset]["edits"]:
        if not (Path(const.APPDIR) / relative).is_file():
            raise FileNotFoundError(relative)
    for config in Path(const.APPDIR).rglob("*"):
        if config.is_file() and config.name.lower() == "nuget.config":
            config.unlink()
    configuration = ET.Element("configuration")
    sources = ET.SubElement(configuration, "packageSources")
    ET.SubElement(sources, "clear")
    for index, vendored in enumerate(Path(const.APPDIR).rglob(".perfstar-packages")):
        if vendored.is_dir():
            ET.SubElement(sources, "add", key=f"perfstar-vendored-{index}",
                          value=str(vendored.resolve()))
    feeds = ["dotnet-public", "dotnet-eng", "dotnet-tools"]
    for version in range(6, 12):
        feeds.extend((f"dotnet{version}", f"dotnet{version}-transport"))
    for feed in feeds:
        ET.SubElement(sources, "add", key=feed,
                      value=f"https://pkgs.dev.azure.com/dnceng/public/_packaging/{feed}/nuget/v3/index.json")
    for name in ("fallbackPackageFolders", "disabledPackageSources"):
        ET.SubElement(ET.SubElement(configuration, name), "clear")
    ET.ElementTree(configuration).write(Path(const.APPDIR) / "NuGet.config",
                                        encoding="utf-8", xml_declaration=True)
    (Path(const.APPDIR) / "global.json").write_text(json.dumps({
        "sdk": {"version": manifest["base_sdk_version"], "rollForward": "disable",
                "allowPrerelease": True},
    }), encoding="utf-8")


def measure(asset, dotnet, iterations, timeout, mt, diagnostics=False, linux_trace=False,
            scenario=None, evaluation_collection="off"):
    definition = scenario or {"asset": asset, "kind": "rebuild", "mt": mt}
    return execute_scenario(definition, dotnet, iterations, timeout, diagnostics,
                            linux_trace, evaluation_collection, legacy=scenario is None)


def normalize_future_sdk_timestamps(sdk: Path) -> int:
    cutoff = time.time_ns()
    changed = 0
    for path in Path(sdk).rglob("*"):
        if path.is_file() and not path.is_symlink():
            state = path.stat()
            if state.st_mtime_ns > cutoff:
                os.utime(path, ns=(state.st_atime_ns, cutoff))
                changed += 1
    return changed


def run(traits, iterations, timeout, mt, diagnostics, linux_trace=False,
        scenario_id=None, evaluation_collection="current"):
    if sys.platform not in ("win32", "linux"):
        raise ValueError("MSBuild scenario collection supports Windows and Linux only.")
    root = input_directory()
    dotnet = Path(os.environ.get("DOTNET_ROOT", str(root / "sdk"))) / ("dotnet" + extension())
    if not dotnet.is_file():
        raise FileNotFoundError(dotnet)
    allowed = {(root / "sdk").resolve(), (root.parent / "dotnet").resolve()}
    if dotnet.parent.resolve() not in allowed:
        raise ValueError("DOTNET_ROOT must reference the composed or staged SDK, not a system SDK.")
    manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
    host_os = "windows" if os.name == "nt" else "linux"
    verify_sdk(dotnet.parent, manifest, host_os)
    if os.name == "nt":
        changed = normalize_future_sdk_timestamps(dotnet.parent)
        getLogger().info("Normalized %s future-dated SDK files before setup.", changed)
    if not scenario_id and traits.exename not in ASSET_DEFINITIONS:
        raise ValueError("The generic MSBuild entrypoint requires --scenario-id.")
    startup = StartupWrapper()
    scenario = None
    if scenario_id:
        if scenario_id not in SCENARIOS:
            raise ValueError(f"Unknown or disabled MSBuild scenario: {scenario_id}")
        scenario = SCENARIOS[scenario_id]
        if host_os not in scenario["supported_os"]:
            raise ValueError(f"Scenario {scenario_id} does not support {host_os}.")
        if mt and not scenario["mt"]:
            raise ValueError("--mt conflicts with the selected non-MT scenario.")
        traits.add_traits(
            exename=scenario["asset"],
            scenarioname=traits.scenarioname or scenario_id)
        os.environ["PERFLAB_DATA_MSBuildScenario"] = scenario_id
        os.environ["PERFLAB_DATA_MSBuildScenarioCatalog"] = CATALOG_SHA256
    else:
        evaluation_collection = "off"
    iterations = iterations if iterations is not None else (scenario["iterations"] if scenario else 10)
    os.environ["PERFLAB_DATA_MSBuildIterations"] = str(iterations)
    os.environ["PERFLAB_DATA_EvaluationCollection"] = evaluation_collection
    # Tool preparation must finish before any timed build.
    try:
        measure(traits.exename, dotnet, iterations, timeout, mt, diagnostics, linux_trace,
                scenario, evaluation_collection)
        traits.add_traits(
            apptorun=str(dotnet), startupmetric="BuildWallClock",
            tracename="build-wall-clock.json")
        startup.parsetraces(traits)
    finally:
        upload = helixuploaddir()
        if upload and Path(const.TRACEDIR).is_dir():
            shutil.copytree(const.TRACEDIR, Path(upload) / "traces", dirs_exist_ok=True)
