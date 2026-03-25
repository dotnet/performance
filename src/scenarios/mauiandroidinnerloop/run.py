#!/usr/bin/env python3
"""run.py — Unified MAUI Android inner loop run script (Windows + Linux)."""

import glob as _glob
import os
import platform
import shutil
import stat
import subprocess
import sys
import tarfile
import time
import zipfile
from datetime import datetime
from urllib.request import urlretrieve

# --- Constants ---
IS_WINDOWS = platform.system() == "Windows"
EXE = ".exe" if IS_WINDOWS else ""

BUILD_TOOLS_VERSION = "35.0.0"
BUILD_TOOLS_URL = (
    "https://dl.google.com/android/repository/build-tools_r35_windows.zip"
    if IS_WINDOWS
    else "https://dl.google.com/android/repository/build-tools_r35_linux.zip"
)
PLATFORM_VERSION = "android-36.1"
PLATFORM_URL = "https://dl.google.com/android/repository/platform-36.1_r01.zip"
JDK_URL = (
    "https://aka.ms/download-jdk/microsoft-jdk-17.0.13-windows-x64.zip"
    if IS_WINDOWS
    else "https://aka.ms/download-jdk/microsoft-jdk-17.0.12-linux-x64.tar.gz"
)

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


# --- Download / extraction helpers ---
def download(url, dest):
    log(f"Downloading {url} -> {dest}")
    urlretrieve(url, dest)


def extract(archive, dest_dir):
    """Extract a ZIP or tar.gz archive to *dest_dir*."""
    log(f"Extracting {archive} -> {dest_dir}")
    os.makedirs(dest_dir, exist_ok=True)
    if archive.endswith(".zip"):
        with zipfile.ZipFile(archive, "r") as zf:
            zf.extractall(dest_dir)
    else:
        with tarfile.open(archive, "r:gz") as tf:
            tf.extractall(dest_dir)


def move_inner_contents(extract_dir, target_dir):
    """Flatten top-level subdirectories from an extracted archive into *target_dir*."""
    os.makedirs(target_dir, exist_ok=True)
    for d in os.listdir(extract_dir):
        src = os.path.join(extract_dir, d)
        if os.path.isdir(src):
            shutil.copytree(src, target_dir, dirs_exist_ok=True)


def _chmod_exec(path):
    if not IS_WINDOWS and os.path.isfile(path):
        os.chmod(path, os.stat(path).st_mode | stat.S_IEXEC | stat.S_IXGRP | stat.S_IXOTH)


# --- Java SDK discovery ---
def find_java():
    """Find or download a Java SDK.  Sets JAVA_HOME and prepends bin to PATH."""
    java_home = os.environ.get("JAVA_HOME", "")
    if java_home and os.path.isfile(os.path.join(java_home, "bin", f"java{EXE}")):
        log(f"JAVA_HOME already set: {java_home}")
        _apply_java_home(java_home)
        return
    log("JAVA_HOME not set — searching for Java SDK...")
    java_home = _find_java_windows() if IS_WINDOWS else _find_java_linux()
    if not java_home:
        log("Java not found. Downloading Microsoft OpenJDK 17...")
        java_home = _download_java()
    if java_home:
        _apply_java_home(java_home)
    else:
        log("ERROR: Java SDK not found after download attempt")


def _apply_java_home(java_home):
    os.environ["JAVA_HOME"] = java_home
    os.environ["PATH"] = os.path.join(java_home, "bin") + os.pathsep + os.environ.get("PATH", "")
    log(f"JAVA_HOME={java_home}")


def _find_java_windows():
    """Search common Windows JDK installation paths."""
    for env_var in ("ProgramW6432", "ProgramFiles"):
        base = os.environ.get(env_var, "")
        if not base:
            continue
        for subdir in ("Microsoft", os.path.join("Android", "openjdk"), "Java", "Eclipse Adoptium"):
            for m in sorted(_glob.glob(os.path.join(base, subdir, "jdk-*"))):
                log(f"Found Java SDK at {m}")
                return m
    return None


def _find_java_linux():
    """Search common Linux JDK installation paths."""
    java_home = None
    for pattern in ["/usr/lib/jvm/msopenjdk-*", "/usr/lib/jvm/temurin-*",
                    "/usr/lib/jvm/java-*"]:
        for m in sorted(_glob.glob(pattern)):
            if os.path.isfile(os.path.join(m, "bin", "java")):
                java_home = m
    if java_home:
        log(f"Found Java SDK at {java_home}")
    return java_home


