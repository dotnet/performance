#!/usr/bin/env python3
"""
run.py — Unified run script for MAUI Android inner loop measurements.

Replaces platform-specific run.cmd (Windows) and run.sh (Linux).
Handles both:
  - Windows device track (Windows.11.Amd64.Pixel.Perf Helix queue)
  - Linux emulator track (Ubuntu.2204.Amd64.Android.29 Helix queue)
"""

import glob as _glob
import os
import platform
import shutil
import stat
import subprocess
import sys
import time
import zipfile
from datetime import datetime
from urllib.request import urlretrieve

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
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

# ---------------------------------------------------------------------------
# Logging helpers
# ---------------------------------------------------------------------------
_logfile = None


def _open_logfile():
    """Open the output log file if HELIX_WORKITEM_UPLOAD_ROOT is set."""
    global _logfile
    upload_root = os.environ.get("HELIX_WORKITEM_UPLOAD_ROOT")
    if upload_root:
        path = os.path.join(upload_root, "output.log")
        _logfile = open(path, "a")


def log(msg, tee=False):
    """Write *msg* with a timestamp to the log file.

    If *tee* is True, also write to stdout.
    """
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{ts}] {msg}"
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


# ---------------------------------------------------------------------------
# Subprocess helper
# ---------------------------------------------------------------------------
def run_cmd(args, check=True, **kwargs):
    """Run a command, logging stdout/stderr to the log file.

    Returns the CompletedProcess.  When *check* is True a non-zero exit code
    raises subprocess.CalledProcessError.
    """
    log(f"Running: {args}")
    result = subprocess.run(
        args,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        **kwargs,
    )
    if result.stdout:
        for line in result.stdout.splitlines():
            log_raw(line)
    if check and result.returncode != 0:
        raise subprocess.CalledProcessError(result.returncode, args, result.stdout)
    return result


# ---------------------------------------------------------------------------
# Download / extraction helpers
# ---------------------------------------------------------------------------
def download(url, dest):
    """Download *url* to *dest* using urllib."""
    log(f"Downloading {url} -> {dest}")
    urlretrieve(url, dest)
    log("Download complete")


def extract_zip(zip_path, dest_dir):
    """Extract a ZIP archive to *dest_dir*."""
    log(f"Extracting {zip_path} -> {dest_dir}")
    os.makedirs(dest_dir, exist_ok=True)
    with zipfile.ZipFile(zip_path, "r") as zf:
        zf.extractall(dest_dir)
    log("Extraction complete")


def extract_tar(tar_path, dest_dir):
    """Extract a tar.gz archive to *dest_dir*."""
    import tarfile

    log(f"Extracting {tar_path} -> {dest_dir}")
    os.makedirs(dest_dir, exist_ok=True)
    with tarfile.open(tar_path, "r:gz") as tf:
        tf.extractall(dest_dir)
    log("Extraction complete")


def move_inner_contents(extract_dir, target_dir):
    """Move contents from each subdirectory inside *extract_dir* into
    *target_dir*.

    ZIPs from Google typically contain one top-level directory; this
    flattens that layer.
    """
    os.makedirs(target_dir, exist_ok=True)
    subdirs = [
        os.path.join(extract_dir, d)
        for d in os.listdir(extract_dir)
        if os.path.isdir(os.path.join(extract_dir, d))
    ]
    for subdir in subdirs:
        log(f"Moving contents from {subdir} to {target_dir}")
        for item in os.listdir(subdir):
            src = os.path.join(subdir, item)
            dst = os.path.join(target_dir, item)
            if os.path.isdir(src):
                if os.path.exists(dst):
                    shutil.rmtree(dst)
                shutil.copytree(src, dst)
            else:
                shutil.copy2(src, dst)


def _chmod_exec(path):
    """Make *path* executable (no-op on Windows)."""
    if not IS_WINDOWS and os.path.isfile(path):
        os.chmod(path, os.stat(path).st_mode | stat.S_IEXEC | stat.S_IXGRP | stat.S_IXOTH)


