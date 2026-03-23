#!/usr/bin/env bash
# run.sh — Linux emulator track for MAUI Android inner loop measurements.
# Runs on Ubuntu.2204.Amd64.Android.29 Helix queue.
# NOTE: The queue does NOT have Android SDK or ADB pre-installed.
# We set up a minimal Android SDK from XHarness (ADB) and Google downloads
# (build-tools, platform), same as the Windows track (run.cmd).

set -e

FRAMEWORK="$1"
MSBUILD_ARGS="$2"
SCENARIO_NAME="$3"
# Remaining arguments (e.g. --upload-to-perflab-container from ScenarioArgs)
shift 3
EXTRA_ARGS=("$@")

LOGFILE="$HELIX_WORKITEM_UPLOAD_ROOT/output.log"

# On any error, log the failure location and dump the log for Helix diagnostics.
on_error() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] FAILED at line $1" | tee -a "$LOGFILE"
    echo "=== DUMPING FULL LOG ===" 
    cat "$LOGFILE"
    exit 1
}
trap 'on_error $LINENO' ERR

# ci_setup.py installs the .NET 11 SDK into $HELIX_CORRELATION_PAYLOAD/dotnet
# but DOTNET_ROOT points to $HELIX_CORRELATION_PAYLOAD/dotnet-cli (has .NET 8).
# Override DOTNET_ROOT to use the correct SDK for building.
export DOTNET_ROOT="$HELIX_CORRELATION_PAYLOAD/dotnet"
export PATH="$DOTNET_ROOT:$PATH"

# === Discover Java SDK ===
# Java 8 is pre-installed at /usr/lib/jvm/java-8-openjdk-amd64 on this queue.
# NOTE: MAUI Android may require Java 11+ — if builds fail with Java version
# errors, a JDK download step (similar to run.cmd) will be needed here.
if [ -z "$JAVA_HOME" ]; then
    echo "JAVA_HOME is empty — searching for Java SDK..." >> "$LOGFILE" 2>&1
    JAVA_FOUND=""

    # Check common JDK paths, prefer newest version
    for pattern in /usr/lib/jvm/msopenjdk-* /usr/lib/jvm/temurin-* /usr/lib/jvm/java-*; do
        for jdir in $(ls -1d $pattern 2>/dev/null | sort -V); do
            if [ -x "$jdir/bin/java" ]; then
                JAVA_FOUND="$jdir"
            fi
        done
    done

    # Fallback: derive from java binary location
    if [ -z "$JAVA_FOUND" ]; then
        JAVA_PATH=$(which java 2>/dev/null || true)
        if [ -n "$JAVA_PATH" ]; then
            JAVA_REAL=$(readlink -f "$JAVA_PATH" 2>/dev/null || echo "$JAVA_PATH")
            JAVA_BIN_DIR=$(dirname "$JAVA_REAL")
            DERIVED_JDK=$(dirname "$JAVA_BIN_DIR")
            if [ -x "$DERIVED_JDK/bin/java" ]; then
                JAVA_FOUND="$DERIVED_JDK"
            fi
        fi
    fi

    if [ -n "$JAVA_FOUND" ]; then
        export JAVA_HOME="$JAVA_FOUND"
        echo "  Found Java SDK at $JAVA_HOME" >> "$LOGFILE" 2>&1
    else
        echo "  WARNING: Could not find Java SDK in any known location" >> "$LOGFILE" 2>&1
    fi
fi

if [ -n "$JAVA_HOME" ]; then
    export PATH="$JAVA_HOME/bin:$PATH"
fi

echo "=== DIAGNOSTICS ===" | tee -a "$LOGFILE"
echo "DOTNET_ROOT=$DOTNET_ROOT" | tee -a "$LOGFILE"
echo "JAVA_HOME=$JAVA_HOME" | tee -a "$LOGFILE"
echo "NUGET_PACKAGES=$NUGET_PACKAGES" | tee -a "$LOGFILE"
echo "PYTHONPATH=$PYTHONPATH" | tee -a "$LOGFILE"
which dotnet >> "$LOGFILE" 2>&1 || true
which java >> "$LOGFILE" 2>&1 || true
which python3 >> "$LOGFILE" 2>&1 || true
"$DOTNET_ROOT/dotnet" --version >> "$LOGFILE" 2>&1
java -version >> "$LOGFILE" 2>&1 || echo "WARNING: java -version failed" >> "$LOGFILE" 2>&1
echo "" >> "$LOGFILE" 2>&1

