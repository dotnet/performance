#!/usr/bin/env python3
"""setup_helix.py — Helix machine setup for MAUI iOS inner loop (macOS).

Runs on the Helix machine BEFORE test.py. Bootstraps the macOS environment
for iOS builds:
  1. Configure DOTNET_ROOT and PATH from the correlation payload SDK.
  2. Select the Xcode version that matches the iOS SDK workload packs.
  3. Validate iOS simulator runtime availability.
  4. Boot the target iOS simulator device.
  5. (Device only) Locate signing artifacts; FAIL the work item if missing.
  6. Install the maui-ios workload.
  7. Restore NuGet packages for the app project.
  8. Disable Spotlight indexing on the workitem directory.

Xcode selection strategy: The iOS SDK packs require a SPECIFIC Xcode version
(e.g. packs 26.2.x need Xcode 26.2). This script derives the required Xcode
major.minor from the ``rollback_maui.json`` file created by pre.py (shipped in
the workitem payload), then selects a matching ``/Applications/Xcode_*.app``.
If rollback_maui.json is absent or unparseable, it falls back to a coarse
``>= _MIN_XCODE_MAJOR`` check. This fails the work item early instead of
wasting 20+ minutes on workload install before hitting _ValidateXcodeVersion.

Device path & infrastructure prerequisites
------------------------------------------
For the physical-device variant (IOS_RID=ios-arm64) the build runs with
``EnableCodeSigning=false`` to keep MSBuild deterministic on Helix; the
post-build ``ioshelper.sign_app_for_device`` re-signs the .app using:

  - ``embedded.mobileprovision`` — staged into HELIX_WORKITEM_ROOT (CWD)
  - ``sign`` tool — symlinked into the venv ``bin/`` so it's on PATH

Both must be present somewhere on the Helix machine (see
``_SIGNING_SEARCH_ROOTS``). The Mac.iPhone.17.Perf queue had Helix machine
prep install them; newer queues like Mac.iPhone.13.Perf currently do NOT,
which is a tracked machine-image gap. When the artifacts are missing,
``find_and_stage_signing_artifacts`` returns False and the work item
FAILS LOUDLY with sys.exit(1) and a ``WORK ITEM FAILED — DEVICE INFRA
UNAVAILABLE`` banner in the console log. We deliberately do NOT mask
the failure as a "skip" / pass: a green build must mean the scenario
actually ran, not that we silently sidestepped a queue gap. The fix is
to provision the queue (Engineering Services ticket), not to flip a
flag in this script.
"""

import json
import os
import platform
import re
import subprocess
import sys
from datetime import datetime

# --- Logging ---
# Follows the same logging pattern as the Android inner loop setup_helix.py:
# structured log file written to HELIX_WORKITEM_UPLOAD_ROOT for post-mortem
# debugging, with key messages also printed to stdout for Helix console output.
_logfile = None


def log(msg, tee=False):
    """Write *msg* with a timestamp to the log file."""
    line = f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] {msg}"
    if _logfile:
        _logfile.write(line + "\n")
        _logfile.flush()
    if tee:
        print(line, flush=True)


def log_raw(msg, tee=False):
    """Write *msg* verbatim (no timestamp) to the log file."""
    if _logfile:
        _logfile.write(msg + "\n")
        _logfile.flush()
    if tee:
        print(msg, flush=True)


def run_cmd(args, check=True, **kwargs):
    """Run a command, logging stdout/stderr. Returns CompletedProcess."""
    log(f"Running: {args}")
    result = subprocess.run(
        args, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
        **kwargs,
    )
    if result.stdout:
        for line in result.stdout.splitlines():
            log_raw(line)
    if check and result.returncode != 0:
        raise subprocess.CalledProcessError(result.returncode, args, result.stdout)
    return result


def _dump_log():
    """Print the full log file to stdout so it appears in Helix console output."""
    if not _logfile:
        return
    _logfile.flush()
    try:
        with open(_logfile.name, "r") as f:
            print(f.read())
    except Exception:
        pass


# --- Setup Steps ---

def print_diagnostics():
    """Log environment variables useful for debugging Helix failures."""
    log_raw("=== DIAGNOSTICS ===", tee=True)
    for var in ["DOTNET_ROOT", "PATH", "DEVELOPER_DIR", "HELIX_WORKITEM_ROOT",
                "HELIX_CORRELATION_PAYLOAD", "TMPDIR", "USER", "HOME"]:
        log_raw(f"  {var}={os.environ.get(var, '<not set>')}")
    log_raw(f"  macOS version:", tee=True)
    run_cmd(["sw_vers"], check=False)
    log_raw(f"  whoami / id:", tee=True)
    run_cmd(["id"], check=False)


# CoreSimulator folders that may need to be owned by the current user before
# 'simctl boot' can succeed. On shared Helix machines, these folders may
# have accumulated state owned by a previous tenant (root, ado_agent, etc.),
# which causes 'simctl boot' to fail with NSCocoaErrorDomain code 513
# ("You don't have permission to save the file ... in the folder
# CoreSimulator") — even for simulators we just created ourselves, because
# the boot writes log/state files into shared CoreSimulator folders we
# don't own.
_CORESIMULATOR_PATHS = [
    "/Library/Developer/CoreSimulator",
    "/Library/Logs/CoreSimulator",
    "~/Library/Developer/CoreSimulator",
    "~/Library/Logs/CoreSimulator",
    "~/Library/Caches/com.apple.CoreSimulator.SimulatorTrampoline",
    "~/Library/Saved Application State/com.apple.CoreSimulator.CoreSimulatorService.savedState",
]

# Subdirectory names underneath /Library/Developer/CoreSimulator that are
# Apple-managed read-only content (mounted runtime images, device-type
# bundles, signed system cryptexes). We have no permission to chown them and
# never need to — `simctl boot` only writes to Devices/ and Logs/. Without
# pruning these names, a recursive chown on /Library/Developer/CoreSimulator
# walks the iOS runtime volume (e.g. .../Volumes/iOS_23E254a/) and emits one
# "Operation not permitted" line per file — ~700k lines / 200+ MB of console
# log spam per Helix work item.
_CORESIMULATOR_PRUNE_NAMES = ("Volumes", "Profiles", "Cryptex", "Images")


def _sudo_chown_pruning(path, owner):
    """``sudo chown -R owner path`` but prune Apple read-only subtrees.

    Equivalent to::

        sudo find <path> \\( -name Volumes -o -name Profiles -o
                             -name Cryptex -o -name Images \\) -prune \\
                          -o -exec chown owner {} +

    Used in place of plain ``chown -R`` for the system-wide CoreSimulator
    paths (see ``_CORESIMULATOR_PRUNE_NAMES`` for why). The plain ``chown -R``
    walks the iOS runtime image and emits hundreds of thousands of
    "Operation not permitted" lines we then dutifully copy into the Helix
    log; this variant skips those read-only mount points at the source.
    """
    name_clause: list[str] = []
    for name in _CORESIMULATOR_PRUNE_NAMES:
        if name_clause:
            name_clause.append("-o")
        name_clause.extend(["-name", name])
    cmd = (
        ["sudo", "find", path, "("] + name_clause + [")", "-prune",
         "-o", "-exec", "chown", owner, "{}", "+"]
    )
    return run_cmd(cmd, check=False)