# ---------------------------------------------------------------------------
# Java SDK discovery
# ---------------------------------------------------------------------------
def find_java():
    """Find or download a Java SDK.  Sets JAVA_HOME and prepends bin to PATH."""
    java_home = os.environ.get("JAVA_HOME", "")
    if java_home and os.path.isfile(os.path.join(java_home, "bin", f"java{EXE}")):
        log(f"JAVA_HOME already set: {java_home}")
        _apply_java_home(java_home)
        return

    log("JAVA_HOME is empty — searching for Java SDK...")

    if IS_WINDOWS:
        java_home = _find_java_windows()
    else:
        java_home = _find_java_linux()

    if not java_home:
        log("Java not found in common paths. Downloading Microsoft OpenJDK 17...")
        java_home = _download_java()

    if java_home:
        _apply_java_home(java_home)
    else:
        log("ERROR: Java SDK still not found after download attempt")
        if IS_WINDOWS:
            # Dump directory listings for debugging
            for env_var in ("ProgramW6432", "ProgramFiles"):
                base = os.environ.get(env_var, "")
                ms_dir = os.path.join(base, "Microsoft") if base else ""
                if ms_dir and os.path.isdir(ms_dir):
                    log_raw(f"Contents of {ms_dir}:")
                    for item in os.listdir(ms_dir):
                        log_raw(f"  {item}")


def _apply_java_home(java_home):
    os.environ["JAVA_HOME"] = java_home
    java_bin = os.path.join(java_home, "bin")
    os.environ["PATH"] = java_bin + os.pathsep + os.environ.get("PATH", "")
    log(f"JAVA_HOME={java_home}")


def _find_java_windows():
    """Search common Windows JDK installation paths."""
    search_patterns = []
    for env_var in ("ProgramW6432", "ProgramFiles"):
        base = os.environ.get(env_var, "")
        if not base:
            continue
        search_patterns.extend([
            os.path.join(base, "Microsoft", "jdk-*"),
            os.path.join(base, "Android", "openjdk", "jdk-*"),
            os.path.join(base, "Java", "jdk-*"),
            os.path.join(base, "Eclipse Adoptium", "jdk-*"),
        ])

    java_home = None
    for pattern in search_patterns:
        matches = sorted(_glob.glob(pattern))
        if matches:
            java_home = matches[-1]  # newest
            break

    if not java_home:
        # Fallback: derive from 'where java'
        result = subprocess.run(
            ["where", "java"], capture_output=True, text=True
        )
        if result.returncode == 0 and result.stdout.strip():
            java_exe = result.stdout.strip().splitlines()[0]
            candidate = os.path.normpath(os.path.join(java_exe, "..", ".."))
            if os.path.isfile(os.path.join(candidate, "bin", "java.exe")):
                java_home = candidate

    if java_home:
        log(f"Found Java SDK at {java_home}")
    return java_home


def _find_java_linux():
    """Search common Linux JDK installation paths."""
    java_home = None
    for pattern in [
        "/usr/lib/jvm/msopenjdk-*",
        "/usr/lib/jvm/temurin-*",
        "/usr/lib/jvm/java-*",
    ]:
        # sort by version; keep iterating to pick the newest match
        for m in sorted(_glob.glob(pattern)):
            if os.path.isfile(os.path.join(m, "bin", "java")):
                java_home = m

    if not java_home:
        # Fallback: derive from 'which java'
        result = subprocess.run(
            ["which", "java"], capture_output=True, text=True
        )
        if result.returncode == 0 and result.stdout.strip():
            java_real = os.path.realpath(result.stdout.strip())
            candidate = os.path.dirname(os.path.dirname(java_real))
            if os.path.isfile(os.path.join(candidate, "bin", "java")):
                java_home = candidate

    if java_home:
        log(f"Found Java SDK at {java_home}")
    return java_home