# === STEP 1: Workload Install ===
echo "=== STEP 1: Workload Install ===" | tee -a "$LOGFILE"
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting workload install" >> "$LOGFILE" 2>&1
"$DOTNET_ROOT/dotnet" workload install maui-android \
    --from-rollback-file "$HELIX_WORKITEM_ROOT/rollback_maui.json" \
    --configfile "$HELIX_WORKITEM_ROOT/app/NuGet.config" \
    >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Workload install succeeded" >> "$LOGFILE" 2>&1

# dotnet workload restore reads the .csproj and installs any implicit workload
# dependencies the SDK requires (e.g. ios workload pulled in by MAUI SDK even
# when only targeting Android).
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Running dotnet workload restore" >> "$LOGFILE" 2>&1
echo "dotnet workload restore $HELIX_WORKITEM_ROOT/app/MauiAndroidInnerLoop.csproj --configfile $HELIX_WORKITEM_ROOT/app/NuGet.config" >> "$LOGFILE" 2>&1
if "$DOTNET_ROOT/dotnet" workload restore \
    "$HELIX_WORKITEM_ROOT/app/MauiAndroidInnerLoop.csproj" \
    --configfile "$HELIX_WORKITEM_ROOT/app/NuGet.config" \
    >> "$LOGFILE" 2>&1; then
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Workload restore succeeded" >> "$LOGFILE" 2>&1
else
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] WARNING: dotnet workload restore failed (non-fatal)" >> "$LOGFILE" 2>&1
fi
echo "" >> "$LOGFILE" 2>&1

