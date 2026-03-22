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

# === Discover Android SDK ===
# On the Ubuntu.2204.Amd64.Android.29 Helix queue, the Android SDK is
# pre-installed at the machine level but ANDROID_HOME / ANDROID_SDK_ROOT
# are NOT propagated into the workitem execution context.  Discover it.
if [ -z "$ANDROID_HOME" ]; then
    echo "ANDROID_HOME is empty — searching for Android SDK..." >> "$LOGFILE" 2>&1
    CANDIDATE_PATHS=(
        "/usr/local/lib/android/sdk"
        "/opt/android-sdk"
        "$HOME/android-sdk"
        "$HOME/Android/Sdk"
        "/root/android-sdk"
        "/root/Android/Sdk"
        "/android"
        "/sdk"
        "/opt/android"
        "/usr/local/android-sdk"
    )
    for candidate in "${CANDIDATE_PATHS[@]}"; do
        if [ -d "$candidate/platform-tools" ]; then
            export ANDROID_HOME="$candidate"
            echo "  Found Android SDK at $ANDROID_HOME" >> "$LOGFILE" 2>&1
            break
        fi
    done

    # Fallback: derive from adb location (adb lives at <sdk>/platform-tools/adb)
    if [ -z "$ANDROID_HOME" ]; then
        ADB_PATH=$(which adb 2>/dev/null || true)
        if [ -n "$ADB_PATH" ]; then
            ADB_REAL=$(readlink -f "$ADB_PATH" 2>/dev/null || echo "$ADB_PATH")
            PLATFORM_TOOLS_DIR=$(dirname "$ADB_REAL")
            DERIVED_SDK=$(dirname "$PLATFORM_TOOLS_DIR")
            if [ -d "$DERIVED_SDK/platform-tools" ]; then
                export ANDROID_HOME="$DERIVED_SDK"
                echo "  Derived Android SDK from adb at $ANDROID_HOME" >> "$LOGFILE" 2>&1
            fi
        fi
    fi

    # Fallback: check /etc/environment for ANDROID_HOME
    if [ -z "$ANDROID_HOME" ]; then
        ANDROID_FROM_ETC=$(grep -i 'ANDROID_HOME' /etc/environment 2>/dev/null | head -1 | cut -d= -f2 | tr -d '"' || true)
        if [ -n "$ANDROID_FROM_ETC" ] && [ -d "$ANDROID_FROM_ETC/platform-tools" ]; then
            export ANDROID_HOME="$ANDROID_FROM_ETC"
            echo "  Found Android SDK from /etc/environment: $ANDROID_HOME" >> "$LOGFILE" 2>&1
        fi
    fi

    # Fallback: find running emulator process and extract SDK path
    if [ -z "$ANDROID_HOME" ]; then
        EMU_PATH=$(ps aux 2>/dev/null | grep -i '[e]mulator' | head -1 | grep -oP '\S*emulator\S*' | head -1 || true)
        if [ -n "$EMU_PATH" ]; then
            EMU_REAL=$(readlink -f "$EMU_PATH" 2>/dev/null || echo "$EMU_PATH")
            EMU_DIR=$(dirname "$EMU_REAL")
            DERIVED_SDK=$(dirname "$EMU_DIR")
            if [ -d "$DERIVED_SDK/platform-tools" ]; then
                export ANDROID_HOME="$DERIVED_SDK"
                echo "  Derived Android SDK from emulator process: $ANDROID_HOME" >> "$LOGFILE" 2>&1
            fi
        fi
    fi

    # Last resort: find adb binary on disk
    if [ -z "$ANDROID_HOME" ]; then
        ADB_FOUND=$(find / -maxdepth 5 -name "adb" -type f 2>/dev/null | head -5 || true)
        if [ -n "$ADB_FOUND" ]; then
            echo "  Found adb binaries via find:" >> "$LOGFILE" 2>&1
            echo "  $ADB_FOUND" >> "$LOGFILE" 2>&1
            # Use the first result and derive SDK path
            FIRST_ADB=$(echo "$ADB_FOUND" | head -1)
            ADB_DIR=$(dirname "$FIRST_ADB")
            DERIVED_SDK=$(dirname "$ADB_DIR")
            if [ -d "$DERIVED_SDK/platform-tools" ]; then
                export ANDROID_HOME="$DERIVED_SDK"
                echo "  Derived Android SDK from find: $ANDROID_HOME" >> "$LOGFILE" 2>&1
            fi
        fi
    fi

    if [ -z "$ANDROID_HOME" ]; then
        echo "  WARNING: Could not find Android SDK in any known location" >> "$LOGFILE" 2>&1

        # Dump comprehensive diagnostics so we can figure out where the SDK is
        echo "" >> "$LOGFILE" 2>&1
        echo "=== ANDROID SDK DISCOVERY DIAGNOSTICS ===" >> "$LOGFILE" 2>&1

        echo "--- /etc/environment ---" >> "$LOGFILE" 2>&1
        cat /etc/environment >> "$LOGFILE" 2>&1 || true

        echo "--- Full environment (sorted) ---" >> "$LOGFILE" 2>&1
        env | sort >> "$LOGFILE" 2>&1 || true

        echo "--- Android/emulator related processes ---" >> "$LOGFILE" 2>&1
        ps aux 2>/dev/null | grep -iE 'emulator|adb|android' | grep -v grep >> "$LOGFILE" 2>&1 || echo "(none)" >> "$LOGFILE" 2>&1

        echo "--- /opt/ contents ---" >> "$LOGFILE" 2>&1
        ls -la /opt/ >> "$LOGFILE" 2>&1 || true

        echo "--- /usr/local/lib/ contents ---" >> "$LOGFILE" 2>&1
        ls -la /usr/local/lib/ >> "$LOGFILE" 2>&1 || true

        echo "--- /root/ contents ---" >> "$LOGFILE" 2>&1
        ls -la /root/ >> "$LOGFILE" 2>&1 || true

        echo "--- /home/ contents ---" >> "$LOGFILE" 2>&1
        ls -la /home/ >> "$LOGFILE" 2>&1 || true

        echo "--- / top-level contents ---" >> "$LOGFILE" 2>&1
        ls -la / >> "$LOGFILE" 2>&1 || true

        echo "--- find adb (maxdepth 6) ---" >> "$LOGFILE" 2>&1
        find / -maxdepth 6 -name "adb" -type f 2>/dev/null | head -10 >> "$LOGFILE" 2>&1 || true

        echo "--- find emulator (maxdepth 6) ---" >> "$LOGFILE" 2>&1
        find / -maxdepth 6 -name "emulator" -type f 2>/dev/null | head -10 >> "$LOGFILE" 2>&1 || true

        echo "--- which adb / which emulator ---" >> "$LOGFILE" 2>&1
        which adb >> "$LOGFILE" 2>&1 || echo "adb not on PATH" >> "$LOGFILE" 2>&1
        which emulator >> "$LOGFILE" 2>&1 || echo "emulator not on PATH" >> "$LOGFILE" 2>&1

        echo "=== END DISCOVERY DIAGNOSTICS ===" >> "$LOGFILE" 2>&1
        echo "" >> "$LOGFILE" 2>&1
    fi