def _download_java():
    """Download Microsoft OpenJDK 17 and return the JAVA_HOME path."""
    workitem_root = os.environ.get("HELIX_WORKITEM_ROOT", ".")
    if IS_WINDOWS:
        jdk_archive = os.path.join(workitem_root, "openjdk17.zip")
    else:
        jdk_archive = os.path.join(workitem_root, "openjdk17.tar.gz")
    jdk_extract = os.path.join(workitem_root, "jdk")

    try:
        download(JDK_URL, jdk_archive)
    except Exception as e:
        log(f"ERROR: Failed to download OpenJDK: {e}")
        return None

    try:
        if IS_WINDOWS:
            extract_zip(jdk_archive, jdk_extract)
        else:
            extract_tar(jdk_archive, jdk_extract)
    except Exception as e:
        log(f"ERROR: Failed to extract OpenJDK: {e}")
        return None

    # Find the extracted jdk-* directory
    for entry in sorted(os.listdir(jdk_extract)):
        candidate = os.path.join(jdk_extract, entry)
        if os.path.isdir(candidate) and entry.startswith("jdk-"):
            log(f"Downloaded JDK JAVA_HOME={candidate}")
            return candidate

    log("ERROR: Could not find jdk-* directory after extraction")
    if os.path.isdir(jdk_extract):
        log_raw("Contents of jdk extract dir:")
        for item in os.listdir(jdk_extract):
            log_raw(f"  {item}")
    return None


# ---------------------------------------------------------------------------
# ADB device setup (platform-specific)
# ---------------------------------------------------------------------------
def _count_adb_devices():
    """Count the number of ADB devices in 'device' state."""
    result = subprocess.run(
        ["adb", "devices"], capture_output=True, text=True
    )
    count = 0
    for line in result.stdout.splitlines()[1:]:  # skip header
        parts = line.split()
        if len(parts) >= 2 and parts[1] == "device":
            count += 1
    return count


def _setup_adb_windows(android_home):
    """Windows: detect Pixel device, count devices, wait if needed."""
    # Check USB devices for Android hardware
    log_raw("--- USB Android devices (wmic) ---")
    run_cmd(
        ["wmic", "path", "Win32_PnPEntity", "where",
         "Name like '%Android%'", "get", "Name,DeviceID"],
        check=False,
    )

    device_count = _count_adb_devices()
    log_raw(f"Device count: {device_count}")

    if device_count == 0:
        log_raw("*** CRITICAL: NO DEVICES DETECTED ***")
        log_raw("This indicates a hardware/driver issue, not a software one.")
        log_raw("Possible causes:")
        log_raw("  - No Android device physically connected")
        log_raw("  - USB driver not installed")
        log_raw("  - Device not authorized for USB debugging")
        log_raw("  - ADB binary incompatible with device")

        adb_full = os.path.join(android_home, "platform-tools", "adb.exe")
        log(f"Retrying with full ADB path: {adb_full}")
        run_cmd([adb_full, "devices", "-l"], check=False)

        log("Waiting for device (timeout 30s)...")
        try:
            run_cmd(["adb", "wait-for-device"], check=False, timeout=30)
        except subprocess.TimeoutExpired:
            log("WARNING: adb wait-for-device timed out")
    else:
        log_raw("Devices detected. Proceeding.")

    final_count = _count_adb_devices()
    log_raw(f"Final device count: {final_count}")
    if final_count == 0:
        log_raw("*** CRITICAL: STILL NO DEVICES AFTER ALL DIAGNOSTICS ***")
        log_raw("The build -t:Install step WILL FAIL with XA0010.")


