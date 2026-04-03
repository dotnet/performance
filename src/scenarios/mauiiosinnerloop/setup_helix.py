#!/usr/bin/env python3
"""setup_helix.py — Helix machine setup for MAUI iOS inner loop (macOS).

Runs on the Helix machine BEFORE test.py. Bootstraps the macOS environment
for iOS builds:
  1. Configure DOTNET_ROOT and PATH from the correlation payload SDK.
  2. Select the correct Xcode version (highest versioned Xcode_*.app).
  3. Validate iOS simulator runtime availability.
  4. Boot the target iOS simulator device.
  5. Install the maui-ios workload.
  6. Restore NuGet packages for the app project.
  7. Disable Spotlight indexing on the workitem directory.
"""

import os
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


def select_xcode():
    """Select the highest versioned Xcode_*.app installation.

    Follows the same pattern as maui_scenarios_ios.proj PreparePayloadWorkItem:
    find /Applications -maxdepth 1 -type d -name 'Xcode_*.app' | sort ... | tail -1

    This avoids runner-image symlink aliases that don't work with the iOS SDK.
    If XCODE_PATH env var is already set, uses that instead of auto-detecting.
    """
    log_raw("=== XCODE SELECTION ===", tee=True)

    xcode_path = os.environ.get("XCODE_PATH", "")
    if not xcode_path:
        # Auto-detect: find highest versioned Xcode_*.app
        result = run_cmd(
            ["find", "/Applications", "-maxdepth", "1", "-type", "d",
             "-name", "Xcode_*.app"],
            check=False,
        )
        candidates = [line.strip() for line in (result.stdout or "").splitlines()
                      if line.strip()]
        if not candidates:
            log("WARNING: No Xcode_*.app found in /Applications. "
                "Falling back to system default Xcode.", tee=True)
            run_cmd(["xcode-select", "-p"], check=False)
            run_cmd(["xcodebuild", "-version"], check=False)
            return

        # Sort by version number (Xcode_16.2.app → key on "16.2").
        # Use tuple-of-ints to get correct version ordering (e.g., 16.10 > 16.2).
        def _xcode_version_key(path):
            ver = path.rsplit("_", 1)[-1].replace(".app", "")
            try:
                return tuple(int(x) for x in ver.split('.'))
            except ValueError:
                return (0,)
        candidates.sort(key=_xcode_version_key)
        xcode_path = candidates[-1]

    log(f"Selected Xcode: {xcode_path}", tee=True)

    if not os.path.isdir(os.path.join(xcode_path, "Contents", "Developer")):
        log(f"WARNING: {xcode_path} does not look like a valid Xcode installation "
            "(missing Contents/Developer)", tee=True)
        return

    # Use sudo xcode-select -s to switch the system Xcode (same as .proj pattern)
    result = run_cmd(
        ["sudo", "xcode-select", "-s", xcode_path],
        check=False,
    )
    if result.returncode != 0:
        log(f"WARNING: xcode-select -s failed (exit {result.returncode}). "
            "Falling back to DEVELOPER_DIR.", tee=True)
        os.environ["DEVELOPER_DIR"] = os.path.join(xcode_path, "Contents", "Developer")
    else:
        log(f"Xcode switched to: {xcode_path}")

    # Log the active Xcode version for diagnostics
    run_cmd(["xcodebuild", "-version"], check=False)


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


def install_workload(ctx):
    """Install the maui-ios workload using the shipped SDK.

    Uses the rollback file created by pre.py to pin to the exact workload
    version. Falls back to a plain install if no rollback file is present.
    Always uses --ignore-failed-sources because dead NuGet feeds are common
    in CI.
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

    result = run_cmd(install_args, check=False)
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
    ios_rid = os.environ.get("IOS_RID", "iossimulator-arm64")
    is_physical_device = (ios_rid == "ios-arm64")

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

    # Step 2: Select the correct Xcode version
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