def fix_coresimulator_permissions():
    """Take ownership of CoreSimulator folders so simctl boot can succeed.

    Helix machines are shared; CoreSimulator folders may have accumulated
    state owned by prior tenants. ``simctl boot`` writes log/state files
    into several Library folders, and even a freshly-created device fails
    to boot if any one of them is not writable by the current user. This
    function chowns the relevant folders to the current user so that boot,
    spawn, and shutdown all work.

    For the system-wide ``/Library/Developer/CoreSimulator`` tree we use
    ``find ... -prune`` rather than a plain ``chown -R`` to avoid recursing
    into Apple's read-only runtime/profile/cryptex mount points (see
    ``_CORESIMULATOR_PRUNE_NAMES``). For per-user paths under ``~/Library``
    we use plain ``chown -R`` because they don't contain those mounts.

    It is best-effort: paths that don't exist are skipped, and chown
    failures are logged as warnings so the rest of setup still runs (the
    boot itself will fail loudly with diagnostics if permissions are still
    wrong).
    """
    log_raw("=== COREsimulator PERMISSIONS ===", tee=True)
    user = os.environ.get("USER") or ""
    if not user:
        # Fallback: query the OS
        result = run_cmd(["whoami"], check=False)
        user = (result.stdout or "").strip()
    if not user:
        log("WARNING: Cannot determine current user; skipping CoreSimulator chown",
            tee=True)
        return
    owner = f"{user}:staff"

    for raw in _CORESIMULATOR_PATHS:
        path = os.path.expanduser(raw)
        if not os.path.exists(path):
            log(f"  skip (not present): {path}")
            continue
        # Only the system-wide /Library/Developer/CoreSimulator tree contains
        # the Apple-mounted read-only volumes that explode chown -R output.
        if path == "/Library/Developer/CoreSimulator":
            log(f"  sudo find {path} (prune {_CORESIMULATOR_PRUNE_NAMES}) "
                f"-exec chown {owner}", tee=True)
            result = _sudo_chown_pruning(path, owner)
        else:
            log(f"  sudo chown -R {owner} {path}", tee=True)
            result = run_cmd(["sudo", "chown", "-R", owner, path], check=False)
        if result.returncode != 0:
            log(f"WARNING: chown on {path} failed (exit {result.returncode}). "
                f"simctl boot may still hit permission errors.", tee=True)
        # Show ownership for diagnostics (top-level only — ls -la is non-recursive)
        run_cmd(["ls", "-la", path], check=False)


def setup_dotnet(correlation_payload):
    """Set DOTNET_ROOT and PATH to point at the SDK in the correlation payload.

    Returns the path to the dotnet executable.
    """
    dotnet_root = os.path.join(correlation_payload, "dotnet")
    dotnet_exe = os.path.join(dotnet_root, "dotnet")

    os.environ["DOTNET_ROOT"] = dotnet_root
    os.environ["PATH"] = dotnet_root + ":" + os.environ.get("PATH", "")

    log(f"DOTNET_ROOT={dotnet_root}", tee=True)
    run_cmd([dotnet_exe, "--version"], check=False)
    return dotnet_exe


# Coarse pre-filter: packs are not yet installed at this point (workload
# install happens at step 5), so we can't read _RecommendedXcodeVersion from
# the pack's Versions.props. This >= 26 check catches clearly incompatible
# machines early. The SDK's _ValidateXcodeVersion target performs the exact
# version check at build time.
_MIN_XCODE_MAJOR = 26


def _parse_xcode_version(output):
    """Parse the major.minor version tuple from ``xcodebuild -version`` output.

    Expects a line like "Xcode 26.2" or "Xcode 15.0.1".
    Returns (major, minor) as ints, or None if parsing fails.
    """
    m = re.search(r"Xcode\s+(\d+)\.(\d+)", output or "")
    if m:
        return int(m.group(1)), int(m.group(2))
    return None


