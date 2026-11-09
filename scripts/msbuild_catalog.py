"""Load the self-contained, generated PerfStar scenario catalog."""

import hashlib
import json
from pathlib import Path


CATALOG = json.loads(Path(__file__).with_name("msbuild_scenarios.json").read_text(encoding="utf-8"))
if CATALOG.get("schema_version") != 1:
    raise ValueError("Unsupported MSBuild scenario catalog schema.")
CATALOG_SHA256 = hashlib.sha256(
    json.dumps(CATALOG, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()
ASSET_DEFINITIONS = CATALOG["assets"]
SCENARIOS = {item["name"]: item for item in CATALOG["scenarios"]}
if len(SCENARIOS) != len(CATALOG["scenarios"]):
    raise ValueError("Duplicate MSBuild scenario identities in the catalog.")


def select_scenarios(selection="", target_os=None):
    names = [name.strip() for name in selection.replace(";", ",").split(",") if name.strip()]
    if len(names) != len(set(names)):
        raise ValueError("Duplicate scenario selection.")
    unknown = set(names) - SCENARIOS.keys()
    if unknown:
        raise ValueError(f"Unknown or disabled MSBuild scenarios: {', '.join(sorted(unknown))}")
    selected = [SCENARIOS[name] for name in names] if names else list(SCENARIOS.values())
    if target_os:
        unsupported = [item["name"] for item in selected if target_os not in item["supported_os"]]
        if unsupported:
            raise ValueError(f"Scenarios do not support {target_os}: {', '.join(unsupported)}")
    return selected
