#!/usr/bin/env python3
"""setup_helix.py — Helix machine setup for MAUI Android inner loop (Windows + Linux)."""

import os
import platform
import re
import subprocess
import sys
import time
from datetime import datetime

# --- Constants ---
IS_WINDOWS = platform.system() == "Windows"
EXE = ".exe" if IS_WINDOWS else ""

# --- Logging ---
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


# --- ADB device setup ---
def _count_adb_devices():
    result = subprocess.run(["adb", "devices"], capture_output=True, text=True)
    count = 0
    for line in result.stdout.splitlines()[1:]:
        parts = line.split()
        if len(parts) >= 2 and parts[1] == "device":
            count += 1
    return count


def _setup_adb_windows(android_home):
    """Windows: wait for a physical device, then verify."""
    log("Waiting for device (timeout 30s)...")
    try:
        run_cmd(["adb", "wait-for-device"], check=False, timeout=30)
    except subprocess.TimeoutExpired:
        log("WARNING: adb wait-for-device timed out")
    device_count = _count_adb_devices()
    log(f"Device count: {device_count}")
    if device_count == 0:
        log("WARNING: No devices detected — dotnet run will likely fail")


def _setup_adb_linux():
    """Linux: target emulator-5554, wait for device and boot."""
    log("ANDROID_SERIAL=emulator-5554 (set by PreCommands)")
    log("Waiting for device (timeout 30s)...")
    try:
        run_cmd(["adb", "wait-for-device"], check=False, timeout=30)
    except subprocess.TimeoutExpired:
        log("WARNING: adb wait-for-device timed out")
    device_count = _count_adb_devices()
    log(f"Device count: {device_count}")
    if device_count == 0:
        log("WARNING: No devices detected — dotnet run will likely fail")
        return
    # Wait for emulator boot
    log("Waiting for emulator to fully boot (up to 60s)...")
    boot_wait = 0
    boot_completed = ""
    while boot_wait < 60:
        try:
            result = subprocess.run(
                ["adb", "shell", "getprop", "sys.boot_completed"],
                capture_output=True, text=True,
                timeout=10,
            )
            boot_completed = result.stdout.strip()
        except subprocess.TimeoutExpired:
            log("  adb getprop timed out after 10s; retrying")
            boot_completed = ""
        if boot_completed == "1":
            log(f"Emulator fully booted after {boot_wait}s")
            break
        time.sleep(5)
        boot_wait += 5
        log(f"  sys.boot_completed={boot_completed} (waited {boot_wait}s)")
    if boot_completed != "1":
        log(f"WARNING: Emulator did not report boot_completed=1 after 60s")


def _dump_log():
    if not _logfile:
        return
    _logfile.flush()
    try:
        with open(_logfile.name, "r") as f:
            print(f.read())
    except Exception:
        pass


# --- Orchestration ---
def print_diagnostics():
    log_raw("=== DIAGNOSTICS ===", tee=True)
    log_raw(f"DOTNET_ROOT={os.environ.get('DOTNET_ROOT', '')}")


def setup_dotnet(correlation_payload):
    """Return the path to the dotnet executable in the correlation payload."""
    dotnet_root = os.path.join(correlation_payload, "dotnet")
    dotnet_exe = os.path.join(dotnet_root, f"dotnet{EXE}")
    log(f"DOTNET_ROOT={dotnet_root}")
    run_cmd([dotnet_exe, "--version"], check=False)
    return dotnet_exe


def install_workload(ctx):
    """Install workloads: ``workload restore`` first to satisfy dependencies
    at any available version, then ``workload install maui-android
    --from-rollback-file`` to pin maui-android to the version under test.
    """
    log_raw("=== STEP 1: Workload Install ===", tee=True)

    log("Step 1a: workload restore (satisfy project dependencies)", tee=True)
    result = run_cmd(
        [ctx["dotnet_exe"], "workload", "restore", ctx["csproj"],
         "--configfile", ctx["nuget_config"],
         f"-p:TargetFrameworks={ctx['framework']}"],
        check=False,
    )
    if result.returncode != 0:
        log(f"WARNING: workload restore exited with code {result.returncode} "
            "— continuing with pinned install", tee=True)
    else:
        log("Workload restore succeeded")

    rollback_file = os.path.join(ctx["workitem_root"], "rollback_maui.json")
    log(f"Step 1b: workload install maui-android "
        f"(pinned via {rollback_file})", tee=True)
    with open(rollback_file, "r", encoding="utf-8") as f:
        log_raw(f"rollback_maui.json contents:\n{f.read()}", tee=True)
    result = run_cmd(
        [ctx["dotnet_exe"], "workload", "install", "maui-android",
         "--from-rollback-file", rollback_file,
         "--configfile", ctx["nuget_config"]],
        check=False,
    )
    if result.returncode != 0:
        log(f"STEP 1 FAILED: workload install exited with code "
            f"{result.returncode}", tee=True)
        _dump_log()
        sys.exit(1)
    log("Workload install (pinned maui-android) succeeded")