def _setup_adb_linux():
    """Linux: target emulator-5554, wait for boot."""
    # Pin to emulator-5554 (avoids "more than one device/emulator" errors)
    os.environ["ANDROID_SERIAL"] = "emulator-5554"
    log(f"ANDROID_SERIAL={os.environ['ANDROID_SERIAL']}")

    device_count = _count_adb_devices()
    log_raw(f"Device count: {device_count}")

    if device_count == 0:
        log_raw("*** CRITICAL: NO DEVICES DETECTED ***")
        log("Checking if emulator process is running...")

        log_raw("--- Emulator processes ---")
        result = subprocess.run(
            "ps aux 2>/dev/null | grep -i emulator | grep -v grep",
            shell=True, capture_output=True, text=True,
        )
        if result.stdout.strip():
            for line in result.stdout.strip().splitlines():
                log_raw(f"  {line}")
        else:
            log_raw("  No emulator processes found")

        log_raw("--- Port 5554 (emulator) check ---")
        port_result = subprocess.run(
            "ss -tlnp 2>/dev/null | grep 5554 || "
            "netstat -tlnp 2>/dev/null | grep 5554",
            shell=True, capture_output=True, text=True,
        )
        if port_result.stdout.strip():
            log_raw(port_result.stdout.strip())
        else:
            log_raw("  Nothing listening on port 5554")

        log("Trying adb connect localhost:5554...")
        run_cmd(["adb", "connect", "localhost:5554"], check=False)

        log("Devices after connect attempt:")
        run_cmd(["adb", "devices", "-l"], check=False)

        log("Waiting for device (timeout 30s)...")
        try:
            run_cmd(["timeout", "30", "adb", "wait-for-device"], check=False)
        except Exception:
            log("WARNING: adb wait-for-device timed out or failed")
    else:
        log_raw("Devices detected. Proceeding.")

    final_count = _count_adb_devices()
    log_raw(f"Final device count: {final_count}")
    if final_count == 0:
        log_raw("*** CRITICAL: STILL NO DEVICES AFTER ALL DIAGNOSTICS ***")
        log_raw("The build -t:Install step WILL FAIL with XA0010.")

    # Wait for emulator boot if a device is present
    if final_count > 0:
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
            log_raw(f"  sys.boot_completed={boot_completed} (waited {boot_wait}s)")
        if boot_completed != "1":
            log(f"WARNING: Emulator did not report sys.boot_completed=1 "
                f"after 60s (got '{boot_completed}')")
            log("Continuing anyway — deploy may fail")


# ---------------------------------------------------------------------------
# Dump the full log to stdout (for Helix diagnostics)
# ---------------------------------------------------------------------------
def _dump_log():
    if _logfile:
        _logfile.flush()
        log_path = _logfile.name
        try:
            with open(log_path, "r") as f:
                print(f.read())
        except Exception:
            pass


# ---------------------------------------------------------------------------
# Orchestration functions
# ---------------------------------------------------------------------------
def parse_args():
    """Parse CLI arguments and return a dict."""
    if len(sys.argv) < 4:
        print(
            f"Usage: {sys.argv[0]} FRAMEWORK MSBUILD_ARGS SCENARIO_NAME "
            f"[EXTRA_ARGS...]",
            file=sys.stderr,
        )
        sys.exit(1)
    return {
        "framework": sys.argv[1],
        "msbuild_args": sys.argv[2],
        "scenario_name": sys.argv[3],
        "extra_args": sys.argv[4:],
    }


def setup_logging():
    """Open the output log file."""
    _open_logfile()


def print_diagnostics():
    """Log essential environment variables and tool locations."""
    log_raw("=== DIAGNOSTICS ===", tee=True)
    for var in ("DOTNET_ROOT", "ANDROID_HOME", "ANDROID_SDK_ROOT", "NUGET_PACKAGES"):
        log_raw(f"{var}={os.environ.get(var, '')}")
    which_cmd = "where" if IS_WINDOWS else "which"
    python_name = "python" if IS_WINDOWS else "python3"
    for tool in ["adb", "dotnet", python_name]:
        run_cmd([which_cmd, tool], check=False)
    run_cmd(["dotnet", "--version"], check=False)
    log_raw("")


