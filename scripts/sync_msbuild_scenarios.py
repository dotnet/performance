"""Import the enabled PerfStar inventory and generate the performance scenario catalog."""

import argparse
import hashlib
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path


BINDINGS = {
    "aspire": ("Aspire/aspire-src.proj", ["Aspire/src/Aspire.Hosting/ConnectionStringResource.cs"]),
    "azure-core": (
        "AzureCore/sdk/core/Azure.Core/tests/Azure.Core.Tests.csproj",
        ["AzureCore/sdk/core/Azure.Core/src/AzureKeyCredential.cs"]),
    "eshop-on-blazor-modified": (
        "eShopOnBlazor/eShopOnBlazor.sln", ["eShopOnBlazor/src/eShopOnBlazor/Program.cs"]),
    "net8-blazorwasm-app": ("Net8BlazorWasmApp/Net8BlazorWasmApp.csproj", ["Net8BlazorWasmApp/Program.cs"]),
    "net8-class-lib": ("Net8ClassLib/Net8ClassLib.csproj", ["Net8ClassLib/Class1.cs"]),
    "net8-console-app": ("Net8ConsoleApp/Net8ConsoleApp.csproj", ["Net8ConsoleApp/Program.cs"]),
    "orchard-core": (
        "OrchardCore/OrchardCore.sln",
        ["OrchardCore/src/OrchardCore.Modules/OrchardCore.Admin/AdminFilter.cs"]),
    "rar-search-stress": ("RarSearchStress/RarSearchStress.proj", []),
    "rar-conflict-details-stress": ("RarConflictDetailsStress/RarConflictDetailsStress.proj", []),
    "rar-conflict-warning-stress": ("RarConflictWarningStress/RarConflictWarningStress.proj", []),
    "very-large-net8-console-app": ("VeryLargeNet8ConsoleApp.csproj", ["Program.cs"]),
    "xunit": ("Xunit/xunit.slnx", ["Xunit/src/xunit.v3.core/CollectionAttribute.cs"]),
}
KINDS = {
    "rebuild", "rebuild-nr-false", "warm-inc-build", "cold-restore", "restore",
    "warm-restore", "pack-evaluation", "publish-evaluation", "project-query-evaluation",
}


def generate(perfstar_root):
    root = Path(perfstar_root)
    inventory_bytes = (root / "tests/ScenarioInventory.json").read_bytes()
    inventory = json.loads(inventory_bytes)
    if inventory.get("schema_version") != 2:
        raise ValueError("Expected PerfStar scenario inventory schema 2.")
    metadata = json.loads((root / "tests/ScenarioMetadata.json").read_text())
    enabled = [item for item in inventory["scenarios"] if item.get("enabled", True)]
    assets = {}
    scenarios = []
    for item in enabled:
        if item["build_app"] != "dotnet" or item["scenario_kind"] not in KINDS:
            raise ValueError(f"Unimplemented enabled scenario: {item['name']}")
        name = item["asset"]
        if (not re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", item["name"])
                or not item["supported_os"]
                or set(item["supported_os"]) - {"windows", "linux"}):
            raise ValueError(f"Invalid scenario routing: {item['name']}")
        project, edits = BINDINGS[name]
        source_asset = next(asset for asset in metadata["assets"] if asset["name"] == name)
        assets[name] = {
            "version": source_asset["version"],
            "package": source_asset.get("package", name),
            "project": project,
            "edits": edits,
            "arguments": ["/clp:ErrorsOnly"] if name == "rar-conflict-warning-stress" else [],
        }
        iterations = item["configured_iterations"]
        if type(iterations) is not int or not 1 <= iterations <= 2_147_483_647:
            raise ValueError(f"Invalid iteration count: {item['name']}")
        scenarios.append({
            "name": item["name"], "asset": name, "kind": item["scenario_kind"],
            "mt": item["mt"], "iterations": iterations, "supported_os": item["supported_os"],
        })
    if len({item["name"] for item in scenarios}) != len(scenarios):
        raise ValueError("Duplicate scenario identity.")
    catalog = {
        "schema_version": 1,
        "source_inventory_sha256": hashlib.sha256(
            json.dumps(inventory, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest(),
        "assets": dict(sorted(assets.items())),
        "scenarios": sorted(scenarios, key=lambda item: item["name"]),
    }
    project = ET.Element("Project")
    group = ET.SubElement(project, "ItemGroup")
    for item in catalog["scenarios"]:
        entry = ET.SubElement(group, "MSBuildScenario", Include=item["name"])
        if item["supported_os"] == ["windows"]:
            entry.set("Condition", "'$(TargetsWindows)' == 'true'")
        elif item["supported_os"] == ["linux"]:
            entry.set("Condition", "'$(TargetsWindows)' != 'true'")
        ET.SubElement(entry, "SupportedOS").text = ";".join(item["supported_os"])
    ET.indent(project, space="  ")
    return json.dumps(catalog, indent=2) + "\n", ET.tostring(project, encoding="unicode") + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--perfstar-root", required=True)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[1]
    contents = generate(args.perfstar_root)
    paths = (repo / "scripts/msbuild_scenarios.json", repo / "eng/performance/MSBuildScenarios.Generated.props")
    for path, content in zip(paths, contents):
        if args.check:
            if not path.is_file() or path.read_text(encoding="utf-8") != content:
                raise ValueError(f"Generated scenario catalog is stale: {path}")
        else:
            path.write_text(content, encoding="utf-8")


if __name__ == "__main__":
    main()