# === Set up ANDROID_HOME from XHarness bundled ADB ===
# The Helix queue does NOT have ANDROID_HOME set or ADB available.
# XHarness ships a bundled ADB, so we create a minimal Android SDK directory
# and point ANDROID_HOME at it.
echo "=== Setting up Android SDK ===" >> "$LOGFILE" 2>&1
XHARNESS_DIR=$(ls -1d "$HELIX_CORRELATION_PAYLOAD/microsoft.dotnet.xharness.cli"/*/ 2>/dev/null | head -1)
XHARNESS_DIR="${XHARNESS_DIR%/}"
ADB_SRC="$XHARNESS_DIR/runtimes/any/native/adb/linux"
export ANDROID_HOME="$HELIX_WORKITEM_ROOT/android-sdk"
mkdir -p "$ANDROID_HOME/platform-tools"
if [ -d "$ADB_SRC" ]; then
    cp -a "$ADB_SRC"/* "$ANDROID_HOME/platform-tools/"
    chmod +x "$ANDROID_HOME/platform-tools/adb" 2>/dev/null || true
    echo "Copied ADB from XHarness: $ADB_SRC" >> "$LOGFILE" 2>&1
else
    echo "WARNING: XHarness ADB directory not found at $ADB_SRC" >> "$LOGFILE" 2>&1
    echo "XHARNESS_DIR=$XHARNESS_DIR" >> "$LOGFILE" 2>&1
    ls -la "$HELIX_CORRELATION_PAYLOAD/microsoft.dotnet.xharness.cli/" >> "$LOGFILE" 2>&1 || true
fi
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export PATH="$ANDROID_HOME/platform-tools:$PATH"
echo "ANDROID_HOME=$ANDROID_HOME" >> "$LOGFILE" 2>&1
which adb >> "$LOGFILE" 2>&1 || echo "WARNING: adb not on PATH" >> "$LOGFILE" 2>&1
echo "" >> "$LOGFILE" 2>&1

# === Set up Android Build-Tools (aapt2, zipalign) ===
# dotnet build for Android requires aapt2 and zipalign from Android SDK
# Build-Tools.  Download the complete package from Google.
echo "=== Android Build-Tools Setup ===" >> "$LOGFILE" 2>&1
BUILD_TOOLS_DIR="$ANDROID_HOME/build-tools/35.0.0"

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Downloading Android SDK Build-Tools from Google..." >> "$LOGFILE" 2>&1
BT_ZIP="$HELIX_WORKITEM_ROOT/build-tools.zip"
BT_EXTRACT="$HELIX_WORKITEM_ROOT/build-tools-extract"
if ! curl -L -o "$BT_ZIP" "https://dl.google.com/android/repository/build-tools_r35_linux.zip" >> "$LOGFILE" 2>&1; then
    echo "ERROR: Failed to download Build-Tools. Build will likely fail with XA5205." >> "$LOGFILE" 2>&1
else
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Download complete. Extracting..." >> "$LOGFILE" 2>&1
    mkdir -p "$BT_EXTRACT"
    unzip -q "$BT_ZIP" -d "$BT_EXTRACT" >> "$LOGFILE" 2>&1
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Extraction complete" >> "$LOGFILE" 2>&1
    mkdir -p "$BUILD_TOOLS_DIR"
    # Find the top-level directory inside the ZIP (e.g. android-15/)
    for d in "$BT_EXTRACT"/*/; do
        echo "Moving contents from $d to $BUILD_TOOLS_DIR" >> "$LOGFILE" 2>&1
        cp -a "$d"* "$BUILD_TOOLS_DIR/" >> "$LOGFILE" 2>&1
    done
fi

if [ -f "$BUILD_TOOLS_DIR/aapt2" ]; then
    chmod +x "$BUILD_TOOLS_DIR/aapt2"
    echo "aapt2 found at $BUILD_TOOLS_DIR/aapt2" >> "$LOGFILE" 2>&1
else
    echo "WARNING: aapt2 NOT found. Build will likely fail with XA5205." >> "$LOGFILE" 2>&1
fi
if [ -f "$BUILD_TOOLS_DIR/zipalign" ]; then
    chmod +x "$BUILD_TOOLS_DIR/zipalign"
    echo "zipalign found at $BUILD_TOOLS_DIR/zipalign" >> "$LOGFILE" 2>&1
else
    echo "WARNING: zipalign NOT found. Build may fail." >> "$LOGFILE" 2>&1
fi
ls -la "$BUILD_TOOLS_DIR" >> "$LOGFILE" 2>&1 || true
echo "" >> "$LOGFILE" 2>&1

# === Set up android.jar (platforms) ===
# android.jar is a Google Android SDK Platform artifact — it is NOT bundled
# in any .NET MAUI workload pack.  The CI Android SDK (36.99.0-ci.main.0)
# requires API level 36.1.  Download the platform ZIP from Google and place
# android.jar at ANDROID_HOME/platforms/android-36.1/android.jar where
# MSBuild expects it.
echo "=== Android Platforms (android.jar) Setup ===" >> "$LOGFILE" 2>&1
PLATFORM_DIR="$ANDROID_HOME/platforms/android-36.1"

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Downloading Android SDK Platform from Google..." >> "$LOGFILE" 2>&1
PLAT_ZIP="$HELIX_WORKITEM_ROOT/platform.zip"
PLAT_EXTRACT="$HELIX_WORKITEM_ROOT/platform-extract"
if ! curl -L -o "$PLAT_ZIP" "https://dl.google.com/android/repository/platform-36.1_r01.zip" >> "$LOGFILE" 2>&1; then
    echo "ERROR: Failed to download Android SDK Platform. Build will likely fail." >> "$LOGFILE" 2>&1
else
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Download complete. Extracting..." >> "$LOGFILE" 2>&1
    mkdir -p "$PLAT_EXTRACT"
    unzip -q "$PLAT_ZIP" -d "$PLAT_EXTRACT" >> "$LOGFILE" 2>&1
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Extraction complete" >> "$LOGFILE" 2>&1
    # The ZIP contains a top-level directory (e.g. android-16/) with android.jar inside
    mkdir -p "$PLATFORM_DIR"
    for d in "$PLAT_EXTRACT"/*/; do
        echo "Moving contents from $d to $PLATFORM_DIR" >> "$LOGFILE" 2>&1
        cp -a "$d"* "$PLATFORM_DIR/" >> "$LOGFILE" 2>&1
    done
fi

if [ -f "$PLATFORM_DIR/android.jar" ]; then
    echo "android.jar found at $PLATFORM_DIR/android.jar" >> "$LOGFILE" 2>&1
else
    echo "WARNING: android.jar NOT found at $PLATFORM_DIR/android.jar. Build will likely fail." >> "$LOGFILE" 2>&1
fi
ls -la "$PLATFORM_DIR" >> "$LOGFILE" 2>&1 || true
echo "" >> "$LOGFILE" 2>&1

# === ADB Device Setup ===
# Start the ADB server and verify the emulator is visible. On the emulator
# queue the emulator should already be running at emulator-5554.
echo "=== ADB DEVICE SETUP ===" >> "$LOGFILE" 2>&1

# Log environment for debugging connectivity issues
echo "[$(date '+%Y-%m-%d %H:%M:%S')] ADB diagnostics starting" >> "$LOGFILE" 2>&1
echo "ANDROID_HOME=$ANDROID_HOME" >> "$LOGFILE" 2>&1
echo "PATH=$PATH" >> "$LOGFILE" 2>&1

# Log ADB binary location and version
echo "--- ADB binary info ---" >> "$LOGFILE" 2>&1
which adb >> "$LOGFILE" 2>&1 || echo "CRITICAL: adb not found on PATH" >> "$LOGFILE" 2>&1
adb version >> "$LOGFILE" 2>&1 || echo "CRITICAL: adb version failed" >> "$LOGFILE" 2>&1

# Kill any existing ADB server and start fresh
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Killing existing ADB server..." >> "$LOGFILE" 2>&1
adb kill-server >> "$LOGFILE" 2>&1 || echo "WARNING: adb kill-server failed (may not have been running)" >> "$LOGFILE" 2>&1

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting fresh ADB server..." >> "$LOGFILE" 2>&1
adb start-server >> "$LOGFILE" 2>&1 || echo "WARNING: adb start-server failed" >> "$LOGFILE" 2>&1

# First device listing (verbose)
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Initial device listing:" >> "$LOGFILE" 2>&1
adb devices -l >> "$LOGFILE" 2>&1 || echo "WARNING: adb devices failed" >> "$LOGFILE" 2>&1

# Count devices (skip header line)
DEVICE_COUNT=$(adb devices 2>/dev/null | tail -n +2 | grep -c -w "device" || true)
echo "Device count: $DEVICE_COUNT" >> "$LOGFILE" 2>&1

if [ "$DEVICE_COUNT" -eq 0 ] 2>/dev/null || [ -z "$DEVICE_COUNT" ]; then
    echo "*** CRITICAL: NO DEVICES DETECTED ***" >> "$LOGFILE" 2>&1
    echo "Checking if emulator process is running..." >> "$LOGFILE" 2>&1

    # Check for running emulator processes
    echo "--- Emulator processes ---" >> "$LOGFILE" 2>&1
    ps aux 2>/dev/null | grep -i emulator | grep -v grep >> "$LOGFILE" 2>&1 || echo "  No emulator processes found" >> "$LOGFILE" 2>&1

    # Check if anything is listening on the default emulator port
    echo "--- Port 5554 (emulator) check ---" >> "$LOGFILE" 2>&1
    ss -tlnp 2>/dev/null | grep 5554 >> "$LOGFILE" 2>&1 || \
        netstat -tlnp 2>/dev/null | grep 5554 >> "$LOGFILE" 2>&1 || \
        echo "  Nothing listening on port 5554" >> "$LOGFILE" 2>&1

    # Try connecting to the emulator explicitly
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Trying adb connect localhost:5554..." >> "$LOGFILE" 2>&1
    adb connect localhost:5554 >> "$LOGFILE" 2>&1 || echo "WARNING: adb connect localhost:5554 failed" >> "$LOGFILE" 2>&1

    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Devices after connect attempt:" >> "$LOGFILE" 2>&1
    adb devices -l >> "$LOGFILE" 2>&1 || echo "WARNING: adb devices failed" >> "$LOGFILE" 2>&1

    # Wait for device with 30-second timeout
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Waiting for device (timeout 30s)..." >> "$LOGFILE" 2>&1
    timeout 30 adb wait-for-device >> "$LOGFILE" 2>&1 || echo "WARNING: adb wait-for-device timed out or failed" >> "$LOGFILE" 2>&1
else
    echo "Devices detected. Proceeding." >> "$LOGFILE" 2>&1
fi

# Final device listing after all diagnostics
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Final device listing:" >> "$LOGFILE" 2>&1
adb devices -l >> "$LOGFILE" 2>&1 || echo "WARNING: adb devices failed" >> "$LOGFILE" 2>&1

# Re-count devices after diagnostics
FINAL_COUNT=$(adb devices 2>/dev/null | tail -n +2 | grep -c -w "device" || true)
echo "Final device count: $FINAL_COUNT" >> "$LOGFILE" 2>&1
if [ "$FINAL_COUNT" -eq 0 ] 2>/dev/null || [ -z "$FINAL_COUNT" ]; then
    echo "*** CRITICAL: STILL NO DEVICES AFTER ALL DIAGNOSTICS ***" >> "$LOGFILE" 2>&1
    echo "The build -t:Install step WILL FAIL with XA0010." >> "$LOGFILE" 2>&1
fi

# Check emulator boot status if a device is present
if [ "$FINAL_COUNT" -gt 0 ] 2>/dev/null; then
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Waiting for emulator to fully boot (up to 60s)..." >> "$LOGFILE" 2>&1
    BOOT_WAIT=0
    BOOT_COMPLETED=""
    while [ "$BOOT_WAIT" -lt 60 ]; do
        BOOT_COMPLETED=$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r\n' || true)
        if [ "$BOOT_COMPLETED" = "1" ]; then
            echo "[$(date '+%Y-%m-%d %H:%M:%S')] Emulator fully booted after ${BOOT_WAIT}s" >> "$LOGFILE" 2>&1
            break
        fi
        sleep 5
        BOOT_WAIT=$((BOOT_WAIT + 5))
        echo "  sys.boot_completed=$BOOT_COMPLETED (waited ${BOOT_WAIT}s)" >> "$LOGFILE" 2>&1
    done
    if [ "$BOOT_COMPLETED" != "1" ]; then
        echo "WARNING: Emulator did not report sys.boot_completed=1 after 60s (got '$BOOT_COMPLETED')" >> "$LOGFILE" 2>&1
        echo "Continuing anyway — deploy may fail" >> "$LOGFILE" 2>&1
    fi
fi
echo "" >> "$LOGFILE" 2>&1

# === STEP 2: Restore ===
echo "=== STEP 2: Restore ===" | tee -a "$LOGFILE"
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting restore" >> "$LOGFILE" 2>&1
"$DOTNET_ROOT/dotnet" restore \
    "$HELIX_WORKITEM_ROOT/app/MauiAndroidInnerLoop.csproj" \
    --configfile "$HELIX_WORKITEM_ROOT/app/NuGet.config" \
    /p:TargetFrameworks=$FRAMEWORK \
    $MSBUILD_ARGS \
    >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Restore succeeded" >> "$LOGFILE" 2>&1
echo "" >> "$LOGFILE" 2>&1

# === STEP 3: Test ===
echo "=== STEP 3: Test ===" | tee -a "$LOGFILE"
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting test.py" >> "$LOGFILE" 2>&1

# Pass MSBuild args via environment variable to avoid shell quoting issues.
# runner.py reads PERFLAB_MSBUILD_ARGS as fallback when --msbuild-args is empty.
export PERFLAB_MSBUILD_ARGS="$MSBUILD_ARGS"
echo "PERFLAB_MSBUILD_ARGS=$PERFLAB_MSBUILD_ARGS" >> "$LOGFILE" 2>&1

python3 test.py androidinnerloop \
    --csproj-path app/MauiAndroidInnerLoop.csproj \
    --edit-src src/MainPage.xaml.cs \
    --edit-dest app/MainPage.xaml.cs \
    -f "$FRAMEWORK" \
    -c Debug \
    --scenario-name "$SCENARIO_NAME" \
    "${EXTRA_ARGS[@]}" \
    >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] test.py succeeded" >> "$LOGFILE" 2>&1
echo "" >> "$LOGFILE" 2>&1

# === STEP 4: Measure App Startup ===
echo "=== STEP 4: Measure App Startup ===" | tee -a "$LOGFILE"
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting startup measurement" >> "$LOGFILE" 2>&1
python3 test.py devicestartup \
    --device-type android \
    --package-name com.companyname.mauiandroidinnerloop \
    --package-path "app/bin/Debug/$FRAMEWORK/android-x64/com.companyname.mauiandroidinnerloop-Signed.apk" \
    --startup-iterations 5 \
    --disable-animations \
    --scenario-name "$SCENARIO_NAME - Startup" \
    "${EXTRA_ARGS[@]}" \
    >> "$LOGFILE" 2>&1

echo "=== ALL STEPS SUCCEEDED ===" | tee -a "$LOGFILE"
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Complete" >> "$LOGFILE" 2>&1
cat "$LOGFILE"