def _download_java():
    """Download Microsoft OpenJDK 17 and return the JAVA_HOME path."""
    workitem_root = os.environ.get("HELIX_WORKITEM_ROOT", ".")
    ext = "zip" if IS_WINDOWS else "tar.gz"
    jdk_archive = os.path.join(workitem_root, f"openjdk17.{ext}")
    jdk_extract = os.path.join(workitem_root, "jdk")
    download(JDK_URL, jdk_archive)
    extract(jdk_archive, jdk_extract)
    for entry in sorted(os.listdir(jdk_extract)):
        candidate = os.path.join(jdk_extract, entry)
        if os.path.isdir(candidate) and entry.startswith("jdk-"):
            log(f"Downloaded JDK JAVA_HOME={candidate}")
            return candidate
    log("ERROR: Could not find jdk-* directory after extraction")
    return None


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
    """Windows: detect device, wait if needed."""
    device_count = _count_adb_devices()
    log(f"Device count: {device_count}")
    if device_count == 0:
        log("No devices detected. Waiting for device (timeout 30s)...")
        try:
            run_cmd(["adb", "wait-for-device"], check=False, timeout=30)
        except subprocess.TimeoutExpired:
            log("WARNING: adb wait-for-device timed out")
    final_count = _count_adb_devices()
    log(f"Final device count: {final_count}")
    if final_count == 0:
        log("WARNING: No devices detected — build -t:Install will likely fail")


def _setup_adb_linux():
    """Linux: target emulator-5554, wait for boot."""
    os.environ["ANDROID_SERIAL"] = "emulator-5554"
    log("ANDROID_SERIAL=emulator-5554")
    device_count = _count_adb_devices()
    log(f"Device count: {device_count}")
    if device_count == 0:
        log("No devices detected. Waiting for device (timeout 30s)...")
        try:
            run_cmd(["timeout", "30", "adb", "wait-for-device"], check=False)
        except Exception:
            log("WARNING: adb wait-for-device timed out or failed")
        device_count = _count_adb_devices()
        log(f"Device count after wait: {device_count}")
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
    for var in ("DOTNET_ROOT", "ANDROID_HOME"):
        log_raw(f"{var}={os.environ.get(var, '')}")
    run_cmd(["dotnet", "--version"], check=False)


def setup_dotnet(correlation_payload):
    """Override DOTNET_ROOT to use the SDK from the correlation payload."""
    dotnet_root = os.path.join(correlation_payload, "dotnet")
    os.environ["DOTNET_ROOT"] = dotnet_root
    os.environ["PATH"] = dotnet_root + os.pathsep + os.environ.get("PATH", "")
    dotnet_exe = os.path.join(dotnet_root, f"dotnet{EXE}")
    log(f"DOTNET_ROOT={dotnet_root}")
    run_cmd([dotnet_exe, "--version"], check=False)
    return dotnet_exe


def install_workload(ctx):
    """Step 1: Install the maui-android workload and run workload restore."""
    log_raw("=== STEP 1: Workload Install ===", tee=True)
    result = run_cmd(
        [ctx["dotnet_exe"], "workload", "install", "maui-android",
         "--from-rollback-file",
         os.path.join(ctx["workitem_root"], "rollback_maui.json"),
         "--configfile", ctx["nuget_config"]],
        check=False,
    )
    if result.returncode != 0:
        log(f"STEP 1 FAILED with exit code {result.returncode}", tee=True)
        _dump_log()
        sys.exit(1)
    log("Workload install succeeded")
    # workload restore — non-fatal
    result = run_cmd(
        [ctx["dotnet_exe"], "workload", "restore", ctx["csproj"],
         "--configfile", ctx["nuget_config"]],
        check=False,
    )
    if result.returncode != 0:
        log(f"WARNING: workload restore returned {result.returncode} (non-fatal)")
    else:
        log("Workload restore succeeded")