def install_android_dependencies(ctx):
    """Restore the MAUI csproj and run InstallAndroidDependencies."""
    log_raw("=== Installing Android SDK & Java Dependencies ===", tee=True)

    android_home = os.path.join(ctx["workitem_root"], "android-sdk")
    java_home = os.path.join(ctx["workitem_root"], "jdk")
    os.makedirs(android_home, exist_ok=True)
    os.makedirs(java_home, exist_ok=True)

    csproj = ctx["csproj"]
    log(f"Using project: {csproj}")

    result = run_cmd(
        [ctx["dotnet_exe"], "restore", csproj,
         "--configfile", ctx["nuget_config"],
         f"-p:TargetFrameworks={ctx['framework']}"],
        check=False,
    )
    if result.returncode != 0:
        log("WARNING: restore failed — "
            "InstallAndroidDependencies may not work")

    result = run_cmd(
        [ctx["dotnet_exe"], "msbuild", csproj,
         "-t:InstallAndroidDependencies",
         f"/p:AndroidSdkDirectory={android_home}",
         f"/p:JavaSdkDirectory={java_home}",
         "/p:AcceptAndroidSdkLicenses=True",
         f"/p:TargetFramework={ctx['framework']}"],
        check=False,
    )
    if result.returncode != 0:
        log("InstallAndroidDependencies FAILED", tee=True)
        _dump_log()
        sys.exit(1)
    log("Android SDK and Java dependencies installed successfully")

    log(f"ANDROID_HOME={android_home}")
    log(f"JAVA_HOME={java_home}")

    ctx["android_home"] = android_home


def setup_android_sdk(ctx):
    """Install Android SDK and Java via the built-in MSBuild target."""
    log_raw("=== Setting up Android SDK ===")
    install_android_dependencies(ctx)


def setup_adb_device(ctx):
    """Restart ADB server and run platform-specific device setup."""
    log_raw("=== ADB DEVICE SETUP ===")
    run_cmd(["adb", "kill-server"], check=False)
    run_cmd(["adb", "start-server"], check=False)
    run_cmd(["adb", "devices", "-l"], check=False)
    if IS_WINDOWS:
        _setup_adb_windows(ctx["android_home"])
    else:
        _setup_adb_linux()


def restore_packages(ctx):
    """Step 2: Restore NuGet packages."""
    log_raw("=== STEP 2: Restore ===", tee=True)
    restore_args = [
        ctx["dotnet_exe"], "restore", ctx["csproj"],
        "--configfile", ctx["nuget_config"],
        f"/p:TargetFrameworks={ctx['framework']}",
    ]
    if ctx["msbuild_args"]:
        restore_args.extend(arg for arg in re.split(r'[;\s]+', ctx["msbuild_args"]) if arg)
    result = run_cmd(restore_args, check=False)
    if result.returncode != 0:
        log(f"STEP 2 FAILED with exit code {result.returncode}", tee=True)
        _dump_log()
        sys.exit(2)
    log("Restore succeeded")


# --- Main ---
def main():
    if len(sys.argv) < 3:
        print(f"Usage: {sys.argv[0]} FRAMEWORK MSBUILD_ARGS",
              file=sys.stderr)
        sys.exit(1)

    global _logfile
    upload_root = os.environ.get("HELIX_WORKITEM_UPLOAD_ROOT")
    if upload_root:
        _logfile = open(os.path.join(upload_root, "output.log"), "a")

    workitem_root = os.environ.get("HELIX_WORKITEM_ROOT", ".")
    correlation_payload = os.environ.get("HELIX_CORRELATION_PAYLOAD", ".")
    ctx = {
        "framework": sys.argv[1],
        "msbuild_args": sys.argv[2],
        "workitem_root": workitem_root,
        "correlation_payload": correlation_payload,
        "nuget_config": os.path.join(workitem_root, "app", "NuGet.config"),
        "csproj": os.path.join(workitem_root, "app", "MauiAndroidInnerLoop.csproj"),
    }

    ctx["dotnet_exe"] = setup_dotnet(correlation_payload)
    print_diagnostics()

    install_workload(ctx)
    setup_android_sdk(ctx)
    run_cmd([ctx["dotnet_exe"], "--info"], check=False)
    setup_adb_device(ctx)
    restore_packages(ctx)

    log_raw("=== SETUP SUCCEEDED ===", tee=True)
    _dump_log()
    return 0


if __name__ == "__main__":
    sys.exit(main())
