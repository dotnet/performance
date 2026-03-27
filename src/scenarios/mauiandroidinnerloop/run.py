#!/usr/bin/env python3
"""run.py — Unified MAUI Android inner loop run script (Windows + Linux)."""

import os
import platform
import stat
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


def _chmod_exec(path):
    if not IS_WINDOWS and os.path.isfile(path):
        os.chmod(path, os.stat(path).st_mode | stat.S_IEXEC | stat.S_IXGRP | stat.S_IXOTH)


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
        log("WARNING: No devices detected — build -t:Install will likely fail")


def _setup_adb_linux():
    """Linux: target emulator-5554, wait for device and boot."""
    os.environ["ANDROID_SERIAL"] = "emulator-5554"
    log("ANDROID_SERIAL=emulator-5554")
    log("Waiting for device (timeout 30s)...")
    try:
        run_cmd(["adb", "wait-for-device"], check=False, timeout=30)
    except subprocess.TimeoutExpired:
        log("WARNING: adb wait-for-device timed out")
    device_count = _count_adb_devices()
    log(f"Device count: {device_count}")
    if device_count == 0:
        log("WARNING: No devices detected — build -t:Install will likely fail")
        return
    # Wait for emulator boot
    log("Waiting for emulator to fully boot (up to 60s)...")
    boot_wait = 0
    boot_completed = ""
    while boot_wait < 60:
        result = subprocess.run(
            ["adb", "shell", "getprop", "sys.boot_completed"],
            capture_output=True, text=True,
        )
        boot_completed = result.stdout.strip()
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
    """Override DOTNET_ROOT to use the SDK from the correlation payload."""
    dotnet_root = os.path.join(correlation_payload, "dotnet")
    os.environ["DOTNET_ROOT"] = dotnet_root
    os.environ["PATH"] = dotnet_root + os.pathsep + os.environ.get("PATH", "")
    os.environ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    os.environ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
    dotnet_exe = os.path.join(dotnet_root, f"dotnet{EXE}")
    log(f"DOTNET_ROOT={dotnet_root}")
    run_cmd([dotnet_exe, "--version"], check=False)
    return dotnet_exe


def install_workload(ctx):
    """Step 1: Install workloads via restore (dependencies) then install (pinned).

    First, ``workload restore`` installs whatever workloads the project
    needs (including iOS/MacCatalyst if MAUI requires them) at whatever
    version is available in the feeds.  This satisfies dependency packages
    that may not exist at the pinned version.

    Then, ``workload install maui-android --from-rollback-file`` pins the
    android workload to the exact version from the rollback file we want
    to test against.
    """
    log_raw("=== STEP 1: Workload Install ===", tee=True)

    # 1a. Workload restore — satisfy all project dependencies first.
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

    # 1b. Workload install — pin maui-android to the rollback version.
    rollback_file = os.path.join(ctx["workitem_root"], "rollback_maui.json")
    log(f"Step 1b: workload install maui-android "
        f"(pinned via {rollback_file})", tee=True)
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
    """Restore the real MAUI csproj and run InstallAndroidDependencies.

    Uses the real project at ``ctx["csproj"]`` directly.
    ``AllowMissingPrunePackageData=true`` bypasses NETSDK1226 on preview
    SDKs that lack prune package data.
    """
    log_raw("=== Installing Android SDK & Java Dependencies ===", tee=True)

    android_home = os.path.join(ctx["workitem_root"], "android-sdk")
    java_home = os.path.join(ctx["workitem_root"], "jdk")
    os.makedirs(android_home, exist_ok=True)
    os.makedirs(java_home, exist_ok=True)

    csproj = ctx["csproj"]
    log(f"Using project: {csproj}")

    # Restore the project. Override TargetFrameworks to android-only.
    result = run_cmd(
        [ctx["dotnet_exe"], "restore", csproj,
         "--configfile", ctx["nuget_config"],
         f"-p:TargetFrameworks={ctx['framework']}"],
        check=False,
    )
    if result.returncode != 0:
        log("WARNING: restore failed — "
            "InstallAndroidDependencies may not work")

    # Use dotnet msbuild (not dotnet build) to run only this target
    # without the full build pipeline.
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

    # Set environment variables
    platform_tools = os.path.join(android_home, "platform-tools")
    java_bin = os.path.join(java_home, "bin")
    os.environ["ANDROID_HOME"] = android_home
    os.environ["ANDROID_SDK_ROOT"] = android_home
    os.environ["JAVA_HOME"] = java_home
    os.environ["PATH"] = os.pathsep.join([
        platform_tools, java_bin, os.environ.get("PATH", "")
    ])
    log(f"ANDROID_HOME={android_home}")
    log(f"JAVA_HOME={java_home}")

    _chmod_exec(os.path.join(platform_tools, "adb"))
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
        restore_args.extend(ctx["msbuild_args"].split())
    result = run_cmd(restore_args, check=False)
    if result.returncode != 0:
        log(f"STEP 2 FAILED with exit code {result.returncode}", tee=True)
        _dump_log()
        sys.exit(2)
    log("Restore succeeded")


