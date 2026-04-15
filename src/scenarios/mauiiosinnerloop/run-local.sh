#!/usr/bin/env bash
# run-local.sh — Local developer convenience script for MAUI iOS inner loop measurements.
# Orchestrates init.sh, pre.py, test.py, and post.py with Xcode selection,
# simulator boot, PERFLAB env vars, config→msbuild-args mapping, and NuGet restore.
set -euo pipefail

# ── Constants ──
EXENAME="MauiiOSInnerLoop"
BUNDLE_ID="com.companyname.mauiiosinnerloop"
# pre.py normalizes the MAUI template to always place MainPage under app/Pages/.
# These paths are constants, not detected at runtime.
EDIT_SRC="src/MainPage.xaml.cs;src/MainPage.xaml"
EDIT_DEST="app/Pages/MainPage.xaml.cs;app/Pages/MainPage.xaml"

# ── Defaults ──
CONFIGS="mono-default"  ITERATIONS=5  FRAMEWORK=""  CHANNEL=""  SDK_VERSION=""
DEVICE_TYPE="simulator"  DEVICE_NAME="iPhone 16"  XCODE_PATH=""  RID=""
SKIP_SETUP=false  DRY_RUN=false  HAS_WORKLOAD=false

# ── Self-location (derived from this script's path — no --repo-root arg needed) ──
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCENARIO_DIR="$SCRIPT_DIR"
SCENARIOS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# ── Helpers ──
log()  { echo "==> $*"; }
die()  { echo "ERROR: $*" >&2; exit 1; }
# Dry-run guard: logs and returns 1 so callers can use `dry || real_command`.
dry()  { [[ "$DRY_RUN" == true ]] && log "[DRY-RUN] would run: $*"; }

usage() {
    cat <<EOF
Usage: $(basename "$0") [OPTIONS]
  --configs CONFIGS     Space-separated configs (default: "mono-default"; valid: mono-default, coreclr-default)
  --iterations N        Inner loop iterations (default: 5)
  --channel CHANNEL     SDK channel for init.sh (auto-detected from global.json)
  --framework TFM       Target framework (auto-detected from global.json, e.g. net10.0-ios)
  --device-type TYPE    simulator or device (default: simulator)
  --device-name NAME    Simulator name (default: "iPhone 16")
  --xcode-path PATH     Path to Xcode.app (auto-detected if omitted)
  --rid RID             RuntimeIdentifier (auto-detected from device-type)
  --skip-setup          Skip init.sh/pre.py (reuse existing SDK+app)
  --has-workload        Tell pre.py the SDK already has the maui-ios workload
  --dry-run             Print commands without executing
  -h, --help            Show this help
EOF
    exit 0
}

parse_args() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --configs)       CONFIGS="$2"; shift 2 ;;
            --iterations)    ITERATIONS="$2"; shift 2 ;;
            --channel)       CHANNEL="$2"; shift 2 ;;
            --framework)     FRAMEWORK="$2"; shift 2 ;;
            --device-type)   DEVICE_TYPE="$2"; shift 2 ;;
            --device-name)   DEVICE_NAME="$2"; shift 2 ;;
            --xcode-path)    XCODE_PATH="$2"; shift 2 ;;
            --rid)           RID="$2"; shift 2 ;;
            --skip-setup)    SKIP_SETUP=true; shift ;;
            --has-workload)  HAS_WORKLOAD=true; shift ;;
            --dry-run)       DRY_RUN=true; shift ;;
            -h|--help)       usage ;;
            *) die "Unknown option: $1. Use --help for usage." ;;
        esac
    done
}

detect_rid() {
    if [[ -n "$RID" ]]; then return 0; fi
    local arch; arch="$(uname -m)"
    case "$DEVICE_TYPE" in
        simulator) [[ "$arch" == "arm64" ]] && RID="iossimulator-arm64" || RID="iossimulator-x64" ;;
        device)    RID="ios-arm64" ;;
        *)         die "Unknown device-type: $DEVICE_TYPE" ;;
    esac
}

