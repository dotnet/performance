#!/usr/bin/env bash
#
# Run the MAUI iOS inner-loop *simulator* perf scenario directly on the AzDO
# hosted macOS agent, instead of dispatching it to a Helix Mac device queue.
#
# The Helix Mac perf queues (Mac.iPhone.13/17.Perf) are EOL 2026-06-01 and need
# infra (Xcode) updates we can't get. The hosted `macos-26` image already ships
# Xcode + iOS simulators and is auto-updated, so for the simulator scenario we
# run setup_helix.py -> test.py -> post.py on the agent and emulate the small set
# of HELIX_* environment variables those scripts expect.
#
# Prerequisite: scripts/run_performance_job.py has already run on this agent and
# produced CorrelationStaging/payload, including:
#   payload/dotnet                                   - the .NET SDK
#   payload/root/machine-setup.sh                    - PERFLAB_*/DOTNET_* exports
#   payload/scripts, payload/shared                  - python modules (PYTHONPATH)
#   payload/performance/src/scenarios/mauiiosinnerloop with the scaffolded `app/`
#     and rollback_maui.json produced by `pre.py default` (PreparePayloadWorkItems).
#
# This driver intentionally mirrors the simulator HelixWorkItem in
# eng/performance/maui_scenarios_ios.proj (the _MSBuildArgs, scenario name,
# edit-src/edit-dest and test.py arguments). Keep the two in sync.

set -uo pipefail

payload_dir=""
runtime_flavor=""
framework=""
ios_rid="iossimulator-x64"
inner_loop_iterations="3"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --payload-dir) payload_dir="$2"; shift 2 ;;
    --runtime-flavor) runtime_flavor="$2"; shift 2 ;;
    --framework) framework="$2"; shift 2 ;;
    --ios-rid) ios_rid="$2"; shift 2 ;;
    --inner-loop-iterations) inner_loop_iterations="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 1 ;;
  esac
done

if [[ -z "$payload_dir" || -z "$runtime_flavor" ]]; then
  echo "Usage: $0 --payload-dir <dir> --runtime-flavor <mono|coreclr> [--framework <tfm>] [--ios-rid <rid>] [--inner-loop-iterations <n>]" >&2
  exit 1
fi

# Resolve to an absolute path so the rest of the script is location-independent.
payload_dir="$(cd "$payload_dir" && pwd)"
scenario_dir="$payload_dir/performance/src/scenarios/mauiiosinnerloop"

if [[ ! -d "$scenario_dir" ]]; then
  echo "Scenario directory not found: $scenario_dir" >&2
  echo "Did scripts/run_performance_job.py / PreparePayloadWorkItems run first?" >&2
  exit 1
fi

# Load PERFLAB_* / DOTNET_VERSION / PERFLAB_TARGET_FRAMEWORKS, etc. This is the
# same file the Helix work item sources via helix_pre_commands.
# Emulate the HELIX_* environment the scenario scripts (and the generated
# machine-setup.sh, which references $HELIX_CORRELATION_PAYLOAD) expect. These
# MUST be exported before sourcing machine-setup.sh. On Helix the correlation
# payload and the work item payload (the scenario dir) are unpacked separately;
# here we collapse the work item root onto the prepared scenario dir inside the
# payload, which is fine for a single local work item.
export HELIX_CORRELATION_PAYLOAD="$payload_dir"
export HELIX_WORKITEM_ROOT="$scenario_dir"
export HELIX_WORKITEM_UPLOAD_ROOT="$payload_dir/../uploadroot"
mkdir -p "$HELIX_WORKITEM_UPLOAD_ROOT"
HELIX_WORKITEM_UPLOAD_ROOT="$(cd "$HELIX_WORKITEM_UPLOAD_ROOT" && pwd)"
export HELIX_WORKITEM_UPLOAD_ROOT
export HELIX_WORKITEM_ID="MAUIiOSInnerLoop_Simulator_${runtime_flavor}_${BUILD_BUILDID:-local}"

machine_setup="$payload_dir/root/machine-setup.sh"
if [[ -f "$machine_setup" ]]; then
  echo "Sourcing $machine_setup"
  # machine-setup.sh is generated for the Helix environment and may reference
  # variables we don't set; relax nounset just for the source.
  set +u
  # shellcheck disable=SC1090
  source "$machine_setup"
  set -u
else
  echo "machine-setup.sh not found at $machine_setup" >&2
  exit 1
fi

# machine-setup.sh exports PERFLAB_TARGET_FRAMEWORKS (e.g. net11.0) but not
# PERFLAB_Framework, so prefer the explicitly passed framework and fall back to
# the sourced value.
framework="${framework:-}"
if [[ -z "$framework" ]]; then
  framework="${PERFLAB_TARGET_FRAMEWORKS:-}"