def run_test(ctx):
    """Step 3: Run test.py for the inner-loop measurement."""
    log_raw("=== STEP 3: Test ===", tee=True)
    # Pass MSBuild args via env var to avoid shell quoting issues.
    # Add TargetFrameworks override so dotnet build's implicit restore
    # only evaluates the android TFM, not multi-platform TFMs in the csproj.
    msbuild_args = ctx["msbuild_args"]
    if "/p:TargetFrameworks=" not in msbuild_args:
        msbuild_args += f" /p:TargetFrameworks={ctx['framework']}"
    # Force minSdkVersion 23+ — the MAUI template sets SupportedOSPlatformVersion
    # conditionally in the csproj so csproj injection is skipped, but /p: overrides
    # take highest precedence in MSBuild.
    msbuild_args += " /p:SupportedOSPlatformVersion=23"
    os.environ["PERFLAB_MSBUILD_ARGS"] = msbuild_args
    log(f"PERFLAB_MSBUILD_ARGS={msbuild_args}")
    test_cmd = [
        sys.executable, "test.py", "androidinnerloop",
        "--csproj-path", os.path.join("app", "MauiAndroidInnerLoop.csproj"),
        "--edit-src", ";".join([
            os.path.join("src", "MainPage.xaml.cs"),
            os.path.join("src", "MainPage.xaml"),
        ]),
        "--edit-dest", ";".join([
            os.path.join("app", "Pages", "MainPage.xaml.cs"),
            os.path.join("app", "Pages", "MainPage.xaml"),
        ]),
        "--package-name", "com.companyname.mauiandroidinnerloop",
        "-f", ctx["framework"],
        "-c", "Debug",
        "--scenario-name", ctx["scenario_name"],
    ] + ctx["extra_args"]
    result = run_cmd(test_cmd, check=False, cwd=ctx["workitem_root"])
    if result.returncode != 0:
        log(f"STEP 3 FAILED with exit code {result.returncode}", tee=True)
        _dump_log()
        sys.exit(3)
    log("test.py succeeded")


# --- Main ---
def main():
    if len(sys.argv) < 4:
        print(f"Usage: {sys.argv[0]} FRAMEWORK MSBUILD_ARGS SCENARIO_NAME "
              f"[EXTRA_ARGS...]", file=sys.stderr)
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
        "scenario_name": sys.argv[3],
        "extra_args": sys.argv[4:],
        "workitem_root": workitem_root,
        "correlation_payload": correlation_payload,
        "nuget_config": os.path.join(workitem_root, "app", "NuGet.config"),
        "csproj": os.path.join(workitem_root, "app", "MauiAndroidInnerLoop.csproj"),
    }

    ctx["dotnet_exe"] = setup_dotnet(correlation_payload)
    print_diagnostics()

    install_workload(ctx)
    setup_android_sdk(ctx)
    setup_adb_device(ctx)
    restore_packages(ctx)
    run_test(ctx)

    log_raw("=== ALL STEPS SUCCEEDED ===", tee=True)
    _dump_log()


if __name__ == "__main__":
    main()
