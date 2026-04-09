#!/usr/bin/env python3
"""setup_helix.py — Helix machine setup for MAUI iOS inner loop (macOS).

Runs on the Helix machine BEFORE test.py. Bootstraps the macOS environment
for iOS builds:
  1. Configure DOTNET_ROOT and PATH from the correlation payload SDK.
  2. Select and validate the system Xcode (>= 26.0).
  3. Validate iOS simulator runtime availability.
  4. Boot the target iOS simulator device.
  5. Install the maui-ios workload.
  6. Restore NuGet packages for the app project.
  7. Disable Spotlight indexing on the workitem directory.

Xcode selection strategy: Helix machines in the Mac.iPhone.17.Perf pool may
have multiple Xcode versions installed, and the default can vary per machine.
This script finds the highest-versioned /Applications/Xcode_*.app, activates
it via ``sudo xcode-select -s``, and validates that the version meets the
minimum required by the iOS SDK packs (>= 26.0). This fails the work item
early instead of wasting 20+ minutes on workload install before hitting
_ValidateXcodeVersion.
"""

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


def select_xcode():
    """Select the highest-versioned Xcode and validate it meets the minimum.

    Searches /Applications/Xcode_*.app for available Xcode installations,
    selects the highest version via ``sudo xcode-select -s``, then validates
    the version is >= _MIN_XCODE_MAJOR. Exits early if validation fails to
    avoid wasting time on workload install.
    """
    log_raw("=== XCODE SELECTION ===", tee=True)

    # Find all /Applications/Xcode_*.app directories
    xcode_apps = sorted(
        (
            entry
            for entry in os.listdir("/Applications")
            if entry.startswith("Xcode_") and entry.endswith(".app")
            and os.path.isdir(os.path.join("/Applications", entry))
        ),
        # Sort by numeric version components extracted from the name
        # e.g. "Xcode_26.2.app" → [26, 2], "Xcode_15.0.app" → [15, 0]
        key=lambda name: [
            int(x) for x in re.findall(r"\d+", name.replace(".app", ""))
        ],
    )

    if xcode_apps:
        selected = os.path.join("/Applications", xcode_apps[-1])
        log(f"Found Xcode installations: {xcode_apps}", tee=True)
        log(f"Selecting highest version: {selected}", tee=True)
        run_cmd(["sudo", "xcode-select", "-s", selected], check=False)
    else:
        log("WARNING: No /Applications/Xcode_*.app found. "
            "Continuing with system default Xcode.", tee=True)

    # Log the active Xcode version
    result = run_cmd(["xcodebuild", "-version"], check=False)

    # Validate the Xcode version meets our minimum
    version = _parse_xcode_version(result.stdout)
    if version is None:
        log("ERROR: Could not parse Xcode version from xcodebuild output. "
            "Ensure Xcode is installed.", tee=True)
        _dump_log()
        sys.exit(1)

    major, minor = version
    log(f"Detected Xcode version: {major}.{minor}", tee=True)

    if major < _MIN_XCODE_MAJOR:
        log(f"ERROR: Xcode {major}.{minor} is below the minimum required "
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
        # When a new manifest is published to the feed, referenced SDK packs
        # may not have propagated to all NuGet feeds yet, causing
        # "package NOT FOUND".  Retry without the rollback file so the SDK
        # resolves a recent stable version that is already fully available.
        log(f"WARNING: Workload install with rollback file failed "
            f"(exit code {result.returncode}, possible NuGet version skew)", tee=True)
        log("Retrying without rollback file (will use SDK default version)...", tee=True)

        retry_args = [
            ctx["dotnet_exe"], "workload", "install", "maui-ios",
            # --skip-manifest-update prevents the SDK from pulling a newer
            # manifest that may reference packs not yet published to all feeds.
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
                    device_udid = device.get("identifier", "")
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

    # Step 2: Select the highest-versioned Xcode and validate >= 26.0.
    # Helix machines may default to an older Xcode; selecting early avoids
    # wasting 20+ min on workload install before _ValidateXcodeVersion fails.
    select_xcode()

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