def _setup_xharness_adb(correlation_payload, workitem_root):
    """Copy ADB from XHarness into a local android-sdk directory."""
    xharness_base = os.path.join(correlation_payload, "microsoft.dotnet.xharness.cli")
    xharness_dir = None
    if os.path.isdir(xharness_base):
        for d in sorted(os.listdir(xharness_base)):
            candidate = os.path.join(xharness_base, d)
            if os.path.isdir(candidate):
                xharness_dir = candidate

    adb_platform = "windows" if IS_WINDOWS else "linux"
    adb_src = os.path.join(
        xharness_dir or "", "runtimes", "any", "native", "adb", adb_platform
    )
    android_home = os.path.join(workitem_root, "android-sdk")
    platform_tools = os.path.join(android_home, "platform-tools")
    os.makedirs(platform_tools, exist_ok=True)

    if os.path.isdir(adb_src):
        for item in os.listdir(adb_src):
            src, dst = os.path.join(adb_src, item), os.path.join(platform_tools, item)
            if os.path.isdir(src):
                if os.path.exists(dst):
                    shutil.rmtree(dst)
                shutil.copytree(src, dst)
            else:
                shutil.copy2(src, dst)
        _chmod_exec(os.path.join(platform_tools, "adb"))
        log(f"Copied ADB from XHarness: {adb_src}")
    else:
        log(f"WARNING: XHarness ADB directory not found at {adb_src}")

    os.environ["ANDROID_HOME"] = android_home
    os.environ["ANDROID_SDK_ROOT"] = android_home
    os.environ["PATH"] = platform_tools + os.pathsep + os.environ.get("PATH", "")
    log(f"ANDROID_HOME={android_home}")
    return android_home


def _download_build_tools(android_home, workitem_root):
    """Download and install Android SDK Build-Tools."""
    build_tools_dir = os.path.join(android_home, "build-tools", BUILD_TOOLS_VERSION)
    bt_zip = os.path.join(workitem_root, "build-tools.zip")
    bt_extract = os.path.join(workitem_root, "build-tools-extract")
    try:
        download(BUILD_TOOLS_URL, bt_zip)
        extract(bt_zip, bt_extract)
        move_inner_contents(bt_extract, build_tools_dir)
    except Exception as e:
        log(f"ERROR: Failed to set up Build-Tools: {e}")
        return
    for tool_name in ("aapt2", "zipalign"):
        tool_path = os.path.join(build_tools_dir, f"{tool_name}{EXE}")
        if os.path.isfile(tool_path):
            _chmod_exec(tool_path)
        else:
            log(f"WARNING: {tool_name} NOT found in build-tools. Build may fail.")
    if not IS_WINDOWS and os.path.isdir(build_tools_dir):
        for item in os.listdir(build_tools_dir):
            _chmod_exec(os.path.join(build_tools_dir, item))


def _download_platform(android_home, workitem_root):
    """Download and install the Android SDK Platform (android.jar)."""
    platform_dir = os.path.join(android_home, "platforms", PLATFORM_VERSION)
    plat_zip = os.path.join(workitem_root, "platform.zip")
    plat_extract = os.path.join(workitem_root, "platform-extract")
    try:
        download(PLATFORM_URL, plat_zip)
        extract(plat_zip, plat_extract)
        move_inner_contents(plat_extract, platform_dir)
    except Exception as e:
        log(f"ERROR: Failed to set up Android Platform: {e}")
        return
    android_jar = os.path.join(platform_dir, "android.jar")
    if not os.path.isfile(android_jar):
        log(f"WARNING: android.jar NOT found at {android_jar}")


def setup_android_sdk(ctx):
    """Set up ANDROID_HOME with XHarness ADB, Build-Tools, and Platform."""
    log_raw("=== Setting up Android SDK ===")
    ctx["android_home"] = _setup_xharness_adb(
        ctx["correlation_payload"], ctx["workitem_root"]
    )
    _download_build_tools(ctx["android_home"], ctx["workitem_root"])
    _download_platform(ctx["android_home"], ctx["workitem_root"])


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
    os.environ["PERFLAB_MSBUILD_ARGS"] = ctx["msbuild_args"]
    log(f"PERFLAB_MSBUILD_ARGS={ctx['msbuild_args']}")
    test_cmd = [
        sys.executable, "test.py", "androidinnerloop",
        "--csproj-path", os.path.join("app", "MauiAndroidInnerLoop.csproj"),
        "--edit-src", os.path.join("src", "MainPage.xaml.cs"),
        "--edit-dest", os.path.join("app", "MainPage.xaml.cs"),
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

    log_raw("=== Java SDK Setup ===")
    find_java()
    if not IS_WINDOWS:
        run_cmd(["java", "-version"], check=False)

    install_workload(ctx)
    setup_android_sdk(ctx)
    setup_adb_device(ctx)
    restore_packages(ctx)
    run_test(ctx)

    log_raw("=== ALL STEPS SUCCEEDED ===", tee=True)
    _dump_log()


if __name__ == "__main__":
    main()