# Derive CHANNEL and FRAMEWORK from global.json + channel_map.py when not
# explicitly provided.  Uses the repo's authoritative channel_map so the
# script always stays in sync with whatever SDK version the repo pins.
detect_channel_and_framework() {
    if [[ -n "$CHANNEL" && -n "$FRAMEWORK" && -n "$SDK_VERSION" ]]; then return 0; fi

    local result
    result=$(python3 -c "
import json, sys, os
sys.path.insert(0, os.path.join('$REPO_ROOT', 'scripts'))
from channel_map import ChannelMap

with open(os.path.join('$REPO_ROOT', 'global.json')) as f:
    sdk_ver = json.load(f)['sdk']['version']
major = sdk_ver.split('.')[0]
tfm = 'net' + major + '.0'
channel = ChannelMap.get_channel_from_target_framework_moniker(tfm)
print(sdk_ver + ' ' + channel + ' ' + tfm + '-ios')
") || die "Failed to derive channel/framework from global.json"

    local derived_sdk derived_channel derived_framework
    derived_sdk="${result%% *}"
    local remainder="${result#* }"
    derived_channel="${remainder%% *}"
    derived_framework="${remainder#* }"

    [[ -z "$SDK_VERSION" ]] && SDK_VERSION="$derived_sdk"
    [[ -z "$CHANNEL" ]]     && CHANNEL="$derived_channel"
    [[ -z "$FRAMEWORK" ]]   && FRAMEWORK="$derived_framework"

    log "Auto-detected SDK_VERSION=$SDK_VERSION  CHANNEL=$CHANNEL  FRAMEWORK=$FRAMEWORK (from global.json)"
}

select_xcode() {
    if [[ -z "$XCODE_PATH" ]]; then
        # Use the Python helper that matches Xcode to the iOS SDK version.
        # It checks rollback_maui.json, then SDK packs, then falls back to highest.
        # stderr has diagnostics (suppressed here); stdout has the path.
        XCODE_PATH=$(python3 "$SCENARIO_DIR/select_xcode.py" \
            --scenario-dir "$SCENARIO_DIR" \
            ${DOTNET_ROOT:+--dotnet-root "$DOTNET_ROOT"} 2>/dev/null) \
            || XCODE_PATH=""
        if [[ -z "$XCODE_PATH" || ! -d "$XCODE_PATH" ]]; then
            log "WARNING: select_xcode.py failed; falling back to /Applications/Xcode.app"
            XCODE_PATH="/Applications/Xcode.app"
        fi
    fi
    log "Using Xcode: $XCODE_PATH"
    export DEVELOPER_DIR="$XCODE_PATH/Contents/Developer"
    [[ -d "$DEVELOPER_DIR" ]] || die "DEVELOPER_DIR does not exist: $DEVELOPER_DIR"
}

boot_simulator() {
    [[ "$DEVICE_TYPE" == "simulator" ]] || return 0
    log "Booting simulator: $DEVICE_NAME"
    dry "xcrun simctl boot '$DEVICE_NAME'" && return 0
    # Idempotent — boot returns non-zero if already booted, which is fine.
    xcrun simctl boot "$DEVICE_NAME" 2>/dev/null || true
    if ! xcrun simctl list devices booted 2>/dev/null | grep -q "$DEVICE_NAME"; then
        die "Failed to boot simulator '$DEVICE_NAME'"
    fi
}

validate_prereqs() {
    local cmd; for cmd in python3 xcrun git; do
        command -v "$cmd" >/dev/null 2>&1 || die "$cmd not found in PATH"
    done
}

bootstrap() {
    if [[ "$SKIP_SETUP" == true ]]; then
        # Find existing SDK from a prior init.sh run (avoid re-downloading).
        local arch; arch="$(uname -m)"
        [[ "$arch" == "x86_64" ]] && arch="x64"
        local dotnet_dir="$SCENARIOS_DIR/tools/dotnet/$arch"
        [[ -d "$dotnet_dir" ]] || die "No SDK found at $dotnet_dir. Run without --skip-setup first."
        export DOTNET_ROOT="$dotnet_dir" PATH="$dotnet_dir:$PATH"
        export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_MULTILEVEL_LOOKUP=0
        # Re-enable Roslyn compiler server to match real developer inner loop.
        # The perf repo disables it globally for BenchmarkDotNet isolation.
        export UseSharedCompilation=true
        # init.sh normally sets PYTHONPATH; replicate the essential part.
        set +u  # PYTHONPATH may be unset
        export PYTHONPATH="${PYTHONPATH:+$PYTHONPATH:}$REPO_ROOT/scripts:$SCENARIOS_DIR"
        set -u
    else
        dry ". init.sh → dotnet.py install + init.sh -dotnetdir" && return 0
        # Step 1: Install exact SDK version directly from CDN.
        # Bypasses channel/quality resolution that fails for .NET 10.0 RC
        # (channel_map.py maps 10.0 → quality 'daily', but daily builds don't exist).
        python3 "$REPO_ROOT/scripts/dotnet.py" install --dotnet-versions "$SDK_VERSION" -v

        # Step 2: Setup environment (DOTNET_ROOT, PATH, telemetry) using installed SDK.
        # dotnet.py installs to <repo_root>/tools/dotnet/<arch>; pass that as -dotnetdir.
        local arch; arch="$(uname -m)"
        [[ "$arch" == "x86_64" ]] && arch="x64"
        pushd "$SCENARIOS_DIR" > /dev/null
        # init.sh references $PYTHONPATH without a default, which fails under
        # set -u if PYTHONPATH hasn't been exported yet. Temporarily relax.
        set +u
        # shellcheck disable=SC1091
        . ./init.sh -dotnetdir "$REPO_ROOT/tools/dotnet/$arch"
        set -u
        popd > /dev/null
        export UseSharedCompilation=true  # Override init.sh default (false) for realistic inner loop
    fi
    log "DOTNET_ROOT=${DOTNET_ROOT:-<unset>}"
}

config_settings() {
    local config="$1"
    case "$config" in
        mono-default)
            RUNTIME_FLAVOR="mono"
            MSBUILD_ARGS="/p:UseMonoRuntime=true /p:RuntimeIdentifier=$RID /p:MtouchLink=None"
            SCENARIO_NAME="MAUI iOS Inner Loop - Mono Default" ;;
        coreclr-default)
            RUNTIME_FLAVOR="coreclr"
            MSBUILD_ARGS="/p:UseMonoRuntime=false /p:RuntimeIdentifier=$RID /p:MtouchLink=None"
            SCENARIO_NAME="MAUI iOS Inner Loop - CoreCLR Default" ;;
        *) die "Unknown config: $config. Use mono-default or coreclr-default." ;;
    esac
}

