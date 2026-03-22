#!/usr/bin/env bash
# run.sh — Linux emulator track for MAUI Android inner loop measurements.
# Runs on Ubuntu.2204.Amd64.Android.29 Helix queue which has:
#   - Android emulator already running (emulator-5554)
#   - ANDROID_HOME, ADB, Java JDK pre-installed
#   - KVM enabled
# This is the bash equivalent of run.cmd (Windows device track), but much
# simpler because the environment is pre-configured.

set -e

FRAMEWORK="$1"
MSBUILD_ARGS="$2"
SCENARIO_NAME="$3"

LOGFILE="$HELIX_WORKITEM_UPLOAD_ROOT/output.log"

# On any error, log the failure location and dump the log for Helix diagnostics.
on_error() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] FAILED at line $1" >> "$LOGFILE" 2>&1
    cat "$LOGFILE"
    exit 1
}
trap 'on_error $LINENO' ERR

# ci_setup.py installs the .NET 11 SDK into $HELIX_CORRELATION_PAYLOAD/dotnet
# but DOTNET_ROOT points to $HELIX_CORRELATION_PAYLOAD/dotnet-cli (has .NET 8).
# Override DOTNET_ROOT to use the correct SDK for building.
export DOTNET_ROOT="$HELIX_CORRELATION_PAYLOAD/dotnet"
export PATH="$DOTNET_ROOT:$PATH"

# CI packages are signed with internal certs not in the Helix machine trust store.
# Disable signature verification entirely for workload install and restore.
export DOTNET_NUGET_SIGNATURE_VERIFICATION=false
export NUGET_CERT_REVOCATION_MODE=offline

echo "=== DIAGNOSTICS ===" >> "$LOGFILE" 2>&1
echo "DOTNET_ROOT=$DOTNET_ROOT" >> "$LOGFILE" 2>&1
echo "ANDROID_HOME=$ANDROID_HOME" >> "$LOGFILE" 2>&1
echo "ANDROID_SDK_ROOT=$ANDROID_SDK_ROOT" >> "$LOGFILE" 2>&1
echo "JAVA_HOME=$JAVA_HOME" >> "$LOGFILE" 2>&1
echo "NUGET_PACKAGES=$NUGET_PACKAGES" >> "$LOGFILE" 2>&1
echo "PYTHONPATH=$PYTHONPATH" >> "$LOGFILE" 2>&1
which adb >> "$LOGFILE" 2>&1 || true
which dotnet >> "$LOGFILE" 2>&1 || true
which python3 >> "$LOGFILE" 2>&1 || true
"$DOTNET_ROOT/dotnet" --version >> "$LOGFILE" 2>&1
echo "" >> "$LOGFILE" 2>&1

# === Verify emulator is ready ===
echo "=== EMULATOR STATUS ===" >> "$LOGFILE" 2>&1
adb devices >> "$LOGFILE" 2>&1
adb wait-for-device >> "$LOGFILE" 2>&1
BOOT_COMPLETED=$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r\n')
echo "sys.boot_completed=$BOOT_COMPLETED" >> "$LOGFILE" 2>&1
if [ "$BOOT_COMPLETED" != "1" ]; then
    echo "ERROR: Emulator is not fully booted (sys.boot_completed=$BOOT_COMPLETED)" >> "$LOGFILE" 2>&1
    cat "$LOGFILE"
    exit 1
fi
echo "Emulator is ready" >> "$LOGFILE" 2>&1
echo "" >> "$LOGFILE" 2>&1

# === STEP 1: Workload Install ===
echo "=== STEP 1: Workload Install ===" >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting workload install" >> "$LOGFILE" 2>&1
"$DOTNET_ROOT/dotnet" workload install maui-android \
    --from-rollback-file "$HELIX_WORKITEM_ROOT/rollback_maui.json" \
    --configfile "$HELIX_WORKITEM_ROOT/app/NuGet.config" \
    >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Workload install succeeded" >> "$LOGFILE" 2>&1
echo "" >> "$LOGFILE" 2>&1

# === STEP 2: Restore ===
echo "=== STEP 2: Restore ===" >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting restore" >> "$LOGFILE" 2>&1
"$DOTNET_ROOT/dotnet" restore \
    "$HELIX_WORKITEM_ROOT/app/MauiAndroidInnerLoop.csproj" \
    --configfile "$HELIX_WORKITEM_ROOT/app/NuGet.config" \
    /p:AllowMissingPrunePackageData=true \
    >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Restore succeeded" >> "$LOGFILE" 2>&1
echo "" >> "$LOGFILE" 2>&1

# === STEP 3: Test ===
echo "=== STEP 3: Test ===" >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting test.py" >> "$LOGFILE" 2>&1
python3 test.py androidinnerloop \
    --csproj-path app/MauiAndroidInnerLoop.csproj \
    --edit-src src/MainPage.xaml.cs \
    --edit-dest app/MainPage.xaml.cs \
    -f "$FRAMEWORK" \
    -c Debug \
    --msbuild-args "$MSBUILD_ARGS /p:AllowMissingPrunePackageData=true" \
    --scenario-name "$SCENARIO_NAME" \
    >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] test.py succeeded" >> "$LOGFILE" 2>&1
echo "" >> "$LOGFILE" 2>&1

echo "=== ALL STEPS SUCCEEDED ===" >> "$LOGFILE" 2>&1
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Complete" >> "$LOGFILE" 2>&1
cat "$LOGFILE"
exit 0
