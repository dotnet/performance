#!/usr/bin/env python3
"""setup_helix.py — Helix machine setup for MAUI iOS inner loop (macOS).

Runs on the Helix machine BEFORE test.py. Bootstraps the macOS environment
for iOS builds:
  1. Configure DOTNET_ROOT and PATH from the correlation payload SDK.
  2. Select the Xcode version that matches the iOS SDK workload packs.
  3. Validate iOS simulator runtime availability.
  4. Boot the target iOS simulator device.
  5. Install the maui-ios workload.
  6. Restore NuGet packages for the app project.
  7. Disable Spotlight indexing on the workitem directory.

Xcode selection strategy: The iOS SDK packs require a SPECIFIC Xcode version
(e.g. packs 26.2.x need Xcode 26.2). This script derives the required Xcode
major.minor from the ``rollback_maui.json`` file created by pre.py (shipped in
the workitem payload), then selects a matching ``/Applications/Xcode_*.app``.
If rollback_maui.json is absent or unparseable, it falls back to a coarse
``>= _MIN_XCODE_MAJOR`` check. This fails the work item early instead of
wasting 20+ minutes on workload install before hitting _ValidateXcodeVersion.
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
                "HELIX_CORRELATION_PAYLOAD"]:
        log_raw(f"  {var}={os.environ.get(var, '<not set>')}")
    log_raw(f"  macOS version:", tee=True)
    run_cmd(["sw_vers"], check=False)


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


def _find_latest_iphone_simulator():
    """Find the latest available iPhone simulator device name.

    Parses 'xcrun simctl list devices available' output to find iPhone devices
    and returns the last one (typically the latest model).
    Returns the device name string, or None if none found.
    """
    import re
    result = run_cmd(["xcrun", "simctl", "list", "devices", "available"], check=False)
    if result.returncode != 0 or not result.stdout:
        return None

    # Match lines like "    iPhone 16 Pro Max (UUID) (Shutdown)"
    iphone_names = []
    for line in result.stdout.splitlines():
        m = re.match(r'\s+(iPhone\s+\d+[^(]*?)\s+\(', line)
        if m:
            iphone_names.append(m.group(1).strip())

    if not iphone_names:
        return None

    # Return the last match — simctl lists devices in order, so the last
    # iPhone entry is typically the latest model.
    return iphone_names[-1]


def boot_simulator(device_name):
    """Boot the target iOS simulator device.

    Handles the case where the device is already booted (exit code 149
    from simctl boot = "Unable to boot device in current state: Booted").
    If the requested device fails to boot, tries the latest available iPhone
    simulator as a fallback. Exits with code 1 if no simulator can be booted.
    """
    log_raw("=== SIMULATOR BOOT ===", tee=True)
    log(f"Booting simulator device: '{device_name}'", tee=True)

    result = run_cmd(
        ["xcrun", "simctl", "boot", device_name],
        check=False,
    )

    if result.returncode == 0:
        log(f"Simulator '{device_name}' booted successfully.")
    elif "Booted" in (result.stdout or "") or result.returncode == 149:
        # Already booted — not an error
        log(f"Simulator '{device_name}' is already booted (OK).")
    else:
        log(f"Failed to boot simulator '{device_name}' "
            f"(exit code {result.returncode}). "
            "Trying dynamic fallback...", tee=True)

        # Try to find and boot the latest available iPhone simulator
        fallback = _find_latest_iphone_simulator()
        if fallback and fallback != device_name:
            log(f"Attempting fallback device: '{fallback}'", tee=True)
            fb_result = run_cmd(
                ["xcrun", "simctl", "boot", fallback],
                check=False,
            )
            if fb_result.returncode == 0:
                log(f"Fallback simulator '{fallback}' booted successfully.", tee=True)
            elif "Booted" in (fb_result.stdout or "") or fb_result.returncode == 149:
                log(f"Fallback simulator '{fallback}' is already booted (OK).", tee=True)
            else:
                log(f"ERROR: Fallback simulator '{fallback}' also failed to boot "
                    f"(exit code {fb_result.returncode}).", tee=True)
                run_cmd(["xcrun", "simctl", "list", "devices", "available"], check=False)
                _dump_log()
                sys.exit(1)
        else:
            log("ERROR: No fallback iPhone simulator found. Available devices:", tee=True)
            run_cmd(["xcrun", "simctl", "list", "devices", "available"], check=False)
            _dump_log()
            sys.exit(1)

    # Log booted devices for confirmation
    log("Currently booted devices:")
    run_cmd(["xcrun", "simctl", "list", "devices", "booted"], check=False)


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

    result = run_cmd(restore_args, check=False)
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

    # The simulator device name can be overridden via env var; default to
    # "iPhone 16" which is available on current macOS Helix images.
    device_name = os.environ.get("IOS_SIMULATOR_DEVICE", "iPhone 16")

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

    # Step 3 & 4: Device-type-specific setup
    if is_physical_device:
        # Detect and validate the connected physical device
        device_udid = detect_physical_device()
        if not device_udid:
            log("WARNING: No physical device found. Build may still succeed "
                "but deploy will fail.", tee=True)
        else:
            # Log the detected UDID for diagnostics. Note: os.environ changes
            # in this Python process do NOT persist to subsequent Helix commands
            # (test.py, post.py). runner.py re-detects the device independently
            # via iOSHelper.detect_connected_device().
            os.environ["IOS_DEVICE_UDID"] = device_udid
            log(f"IOS_DEVICE_UDID detected: {device_udid}", tee=True)
    else:
        # Simulator: validate runtimes and boot the device
        validate_simulator_runtimes()
        boot_simulator(device_name)

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
