#!/usr/bin/env python3
"""setup_helix.py — Helix machine setup for MAUI Android inner loop (Windows + Linux)."""

import os
import platform
import re
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


# --- Workaround for https://github.com/dotnet/android/issues/11319 ---
# InstallAndroidDependencies sometimes silently skips a required SDK package
# while exiting 0. The skipped package varies by manifest version (e.g. the
# default manifest dropped platforms;android-37.0; GoogleV2 dropped
# platform-tools). Parse the warning and install the missing packages
# explicitly via sdkmanager. Also unconditionally re-install platform-tools
# if adb is missing (postcondition-driven repair, in case the warning text
# changes upstream).
_MISSING_DEP_RE = re.compile(
    r"Dependency `([^`]+)` should have been installed but could not be resolved"
)
_PKG_NAME_RE = re.compile(r"^[A-Za-z0-9_.;-]+$")
_VERSION_DIR_RE = re.compile(r"^(\d+)(?:\.(\d+))?(?:\.(\d+))?$")


def _natural_version_key(name):
    m = _VERSION_DIR_RE.match(name)
    if not m:
        return (-1, -1, -1, name)
    return (int(m.group(1)), int(m.group(2) or 0), int(m.group(3) or 0), name)


def _find_sdkmanager(android_home):
    """Locate sdkmanager binary; cmdline-tools may live under 'latest' or a versioned dir."""
    ct_root = os.path.join(android_home, "cmdline-tools")
    if not os.path.isdir(ct_root):
        return None
    entries = os.listdir(ct_root)
    versioned = [e for e in entries if e != "latest"]
    versioned.sort(key=_natural_version_key, reverse=True)
    candidates = (["latest"] if "latest" in entries else []) + versioned
    suffix = ".bat" if IS_WINDOWS else ""
    for cand in candidates:
        path = os.path.join(ct_root, cand, "bin", f"sdkmanager{suffix}")
        if os.path.isfile(path):
            return path
    return None


def _sdkmanager_env(workitem_root):
    """Return a copy of os.environ with JAVA_HOME set iff <workitem_root>/jdk has bin/java."""
    env = dict(os.environ)
    java_home = os.path.join(workitem_root, "jdk")
    java_exe = os.path.join(java_home, "bin", "java" + EXE)
    if os.path.isfile(java_exe):
        env["JAVA_HOME"] = java_home
    return env


def _run_sdkmanager_install(sdkmanager, android_home, package, env):
    log(f"Installing {package!r} via sdkmanager", tee=True)
    try:
        r = subprocess.run(
            [sdkmanager, f"--sdk_root={android_home}", "--install", package],
            input="y\n" * 50, text=True,
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, env=env,
            timeout=600,
        )
    except subprocess.TimeoutExpired as e:
        log(f"sdkmanager --install {package} TIMED OUT after 600s", tee=True)
        if e.stdout:
            for line in (e.stdout if isinstance(e.stdout, str)
                         else e.stdout.decode("utf-8", "replace")).splitlines():
                log_raw(line)
        return False
    for line in (r.stdout or "").splitlines():
        log_raw(line)
    if r.returncode != 0:
        log(f"sdkmanager --install {package} FAILED (exit {r.returncode})", tee=True)
        return False
    return True


def _repair_android_dependencies(ctx, install_stdout):
    """Install any packages that InstallAndroidDependencies silently skipped.

    1. Parse the warning text to discover explicitly-named missing packages.
    2. Always ensure 'platform-tools' is installed (postcondition: adb must exist).
    3. Verify adb is present and runnable; abort the script on failure.
    """
    android_home = ctx["android_home"]
    parsed = _MISSING_DEP_RE.findall(install_stdout or "")
    missing = sorted({p for p in parsed if _PKG_NAME_RE.match(p)})
    if missing:
        log(f"InstallAndroidDependencies silently skipped: {missing}", tee=True)

    adb_path = os.path.join(android_home, "platform-tools", "adb" + EXE)
    needs_platform_tools = not os.path.isfile(adb_path)
    if needs_platform_tools and "platform-tools" not in missing:
        log("platform-tools missing post-install; will install regardless of warnings",
            tee=True)
        missing.append("platform-tools")

    if not missing:
        return

    sdkmanager = _find_sdkmanager(android_home)
    if not sdkmanager:
        log(f"ERROR: cannot locate sdkmanager under {android_home}/cmdline-tools",
            tee=True)
        _dump_log(); sys.exit(1)
    log(f"Using sdkmanager: {sdkmanager}", tee=True)

    env = _sdkmanager_env(ctx["workitem_root"])
    log(f"sdkmanager env JAVA_HOME={env.get('JAVA_HOME', '<unset>')}")

    for pkg in missing:
        if not _run_sdkmanager_install(sdkmanager, android_home, pkg, env):
            _dump_log(); sys.exit(1)
    log("Repair complete: all silently-skipped dependencies installed", tee=True)


def _verify_adb(android_home):
    adb_path = os.path.join(android_home, "platform-tools", "adb" + EXE)
    if not os.path.isfile(adb_path):
        log(f"ERROR: adb not found at {adb_path} after install/repair", tee=True)
        _dump_log(); sys.exit(1)
    _chmod_exec(adb_path)
    r = subprocess.run([adb_path, "version"], capture_output=True, text=True)
    for line in (r.stdout or "").splitlines():
        log_raw(line)
    if r.returncode != 0:
        log(f"ERROR: '{adb_path} version' failed (exit {r.returncode}): {r.stderr}",
            tee=True)
        _dump_log(); sys.exit(1)


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
    # TODO: Remove -p:AndroidManifestType=GoogleV2 once
    # https://github.com/dotnet/android/issues/11319 gets resolved.
    result = run_cmd(
        [ctx["dotnet_exe"], "msbuild", csproj,
         "-t:InstallAndroidDependencies",
         f"/p:AndroidSdkDirectory={android_home}",
         f"/p:JavaSdkDirectory={java_home}",
         "/p:AcceptAndroidSdkLicenses=True",
         "/p:AndroidManifestType=GoogleV2",
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

    # Workaround for https://github.com/dotnet/android/issues/11319: the
    # target above can exit 0 while skipping required packages.
    _repair_android_dependencies(ctx, result.stdout or "")
    _verify_adb(android_home)


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