fi

export ANDROID_SDK_ROOT="${ANDROID_SDK_ROOT:-$ANDROID_HOME}"

# Add Android tools to PATH
if [ -n "$ANDROID_HOME" ]; then
    export PATH="$ANDROID_HOME/platform-tools:$PATH"
    export PATH="$ANDROID_HOME/emulator:$PATH"
    # Add the latest build-tools version to PATH
    if [ -d "$ANDROID_HOME/build-tools" ]; then
        LATEST_BUILD_TOOLS=$(ls -1d "$ANDROID_HOME/build-tools"/*/ 2>/dev/null | sort -V | tail -1)
        if [ -n "$LATEST_BUILD_TOOLS" ]; then
            export PATH="${LATEST_BUILD_TOOLS%/}:$PATH"
            echo "  Added build-tools to PATH: $LATEST_BUILD_TOOLS" >> "$LOGFILE" 2>&1
        fi
    fi
fi

# === Discover Java SDK ===
# Same problem: JAVA_HOME may not be set in the workitem context.
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
            # java is at <jdk>/bin/java
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

echo "=== DIAGNOSTICS ===" >> "$LOGFILE" 2>&1
echo "DOTNET_ROOT=$DOTNET_ROOT" >> "$LOGFILE" 2>&1
echo "ANDROID_HOME=$ANDROID_HOME" >> "$LOGFILE" 2>&1
echo "ANDROID_SDK_ROOT=$ANDROID_SDK_ROOT" >> "$LOGFILE" 2>&1
echo "JAVA_HOME=$JAVA_HOME" >> "$LOGFILE" 2>&1
echo "NUGET_PACKAGES=$NUGET_PACKAGES" >> "$LOGFILE" 2>&1
echo "PYTHONPATH=$PYTHONPATH" >> "$LOGFILE" 2>&1
echo "PATH=$PATH" >> "$LOGFILE" 2>&1
which adb >> "$LOGFILE" 2>&1 || true
which dotnet >> "$LOGFILE" 2>&1 || true
which java >> "$LOGFILE" 2>&1 || true
which python3 >> "$LOGFILE" 2>&1 || true
"$DOTNET_ROOT/dotnet" --version >> "$LOGFILE" 2>&1
adb version >> "$LOGFILE" 2>&1 || echo "WARNING: adb version failed" >> "$LOGFILE" 2>&1
java -version >> "$LOGFILE" 2>&1 || echo "WARNING: java -version failed" >> "$LOGFILE" 2>&1
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