main() {
    parse_args "$@"
    CONFIGS="${CONFIGS//,/ }"  # Normalize comma-separated configs to space-separated
    validate_prereqs
    detect_rid
    detect_channel_and_framework
    select_xcode
    boot_simulator
    bootstrap
    # PERFLAB env vars — required for JSON report generation by runner.py.
    export PERFLAB_INLAB=1
    export PERFLAB_BUILDTIMESTAMP="$(date -u +%Y-%m-%dT%H:%M:%S.0000000Z)"
    export PERFLAB_HASH="$(git -C "$REPO_ROOT" rev-parse HEAD 2>/dev/null || echo 'local')"
    export PERFLAB_REPO="dotnet/performance"
    export PERFLAB_BRANCH="$(git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD 2>/dev/null || echo 'local')"
    export PERFLAB_BUILDNUM="local-$(date +%Y%m%d%H%M%S)"

    cd "$SCENARIO_DIR"
    local workload_flag=""; [[ "$HAS_WORKLOAD" == true ]] && workload_flag="--has-workload"

    for config in $CONFIGS; do
        config_settings "$config"
        export RUNTIME_FLAVOR
        # post.py uses IOS_RID to determine device vs simulator cleanup
        export IOS_RID="$RID"
        log "--- Config: $config (RUNTIME_FLAVOR=$RUNTIME_FLAVOR) ---"

        # Setup — create template and install workload
        if [[ "$SKIP_SETUP" != true ]]; then
            # Clean stale artifacts from interrupted runs — XAML source generators
            # produce duplicate errors when leftover obj/ exists alongside a fresh template.
            if [[ -d app || -d traces ]]; then
                log "Cleaning stale app/ and traces/ from prior run..."
                dry "rm -rf app/ traces/" || rm -rf app/ traces/
            fi
            log "Running pre.py for $config..."
            # shellcheck disable=SC2086
            dry "python3 pre.py default -f $FRAMEWORK $workload_flag" \
                || python3 pre.py default -f "$FRAMEWORK" $workload_flag
        fi

        # NuGet restore
        log "Restoring NuGet packages..."
        dry "dotnet restore app/$EXENAME.csproj" \
            || dotnet restore "app/$EXENAME.csproj" --ignore-failed-sources /p:NuGetAudit=false

        # Measure
        log "Running measurements for $config ($ITERATIONS iterations)..."
        dry "python3 test.py iosinnerloop ..." || \
        python3 test.py iosinnerloop \
            --csproj-path "app/$EXENAME.csproj" \
            --edit-src "$EDIT_SRC" --edit-dest "$EDIT_DEST" \
            --bundle-id "$BUNDLE_ID" \
            -f "$FRAMEWORK" -c Debug --msbuild-args "$MSBUILD_ARGS" \
            --device-type "$DEVICE_TYPE" \
            --inner-loop-iterations "$ITERATIONS" \
            --scenario-name "$SCENARIO_NAME"

        # Preserve binlogs and version metadata before cleanup deletes traces/
        if [[ "$DRY_RUN" != true ]] && compgen -G "traces/*.binlog" >/dev/null; then
            mkdir -p "results/$RUNTIME_FLAVOR"
            cp traces/*.binlog "results/$RUNTIME_FLAVOR/"
            if compgen -G "traces/*-versions.json" >/dev/null; then
                cp traces/*-versions.json "results/$RUNTIME_FLAVOR/"
            fi
            log "Copied binlogs to results/$RUNTIME_FLAVOR/"
        fi

        # Cleanup between configs
        if [[ "$SKIP_SETUP" != true ]]; then
            dry "python3 post.py" || python3 post.py
        else
            # Only clean build artifacts; keep the app template for reuse.
            dry "rm -rf app/bin app/obj traces/" || rm -rf app/bin app/obj traces/
        fi
    done

    log "Done! Results in: $SCENARIO_DIR/results/"
    ls -la "$SCENARIO_DIR/results/" 2>/dev/null || true
}

main "$@"