def setup_dotnet(correlation_payload):
    """Override DOTNET_ROOT to use the SDK from the correlation payload.

    ci_setup.py installs the .NET SDK into $HELIX_CORRELATION_PAYLOAD/dotnet
    but DOTNET_ROOT may point to a different (older) SDK.  Override it.

    Returns the path to the dotnet executable.
    """
    dotnet_root = os.path.join(correlation_payload, "dotnet")
    os.environ["DOTNET_ROOT"] = dotnet_root
    os.environ["PATH"] = dotnet_root + os.pathsep + os.environ.get("PATH", "")
    dotnet_exe = os.path.join(dotnet_root, f"dotnet{EXE}")
    log(f"DOTNET_ROOT={dotnet_root}")
    run_cmd([dotnet_exe, "--version"], check=False)
    log_raw("")
    return dotnet_exe


def setup_java():
    """Find or download a Java SDK and set JAVA_HOME."""
    log_raw("=== Java SDK Setup ===")
    find_java()
    if not IS_WINDOWS:
        run_cmd(["java", "-version"], check=False)
    log_raw("")


def install_workload(ctx):
    """Step 1: Install the maui-android workload and run workload restore.

    Exits with code 1 on failure.
    """
    log_raw("=== STEP 1: Workload Install ===", tee=True)
    result = run_cmd(
        [
            ctx["dotnet_exe"], "workload", "install", "maui-android",
            "--from-rollback-file",
            os.path.join(ctx["workitem_root"], "rollback_maui.json"),
            "--configfile", ctx["nuget_config"],
        ],
        check=False,
    )
    if result.returncode != 0:
        log(f"STEP 1 FAILED with exit code {result.returncode}", tee=True)
        _dump_log()
        sys.exit(1)
    log("Workload install succeeded")

    # workload restore — installs implicit workload deps (non-fatal)
    result = run_cmd(
        [ctx["dotnet_exe"], "workload", "restore", ctx["csproj"],
         "--configfile", ctx["nuget_config"]],
        check=False,
    )
    if result.returncode != 0:
        log(f"WARNING: dotnet workload restore returned {result.returncode} "
            f"(non-fatal)")
    else:
        log("Workload restore succeeded")
    log_raw("")


def _setup_xharness_adb(correlation_payload, workitem_root):
    """Copy ADB from XHarness into a local android-sdk directory.

    Sets ANDROID_HOME, ANDROID_SDK_ROOT, and prepends platform-tools to PATH.
    Returns the android_home path.
    """
    xharness_base = os.path.join(
        correlation_payload, "microsoft.dotnet.xharness.cli"
    )
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
            src = os.path.join(adb_src, item)
            dst = os.path.join(platform_tools, item)
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
        if os.path.isdir(xharness_base):
            log_raw(f"Contents of {xharness_base}:")
            for item in os.listdir(xharness_base):
                log_raw(f"  {item}")

    os.environ["ANDROID_HOME"] = android_home
    os.environ["ANDROID_SDK_ROOT"] = android_home
    os.environ["PATH"] = platform_tools + os.pathsep + os.environ.get("PATH", "")
    log(f"ANDROID_HOME={android_home}")
    return android_home


def _download_build_tools(android_home, workitem_root):
    """Download and install Android SDK Build-Tools (aapt2, zipalign)."""
    build_tools_dir = os.path.join(
        android_home, "build-tools", BUILD_TOOLS_VERSION
    )
    bt_zip = os.path.join(workitem_root, "build-tools.zip")
    bt_extract = os.path.join(workitem_root, "build-tools-extract")

    try:
        download(BUILD_TOOLS_URL, bt_zip)
        extract_zip(bt_zip, bt_extract)
        move_inner_contents(bt_extract, build_tools_dir)
    except Exception as e:
        log(f"ERROR: Failed to set up Build-Tools: {e}")
        log("Build will likely fail with XA5205.")
        return

    for tool_name in ("aapt2", "zipalign"):
        tool_path = os.path.join(build_tools_dir, f"{tool_name}{EXE}")
        if os.path.isfile(tool_path):
            _chmod_exec(tool_path)
            log(f"{tool_name} found at {tool_path}")
        else:
            log(f"WARNING: {tool_name} NOT found. Build may fail.")

    # Ensure all binaries are executable on Linux
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
        extract_zip(plat_zip, plat_extract)
        move_inner_contents(plat_extract, platform_dir)
    except Exception as e:
        log(f"ERROR: Failed to set up Android Platform: {e}")
        return

    android_jar = os.path.join(platform_dir, "android.jar")
    if os.path.isfile(android_jar):
        log(f"android.jar found at {android_jar}")
    else:
        log(f"WARNING: android.jar NOT found at {android_jar}. "
            f"Build will likely fail.")