def _get_required_xcode_version(workitem_root):
    """Derive the required Xcode major.minor from rollback_maui.json.

    The rollback file is created by pre.py and shipped in the workitem payload.
    It contains e.g.::

        {"microsoft.net.sdk.ios": "26.2.11591-net11-p4/11.0.100-preview.3"}

    The version prefix ``26.2`` IS the required Xcode version (major.minor).
    Returns ``(major, minor)`` as ints, or ``None`` if the file is absent or
    unparseable.
    """
    rollback_path = os.path.join(workitem_root, "rollback_maui.json")
    if not os.path.isfile(rollback_path):
        log("rollback_maui.json not found — cannot derive required Xcode version")
        return None

    try:
        with open(rollback_path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except (json.JSONDecodeError, OSError) as e:
        log(f"WARNING: Failed to read rollback_maui.json: {e}")
        return None

    ios_value = data.get("microsoft.net.sdk.ios")
    if not ios_value:
        log("WARNING: rollback_maui.json has no 'microsoft.net.sdk.ios' key")
        return None

    # Value format: "26.2.11591-net11-p4/11.0.100-preview.3"
    # Extract version prefix before the "/" (band), then parse major.minor
    version_part = ios_value.split("/")[0]  # "26.2.11591-net11-p4"
    m = re.match(r"(\d+)\.(\d+)", version_part)
    if not m:
        log(f"WARNING: Could not parse major.minor from iOS SDK version '{version_part}'")
        return None

    major, minor = int(m.group(1)), int(m.group(2))
    log(f"Required Xcode version from rollback_maui.json: {major}.{minor} "
        f"(from '{ios_value}')", tee=True)
    return (major, minor)


def _parse_xcode_dir_version(dirname):
    """Parse version components from an Xcode directory name.

    ``"Xcode_26.2.app"`` → ``(26, 2)``, ``"Xcode_26.2.1.app"`` → ``(26, 2, 1)``.
    Returns a tuple of ints, or ``None`` if parsing fails.
    """
    # Strip "Xcode_" prefix and ".app" suffix, then split on "."
    stem = dirname.replace("Xcode_", "").replace(".app", "")
    parts = stem.split(".")
    try:
        return tuple(int(p) for p in parts)
    except ValueError:
        return None


def select_xcode(workitem_root):
    """Select the Xcode version that matches the iOS SDK workload packs.

    Derives the required Xcode major.minor from rollback_maui.json (created by
    pre.py). If the system-default Xcode already matches, no switching is
    needed. Otherwise, searches /Applications/Xcode_*.app for a matching
    version and activates it via ``sudo xcode-select -s``. Falls back to the
    coarse ``>= _MIN_XCODE_MAJOR`` check when rollback_maui.json is absent.
    """
    log_raw("=== XCODE SELECTION ===", tee=True)

    required = _get_required_xcode_version(workitem_root)

    # Log the current default before any changes
    run_cmd(["xcode-select", "-p"], check=False)
    result = run_cmd(["xcodebuild", "-version"], check=False)

    current = _parse_xcode_version(result.stdout)
    if current is None:
        log("ERROR: Could not parse Xcode version from xcodebuild output. "
            "Ensure Xcode is installed.", tee=True)
        _dump_log()
        sys.exit(1)

    cur_major, cur_minor = current
    log(f"Default Xcode version: {cur_major}.{cur_minor}", tee=True)

    # --- Precise matching mode (rollback file provides required version) ---
    if required is not None:
        req_major, req_minor = required

        if cur_major == req_major and cur_minor == req_minor:
            log(f"Default Xcode {cur_major}.{cur_minor} matches required "
                f"{req_major}.{req_minor} — no switching needed.", tee=True)
            return

        # Default doesn't match — search for a matching Xcode_*.app
        log(f"Default Xcode {cur_major}.{cur_minor} does not match required "
            f"{req_major}.{req_minor}. Searching /Applications/Xcode_*.app...",
            tee=True)

        matching_apps = []
        for entry in os.listdir("/Applications"):
            if not (entry.startswith("Xcode_") and entry.endswith(".app")):
                continue
            if not os.path.isdir(os.path.join("/Applications", entry)):
                continue
            ver = _parse_xcode_dir_version(entry)
            if ver and len(ver) >= 2 and ver[0] == req_major and ver[1] == req_minor:
                matching_apps.append((entry, ver))

        if not matching_apps:
            # List all available Xcode installations for diagnostics
            all_xcodes = sorted(
                e for e in os.listdir("/Applications")
                if e.startswith("Xcode") and e.endswith(".app")
            )
            log(f"ERROR: No Xcode matching {req_major}.{req_minor} found. "
                f"Available: {all_xcodes}", tee=True)
            log(f"The iOS SDK packs require Xcode {req_major}.{req_minor}. "
                "Install the matching Xcode or update the workload pin.",
                tee=True)
            _dump_log()
            sys.exit(1)

        # If multiple match (e.g. Xcode_26.2.app and Xcode_26.2.1.app),
        # pick the highest patch version
        matching_apps.sort(key=lambda x: x[1])
        selected_name, selected_ver = matching_apps[-1]
        selected = os.path.join("/Applications", selected_name)

        log(f"Found matching Xcode installations: "
            f"{[name for name, _ in matching_apps]}", tee=True)
        log(f"Selecting: {selected}", tee=True)
        run_cmd(["sudo", "xcode-select", "-s", selected], check=False)

        # Log the new state after switching
        run_cmd(["xcode-select", "-p"], check=False)
        result = run_cmd(["xcodebuild", "-version"], check=False)

        version = _parse_xcode_version(result.stdout)
        if version:
            log(f"Xcode version after switching: {version[0]}.{version[1]}",
                tee=True)

        # Validate the switch actually produced the required version
        if version is None or version[0] != req_major or version[1] != req_minor:
            effective = f"{version[0]}.{version[1]}" if version else "unknown"
            log(f"ERROR: Xcode switch failed — active version is {effective} "
                f"but {req_major}.{req_minor} is required. "
                f"sudo xcode-select -s may have failed silently.", tee=True)
            _dump_log()
            sys.exit(1)
        return

    # --- Fallback mode (no rollback file — coarse >= check) ---
    log("WARNING: No rollback_maui.json — falling back to coarse "
        f">= {_MIN_XCODE_MAJOR}.0 check", tee=True)

    if cur_major >= _MIN_XCODE_MAJOR:
        log(f"Default Xcode {cur_major}.{cur_minor} meets minimum "
            f"({_MIN_XCODE_MAJOR}.0) — no switching needed.", tee=True)
        return

    # Default Xcode is too old — search for a newer Xcode_*.app
    log(f"Default Xcode {cur_major}.{cur_minor} is below minimum "
        f"({_MIN_XCODE_MAJOR}.0). "
        "Searching for a newer /Applications/Xcode_*.app...", tee=True)

    xcode_apps = sorted(
        (
            entry
            for entry in os.listdir("/Applications")
            if entry.startswith("Xcode_") and entry.endswith(".app")
            and os.path.isdir(os.path.join("/Applications", entry))
        ),
        key=lambda name: [
            int(x) for x in re.findall(r"\d+", name.replace(".app", ""))
        ],
    )

    version = current
    if xcode_apps:
        selected = os.path.join("/Applications", xcode_apps[-1])
        log(f"Found Xcode installations: {xcode_apps}", tee=True)
        log(f"Selecting highest version: {selected}", tee=True)
        run_cmd(["sudo", "xcode-select", "-s", selected], check=False)

        # Log the new state after switching
        run_cmd(["xcode-select", "-p"], check=False)
        result = run_cmd(["xcodebuild", "-version"], check=False)

        version = _parse_xcode_version(result.stdout)
        if version:
            log(f"Xcode version after switching: {version[0]}.{version[1]}",
                tee=True)
    else:
        log("WARNING: No /Applications/Xcode_*.app found.", tee=True)

    # Final validation — fail fast if still below minimum
    if version is None or version[0] < _MIN_XCODE_MAJOR:
        effective = f"{version[0]}.{version[1]}" if version else "unknown"
        log(f"ERROR: Xcode {effective} is still below the minimum required "
            f"version ({_MIN_XCODE_MAJOR}.0). The iOS SDK packs require "
            f"Xcode >= {_MIN_XCODE_MAJOR}.0. Failing early to avoid wasting "
            "time on workload install.", tee=True)
        _dump_log()
        sys.exit(1)


def validate_simulator_runtimes():
    """Check that iOS simulator runtimes are available on this machine."""
    log_raw("=== SIMULATOR RUNTIME VALIDATION ===", tee=True)
    result = run_cmd(["xcrun", "simctl", "list", "runtimes"], check=False)
    if result.returncode != 0:
        log("WARNING: 'xcrun simctl list runtimes' failed. "
            "Simulator may not work.", tee=True)
        return

    # Check that at least one iOS runtime is listed
    ios_runtimes = [line for line in (result.stdout or "").splitlines()
                    if "iOS" in line]
    if ios_runtimes:
        log(f"Found {len(ios_runtimes)} iOS runtime(s):", tee=True)
        for rt in ios_runtimes:
            log(f"  {rt.strip()}")
    else:
        log("WARNING: No iOS simulator runtimes found. "
            "Simulator-based testing will fail.", tee=True)


# Preferred iPhone device types in priority order. Used to pick a device type
# when creating our own simulator. We prefer plain models over Pro/Pro Max for
# predictable performance characteristics, and skip mini/SE variants because
# their availability across Xcode versions is inconsistent. The list is wide
# enough to survive Xcode/runtime upgrades on Helix machines.
_PREFERRED_IPHONE_MODELS = [
    "iPhone 17", "iPhone 17 Pro", "iPhone 17 Pro Max", "iPhone Air",
    "iPhone 16", "iPhone 16 Pro", "iPhone 16 Pro Max", "iPhone 16 Plus",
    "iPhone 15", "iPhone 15 Pro", "iPhone 15 Pro Max", "iPhone 15 Plus",
    "iPhone 14", "iPhone 14 Pro", "iPhone 14 Pro Max", "iPhone 14 Plus",
    "iPhone 13", "iPhone 13 Pro", "iPhone 13 Pro Max",
    "iPhone 12", "iPhone 12 Pro", "iPhone 12 Pro Max",
    "iPhone 11", "iPhone 11 Pro", "iPhone 11 Pro Max",
]


def _find_ios_runtime():
    """Return the (identifier, version) of the highest-version available iOS runtime.

    Returns ``(None, None)`` if no iOS runtime is available.
    """
    result = run_cmd(["xcrun", "simctl", "list", "runtimes", "-j"], check=False)
    if result.returncode != 0 or not result.stdout:
        return (None, None)

    try:
        data = json.loads(result.stdout)
    except json.JSONDecodeError as e:
        log(f"WARNING: Failed to parse runtimes JSON: {e}")
        return (None, None)

    available = []
    for rt in data.get("runtimes", []):
        if not rt.get("isAvailable"):
            continue
        name = rt.get("name", "")
        if "iOS" not in name:
            continue
        identifier = rt.get("identifier", "")
        version = rt.get("version", "0")
        available.append((identifier, version, name))

    if not available:
        return (None, None)

    # Sort by version (descending) so the latest iOS runtime wins. version
    # strings like "26.4.1" sort correctly with packaging-style key, but we
    # only need a stable preference for the latest, so simple tuple sort works.
    def _version_key(item):
        try:
            return tuple(int(p) for p in item[1].split(".") if p.isdigit())
        except Exception:
            return (0,)

    available.sort(key=_version_key, reverse=True)
    rt_id, rt_ver, rt_name = available[0]
    log(f"Selected iOS runtime: {rt_name} (id={rt_id}, version={rt_ver})", tee=True)
    return (rt_id, rt_ver)


def _find_iphone_device_type():
    """Return the identifier of a usable iPhone simulator device type.

    Walks ``_PREFERRED_IPHONE_MODELS`` in order and returns the identifier of
    the first model present in ``simctl list devicetypes -j``. Returns ``None``
    if none of the preferred models are available.
    """
    result = run_cmd(["xcrun", "simctl", "list", "devicetypes", "-j"], check=False)
    if result.returncode != 0 or not result.stdout:
        return None

    try:
        data = json.loads(result.stdout)
    except json.JSONDecodeError as e:
        log(f"WARNING: Failed to parse devicetypes JSON: {e}")
        return None

    by_name = {dt.get("name", ""): dt.get("identifier", "")
               for dt in data.get("devicetypes", [])}
    for model in _PREFERRED_IPHONE_MODELS:
        if model in by_name and by_name[model]:
            log(f"Selected iPhone device type: {model} (id={by_name[model]})", tee=True)
            return by_name[model]

    log(f"WARNING: None of the preferred iPhone models are installed. "
        f"Available iPhones: {[n for n in by_name if n.startswith('iPhone')]}",
        tee=True)
    return None


def _unique_simulator_name():
    """Build a per-workitem unique simulator name to avoid collisions.

    Each Helix work item gets its own name so concurrent work items on the
    same machine never share or delete each other's simulators.
    """
    workitem_id = (os.environ.get("HELIX_WORKITEM_ID") or
                   os.environ.get("HELIX_WORKITEM_FRIENDLYNAME") or
                   "")
    if workitem_id:
        # Strip filesystem-unfriendly chars from friendly names like
        # "Inner Loop Simulator - MAUI iOS Inner Loop"
        suffix = re.sub(r"[^A-Za-z0-9_-]+", "-", workitem_id).strip("-")[:48]
    else:
        # Fallback: epoch + PID. Still unique within a single machine session.
        suffix = f"{int(datetime.now().timestamp())}-{os.getpid()}"
    return f"PerfTest-iPhone-{suffix}"


def _write_sim_udid(workitem_root, udid):
    """Persist the UDID of the simulator we created so test.py / post.py can
    read it. Without this, ioshelper would fall back to "first booted device"
    which is unsafe if any other simulator happens to be booted.
    """
    path = os.path.join(workitem_root, "sim_udid.txt")
    try:
        with open(path, "w", encoding="utf-8") as f:
            f.write(udid + "\n")
        log(f"Wrote simulator UDID to {path}", tee=True)
    except OSError as e:
        log(f"WARNING: Could not write {path}: {e}", tee=True)


def _simulator_preflight(udid):
    """Verify the booted simulator is actually usable for spawning processes.

    The CoreSimulator daemon can list a device as "Booted" while still
    refusing to spawn agents (for example when the device's underlying data
    container is owned by a different user — the failure mode that breaks
    actool's AssetCatalogSimulatorAgent during ``dotnet build``).
    Running ``simctl spawn <udid> /usr/bin/true`` proves end-to-end that we
    can launch processes inside the simulator under the current user.
    Raises ``SystemExit(1)`` if the spawn fails.
    """
    log(f"Preflight: spawning /usr/bin/true inside simulator {udid}", tee=True)
    result = run_cmd(
        ["xcrun", "simctl", "spawn", udid, "/usr/bin/true"],
        check=False,
    )
    if result.returncode != 0:
        log(f"ERROR: simctl spawn preflight failed (exit {result.returncode}). "
            f"CoreSimulator cannot launch processes in this device. "
            f"actool / mlaunch will also fail.", tee=True)
        _dump_log()
        sys.exit(1)
    log("Preflight OK — simulator can spawn processes.", tee=True)


def _sweep_leaked_perftest_simulators():
    """Shutdown and delete any leaked ``PerfTest-iPhone-*`` simulators from
    previous workitems on this Helix machine.

    Each booted simulator forks ~150–200 child processes (launchd_sim plus
    a long tail of system daemons). A handful of leaked simulators is enough
    to push the per-user process count past the macOS ``maxUserProcs``
    rlimit (typically 1333), at which point ``simctl boot`` for THIS work
    item fails with NSPOSIXErrorDomain code 67 / "Unable to boot device
    due to insufficient system resources". Leaks happen when a previous
    workitem's post.py crashed before reaching delete_simulator(), or when
    the workitem was killed mid-run by a Helix timeout.

    We're conservative: we only touch devices whose name starts with the
    well-known ``PerfTest-iPhone-`` prefix used by ``_unique_simulator_name``,
    so we never disturb shared queue infrastructure or non-perf workitems.
    Best-effort — failures are logged but don't abort setup.
    """
    log_raw("=== LEAKED PERFTEST SIMULATOR SWEEP ===", tee=True)
    try:
        listing = run_cmd(["xcrun", "simctl", "list", "devices", "-j"],
                          check=False)
    except Exception as e:
        log(f"WARNING: could not list simulators for sweep: {e}", tee=True)
        return
    if listing.returncode != 0 or not listing.stdout:
        log(f"WARNING: simctl list devices -j failed (exit {listing.returncode}); "
            "skipping sweep.", tee=True)
        return
    try:
        devices_by_runtime = json.loads(listing.stdout).get("devices", {})
    except (json.JSONDecodeError, AttributeError) as e:
        log(f"WARNING: could not parse simctl device JSON for sweep: {e}",
            tee=True)
        return

    leaked = []
    for runtime_devices in devices_by_runtime.values():
        for dev in runtime_devices or []:
            name = (dev.get("name") or "").strip()
            udid = (dev.get("udid") or "").strip()
            if name.startswith("PerfTest-iPhone-") and udid:
                leaked.append((name, udid, dev.get("state", "Unknown")))

    if not leaked:
        log("No leaked PerfTest-iPhone-* simulators found.", tee=True)
        return

    log(f"Found {len(leaked)} leaked PerfTest-iPhone-* simulator(s); "
        "shutting down and deleting...", tee=True)
    for name, udid, state in leaked:
        log(f"  Cleaning {name} ({udid}, state={state})", tee=True)
        # shutdown is best-effort and idempotent on already-shutdown devices
        run_cmd(["xcrun", "simctl", "shutdown", udid], check=False)
        run_cmd(["xcrun", "simctl", "delete", udid], check=False)


def create_and_boot_simulator(workitem_root):
    """Create a fresh iPhone simulator under the current user's CoreSimulator
    namespace and boot it.

    Why a fresh device every run:
      Helix machines accumulate simulator devices across runs. When those
      pre-existing devices are owned by a different user (root, or a previous
      tenant), ``simctl boot`` on them fails with NSCocoaErrorDomain code 513
      ("You don't have permission to save the file ... in the folder
      CoreSimulator"). Creating our own device with ``simctl create`` writes
      to ``~/Library/Developer/CoreSimulator/Devices/`` of THIS user, so we
      always have a writable device to boot.

    Why both simulator AND device jobs need this:
      ``dotnet build`` for ios-arm64 invokes ``actool`` which spawns
      ``AssetCatalogSimulatorAgent`` via CoreSimulator to compile the asset
      catalog. Without a writable, booted simulator under our user, that
      spawn fails and the build aborts.

    Returns the UDID of the booted device. Persists it to
    ``<workitem_root>/sim_udid.txt`` for test.py / post.py to read.
    Exits with code 1 on any unrecoverable failure.
    """
    log_raw("=== SIMULATOR CREATE & BOOT ===", tee=True)

    # Defensive cleanup: kill any leaked PerfTest-iPhone-* simulators from
    # previous workitems before we try to boot our own. Without this, on a
    # machine where prior post.py runs crashed or were killed by timeout,
    # the per-user process count stays pinned just below maxUserProcs and
    # this workitem's simctl boot fails with exit 67. Only touches devices
    # we own (PerfTest-iPhone-* naming) so other workitems are unaffected.
    _sweep_leaked_perftest_simulators()

    runtime_id, _ = _find_ios_runtime()
    if not runtime_id:
        log("ERROR: No available iOS simulator runtime found.", tee=True)
        run_cmd(["xcrun", "simctl", "list", "runtimes"], check=False)
        _dump_log()
        sys.exit(1)

    device_type_id = _find_iphone_device_type()
    if not device_type_id:
        log("ERROR: No usable iPhone simulator device type found.", tee=True)
        run_cmd(["xcrun", "simctl", "list", "devicetypes"], check=False)
        _dump_log()
        sys.exit(1)

    name = _unique_simulator_name()
    log(f"Creating simulator: name='{name}' type={device_type_id} runtime={runtime_id}",
        tee=True)
    create = run_cmd(
        ["xcrun", "simctl", "create", name, device_type_id, runtime_id],
        check=False,
    )
    if create.returncode != 0 or not create.stdout:
        log(f"ERROR: simctl create failed (exit {create.returncode}).", tee=True)
        _dump_log()
        sys.exit(1)

    # `simctl create` prints just the UDID on stdout (one line).
    udid = (create.stdout or "").strip().splitlines()[-1].strip()
    if not re.match(r"^[0-9A-Fa-f-]{36}$", udid):
        log(f"ERROR: simctl create produced unexpected output: {create.stdout!r}",
            tee=True)
        _dump_log()
        sys.exit(1)
    log(f"Created simulator UDID: {udid}", tee=True)

    log(f"Booting simulator {udid}", tee=True)
    boot = run_cmd(["xcrun", "simctl", "boot", udid], check=False)
    if boot.returncode != 0 and "Booted" not in (boot.stdout or ""):
        # On shared Helix machines, even a fresh device can fail to boot
        # because CoreSimulator writes log/state files into shared folders
        # that may still have foreign ownership the chown didn't catch.
        # Run another chown then retry once. If the retry still fails, give
        # up — the diagnostics from chown should explain why.
        log(f"WARNING: simctl boot failed (exit {boot.returncode}). "
            f"Re-running CoreSimulator chown and retrying once.", tee=True)
        fix_coresimulator_permissions()
        boot = run_cmd(["xcrun", "simctl", "boot", udid], check=False)
        if boot.returncode != 0 and "Booted" not in (boot.stdout or ""):
            log(f"ERROR: simctl boot retry failed (exit {boot.returncode}).",
                tee=True)
            _dump_log()
            sys.exit(1)
        log("Boot retry succeeded after chown.", tee=True)

    # Wait for the simulator to finish booting. Without this, downstream
    # actool / mlaunch race against the boot and intermittently fail.
    log(f"Waiting for boot to complete (simctl bootstatus -b)", tee=True)
    bootstatus = run_cmd(["xcrun", "simctl", "bootstatus", udid, "-b"], check=False)
    if bootstatus.returncode != 0:
        log(f"WARNING: bootstatus reported exit {bootstatus.returncode}; "
            f"continuing anyway and relying on preflight.", tee=True)

    _simulator_preflight(udid)
    _write_sim_udid(workitem_root, udid)

    log("Currently booted devices:")
    run_cmd(["xcrun", "simctl", "list", "devices", "booted"], check=False)
    return udid


def _run_workload_cmd(args, timeout_seconds):
    """Run a workload install command with a timeout.

    Returns a CompletedProcess. If the command times out, kills the process
    and returns a synthetic CompletedProcess with returncode=-1.

    The dotnet CLI can hang for hours when NuGet feeds are slow or broken
    (internal download retries). A timeout ensures the fallback retry has
    time to run within the Helix work item timeout.
    """
    try:
        return run_cmd(args, check=False, timeout=timeout_seconds)
    except subprocess.TimeoutExpired as e:
        log(f"WARNING: Command timed out after {timeout_seconds}s — killed", tee=True)
        # Log tail of any partial output captured before the kill
        partial = getattr(e, 'output', '') or ''
        if partial:
            if not isinstance(partial, str):
                partial = partial.decode('utf-8', errors='replace')
            for line in partial.splitlines()[-20:]:
                log_raw(line)
        return subprocess.CompletedProcess(args, returncode=-1)


# Cap each workload install attempt so there's time for the fallback retry
# within the Helix work item timeout (2:30). Without this, the dotnet CLI
# can hang for 2+ hours on NuGet download failures (internal retries).
_WORKLOAD_INSTALL_TIMEOUT = 1200  # 20 minutes per attempt

# NuGet restore should complete much faster than workload install, but can
# still hang on dead feeds. 10 minutes is generous; prevents consuming the
# entire Helix work item timeout (2:30) on a hung restore.
_RESTORE_TIMEOUT = 600  # 10 minutes


def install_workload(ctx):
    """Install the maui-ios workload using the shipped SDK.

    Uses the rollback file created by pre.py to pin to the exact workload
    version (latest nightly packs). Falls back to a plain install if no
    rollback file is present. Always uses --ignore-failed-sources because
    dead NuGet feeds are common in CI.

    Each attempt is capped at _WORKLOAD_INSTALL_TIMEOUT seconds to prevent
    slow NuGet downloads from consuming the entire Helix work item timeout.

    Manifest-patching dependency (pre.py):
        When the iOS workload manifest references net10.0 cross-targeting
        packs that don't exist on NuGet, pre.py patches the manifest to
        remove those entries and places the patched files inside the SDK tree
        at ``$DOTNET_ROOT/sdk-manifests/{band}/microsoft.net.sdk.ios/{ver}/``.
        The SDK tree ships to Helix as the correlation payload, so the
        patched manifest is already on disk. The ``--skip-manifest-update``
        retry below tells the CLI to use on-disk manifests instead of
        downloading new ones, which picks up the patched manifest and only
        installs packs that actually exist.
    """
    log_raw("=== WORKLOAD INSTALL ===", tee=True)

    rollback_file = os.path.join(ctx["workitem_root"], "rollback_maui.json")
    nuget_config = ctx["nuget_config"]

    install_args = [
        ctx["dotnet_exe"], "workload", "install", "maui-ios",
    ]

    if os.path.isfile(rollback_file):
        log(f"Using rollback file: {rollback_file}")
        install_args.extend(["--from-rollback-file", rollback_file])
    else:
        log("No rollback_maui.json found — installing latest maui-ios workload")

    if os.path.isfile(nuget_config):
        install_args.extend(["--configfile", nuget_config])

    # Dead NuGet feeds are common in CI — always tolerate failures
    install_args.append("--ignore-failed-sources")

    result = _run_workload_cmd(install_args, _WORKLOAD_INSTALL_TIMEOUT)
    if result.returncode != 0 and os.path.isfile(rollback_file):
        # The --from-rollback-file attempt typically fails for one of two
        # reasons:
        #   1. NuGet version skew — packs referenced by a new manifest have
        #      not propagated to all NuGet feeds yet ("package NOT FOUND").
        #   2. net10.0 cross-targeting packs — the manifest references packs
        #      that don't exist on any feed (upstream coherency issue).
        #
        # In case (2), pre.py's manifest-patching fallback has already
        # placed a patched manifest (with net10.0 entries removed) inside
        # the SDK tree, which was shipped here as the correlation payload.
        # --skip-manifest-update tells the CLI to use that on-disk manifest
        # instead of downloading a new one, so it only resolves packs that
        # actually exist.
        log(f"WARNING: Workload install with rollback file failed "
            f"(exit code {result.returncode})", tee=True)
        log("Retrying with --skip-manifest-update (uses on-disk manifest "
            "from correlation payload)...", tee=True)

        retry_args = [
            ctx["dotnet_exe"], "workload", "install", "maui-ios",
            "--skip-manifest-update",
        ]
        if os.path.isfile(nuget_config):
            retry_args.extend(["--configfile", nuget_config])
        retry_args.append("--ignore-failed-sources")

        result = _run_workload_cmd(retry_args, _WORKLOAD_INSTALL_TIMEOUT)

    if result.returncode != 0:
        log(f"WORKLOAD INSTALL FAILED (exit code {result.returncode})", tee=True)
        _dump_log()
        sys.exit(1)

    log("maui-ios workload installed successfully")


def restore_packages(ctx):
    """Restore NuGet packages for the app project.

    Uses --ignore-failed-sources and /p:NuGetAudit=false to handle dead
    feeds and avoid audit warnings that slow down restore.
    """
    log_raw("=== NUGET RESTORE ===", tee=True)

    csproj = ctx["csproj"]
    if not os.path.isfile(csproj):
        log(f"ERROR: Project file not found at {csproj}", tee=True)
        _dump_log()
        sys.exit(2)

    restore_args = [
        ctx["dotnet_exe"], "restore", csproj,
        "--ignore-failed-sources",
        "/p:NuGetAudit=false",
    ]

    nuget_config = ctx["nuget_config"]
    if os.path.isfile(nuget_config):
        restore_args.extend(["--configfile", nuget_config])

    framework = ctx.get("framework")
    if framework:
        restore_args.append(f"/p:TargetFrameworks={framework}")

    msbuild_args = ctx.get("msbuild_args")
    if msbuild_args:
        restore_args.extend(msbuild_args.split())

    try:
        result = run_cmd(restore_args, check=False, timeout=_RESTORE_TIMEOUT)
    except subprocess.TimeoutExpired:
        log(f"RESTORE TIMED OUT after {_RESTORE_TIMEOUT}s", tee=True)
        _dump_log()
        sys.exit(2)
    if result.returncode != 0:
        log(f"RESTORE FAILED (exit code {result.returncode})", tee=True)
        _dump_log()
        sys.exit(2)

    log("NuGet restore succeeded")


def disable_spotlight(workitem_root):
    """Disable Spotlight indexing on the workitem directory.

    Spotlight's mds_stores process can hold file locks during builds,
    causing intermittent build failures. This is a well-known issue on
    macOS CI machines.
    """
    log_raw("=== DISABLE SPOTLIGHT ===", tee=True)
    result = run_cmd(
        ["sudo", "mdutil", "-i", "off", workitem_root],
        check=False,
    )
    if result.returncode != 0:
        # Non-fatal — Spotlight interference is intermittent
        log(f"WARNING: mdutil -i off failed (exit {result.returncode}). "
            "Spotlight may interfere with builds.", tee=True)
    else:
        log(f"Spotlight indexing disabled for {workitem_root}")


def detect_physical_device():
    """Detect whether a physical iOS device is connected and return its UDID.

    Checks IOS_DEVICE_UDID env var first, then uses 'xcrun devicectl list devices'.
    Returns the UDID string, or None if no device is found.

    NOTE: This duplicates iOSHelper.detect_connected_device() intentionally.
    setup_helix.py runs as a standalone Helix pre-command with minimal imports
    (no performance.common, no shared.ioshelper). Keeping this self-contained
    avoids import failures on the Helix machine.
    """
    log_raw("=== PHYSICAL DEVICE DETECTION ===", tee=True)

    udid = os.environ.get("IOS_DEVICE_UDID", "").strip()
    if udid:
        log(f"Using IOS_DEVICE_UDID from environment: {udid}", tee=True)
        return udid

    # Auto-detect via devicectl
    result = run_cmd(
        ["xcrun", "devicectl", "list", "devices"],
        check=False,
    )
    if result.returncode != 0:
        log("WARNING: 'xcrun devicectl list devices' failed. "
            "No physical device detection available.", tee=True)
        return None

    # Log the full output for debugging
    log("devicectl output:")
    for line in (result.stdout or "").splitlines():
        log_raw(f"  {line}")

    # Try JSON output for structured parsing
    # Write to temp file instead of /dev/stdout because devicectl mixes
    # human-readable table text and JSON when writing to stdout.
    import tempfile
    fd, json_tmp = tempfile.mkstemp(suffix='.json', prefix='devicectl_')
    os.close(fd)
    try:
        json_result = run_cmd(
            ["xcrun", "devicectl", "list", "devices", "--json-output", json_tmp],
            check=False,
        )
        if json_result.returncode == 0 and os.path.exists(json_tmp):
            try:
                import json
                with open(json_tmp, 'r') as f:
                    data = json.load(f)
                devices = data.get("result", {}).get("devices", [])
                for device in devices:
                    conn = device.get("connectionProperties", {})
                    transport = conn.get("transportType", "")
                    name = device.get("deviceProperties", {}).get("name", "unknown")
                    # Prefer hardware UDID (e.g. 00008020-001965D83C43002E) over
                    # CoreDevice identifier (a UUID). mlaunch requires the hardware
                    # UDID — same logic as iOSHelper.detect_connected_device().
                    hw_udid = device.get("hardwareProperties", {}).get("udid", "")
                    device_udid = hw_udid or device.get("identifier", "")
                    if transport in ("wired", "localNetwork", "wifi") and device_udid:
                        log(f"Found connected device: {name} (UDID: {device_udid}, "
                            f"transport: {transport})", tee=True)
                        return device_udid
            except Exception as e:
                log(f"JSON parsing failed: {e}", tee=True)
    finally:
        if os.path.exists(json_tmp):
            os.remove(json_tmp)

    log("No connected physical devices found.", tee=True)
    return None


# --- Main ---

# --- Device signing artifact discovery ---

# On Mac.iPhone.*.Perf machines, Helix machine prep installs the developer
# provisioning profile (embedded.mobileprovision) and the 'sign' tool at
# known paths. XHarness work items get them in CWD automatically; HelixWorkItem
# (us) has to find and stage them ourselves.
_SIGNING_SEARCH_ROOTS = [
    "/etc/helix-prep",
    "/Users/helix-runner",
    "/Users/Shared/Helix",
    "/var/helix",
    "/usr/local/bin",
    "/usr/local/share",
]


def find_and_stage_signing_artifacts(workitem_root):
    """Locate embedded.mobileprovision and the 'sign' tool on the Helix machine.

    Searches several well-known root directories. If found, stages
    embedded.mobileprovision into ``workitem_root`` (so ioshelper.py picks
    it up via its CWD-based lookup) and symlinks ``sign`` into the work
    item's venv ``bin/`` directory (already on PATH per the .proj
    PreCommands), so ioshelper.py finds it via ``shutil.which('sign')``.

    Returns True if BOTH artifacts were found and staged, False otherwise.
    Does not raise; the caller decides what to do with the result.
    """
    log_raw("=== DEVICE SIGNING ARTIFACT DISCOVERY ===", tee=True)

    provision_path = None
    sign_path = None
    for root in _SIGNING_SEARCH_ROOTS:
        if not os.path.isdir(root):
            continue
        try:
            result = subprocess.run(
                [
                    "find", root, "-maxdepth", "6",
                    "(", "-name", "embedded.mobileprovision",
                    "-o", "-name", "sign", ")",
                    "-not", "-path", "*/.Trash/*",
                ],
                capture_output=True, text=True, timeout=30,
            )
            for line in (result.stdout or "").splitlines():
                line = line.strip()
                if not line:
                    continue
                base = os.path.basename(line)
                if base == "embedded.mobileprovision" and provision_path is None:
                    provision_path = line
                elif base == "sign" and sign_path is None:
                    if os.path.isfile(line) and os.access(line, os.X_OK):
                        sign_path = line
                if provision_path and sign_path:
                    break
        except Exception as e:
            log(f"Search in {root} failed: {e}")
        if provision_path and sign_path:
            break

    if provision_path:
        log(f"Found embedded.mobileprovision at: {provision_path}", tee=True)
        try:
            import shutil
            dest = os.path.join(workitem_root, "embedded.mobileprovision")
            shutil.copy2(provision_path, dest)
            log(f"Copied embedded.mobileprovision to: {dest}", tee=True)
        except Exception as e:
            log(f"WARNING: failed to copy embedded.mobileprovision: {e}", tee=True)
            provision_path = None
    else:
        log("WARNING: embedded.mobileprovision not found in any known location. "
            "Device install will likely fail with 'No code signature found'.",
            tee=True)

    if sign_path:
        log(f"Found 'sign' tool at: {sign_path}", tee=True)
        # Symlink into the workitem venv bin (already on PATH per .proj PreCommands)
        venv_bin = os.path.join(workitem_root, ".venv", "bin")
        try:
            os.makedirs(venv_bin, exist_ok=True)
            link_target = os.path.join(venv_bin, "sign")
            if os.path.lexists(link_target):
                os.remove(link_target)
            os.symlink(sign_path, link_target)
            log(f"Symlinked sign tool to: {link_target}", tee=True)
        except Exception as e:
            log(f"WARNING: failed to symlink sign tool: {e}", tee=True)
            sign_path = None
    else:
        log("WARNING: 'sign' tool not found in any known location. "
            "Device install will likely fail. Searched: "
            f"{', '.join(_SIGNING_SEARCH_ROOTS)}", tee=True)

    return bool(provision_path and sign_path)


def main():
    global _logfile

    # Open log file in HELIX_WORKITEM_UPLOAD_ROOT for post-mortem debugging
    upload_root = os.environ.get("HELIX_WORKITEM_UPLOAD_ROOT")
    if upload_root:
        os.makedirs(upload_root, exist_ok=True)
        _logfile = open(os.path.join(upload_root, "output.log"), "a")

    workitem_root = os.environ.get("HELIX_WORKITEM_ROOT", ".")
    correlation_payload = os.environ.get("HELIX_CORRELATION_PAYLOAD", ".")

    # Determine target device type from iOSRid env var (set by .proj).
    # ios-arm64 → physical device, iossimulator-* → simulator
    ios_rid = os.environ.get("IOS_RID", "iossimulator-x64")
    is_physical_device = (ios_rid == "ios-arm64")

    # Detect host architecture to select the correct simulator RID.
    # Mac.iPhone.17.Perf queue uses Intel x64 machines which need
    # iossimulator-x64, not iossimulator-arm64. Apple Silicon needs
    # iossimulator-arm64. Physical device builds (ios-arm64) target the
    # iPhone hardware, not the Mac, so skip architecture override.
    if not is_physical_device:
        host_arch = platform.machine()
        if host_arch == "x86_64":
            ios_rid = "iossimulator-x64"
        elif host_arch == "arm64":
            ios_rid = "iossimulator-arm64"
        else:
            log(f"WARNING: Unknown architecture '{host_arch}', "
                f"keeping IOS_RID={ios_rid}", tee=True)
        os.environ["IOS_RID"] = ios_rid
        log(f"Host architecture: {host_arch}, using IOS_RID={ios_rid}", tee=True)

    # The simulator device name env var is no longer used: we always create
    # our own fresh simulator under this user's CoreSimulator namespace via
    # create_and_boot_simulator(). Pre-existing devices on Helix machines
    # may belong to other users and refuse to boot with permission errors.

    # Framework and MSBuild args are passed as command-line arguments when
    # available (from the .proj PreCommands), or fall back to env vars.
    framework = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("PERFLAB_Framework", "")
    msbuild_args = sys.argv[2] if len(sys.argv) > 2 else ""

    ctx = {
        "framework": framework,
        "msbuild_args": msbuild_args,
        "workitem_root": workitem_root,
        "correlation_payload": correlation_payload,
        "nuget_config": os.path.join(workitem_root, "app", "NuGet.config"),
        "csproj": os.path.join(workitem_root, "app", "MauiiOSInnerLoop.csproj"),
    }

    log_raw("=== iOS HELIX SETUP START ===", tee=True)
    log(f"Target device type: {'physical device' if is_physical_device else 'simulator'} "
        f"(IOS_RID={ios_rid})", tee=True)

    # Step 1: Configure the .NET SDK from the correlation payload
    ctx["dotnet_exe"] = setup_dotnet(correlation_payload)
    print_diagnostics()

    # Step 2: Select the Xcode version matching the iOS SDK workload packs.
    # The packs require a specific Xcode (e.g. 26.2.x packs need Xcode 26.2).
    # Selecting early avoids wasting 20+ min on workload install before
    # _ValidateXcodeVersion fails.
    select_xcode(workitem_root)

    # Step 3 & 4: Device-type-specific setup.
    # Both job types create + boot a fresh simulator under THIS user's
    # CoreSimulator namespace. Pre-existing simulators on the Helix machine
    # may belong to other users (root or previous tenants) and refuse to boot
    # with permission errors. Even physical-device builds need a writable
    # simulator booted because actool spawns AssetCatalogSimulatorAgent via
    # CoreSimulator during 'dotnet build' for ios-arm64.
    fix_coresimulator_permissions()
    validate_simulator_runtimes()
    create_and_boot_simulator(workitem_root)

    if is_physical_device:
        # Order matters: detect the physical device FIRST, signing artifacts
        # SECOND. Both gates check independent infra (one is hardware on the
        # USB hub, the other is files in the keychain / on disk), and either
        # can fail by itself. Detecting the device first means the log always
        # shows what hardware the queue exposed regardless of signing state,
        # which is what humans actually need to debug a red work item.

        # Detect and validate the connected physical device. We require one;
        # there is NO fallback to the simulator on a device job. A green
        # device job MUST mean device measurements ran on real hardware,
        # never that we silently downgraded to a simulator.
        device_udid = detect_physical_device()
        if not device_udid:
            reason = (
                "No physical iOS device detected on this Helix machine. "
                "Device job requires a connected, paired iPhone — there is "
                "NO simulator fallback by design. This is a queue "
                "provisioning gap (device disconnected, not paired, or "
                "queue capacity issue), not a scenario bug. Verify the "
                "Mac.iPhone.13.Perf machine has its iPhone connected and "
                "paired (xcrun devicectl list devices)."
            )
            log_raw("=" * 70, tee=True)
            log_raw("WORK ITEM FAILED — NO PHYSICAL DEVICE", tee=True)
            log_raw("=" * 70, tee=True)
            log(reason, tee=True)
            log_raw("=" * 70, tee=True)
            _dump_log()
            sys.exit(1)

        # Log the detected UDID for diagnostics. Note: os.environ changes
        # in this Python process do NOT persist to subsequent Helix commands
        # (test.py, post.py). runner.py re-detects the device independently
        # via iOSHelper.detect_connected_device().
        os.environ["IOS_DEVICE_UDID"] = device_udid
        log(f"IOS_DEVICE_UDID detected: {device_udid}", tee=True)

        # Search for embedded.mobileprovision and 'sign' tool on the
        # Helix machine and stage them so ioshelper.py's signing flow
        # can find them.
        signing_ready = find_and_stage_signing_artifacts(workitem_root)

        # Without code-signing infrastructure (cert in keychain +
        # provisioning profile + 'sign' tool), iOS device install
        # cannot succeed — devicectl will fail with
        # "No code signature found" regardless of which install tool
        # we use. Fail loudly so missing queue provisioning shows up
        # as a red build, not a green-with-hidden-skip. Provisioning
        # the queue (Apple Developer cert + provisioning profile +
        # 'sign' tool, same as Mac.iPhone.17.Perf) is an Engineering
        # Services ticket, not a code change here.
        if not signing_ready:
            reason = (
                "Device code-signing infrastructure not available on this "
                "Helix machine (embedded.mobileprovision and/or 'sign' tool "
                "missing). Cannot install signed app on physical device. "
                "This is a queue provisioning gap, not a scenario bug. "
                "Fix: provision the queue with the Apple Developer cert + "
                "provisioning profile + 'sign' tool (same as "
                "Mac.iPhone.17.Perf). Search roots checked: "
                + ", ".join(_SIGNING_SEARCH_ROOTS)
            )
            log_raw("=" * 70, tee=True)
            log_raw("WORK ITEM FAILED — DEVICE INFRA UNAVAILABLE", tee=True)
            log_raw("=" * 70, tee=True)
            log(reason, tee=True)
            log_raw("=" * 70, tee=True)
            _dump_log()
            sys.exit(1)
        log(f"IOS_DEVICE_UDID detected: {device_udid}", tee=True)

    # Step 5: Install the maui-ios workload
    # Must happen BEFORE restore because restore needs workload packs
    install_workload(ctx)

    # Step 6: Restore NuGet packages
    restore_packages(ctx)

    # Step 7: Disable Spotlight indexing to prevent file-lock errors
    disable_spotlight(workitem_root)

    log_raw("=== iOS HELIX SETUP SUCCEEDED ===", tee=True)
    _dump_log()
    return 0


if __name__ == "__main__":
    sys.exit(main())