fi
if [[ -z "$framework" ]]; then
  echo "Could not determine target framework (no --framework and PERFLAB_TARGET_FRAMEWORKS unset)" >&2
  exit 1
fi

# Global Helix pre-commands (from Scenarios.Common.props) so `import shared.*`
# and `import performance.*` resolve.
export PYTHONPATH="$HELIX_CORRELATION_PAYLOAD/scripts:$HELIX_CORRELATION_PAYLOAD"

# _MacEnvVars from maui_scenarios_ios.proj (machine-setup.sh already sets
# DOTNET_ROOT/PATH to the same values; re-export for parity with the proj).
export DOTNET_ROOT="$HELIX_CORRELATION_PAYLOAD/dotnet"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_MULTILEVEL_LOOKUP=0
export NUGET_PACKAGES="$HELIX_WORKITEM_ROOT/.packages"
export PATH="$DOTNET_ROOT:$PATH"
export IOS_RID="$ios_rid"

# Install the Python packages the scenario needs at runtime (azure SDK for
# upload.py, cryptography, etc.). On Helix these are installed via the work
# item's helix_pre_commands; on the agent we must do it ourselves. runner.py
# imports `upload` unconditionally, so this is required even when not uploading.
# --break-system-packages matches the existing osx pip handling in
# scripts/run_performance_job.py.
requirements_file="$HELIX_CORRELATION_PAYLOAD/performance/requirements.txt"
if [[ -f "$requirements_file" ]]; then
  echo "Installing Python requirements from $requirements_file"
  python3 -m pip install --break-system-packages --user --disable-pip-version-check -q -r "$requirements_file" || {
    echo "pip install of requirements failed" >&2
    exit 1
  }
else
  echo "requirements.txt not found at $requirements_file" >&2
  exit 1
fi

# _MSBuildArgs from maui_scenarios_ios.proj (simulator path: iOSRid != ios-arm64).
if [[ "$runtime_flavor" == "mono" ]]; then
  msbuild_args="/p:UseMonoRuntime=true"
elif [[ "$runtime_flavor" == "coreclr" ]]; then
  msbuild_args="/p:UseMonoRuntime=false"
else
  echo "Unsupported runtime flavor: $runtime_flavor" >&2
  exit 1
fi
msbuild_args="$msbuild_args /p:RuntimeIdentifier=$ios_rid /p:MtouchLink=None"

# Only upload to PerfLab from internal, non-PR runs (mirrors the gating in
# run-performance-job.yml). On the agent, upload.py authenticates via the
# AzureCLI@2 service-connection login (AzureCliCredential) that wraps this
# script; PERFLAB_UPLOAD_USE_AZURE_CLI selects that path over the Helix
# UAMI/cert flow.
scenario_args=""
if [[ "${SYSTEM_TEAMPROJECT:-}" != "public" && "${BUILD_REASON:-}" != "PullRequest" ]]; then
  scenario_args="--upload-to-perflab-container"
  export PERFLAB_UPLOAD_USE_AZURE_CLI=1
fi

echo "===== iOS inner loop (simulator) on-agent run ====="
echo "  framework:            $framework"
echo "  runtime flavor:       $runtime_flavor"
echo "  iOS RID:              $ios_rid"
echo "  inner loop iters:     $inner_loop_iterations"
echo "  msbuild args:         $msbuild_args"
echo "  scenario args:        ${scenario_args:-<none>}"
echo "  HELIX_WORKITEM_ROOT:  $HELIX_WORKITEM_ROOT"
echo "  HELIX_CORRELATION_PAYLOAD: $HELIX_CORRELATION_PAYLOAD"
echo "==================================================="

cd "$HELIX_WORKITEM_ROOT" || exit 1

# PreCommands: setup (Xcode select, simulator boot, workload install, restore).
python3 setup_helix.py "$framework-ios" "$msbuild_args" || exit $?

# Command: the actual inner-loop measurement.
rc=0
python3 test.py iosinnerloop \
  --csproj-path app/MauiiOSInnerLoop.csproj \
  --edit-src "src/MainPage.xaml.cs;src/MainPage.xaml" \
  --edit-dest "app/Pages/MainPage.xaml.cs;app/Pages/MainPage.xaml" \
  --bundle-id com.companyname.mauiiosinnerloop \
  -f "$framework-ios" -c Debug \
  --msbuild-args "$msbuild_args" \
  --device-type simulator \
  --inner-loop-iterations "$inner_loop_iterations" \
  --scenario-name "Inner Loop Simulator - MAUI iOS Inner Loop" \
  $scenario_args || rc=$?

# PostCommands: uninstall app, shut down build servers, clean. Best-effort.
python3 post.py || true

exit $rc