def setup_android_sdk(ctx):
    """Set up ANDROID_HOME with XHarness ADB, Build-Tools, and Platform."""
    log_raw("=== Setting up Android SDK ===")
    ctx["android_home"] = _setup_xharness_adb(
        ctx["correlation_payload"], ctx["workitem_root"]
    )
    _download_build_tools(ctx["android_home"], ctx["workitem_root"])
    _download_platform(ctx["android_home"], ctx["workitem_root"])
    log_raw("")


def setup_adb_device(ctx):
    """Restart ADB server, list devices, and run platform-specific setup."""
    log_raw("=== ADB DEVICE SETUP ===")
    run_cmd(["adb", "version"], check=False)

    log("Restarting ADB server...")
    run_cmd(["adb", "kill-server"], check=False)
    run_cmd(["adb", "start-server"], check=False)

    log("Initial device listing:")
    run_cmd(["adb", "devices", "-l"], check=False)

    if IS_WINDOWS:
        _setup_adb_windows(ctx["android_home"])
    else:
        _setup_adb_linux()

    log("Final device listing:")
    run_cmd(["adb", "devices", "-l"], check=False)
    log_raw("")


def restore_packages(ctx):
    """Step 2: Restore NuGet packages.

    Exits with code 2 on failure.
    """
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
    log_raw("")


def run_test(ctx):
    """Step 3: Run test.py for the inner-loop measurement.

    Exits with code 3 on failure.
    """
    log_raw("=== STEP 3: Test ===", tee=True)

    # Pass MSBuild args via environment variable to avoid shell quoting
    # issues.  runner.py reads PERFLAB_MSBUILD_ARGS as a fallback when
    # --msbuild-args is empty.
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

    result = run_cmd(test_cmd, check=False)
    if result.returncode != 0:
        log(f"STEP 3 FAILED with exit code {result.returncode}", tee=True)
        _dump_log()
        sys.exit(3)
    log("test.py succeeded")
    log_raw("")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
def main():
    args = parse_args()
    setup_logging()

    workitem_root = os.environ.get("HELIX_WORKITEM_ROOT", ".")
    correlation_payload = os.environ.get("HELIX_CORRELATION_PAYLOAD", ".")

    ctx = {
        "framework": args["framework"],
        "msbuild_args": args["msbuild_args"],
        "scenario_name": args["scenario_name"],
        "extra_args": args["extra_args"],
        "workitem_root": workitem_root,
        "correlation_payload": correlation_payload,
        "nuget_config": os.path.join(workitem_root, "app", "NuGet.config"),
        "csproj": os.path.join(workitem_root, "app", "MauiAndroidInnerLoop.csproj"),
    }

    print_diagnostics()
    ctx["dotnet_exe"] = setup_dotnet(correlation_payload)
    setup_java()

    install_workload(ctx)      # Step 1
    setup_android_sdk(ctx)     # XHarness ADB + Build-Tools + Platform
    setup_adb_device(ctx)      # Device detection / emulator wait
    restore_packages(ctx)      # Step 2
    run_test(ctx)              # Step 3

    log_raw("=== ALL STEPS SUCCEEDED ===", tee=True)
    _dump_log()


if __name__ == "__main__":
    main()
