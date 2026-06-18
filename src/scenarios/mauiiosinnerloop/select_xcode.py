#!/usr/bin/env python3
"""select_xcode.py — Select the Xcode matching the iOS SDK version.

Standalone (no repo imports). Prints selected path to stdout; logs to stderr.
"""
import argparse, glob, json, os, re, subprocess, sys
import xml.etree.ElementTree as ET

_MIN_XCODE_MAJOR = 26  # Must match setup_helix.py

def _log(msg): print(msg, file=sys.stderr)

def _from_rollback(scenario_dir):
    """Tier 1: Required Xcode major.minor from rollback_maui.json."""
    path = os.path.join(scenario_dir, "rollback_maui.json")
    if not os.path.isfile(path):
        return None
    try:
        with open(path, encoding="utf-8") as f:
            ios = json.load(f).get("microsoft.net.sdk.ios", "")
        # "26.2.11591-net11-p4/11.0.100-preview.3" → major=26, minor=2
        m = re.match(r"(\d+)\.(\d+)", ios.split("/")[0])
        return (int(m.group(1)), int(m.group(2))) if m else None
    except (json.JSONDecodeError, OSError):
        return None

def _from_sdk_packs(dotnet_root):
    """Tier 2: Required Xcode major.minor from SDK Versions.props."""
    if not dotnet_root:
        return None
    pattern = os.path.join(dotnet_root, "packs", "Microsoft.iOS.Sdk.*",
                           "*", "targets", "Microsoft.iOS.Sdk.Versions.props")
    for props in sorted(glob.glob(pattern), reverse=True):
        try:
            for elem in ET.parse(props).iter("_RecommendedXcodeVersion"):
                m = re.match(r"(\d+)\.(\d+)", elem.text or "")
                if m:
                    return (int(m.group(1)), int(m.group(2)))
        except ET.ParseError:
            continue
    return None

def _parse_dir_version(name):
    """'Xcode_26.2.app' or 'Xcode-26.2.0.app' → (26, 2) or (26, 2, 0)."""
    stem = re.sub(r"^Xcode[_-]", "", name.replace(".app", ""))
    try:
        return tuple(int(p) for p in stem.split("."))
    except ValueError:
        return None

def _find_xcode(required):
    """Find best Xcode in /Applications matching required (major, minor).
    If required is None, picks the highest version >= _MIN_XCODE_MAJOR.
    Also considers /Applications/Xcode.app via its embedded xcodebuild."""
    candidates = []
    for entry in sorted(os.listdir("/Applications")):
        full = os.path.join("/Applications", entry)
        if not (entry.endswith(".app") and os.path.isdir(full)):
            continue
        # Handles Xcode_26.2.app (Helix) and Xcode-26.2.0.app (Xcodes.app)
        if re.match(r"Xcode[_-]\d+", entry):
            ver = _parse_dir_version(entry)
        elif entry == "Xcode.app":
            try:
                xb = os.path.join(full, "Contents/Developer/usr/bin/xcodebuild")
                out = subprocess.run([xb, "-version"], capture_output=True,
                                     text=True, timeout=10).stdout
                m = re.search(r"Xcode\s+(\d+)\.(\d+)", out)
                ver = (int(m.group(1)), int(m.group(2))) if m else None
            except (OSError, subprocess.TimeoutExpired):
                continue
        else:
            continue
        if ver and len(ver) >= 2:
            ok = (ver[0] == required[0] and ver[1] == required[1]) if required \
                else (ver[0] >= _MIN_XCODE_MAJOR)
            if ok:
                candidates.append((full, ver))
    candidates.sort(key=lambda x: x[1])
    return candidates[-1][0] if candidates else None

if __name__ == "__main__":
    p = argparse.ArgumentParser(description="Select Xcode matching iOS SDK")
    p.add_argument("--scenario-dir", default=os.path.dirname(os.path.abspath(__file__)))
    p.add_argument("--dotnet-root", default=os.environ.get("DOTNET_ROOT", ""))
    args = p.parse_args()

    req = _from_rollback(args.scenario_dir)
    if req:
        _log(f"Required Xcode {req[0]}.{req[1]} (from rollback_maui.json)")
    else:
        req = _from_sdk_packs(args.dotnet_root)
        if req:
            _log(f"Required Xcode {req[0]}.{req[1]} (from SDK packs)")
        else:
            _log(f"No version constraint; selecting highest >= {_MIN_XCODE_MAJOR}")
    selected = _find_xcode(req)
    if not selected and req:
        _log(f"No Xcode matching {req[0]}.{req[1]}; trying any >= {_MIN_XCODE_MAJOR}")
        selected = _find_xcode(None)
    if not selected:
        _log("ERROR: No suitable Xcode found in /Applications/")
        sys.exit(1)
    _log(f"Selected: {selected}")
    print(selected)
